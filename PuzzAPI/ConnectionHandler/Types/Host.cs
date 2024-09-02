namespace PuzzAPI.ConnectionHandler.Types;

public class Host
{
    public string Type => MessageTypes.Host;
    public string Title { get; set; }
    public int Pieces { get; set; }
    public bool Public { get; set; }
}