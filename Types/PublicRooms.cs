using PuzzAPI.Room;

namespace PuzzAPI.Types;

public class PublicRooms
{
    public string Type { get; set; }
    public IEnumerable<PublicRoom> Rooms { get; set; }
}