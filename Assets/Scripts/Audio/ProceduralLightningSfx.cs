using UnityEngine;

/// <summary>Tiếng sét sinh bằng code — dùng khi chưa có file WAV/MP3 trong Resources.</summary>
public static class ProceduralLightningSfx
{
    private const int SampleRate = 44100;
    private static AudioClip crackClip;
    private static AudioClip zapClip;

    public static AudioClip GetCrack()
    {
        if (crackClip != null)
            return crackClip;

        crackClip = BuildCrack(0.24f, seed: 7919);
        crackClip.name = "LightningCrack";
        return crackClip;
    }

    /// <summary>Tiếng zap ngắn hơn cho nhảy chain giữa các quái.</summary>
    public static AudioClip GetZap()
    {
        if (zapClip != null)
            return zapClip;

        zapClip = BuildCrack(0.14f, seed: 12011, highPass: true);
        zapClip.name = "LightningZap";
        return zapClip;
    }

    private static AudioClip BuildCrack(float duration, int seed, bool highPass = false)
    {
        int count = Mathf.CeilToInt(SampleRate * duration);
        float[] data = new float[count];
        var rng = new System.Random(seed);

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)count;
            float env = Mathf.Exp(-t * (highPass ? 22f : 14f)) * (1f - t * 0.35f);
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            float crack = Mathf.Sin(t * 920f * Mathf.PI * 2f) * Mathf.Exp(-t * 48f);
            float snap = Mathf.Sin(t * 2400f * Mathf.PI * 2f) * Mathf.Exp(-t * 80f) * 0.25f;
            float rumble = Mathf.Sin(t * 55f * Mathf.PI * 2f) * 0.28f * Mathf.Exp(-t * 6f);

            float sample = (noise * (highPass ? 0.7f : 0.5f) + crack * 0.32f + snap + rumble) * env;
            if (highPass)
                sample -= rumble * 0.6f;

            data[i] = Mathf.Clamp(sample, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("ProceduralLightning", count, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
