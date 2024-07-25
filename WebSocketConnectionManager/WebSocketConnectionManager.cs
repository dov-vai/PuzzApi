using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace PuzzAPI.WebSocketConnectionManager;

public class WebSocketConnectionManager : IWebSocketConnectionManager
{
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();

    public string AddSocket(WebSocket socket)
    {
        var socketId = Guid.NewGuid().ToString();
        _connections.TryAdd(socketId, socket);
        return socketId;
    }

    public async Task RemoveSocketAsync(string id)
    {
        _connections.TryRemove(id, out var socket);
        if (socket != null)
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
    }

    public async Task RemoveSocketAsync(string id, WebSocketCloseStatus? closeStatus, string? closeStatusDescription)
    {
        _connections.TryRemove(id, out var socket);
        if (socket != null)
            await socket.CloseAsync(closeStatus.Value, closeStatusDescription, CancellationToken.None);
    }

    public async Task SendMessageAsync(string id, string message)
    {
        _connections.TryGetValue(id, out var socket);
        if (socket?.State == WebSocketState.Open)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            var arraySegment = new ArraySegment<byte>(buffer, 0, buffer.Length);
            await socket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    public async Task BroadcastAsync(string message)
    {
        var buffer = Encoding.UTF8.GetBytes(message);

        foreach (var socket in _connections.Values)
            if (socket.State == WebSocketState.Open)
            {
                var arraySegment = new ArraySegment<byte>(buffer, 0, buffer.Length);
                await socket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
            }
    }

    public async Task BroadcastAsync(string message, WebSocket excludedSocket)
    {
        var buffer = Encoding.UTF8.GetBytes(message);

        foreach (var socket in _connections.Values)
        {
            if (socket.Equals(excludedSocket))
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
        return _connections.Count;
    }

    public bool Contains(string id)
    {
        return _connections.ContainsKey(id);
    }

    public bool Contains(WebSocket socket)
    {
        return _connections.Values.Contains(socket);
    }
}