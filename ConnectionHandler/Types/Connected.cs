namespace PuzzAPI.ConnectionHandler.Types;

public class Connected
{
    public string Type => MessageTypes.Connected;
    public string SocketId { get; set; }
    public string RoomId { get; set; }
}