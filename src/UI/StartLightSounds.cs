using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AuthorTimeHunting.UI;

/// <summary>
///     The two tones the start lights make: a short beep per lamp and a longer one an octave
///     up when the zeepkists are released. Trackmania's countdown, near enough - three of the
///     same note and then the one that means go.
///     The clips are generated rather than shipped. Nothing in Zeepkist's own audio bank
///     sounds like a countdown, borrowing a menu click gave three identical mouse noises, and
///     an mp3 in the plugin would have to be decoded at load and licensed from somewhere. A
///     sine with a second harmonic and a fast decay is what a countdown beep is, and it is
///     forty lines.
///     Volume follows the game's own master and gameplay sliders, so a player who turned the
///     game down does not get beeped at anyway.
/// </summary>
public class StartLightSounds : IDisposable
{
	private const int SampleRate = 44100;

	private const float LampFrequency = 660f;
	private const float LampSeconds = 0.11f;

	private const float GoFrequency = LampFrequency * 2f;
	private const float GoSeconds = 0.32f;

	/// <summary>How much of the octave above the fundamental is mixed in, to give it an edge.</summary>
	private const float Harmonic = 0.22f;

	private const float AttackSeconds = 0.006f;
	private const float DecayRate = 5f;

	private const float BaseVolume = 0.55f;

	private readonly AudioClip _go;
	private readonly AudioClip _lamp;

	private readonly AudioSource _source;

	public StartLightSounds(GameObject host)
	{
		_lamp = Tone("AthStartLamp", LampFrequency, LampSeconds);
		_go = Tone("AthStartGo", GoFrequency, GoSeconds);

		_source = host.AddComponent<AudioSource>();
		_source.playOnAwake = false;
		_source.spatialBlend = 0f;
	}

	public void Dispose()
	{
		Object.Destroy(_lamp);
		Object.Destroy(_go);
	}

	public void Lamp()
	{
		Play(_lamp);
	}

	public void Go()
	{
		Play(_go);
	}

	private void Play(AudioClip clip)
	{
		if (_source == null || clip == null)
		{
			return;
		}

		_source.PlayOneShot(clip, Volume());
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

	private static AudioClip Tone(string name, float frequency, float seconds)
	{
		int samples = Mathf.CeilToInt(SampleRate * seconds);
		float[] data = new float[samples];

		for (int i = 0; i < samples; i++)
		{
			float t = (float)i / SampleRate;
			data[i] = Wave(frequency, t) * Envelope(t, seconds);
		}

		AudioClip clip = AudioClip.Create(name, samples, 1, SampleRate, false);
		clip.SetData(data, 0);

		return clip;
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
