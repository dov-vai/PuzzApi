namespace PuzzAPI.Types;

public class Host
{
    public string Type => MessageTypes.Host;
    public string Title { get; set; }
    public bool Public { get; set; }
}