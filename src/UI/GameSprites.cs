using UnityEngine;

namespace AuthorTimeHunting.UI;

/// <summary>
///     The game's own medal sprites.
///     They hang off PlayerManager as plain public fields, loaded with the game rather than
///     with the mod, so every one of these can be null - in the main menu there is no
///     PlayerManager at all. Callers are expected to handle that and fall back to text; the
///     alternative would be shipping copies of Zeepkist's art in the plugin.
/// </summary>
public static class GameSprites
{
	public static Sprite AuthorMedal => PlayerManager.Instance == null ? null : PlayerManager.Instance.authorMedal;

	public static Sprite GoldMedal => PlayerManager.Instance == null ? null : PlayerManager.Instance.goldMedal;

	public static Sprite SilverMedal => PlayerManager.Instance == null ? null : PlayerManager.Instance.silverMedal;

	public static Sprite BronzeMedal => PlayerManager.Instance == null ? null : PlayerManager.Instance.bronzeMedal;

	/// <summary>
	///     The game's consolation medal. ATH uses it for penalty skips, which is what it means
	///     here too: the level was played and not beaten.
	/// </summary>
	public static Sprite YouTriedMedal => PlayerManager.Instance == null ? null : PlayerManager.Instance.youTriedMedal;
}
