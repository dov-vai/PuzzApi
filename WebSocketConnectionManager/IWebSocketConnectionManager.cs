using System.Net.WebSockets;

namespace PuzzAPI.WebSocketConnectionManager;

public interface IWebSocketConnectionManager
{
    string AddSocket(WebSocket socket);
    Task RemoveSocketAsync(string id);
    Task RemoveSocketAsync(string id, WebSocketCloseStatus? closeStatus, string? closeStatusDescription);
    Task SendMessageAsync(string id, string message);
    Task BroadcastAsync(string message);
    Task BroadcastAsync(string message, WebSocket excludedSocket);
    Task ReceiveMessageAsync(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage);
    int GetCount();
    bool Contains(string id);
    bool Contains(WebSocket socket);
}