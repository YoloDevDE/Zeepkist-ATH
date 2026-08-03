using AuthorTimeHunting.UI;
using ZeepkistClient;
using ZeepSDK.ChatCommands;

namespace AuthorTimeHunting.Commands;

/// <summary>
///     Opens the run report on nothing but the history, so past runs can be looked up between
///     runs rather than only in the seconds after one ends.
/// </summary>
public class CommandAthHistory : ILocalChatCommand
{
	public string Prefix => "/";
	public string Command => "athhistory";

	public string Description => "Shows every ATH run recorded on this machine.";

	public void Handle(string arguments)
	{
		Raise();
	}

	/// <summary>
	///     Reaches the services through the plugin, the way every local chat command has to:
	///     ZeepSDK constructs these itself, so there is nothing to inject into.
	/// </summary>
	public static void Raise()
	{
		Plugin.Instance.Services.Results.Show(RunReportView.HistoryOnly(ZeepkistNetwork.LocalPlayer?.Username,
			Plugin.Instance.Services.History.Records));
	}
}
