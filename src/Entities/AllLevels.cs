using System.Collections.Generic;

namespace AuthorTimeHunting.Entities;

public class AllLevels
{
    public List<AllLevelsNode> Nodes { get; set; }
    public int TotalCount { get; set; }
}