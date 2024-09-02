namespace PuzzAPI.ConnectionHandler.Types;

public class ReceiveInit
{
    public string Type => MessageTypes.ReceiveInit;
    public string SocketId { get; set; }
}