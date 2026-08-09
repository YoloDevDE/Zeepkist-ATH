namespace AuthorTimeHunting.UI.Views;

/// <summary>
///     How a number in the split list compares to the run it is measured against. The colour is
///     not decided here: this is what a table of times knows, and a table of times has no
///     business knowing what green means.
/// </summary>
public enum SplitPace
{
	/// <summary>Nothing to compare against - either this run has not got there, or no run ever has.</summary>
	Unknown = 0,
	Ahead = 1,
	Behind = 2
}
