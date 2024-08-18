namespace PuzzAPI.Types;

public class Join
{
    public string Type => MessageTypes.Join;
    public string RoomId { get; set; }
}