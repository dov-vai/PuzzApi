using System.Net.WebSockets;

namespace PuzzAPI.Room;

public class Peer
{
    public string Id { get; set; }
    public WebSocket Socket { get; set; }
}