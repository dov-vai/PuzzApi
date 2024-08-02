using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using PuzzAPI.Room;

namespace PuzzAPI.RoomManager;

public class RoomManager : IRoomManager
{
    private readonly ConcurrentDictionary<string, Room.Room> _rooms = new();

    public string CreateRoom(string title, WebSocket socket)
    {
        var host = new Peer { Id = Guid.NewGuid().ToString(), Socket = socket };
        var room = new Room.Room
        {
            Id = Guid.NewGuid().ToString(),
            Title = title,
            Host = host,
            Peers = new List<Peer>()
        };
        room.Peers.Add(host);

        _rooms.TryAdd(room.Id, room);

        return room.Id;
    }

    public string AddPeer(string id, WebSocket socket)
    {
        var peer = new Peer
        {
            Id = Guid.NewGuid().ToString(),
            Socket = socket
        };

        _rooms.TryGetValue(id, out var room);

        room?.Peers.Add(peer);

        return peer.Id;
    }

    public async Task RemoveSocketAsync(string id, string peerId)
    {
        _rooms.TryGetValue(id, out var room);
        var peer = room?.Peers.Find(p => p.Id == peerId);
        if (peer != null)
        {
            room?.Peers.Remove(peer);
            await peer.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
        }
    }

    public async Task RemoveSocketAsync(string id, string peerId, WebSocketCloseStatus? closeStatus,
        string? closeStatusDescription)
    {
        _rooms.TryGetValue(id, out var room);
        var peer = room?.Peers.Find(p => p.Id == peerId);
        if (peer != null)
        {
            room?.Peers.Remove(peer);
            await peer.Socket.CloseAsync(closeStatus.Value, closeStatusDescription, CancellationToken.None);
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

    public async Task ReceiveMessageAsync(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
    {
        var buffer = new byte[1024 * 1000];
        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            handleMessage(result, buffer);
        }
    }

    public int GetCount()
    {
        return _rooms.Count;
    }

    public bool Contains(string id)
    {
        return _rooms.ContainsKey(id);
    }

    public bool Contains(string id, WebSocket socket)
    {
        _rooms.TryGetValue(id, out var room);

        if (room == null)
            return false;

        return room.Peers.Select(p => p.Socket).Contains(socket);
    }
}