using System;
using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Gamemodes;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Logger = AuthorTimeHunting.Util.Logger;
using Object = UnityEngine.Object;

namespace AuthorTimeHunting.UI.Hud;

/// <summary>
///     A hunt on the game's own Play Game screen, beside Freeplay, Splitscreen, Adventure and
///     Online.
///     ATH's own menu is one keypress away from anywhere, and that keypress is still one thing a
///     player has to know about before the mod exists for them. The main menu is where somebody
///     decides what to play, so that is where the choice belongs - a hunt reads as a way to play
///     Zeepkist rather than as a mod you first have to summon.
///     The button is the Online button, cloned. Copying the game's own control is the only way to
///     get a control that looks like the game's own: the shape, the hover colours, the click
///     sound and the controller navigation all come with it, and none of them have to be kept in
///     step with a patch later. What is replaced is the text and what happens on the click.
/// </summary>
public class PlayMenuButton
{
	private const string MenuScene = "3D_MainMenu";

	/// <summary>What the game calls the click we are looking for, on the button we want to copy.</summary>
	private const string OnlineMethod = "StartOnline";

	private const string Label = "AUTHOR TIME HUNTING";

	private const string ClassicGamemodeId = "classic";

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
		if (scene.name != MenuScene)
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

	private static void Add()
	{
		StartGameUI screen = Object.FindObjectOfType<StartGameUI>();

		if (screen == null)
		{
			return;
		}

		GenericButton online = Online(screen);

		if (online == null)
		{
			Logger.LogWarning("PlayMenuButton: No Online button to copy, the play menu stays as it was.");

			return;
		}

		Clone(screen, online);
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
			if (button.onClick.GetPersistentMethodName(i) == OnlineMethod)
			{
				return true;
			}
		}

		return false;
	}

	private static void Clone(StartGameUI screen, GenericButton online)
	{
		GameObject copy = Object.Instantiate(online.gameObject, online.transform.parent);

		copy.name = "ATH Quickstart";
		copy.transform.SetSiblingIndex(online.transform.GetSiblingIndex() + 1);

		Place(copy, online, screen);

		GenericButton button = copy.GetComponent<GenericButton>();

		Rename(button);

		button.onClick = new UnityEvent();
		button.onClick.AddListener(Quickstart);
	}

	/// <summary>
	///     One more step along whatever line the buttons already sit on, worked out from the gap
	///     between the last two of them. A layout group, where there is one, moves it again
	///     afterwards and this does no harm; where there is not, this is the whole of the layout.
	/// </summary>
	private static void Place(GameObject copy, GenericButton online, StartGameUI screen)
	{
		RectTransform rect = copy.GetComponent<RectTransform>();
		RectTransform anchor = online.GetComponent<RectTransform>();

		if (rect == null || anchor == null)
		{
			return;
		}

		rect.anchoredPosition = anchor.anchoredPosition + Step(online, screen);
	}

	private static Vector2 Step(GenericButton online, StartGameUI screen)
	{
		RectTransform above = Neighbour(online, screen);
		RectTransform anchor = online.GetComponent<RectTransform>();

		if (above == null)
		{
			return Vector2.zero;
		}

		return anchor.anchoredPosition - above.anchoredPosition;
	}

	/// <summary>The button nearest to the Online one, which is what sets the spacing of the row.</summary>
	private static RectTransform Neighbour(GenericButton online, StartGameUI screen)
	{
		RectTransform anchor = online.GetComponent<RectTransform>();
		RectTransform nearest = null;
		float best = float.MaxValue;

		foreach (GenericButton button in screen.buttonsToDisableWhenGoingIntoAthing)
		{
			float distance = Distance(button, online, anchor);

			if (distance >= best)
			{
				continue;
			}

			best = distance;
			nearest = button.GetComponent<RectTransform>();
		}

		return nearest;
	}

	private static float Distance(GenericButton button, GenericButton online, RectTransform anchor)
	{
		if (button == null || button == online)
		{
			return float.MaxValue;
		}

		RectTransform rect = button.GetComponent<RectTransform>();

		if (rect == null)
		{
			return float.MaxValue;
		}

		return Vector2.Distance(rect.anchoredPosition, anchor.anchoredPosition);
	}

	/// <summary>
	///     A cloned button carries the original's label and, with it, whatever localiser was keeping
	///     that label up to date. The localiser has to go first or it writes "Online" back over this
	///     the next time the language is read.
	/// </summary>
	private static void Rename(GenericButton button)
	{
		Write(button.buttonText);
		Write(button.buttonText2);
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

		label.text = Label;
	}

	/// <summary>
	///     The same thing the mod's own Quickstart tile does: Classic, on the settings it already
	///     has. Everything after this is the master state machine's - from the main menu that means
	///     reaching the lobby server first, which it already knows how to do.
	/// </summary>
	private static void Quickstart()
	{
		GamemodeRegistry registry = Plugin.Instance.Services.Gamemodes;
		IGamemode classic = registry.Resolve(ClassicGamemodeId);

		if (classic != null)
		{
			registry.Selected = classic;
		}

		AthRequests.Start();
	}
}
