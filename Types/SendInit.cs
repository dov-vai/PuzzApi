namespace PuzzAPI.Types;

public class SendInit
{
    public string Type => MessageTypes.SendInit;
    public string SocketId { get; set; }
}