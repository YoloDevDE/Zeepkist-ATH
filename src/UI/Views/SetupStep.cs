namespace AuthorTimeHunting.UI.Views;

/// <summary>
///     One line of the setup checklist: something the mod is doing, and whether it is done with
///     it. Steps are added as they begin, so the list grows down the screen at the pace the work
///     actually happens - which is what makes it read as progress rather than as a plan.
/// </summary>
public class SetupStep
{
	public SetupStep(string label)
	{
		Label = label;
	}

	public string Label { get; }

	public bool Done { get; set; }
}
