namespace PuzzAPI.Types;

public class Connected
{
    public string Type => MessageTypes.Connected;
    public string SocketId { get; set; }
}