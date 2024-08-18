using PuzzAPI.Room;

namespace PuzzAPI.Types;

public class PublicRooms
{
    public string Type => MessageTypes.PublicRooms;
    public IEnumerable<PublicRoom> Rooms { get; set; }
}