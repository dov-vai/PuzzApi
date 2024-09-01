namespace PuzzAPI.ConnectionHandler.Types;

public class SendInit
{
    public string Type => MessageTypes.SendInit;
    public string SocketId { get; set; }
}