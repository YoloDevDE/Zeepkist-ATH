using System;
using AuthorTimeHunting.Util;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Logger = AuthorTimeHunting.Util.Logger;
using Object = UnityEngine.Object;

namespace AuthorTimeHunting.UI.Hud;

/// <summary>
///     A hunt on the game's own Play Game screen, beside Zeepkist Online.
///     ATH's own menu is one keypress away from anywhere, and that keypress is still one thing a
///     player has to know about before the mod exists for them. The main menu is where somebody
///     decides what to play, so that is where the choice belongs - a hunt reads as a way to play
///     Zeepkist rather than as a mod you first have to summon.
///     The button is the Online button, cloned. Copying the game's own control is the only way to
///     get a control that looks like the game's own: the shape, the hover colors, the click sound
///     and the controller navigation all come with it, and none of them have to be kept in step
///     with a patch later. What is replaced is the text, the color, the picture and what happens on
///     the click - so the one button on the screen that is not the game's own says as much by
///     looking like the mod.
/// </summary>
public class PlayMenuButton
{
	private const string _menuScene = "3D_MainMenu";

	/// <summary>What the game calls the click we are looking for, on the button we want to copy.</summary>
	private const string _onlineMethod = "StartOnline";

	private const string _label = "Author Time Hunting";

	private const string _copyName = "ATH Button";

	/// <summary>How much of the button's height the label keeps for itself, under the picture.</summary>
	private const float _labelBand = 0.28f;

	/// <summary>What stays empty between the two halves of the row, in the panel's own units.</summary>
	private const float _gap = 16f;

	public void Listen()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	public void Dispose()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (scene.name != _menuScene)
		{
			return;
		}

		try
		{
			Add();
		}
		catch (Exception e)
		{
			Logger.LogError($"PlayMenuButton: Could not add the button: {e.Message}\n{e.StackTrace}");
		}
	}

	/// <summary>
	///     The play menu is a panel the main menu keeps switched off until somebody picks Play Game,
	///     so the screen is looked for among the inactive objects as well.
	/// </summary>
	private static void Add()
	{
		StartGameUI screen = Object.FindObjectOfType<StartGameUI>(true);

		if (screen == null)
		{
			Logger.LogWarning("PlayMenuButton: No play menu in this scene, the button stays away.");

			return;
		}

		GenericButton online = Online(screen);

		if (online == null)
		{
			Logger.LogWarning("PlayMenuButton: No Online button to copy, the play menu stays as it was.");

			return;
		}

		Transform slot = Slot(online.transform);

		if (slot.parent == null || slot.parent.Find(_copyName) != null)
		{
			return;
		}

		Clone(screen, online, slot);
	}

	/// <summary>
	///     The button that takes you online, found by what it does rather than by what it is called.
	///     A name is localised and a position moves; the click is wired to StartGameUI.StartOnline
	///     in the scene and has been for as long as the screen has existed.
	/// </summary>
	private static GenericButton Online(StartGameUI screen)
	{
		foreach (GenericButton button in screen.buttonsToDisableWhenGoingIntoAthing)
		{
			if (button == null || !StartsOnline(button))
			{
				continue;
			}

			return button;
		}

		return null;
	}

	private static bool StartsOnline(GenericButton button)
	{
		for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
		{
			if (button.onClick.GetPersistentMethodName(i) == _onlineMethod)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	///     The game does not hang its play menu buttons in the panel directly: each one sits by
	///     itself in a numbered holder, and the panel arranges the holders. A button dropped in
	///     beside another button therefore lands inside somebody else's holder, where it is placed
	///     by nothing and ends up behind the panel - which is exactly what happened. What belongs
	///     beside the other rows is a holder, so the holder is what gets copied: the outermost thing
	///     around the button that contains nothing but the button.
	/// </summary>
	private static Transform Slot(Transform button)
	{
		Transform slot = button;

		while (slot.parent != null && slot.parent.childCount == 1)
		{
			slot = slot.parent;
		}

		return slot;
	}

	private static void Clone(StartGameUI screen, GenericButton online, Transform slot)
	{
		GameObject copy = Object.Instantiate(slot.gameObject, slot.parent);

		copy.name = _copyName;
		copy.transform.SetSiblingIndex(slot.GetSiblingIndex() + 1);

		Split(slot as RectTransform, copy.GetComponent<RectTransform>());

		GenericButton button = copy.GetComponentInChildren<GenericButton>(true);

		Rename(button);
		Paint(button);
		Picture(button);

		button.onClick = new UnityEvent();
		button.onClick.AddListener(OpenMenu);

		Navigate(online, button);

		screen.buttonsToDisableWhenGoingIntoAthing.Add(button);

		Report(slot.parent, copy);
	}

	/// <summary>
	///     A copy of a row lands exactly on top of the row it was copied from, which is the hunt
	///     sitting in Online rather than beside it. The rows already fill the panel from top to
	///     bottom, so there is no room for a fifth one; what there is room for is a row of two,
	///     which the panel already does once for Splitscreen and Free Play. Online gives up its
	///     right half and the hunt takes it.
	///     The halves are cut with anchors rather than with pixels. The play menu is switched off
	///     when the button is added, so nothing has laid it out yet and a width read off it is the
	///     width it had in the editor, or none at all; anchors are fractions of the panel and are
	///     right whether anything has been laid out or not.
	/// </summary>
	private static void Split(RectTransform online, RectTransform hunt)
	{
		if (online == null || hunt == null)
		{
			return;
		}

		float middle = (online.anchorMin.x + online.anchorMax.x) / 2f;

		Half(online, online.anchorMin.x, middle, -_gap / 2f);
		Half(hunt, middle, online.anchorMax.x, _gap / 2f);
	}

	/// <summary>
	///     One half of the row's width, and half the gap eaten out of the side that faces the other
	///     half. A row anchored to a single line rather than stretched across one has both anchors in
	///     the same place and cannot be cut this way, so it is halved by its size instead.
	/// </summary>
	private static void Half(RectTransform rect, float from, float to, float inset)
	{
		if (Mathf.Approximately(from, to))
		{
			float shift = rect.sizeDelta.x / 4f + _gap / 4f;

			rect.sizeDelta = new Vector2(rect.sizeDelta.x / 2f - _gap / 2f, rect.sizeDelta.y);
			rect.anchoredPosition = new Vector2(rect.anchoredPosition.x + Mathf.Sign(inset) * shift,
				rect.anchoredPosition.y);

			return;
		}

		rect.anchorMin = new Vector2(from, rect.anchorMin.y);
		rect.anchorMax = new Vector2(to, rect.anchorMax.y);
		rect.offsetMin = new Vector2(inset > 0f ? inset : 0f, rect.offsetMin.y);
		rect.offsetMax = new Vector2(inset < 0f ? inset : 0f, rect.offsetMax.y);
	}

	/// <summary>
	///     Which of the panel's own arrangers ends up placing the new row. Whatever the panel does
	///     with the rows it already had, it now does with one more.
	/// </summary>
	private static void Report(Transform panel, GameObject copy)
	{
		LayoutGroup layout = panel.GetComponent<LayoutGroup>();

		RectTransform rect = copy.GetComponent<RectTransform>();

		Logger.LogInfo($"PlayMenuButton: '{copy.name}' is row {copy.transform.GetSiblingIndex()} "
		               + $"of {panel.childCount} in '{panel.name}', "
		               + $"arranged by {(layout == null ? "nothing but its anchors" : layout.GetType().Name)}, "
		               + $"anchored {rect.anchorMin.x} to {rect.anchorMax.x} at {rect.anchoredPosition}.");

		Parts(copy);
	}

	/// <summary>
	///     What the copy is made of, by name. The play menu is scene data and nothing in the game's
	///     code arranges it, so when the button comes out wrong this is the only place the shape of
	///     the thing that was copied can be read from.
	/// </summary>
	private static void Parts(GameObject copy)
	{
		foreach (Transform part in copy.GetComponentsInChildren<Transform>(true))
		{
			Logger.LogInfo($"PlayMenuButton: part '{part.name}'"
			               + $"{(part.GetComponent<Image>() == null ? "" : ", picture")}"
			               + $"{(part.GetComponent<TMP_Text>() == null ? "" : ", label")}");
		}
	}

	/// <summary>
	///     A cloned button carries the original's label and, with it, whatever localiser was keeping
	///     that label up to date. The localiser has to go first or it writes "Online" back over this
	///     the next time the language is read.
	///     Every label on the button is written, not the two the button names: the Online button
	///     leaves both of those empty and keeps its wording in a text of its own, so a button that
	///     went by the named ones alone kept saying Zeepkist Online.
	/// </summary>
	private static void Rename(GenericButton button)
	{
		foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
		{
			Write(label);
		}
	}

	private static void Write(TMP_Text label)
	{
		if (label == null)
		{
			return;
		}

		Localize localize = label.GetComponent<Localize>();

		if (localize != null)
		{
			Object.Destroy(localize);
		}

		label.text = _label;
	}

	/// <summary>
	///     The hunt's own color, in every state the game paints a button in. Setting the image's
	///     color alone would last one frame: <c>GenericButton.RedrawButton</c> writes one of these
	///     fields over it on every update, so the fields are where a color survives - and the image
	///     is set as well only so the button is right on the frame it appears.
	///     The text is left alone: it is white, and white on this is right.
	/// </summary>
	private static void Paint(GenericButton button)
	{
		Color hunt = Color.Zeepkist.Medal.Author;
		Color lit = Color.Lerp(hunt, Color.white, 0.35f);
		Color pressed = Color.Lerp(hunt, Color.white, 0.6f);
		Color deep = Color.Lerp(hunt, Color.black, 0.4f);
		Color deepLit = Color.Lerp(deep, Color.white, 0.3f);

		button.normalColor = hunt;
		button.hoverColor = lit;
		button.clickColor = pressed;
		button.selectedColor = deep;
		button.selectedHoverColor = deepLit;

		button.normalColor_disabled = Faded(hunt);
		button.hoverColor_disabled = Faded(lit);
		button.clickColor_disabled = Faded(pressed);
		button.selectedColor_disabled = Faded(deep);
		button.selectedHoverColor_disabled = Faded(deepLit);

		button.buttonImage.color = hunt;
	}

	/// <summary>Half there, which is what the game means by a disabled button.</summary>
	private static Color Faded(Color color)
	{
		return new Color(color.r, color.g, color.b, 0.5f);
	}

	/// <summary>
	///     The mod's own picture where the Online one was, across the whole button rather than in the
	///     little square the game uses for a pictogram. The PNG is transparent, so it stands on the
	///     color rather than on a panel of its own, and it goes behind the label rather than over it.
	/// </summary>
	private static void Picture(GenericButton button)
	{
		Sprite thumbnail = Plugin.Instance.Services.Thumbnail.Sprite;

		if (thumbnail == null)
		{
			Logger.LogWarning("PlayMenuButton: No thumbnail, the button keeps the Online picture.");

			return;
		}

		Image icon = null;

		foreach (Image image in button.GetComponentsInChildren<Image>(true))
		{
			if (image == button.buttonImage)
			{
				continue;
			}

			Replace(image, icon == null ? thumbnail : null);

			icon = icon ?? image;
		}

		if (icon == null)
		{
			Logger.LogWarning("PlayMenuButton: The Online button has no picture to replace.");

			return;
		}

		Spread(icon.rectTransform);
		Behind(icon, button.buttonImage);
	}

	/// <summary>
	///     The Online picture is not one picture: it is a stack of them, coins and a hand and a
	///     sparkle, each its own image. The first one carries the mod's picture and the rest go out,
	///     or the Online ones stay lying over it.
	/// </summary>
	private static void Replace(Image image, Sprite thumbnail)
	{
		if (thumbnail == null)
		{
			image.enabled = false;

			return;
		}

		image.sprite = thumbnail;
		image.color = Color.white;
		image.type = Image.Type.Simple;
		image.preserveAspect = true;
	}

	private static void Spread(RectTransform rect)
	{
		rect.anchorMin = new Vector2(0f, _labelBand);
		rect.anchorMax = new Vector2(1f, 1f);
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
	}

	/// <summary>
	///     In front of the button's own back and behind everything else on it. First child is the
	///     back of a canvas, which is the right place only as long as the back is not a child too -
	///     where it is, the picture goes one place after it instead of underneath it.
	/// </summary>
	private static void Behind(Image icon, Image background)
	{
		if (background == null || background.transform.parent != icon.transform.parent)
		{
			icon.transform.SetSiblingIndex(0);

			return;
		}

		icon.transform.SetSiblingIndex(background.transform.GetSiblingIndex() + 1);
	}

	/// <summary>
	///     A button nobody points at can be reached with the mouse and by no other means. The hunt
	///     stands to the right of Online and is reached by going right, which is how the panel's
	///     other row of two is walked. Up and down stay whatever Online's were, so the row still
	///     leads to the same rows above and below whichever half is being pointed at.
	/// </summary>
	private static void Navigate(GenericButton online, GenericButton hunt)
	{
		hunt.up = online.up;
		hunt.down = online.down;
		hunt.left = online;

		online.right = hunt;
	}

	/// <summary>
	///     The mod's own menu, over the main menu. The button used to start Classic on the spot, and
	///     that is one tile inside this menu - beside the other modes, the settings and the history.
	///     A play menu offers ways to play rather than performing one, so this one opens the mod
	///     instead of committing the player to a run they did not get to look at first.
	/// </summary>
	private static void OpenMenu()
	{
		Plugin.Instance.Services.Menu.Visible = true;
	}
}
