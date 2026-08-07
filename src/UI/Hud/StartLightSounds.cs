using System;
using System.Runtime.InteropServices;
using FMOD;
using FMODUnity;
using UnityEngine;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI.Hud;

/// <summary>
///     The two tones the start lights make: a short beep per lamp and a longer one an octave
///     up when the zeepkists are released. Trackmania's countdown, near enough - three of the
///     same note and then the one that means go.
///     The clips are generated rather than shipped. Nothing in Zeepkist's own audio bank
///     sounds like a countdown, borrowing a menu click gave three identical mouse noises, and
///     an mp3 in the plugin would have to be decoded at load and licensed from somewhere. A
///     sine with a second harmonic and a fast decay is what a countdown beep is, and it is
///     forty lines.
///     They go out through FMOD, which is the only audio this game actually has. A Unity
///     AudioSource does technically play here, and that is the trap: the game sets
///     <c>AudioListener.volume</c> to the master slider, so a beep already scaled by that slider
///     got scaled by it a second time. At a master of 10% - an ordinary setting - that is a
///     hundredth of the volume asked for, which is silence with extra steps.
///     The sound is built straight out of PCM through FMOD's core system and played on its master
///     group. That sits below the Studio buses, so the game's own VCAs do not touch it and the
///     sliders are applied here instead, exactly once.
/// </summary>
public class StartLightSounds : IDisposable
{
	private const int SampleRate = 44100;

	private const int Channels = 1;

	private const float LampFrequency = 660f;
	private const float LampSeconds = 0.11f;

	private const float GoFrequency = LampFrequency * 2f;
	private const float GoSeconds = 0.32f;

	/// <summary>
	///     The medal running out. Below the countdown beeps and longer than any of them, because it
	///     has to arrive while the player is looking at the track and be recognised without being
	///     mistaken for a start light.
	/// </summary>
	private const float WarningFrequency = 330f;

	private const float WarningSeconds = 0.5f;

	/// <summary>How much of the octave above the fundamental is mixed in, to give it an edge.</summary>
	private const float Harmonic = 0.22f;

	private const float AttackSeconds = 0.006f;
	private const float DecayRate = 5f;

	private const float BaseVolume = 0.9f;

	private Sound _go;

	private Sound _lamp;

	/// <summary>
	///     The tones are made on the first beep rather than in the constructor. This is built while
	///     the plugin loads, and FMOD is not up that early.
	/// </summary>
	private bool _made;

	private Sound _warning;

	public void Dispose()
	{
		Release(_lamp);
		Release(_go);
		Release(_warning);

		_lamp = default;
		_go = default;
		_warning = default;
	}

	public void Lamp()
	{
		Make();
		Play(_lamp);
	}

	public void Go()
	{
		Make();
		Play(_go);
	}

	public void Warning()
	{
		Make();
		Play(_warning);
	}

	private void Make()
	{
		if (_made)
		{
			return;
		}

		_made = true;
		_lamp = Tone(LampFrequency, LampSeconds);
		_go = Tone(GoFrequency, GoSeconds);
		_warning = Tone(WarningFrequency, WarningSeconds);
	}

	/// <summary>
	///     Started paused so the volume is on it before its first sample is heard, which is the
	///     difference between a beep and a beep with a click in front of it.
	/// </summary>
	private static void Play(Sound sound)
	{
		if (!sound.hasHandle())
		{
			return;
		}

		RESULT result =
			RuntimeManager.CoreSystem.playSound(sound, default, true, out Channel channel);

		if (result != RESULT.OK)
		{
			return;
		}

		channel.setVolume(Volume());
		channel.setPaused(false);
	}

	private static void Release(Sound sound)
	{
		if (!sound.hasHandle())
		{
			return;
		}

		sound.release();
	}

	private static float Volume()
	{
		if (PlayerManager.Instance == null || PlayerManager.Instance.instellingen == null)
		{
			return BaseVolume;
		}

		GameSettingsScriptableObject settings = GetSettings.Get();

		return BaseVolume * Mathf.Clamp01(settings.audio_master / 100f)
		                  * Mathf.Clamp01(settings.audio_gameplay / 100f);
	}

	/// <summary>
	///     Raw PCM handed to FMOD as if it were a file in memory: no header, no decoding, and
	///     <see cref="FMOD.CREATESOUNDEXINFO" /> saying what the bytes are instead.
	/// </summary>
	private static Sound Tone(float frequency, float seconds)
	{
		byte[] pcm = Pcm(frequency, seconds);

		CREATESOUNDEXINFO info = new()
		{
			cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO)),
			length = (uint)pcm.Length,
			numchannels = Channels,
			defaultfrequency = SampleRate,
			format = SOUND_FORMAT.PCMFLOAT
		};

		MODE mode = MODE.OPENMEMORY | MODE.OPENRAW | MODE.CREATESAMPLE | MODE.LOOP_OFF;

		RESULT result = RuntimeManager.CoreSystem.createSound(pcm, mode, ref info, out Sound sound);

		if (result == RESULT.OK)
		{
			return sound;
		}

		Logger.LogWarning($"StartLightSounds: FMOD would not take the tone ({result}), the lights stay silent.");

		return default;
	}

	private static byte[] Pcm(float frequency, float seconds)
	{
		int samples = Mathf.CeilToInt(SampleRate * seconds);
		float[] wave = new float[samples];

		for (int i = 0; i < samples; i++)
		{
			float t = (float)i / SampleRate;
			wave[i] = Wave(frequency, t) * Envelope(t, seconds);
		}

		byte[] pcm = new byte[samples * sizeof(float)];

		Buffer.BlockCopy(wave, 0, pcm, 0, pcm.Length);

		return pcm;
	}

	private static float Wave(float frequency, float t)
	{
		float phase = 2f * Mathf.PI * frequency * t;

		return (Mathf.Sin(phase) + Harmonic * Mathf.Sin(2f * phase)) / (1f + Harmonic);
	}

	private static float Envelope(float t, float seconds)
	{
		if (t < AttackSeconds)
		{
			return t / AttackSeconds;
		}

		return Mathf.Exp(-DecayRate * (t - AttackSeconds) / seconds);
	}
}
