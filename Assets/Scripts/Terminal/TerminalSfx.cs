using UnityEngine;

public class TerminalSfx : MonoBehaviour
{
    private AudioSource _audioSource;
    private AudioClip _keyClick;
    private AudioClip _beep;
    private AudioClip _error;

    private void Awake()
    {
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        BuildClips();
    }

    public void PlayKey()
    {
        _audioSource.PlayOneShot(_keyClick);
    }

    public void PlayBeep()
    {
        _audioSource.PlayOneShot(_beep);
    }

    public void PlayError()
    {
        _audioSource.PlayOneShot(_error);
    }

    private void BuildClips()
    {
        var sampleRate = AudioSettings.outputSampleRate;
        _keyClick = CreateClick(sampleRate);
        _beep = CreateBeep(sampleRate);
        _error = CreateError(sampleRate);
    }

    private static AudioClip CreateClick(int sampleRate)
    {
        var lengthSeconds = Random.Range(0.008f, 0.015f);
        var samples = Mathf.CeilToInt(sampleRate * lengthSeconds);
        var data = new float[samples];
        for (var i = 0; i < samples; i++)
        {
            var envelope = 1f - (i / (float)samples);
            data[i] = (Random.value * 2f - 1f) * envelope * 0.4f;
        }

        var clip = AudioClip.Create("terminal_key_click", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip CreateBeep(int sampleRate)
    {
        var frequency = Random.Range(900f, 1400f);
        var lengthSeconds = Random.Range(0.04f, 0.08f);
        var samples = Mathf.CeilToInt(sampleRate * lengthSeconds);
        var data = new float[samples];
        var fadeLength = Mathf.Max(1, Mathf.FloorToInt(samples * 0.1f));
        for (var i = 0; i < samples; i++)
        {
            var t = i / (float)sampleRate;
            var envelope = 1f;
            if (i < fadeLength)
            {
                envelope = i / (float)fadeLength;
            }
            else if (i > samples - fadeLength)
            {
                envelope = (samples - i) / (float)fadeLength;
            }

            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.45f;
        }

        var clip = AudioClip.Create("terminal_beep", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip CreateError(int sampleRate)
    {
        var frequency = Random.Range(180f, 260f);
        var lengthSeconds = Random.Range(0.2f, 0.3f);
        var samples = Mathf.CeilToInt(sampleRate * lengthSeconds);
        var data = new float[samples];
        for (var i = 0; i < samples; i++)
        {
            var t = i / (float)sampleRate;
            var tone = Mathf.Sin(2f * Mathf.PI * frequency * t);
            var noise = (Random.value * 2f - 1f) * 0.2f;
            var wobble = Mathf.Sin(2f * Mathf.PI * 12f * t) * 0.05f;
            data[i] = (tone + noise + wobble) * 0.6f;
        }

        var clip = AudioClip.Create("terminal_error", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
