namespace PuzzAPI.ConnectionHandler.Types;

public class RemovePeer
{
    public string Type => MessageTypes.RemovePeer;
    public string SocketId { get; set; }
}