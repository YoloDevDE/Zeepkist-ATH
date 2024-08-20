namespace AuthorTimeHunting.Entities;

public class Level
{
    public Level(LevelScriptableObject level)
    {
        LevelUid = level.UID;
        Name = level.Name;
        Author = level.Author;
        AuthorTime = level.TimeAuthor;
        GoldTime = level.TimeGold;
        Attempts = 0;
        Crashes = 0;
        GoldSkipUnlocked = false;
        Levelbeaten = false;
    }

    public string LevelUid { get; set; }
    public string Name { get; set; }
    public string Author { get; set; }
    public double AuthorTime { get; set; }
    public double GoldTime { get; set; }

    public int Attempts { get; set; }
    public int Crashes { get; set; }

    public bool GoldSkipUnlocked { get; set; }
    public bool Levelbeaten { get; set; }
    public bool FirstTimePlayed { get; set; } = true;
}