using System.Net.WebSockets;

namespace PuzzAPI.ConnectionHandler.Room;

public class Peer
{
    public string Id { get; set; }
    public WebSocket Socket { get; set; }
}