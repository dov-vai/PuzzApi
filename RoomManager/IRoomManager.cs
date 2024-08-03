using System.Net.WebSockets;

namespace PuzzAPI.RoomManager;

public interface IRoomManager
{
    bool CreateRoom(string title, WebSocket socket, out string? roomId, out string? peerId);
    bool AddPeer(string id, WebSocket socket, out string? peerId);
    Task RemoveSocketAsync(string id, string peerId);
    Task RemoveSocketAsync(string id, string peerId, WebSocketCloseStatus? closeStatus, string? closeStatusDescription);
    Task SendMessageAsync(string id, string peerId, string message);
    Task BroadcastAsync(string id, string message, WebSocket? excludeSocket = null);
    Task ReceiveMessageAsync(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage);
    int GetCount();
    bool Contains(string id);
    bool Contains(string id, WebSocket socket);
}