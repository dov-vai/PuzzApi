using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using PuzzAPI.ConnectionHandler.Room;

namespace PuzzAPI.ConnectionHandler.RoomManager;

public class RoomManager : IRoomManager
{
    private readonly ConcurrentDictionary<string, Room.Room> _rooms = new();

    public bool CreateRoom(string title, int pieces, bool publicRoom, out string? roomId)
    {
        var room = new Room.Room
        {
            Id = Guid.NewGuid().ToString(),
            Title = title,
            Pieces = pieces,
            Public = publicRoom,
            Peers = new List<Peer>()
        };

        var success = _rooms.TryAdd(room.Id, room);

        roomId = null;

        if (success) roomId = room.Id;

        return success;
    }

    public bool AddPeer(string id, WebSocket socket, out string? peerId)
    {
        var peer = new Peer
        {
            Id = Guid.NewGuid().ToString(),
            Socket = socket
        };

        var success = _rooms.TryGetValue(id, out var room);

        peerId = null;

        if (room != null)
        {
            if (room.Host == null) room.Host = peer;

            room.Peers.Add(peer);
            peerId = peer.Id;
        }

        return success;
    }

    public async Task RemoveSocketAsync(string id, string peerId)
    {
        await RemoveSocketAsync(id, peerId, WebSocketCloseStatus.NormalClosure, "Closed");
    }

    public async Task RemoveSocketAsync(string id, string peerId, WebSocketCloseStatus closeStatus,
        string closeStatusDescription)
    {
        _rooms.TryGetValue(id, out var room);
        var peer = room?.Peers.Find(p => p.Id == peerId);
        if (peer != null)
        {
            room?.Peers.Remove(peer);
            if (peer.Socket.State != WebSocketState.Aborted)
                await peer.Socket.CloseAsync(closeStatus, closeStatusDescription, CancellationToken.None);
            if (room?.Peers.Count == 0) _rooms.TryRemove(id, out _);
        }
    }

    public async Task DisconnectPeerAsync(string id, string peerId)
    {
        _rooms.TryGetValue(id, out var room);
        var peer = room?.Peers.Find(p => p.Id == peerId);
        if (peer != null)
        {
            room?.Peers.Remove(peer);
            if (room?.Peers.Count == 0) _rooms.TryRemove(id, out _);
        }
    }

    public async Task SendMessageAsync(string id, string peerId, string message)
    {
        _rooms.TryGetValue(id, out var room);
        var peer = room?.Peers.Find(p => p.Id == peerId);
        if (peer?.Socket.State == WebSocketState.Open)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            var arraySegment = new ArraySegment<byte>(buffer, 0, buffer.Length);
            await peer.Socket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    public async Task BroadcastAsync(string id, string message, WebSocket? excludeSocket)
    {
        _rooms.TryGetValue(id, out var room);

        if (room == null)
            return;

        var buffer = Encoding.UTF8.GetBytes(message);

        foreach (var socket in room.Peers.Select(p => p.Socket))
        {
            if (excludeSocket?.Equals(socket) ?? false)
                continue;

            if (socket.State == WebSocketState.Open)
            {
                var arraySegment = new ArraySegment<byte>(buffer, 0, buffer.Length);
                await socket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }

    public async Task ReceiveMessageAsync(WebSocket socket, Func<WebSocketReceiveResult, byte[], Task> handleMessage)
    {
        var buffer = new byte[1024 * 1000];
        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            // await for error handling, this _might_ cause performance issues later
            // as this would process the messages sequentially
            // but this would also help avoid concurrency issues (tradeoff)
            await handleMessage(result, buffer);
        }
    }

    public IEnumerable<PublicRoom> GetPublicRooms()
    {
        return _rooms.Values
            .Where(r => r.Public)
            .Select(r => new PublicRoom
            {
                Id = r.Id,
                Title = r.Title,
                Pieces = r.Pieces,
                PlayerCount = r.Peers.Count
            });
    }

    public int GetCount()
    {
        return _rooms.Count;
    }

    public bool Contains(string id)
    {
        return _rooms.ContainsKey(id);
    }

    public bool ContainsPeer(string id, string peerId)
    {
        _rooms.TryGetValue(id, out var room);
        var peer = room?.Peers.Find(p => p.Id == peerId);
        return peer != null;
    }

    public bool Contains(string id, WebSocket socket)
    {
        _rooms.TryGetValue(id, out var room);

        if (room == null)
            return false;

        return room.Peers.Select(p => p.Socket).Contains(socket);
    }

    public string? GetHostId(string id)
    {
        _rooms.TryGetValue(id, out var room);
        return room?.Host.Id;
    }
}