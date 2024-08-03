namespace PuzzAPI.Room;

public class Room
{
    public string Id { get; set; }
    public string Title { get; set; }
    public bool Public { get; set; }
    public Peer Host { get; set; }
    public List<Peer> Peers { get; set; }
}