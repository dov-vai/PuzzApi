using PuzzAPI.ConnectionHandler.Room;

namespace PuzzAPI.ConnectionHandler.Types;

public class PublicRooms
{
    public string Type => MessageTypes.PublicRooms;
    public IEnumerable<PublicRoom> Rooms { get; set; }
}