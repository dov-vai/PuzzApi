namespace PuzzAPI.ConnectionHandler.Types;

public class RtcSignal
{
    public string Type => MessageTypes.RtcSignal;
    public string SocketId { get; set; }
    public string Signal { get; set; }
}