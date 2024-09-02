namespace PuzzAPI.ConnectionHandler.Types;

public class P2PInit
{
    public string Type => MessageTypes.P2PInit;
    public string SocketId { get; set; }
    public string RoomId { get; set; }
    public string HostId { get; set; }
}