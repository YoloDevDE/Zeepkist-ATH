using AuthorTimeHunting.Entities;
using AuthorTimeHunting.States.Ath;
using AuthorTimeHunting.Util;
using TMPro;
using UnityEngine;

namespace AuthorTimeHunting.UI.Hud;

/// <summary>
///     The game's own finish panel, answering the question this mode actually asks.
///     <code>
///     game               ATH
///     00:12.340   ->     GOLD UNLOCKED
///     3 of 3 checkpoints 3 of 3 checkpoints
///     </code>
///     The panel already puts two things on screen when a run ends, and only one of them was
///     still worth reading. The checkpoint count says whether the run counted at all, which is
///     as true here as anywhere, so it is left alone. The time does not: it was already read
///     off the ticker while driving, and by the time the panel is up the only open question is
///     whether it was enough. So that is what the line says instead - the medal that was taken,
///     or that none was, and whether a gold was won for the first time, because that is the one
///     that buys a skip.
///     A run that crossed the line without every checkpoint never gets here. Nothing is written
///     unless the hunt booked a time for the attempt, and it only books one for a finish it
///     would score, so a dnf keeps the game's own panel.
///     The game writes this label once, at the finish, rather than every frame - so unlike the
///     ticker it does not come back on its own, and the text that was replaced is kept and put
///     back when the hunt lets go of the panel.
/// </summary>
public class FinishVerdict
{
	private const string _authorText = "AUTHOR TIME";

	private const string _unlockedText = "GOLD UNLOCKED";

	private const string _goldText = "GOLD";

	private const string _missedText = "NO MEDAL";

	private TMP_Text _label;

	private string _replaced;

	/// <summary>The last thing written here, so a restore can tell its own text from the game's.</summary>
	private string _written;

	public void Draw(TMP_Text label, AthCtx ctx)
	{
		if (label == null)
		{
			return;
		}

		if (_label != label)
		{
			Restore();
		}

		if (_label == null)
		{
			_label = label;
			_replaced = label.text;
		}

		_written = Verdict(ctx);
		label.text = _written;
	}

	/// <summary>
	///     Gives the panel back, and only puts the old text back if the verdict is still the thing
	///     on it. Restarting clears this label itself - the game blanks every result field before
	///     the next attempt - and writing a remembered finish time over that clear left the last
	///     verdict on screen through the whole of the next run. If somebody else has written here
	///     since, they meant to, and what was on the label before ATH touched it stopped being the
	///     right answer at that moment.
	/// </summary>
	public void Restore()
	{
		if (_label != null && _label.text == _written)
		{
			_label.text = _replaced;
		}

		_label = null;
		_replaced = null;
		_written = null;
	}

	private static string Verdict(AthCtx ctx)
	{
		if (ctx.LastRunMedalStatus == LevelStatus.Author)
		{
			return Paint(_authorText, Color.Zeepkist.Medal.Author);
		}

		if (ctx.LastRunMedalStatus != LevelStatus.Gold)
		{
			return Paint(_missedText, Color.Style.Status.Bad);
		}

		return Paint(ctx.LastRunMedalWasNew ? _unlockedText : _goldText, Color.Zeepkist.Medal.Gold);
	}

	private static string Paint(string text, Color32 colour)
	{
		return $"<color=#{colour.r:X2}{colour.g:X2}{colour.b:X2}>{text}</color>";
	}
}
