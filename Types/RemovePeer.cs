namespace PuzzAPI.Types;

public class RemovePeer
{
    public string Type => MessageTypes.RemovePeer;
    public string SocketId { get; set; }
}