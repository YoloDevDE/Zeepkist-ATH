using System;
using System.Runtime.InteropServices;
using FMOD;
using FMODUnity;
using UnityEngine;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI.Hud;

/// <summary>
///     Everything the run ticker says out loud: the start lights counting down, and the three
///     things that can happen to the medal being chased.
///     The clips are generated rather than shipped. Nothing in Zeepkist's own audio bank sounds
///     like a countdown, borrowing a menu click gave three identical mouse noises, and an mp3 in
///     the plugin would have to be decoded at load and licensed from somewhere. A handful of sine
///     partials with a fast decay is what a beep is, and it is a page of arithmetic.
///     Each clip is a stack of harmonics rather than one sine, which is the difference between a
///     tone and a test signal - a single sine has no body and disappears under an engine. On top
///     of that goes a short reverb tail, so the sounds land in the same room as the game instead
///     of in front of it.
///     They go out through FMOD, which is the only audio this game actually has. A Unity
///     AudioSource does technically play here, and that is the trap: the game sets
///     <c>AudioListener.volume</c> to the master slider, so a beep already scaled by that slider
///     got scaled by it a second time. At a master of 10% - an ordinary setting - that is a
///     hundredth of the volume asked for, which is silence with extra steps.
///     The sound is built straight out of PCM through FMOD's core system and played on its master
///     group. That sits below the Studio buses, so the game's own VCAs do not touch it and the
///     sliders are applied here instead, exactly once.
/// </summary>
public class HudSounds : IDisposable
{
	private const int SampleRate = 44100;

	private const int Channels = 1;

	/// <summary>
	///     How long the room takes to fall 60 dB. Under half a second is a small hard room, which is
	///     what a beep wants: enough that it lands somewhere rather than in front of the player's
	///     face, and over before the next lamp lights.
	/// </summary>
	private const float ReverbSeconds = 0.45f;

	/// <summary>How much of the room is heard against the clip itself.</summary>
	private const float WetMix = 0.45f;

	private const float Minus60Db = 0.001f;

	/// <summary>
	///     Room at the end of every clip for the reverb to die away in. The tail is inaudible about
	///     a tenth of a second before this runs out, which is the margin rather than waste - a clip
	///     cut off while it still rings is a click.
	/// </summary>
	private const float TailSeconds = 0.25f;

	/// <summary>
	///     What every clip is scaled to once it is mixed. Normalising rather than trusting the
	///     arithmetic is what keeps the double beep exactly as loud as the single one, and what
	///     keeps three cascaded echoes from clipping.
	/// </summary>
	private const float Headroom = 0.95f;

	private const float AttackSeconds = 0.006f;
	private const float DecayRate = 5f;

	private const float BaseVolume = 0.9f;

	/// <summary>
	///     The partials every clip is built from, as a share of the fundamental. Four of them is
	///     where a beep stops sounding like a signal generator and starts sounding like something
	///     that was struck; past that the top ones only add hiss at the sample rate this runs at.
	/// </summary>
	private static readonly float[] Partials = [1f, 0.34f, 0.16f, 0.07f];

	/// <summary>
	///     The reverb, in samples of delay: 25, 38 and 59 milliseconds. Mutually prime, so the three
	///     of them do not line up and turn a room into a single flutter. They run in parallel, each
	///     one fed the dry clip - in series they would only reverberate each other's output, which
	///     is a longer and thinner sound than three rooms heard at once.
	/// </summary>
	private static readonly int[] Echoes = [1103, 1697, 2593];

	/// <summary>One lamp of the countdown.</summary>
	private static readonly Tone[] LampNotes = [new(660f, 0f, 0.11f)];

	/// <summary>The release, an octave above the lamps and longer. Trackmania's countdown, near enough.</summary>
	private static readonly Tone[] GoNotes = [new(1320f, 0f, 0.32f)];

	/// <summary>
	///     The medal getting tight. One beep, below the lamps so it is not mistaken for a start
	///     light, and short enough to be over before it is in the way.
	/// </summary>
	private static readonly Tone[] WarningNotes = [new(587f, 0f, 0.18f)];

	/// <summary>
	///     The medal about to be gone. Two of them, quick and high: a repeat is heard as urgency
	///     where a single tone of any pitch is only heard as information.
	/// </summary>
	private static readonly Tone[] AlertNotes = [new(880f, 0f, 0.1f), new(880f, 0.15f, 0.1f)];

	/// <summary>
	///     The author time going past. A falling fourth, the second note held - falling is the one
	///     shape nobody has ever read as good news, and it needs no learning.
	/// </summary>
	private static readonly Tone[] MissedNotes = [new(415f, 0f, 0.16f), new(311f, 0.18f, 0.32f)];

	private Sound _alert;

	private Sound _go;

	private Sound _lamp;

	/// <summary>
	///     The tones are made on the first beep rather than in the constructor. This is built while
	///     the plugin loads, and FMOD is not up that early.
	/// </summary>
	private bool _made;

	private Sound _missed;

	private Sound _warning;

	public void Dispose()
	{
		Release(_lamp);
		Release(_go);
		Release(_warning);
		Release(_alert);
		Release(_missed);

		_lamp = default;
		_go = default;
		_warning = default;
		_alert = default;
		_missed = default;
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

	public void Alert()
	{
		Make();
		Play(_alert);
	}

	public void Missed()
	{
		Make();
		Play(_missed);
	}

	private void Make()
	{
		if (_made)
		{
			return;
		}

		_made = true;
		_lamp = Clip(LampNotes);
		_go = Clip(GoNotes);
		_warning = Clip(WarningNotes);
		_alert = Clip(AlertNotes);
		_missed = Clip(MissedNotes);
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
	private static Sound Clip(Tone[] notes)
	{
		byte[] pcm = Pcm(notes);

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

		Logger.LogWarning($"HudSounds: FMOD would not take the tone ({result}), the ticker stays silent.");

		return default;
	}

	private static byte[] Pcm(Tone[] notes)
	{
		float[] wave = new float[Samples(notes)];

		Mix(wave, notes);
		Reverb(wave);
		Normalize(wave);

		byte[] pcm = new byte[wave.Length * sizeof(float)];

		Buffer.BlockCopy(wave, 0, pcm, 0, pcm.Length);

		return pcm;
	}

	/// <summary>As long as its last note plays, plus the room the reverb needs to fall silent in.</summary>
	private static int Samples(Tone[] notes)
	{
		float end = 0f;

		foreach (Tone note in notes)
		{
			end = Mathf.Max(end, note.Start + note.Seconds);
		}

		return Mathf.CeilToInt(SampleRate * (end + TailSeconds));
	}

	private static void Mix(float[] wave, Tone[] notes)
	{
		foreach (Tone note in notes)
		{
			Add(wave, note);
		}
	}

	private static void Add(float[] wave, Tone note)
	{
		int start = Mathf.RoundToInt(note.Start * SampleRate);
		int samples = Mathf.CeilToInt(note.Seconds * SampleRate);

		for (int i = 0; i < samples; i++)
		{
			float t = (float)i / SampleRate;

			wave[start + i] += Wave(note.Frequency, t) * Envelope(t, note.Seconds);
		}
	}

	/// <summary>
	///     Left unnormalised on purpose - <see cref="Normalize" /> scales the finished clip, so
	///     dividing by the weights here would only be undone a moment later.
	/// </summary>
	private static float Wave(float frequency, float t)
	{
		float phase = 2f * Mathf.PI * frequency * t;
		float sum = 0f;

		for (int partial = 0; partial < Partials.Length; partial++)
		{
			sum += Partials[partial] * Mathf.Sin(phase * (partial + 1));
		}

		return sum;
	}

	private static float Envelope(float t, float seconds)
	{
		if (t < AttackSeconds)
		{
			return t / AttackSeconds;
		}

		return Mathf.Exp(-DecayRate * (t - AttackSeconds) / seconds);
	}

	private static void Reverb(float[] wave)
	{
		float[] wet = new float[wave.Length];

		foreach (int delay in Echoes)
		{
			Comb(wave, wet, delay);
		}

		Blend(wave, wet);
	}

	/// <summary>
	///     A feedback comb: every sample picks up an attenuated copy of one a delay earlier, which
	///     then feeds the one after it. That is where the tail comes from - it is not a handful of
	///     echoes stuck on the end, it is one echo running out of energy. The copy is worked on so
	///     the dry clip stays intact for the next comb to be fed the same thing.
	/// </summary>
	private static void Comb(float[] dry, float[] wet, int delay)
	{
		float[] line = (float[])dry.Clone();
		float feedback = Feedback(delay);

		for (int i = delay; i < line.Length; i++)
		{
			line[i] += line[i - delay] * feedback;
		}

		Accumulate(wet, line);
	}

	/// <summary>
	///     What one echo has to be worth for the comb to lose 60 dB in <see cref="ReverbSeconds" />.
	///     A long delay gets fewer repeats in that time, so each of them has to fade faster.
	/// </summary>
	private static float Feedback(int delay)
	{
		return Mathf.Pow(Minus60Db, delay / (float)SampleRate / ReverbSeconds);
	}

	private static void Accumulate(float[] wet, float[] line)
	{
		for (int i = 0; i < wet.Length; i++)
		{
			wet[i] += line[i];
		}
	}

	private static void Blend(float[] wave, float[] wet)
	{
		for (int i = 0; i < wave.Length; i++)
		{
			wave[i] += wet[i] * WetMix;
		}
	}

	private static void Normalize(float[] wave)
	{
		float peak = Peak(wave);

		if (peak <= 0f)
		{
			return;
		}

		Scale(wave, Headroom / peak);
	}

	private static float Peak(float[] wave)
	{
		float peak = 0f;

		foreach (float sample in wave)
		{
			peak = Mathf.Max(peak, Mathf.Abs(sample));
		}

		return peak;
	}

	private static void Scale(float[] wave, float factor)
	{
		for (int i = 0; i < wave.Length; i++)
		{
			wave[i] *= factor;
		}
	}
}
