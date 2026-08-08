using FMOD;
using FMOD.Studio;
using FMODUnity;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.Service;

/// <summary>
///     Keeps the game quiet while the mod builds the lobby the hunt runs in.
///     The setup screen hides what is happening behind it; the sound gives it away anyway. A
///     player who asked ATH for a run hears a lobby being joined, a menu closing, a level loading
///     and a round starting - four noises belonging to a thing they did not do, over a screen
///     saying the mod is getting their run ready.
///     So FMOD's master bus is muted from the moment the setup begins until the round the mod set
///     up actually starts. Muting the bus rather than turning the volume down leaves the player's
///     own settings alone: there is nothing to restore afterwards but a flag.
/// </summary>
public class LobbySilence
{
	private const string _masterBus = "bus:/";

	private bool _muted;

	public void Silence()
	{
		Mute(true);
	}

	public void Restore()
	{
		if (!_muted)
		{
			return;
		}

		Mute(false);
	}

	private void Mute(bool mute)
	{
		if (!RuntimeManager.IsInitialized)
		{
			return;
		}

		RESULT found = RuntimeManager.StudioSystem.getBus(_masterBus, out Bus bus);

		if (found != RESULT.OK)
		{
			Logger.LogWarning($"LobbySilence: No master bus to mute ({found}).");

			return;
		}

		bus.setMute(mute);
		_muted = mute;
	}
}
