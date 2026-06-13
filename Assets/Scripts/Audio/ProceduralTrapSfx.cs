using UnityEngine;

/// <summary>Tiếng chông nhô — procedural fallback.</summary>
public static class ProceduralTrapSfx
{
    private const int SampleRate = 44100;
    private static AudioClip spikeClip;

    public static AudioClip GetSpikePop()
    {
        if (spikeClip != null)
            return spikeClip;

        int count = Mathf.CeilToInt(SampleRate * 0.09f);
        float[] data = new float[count];
        var rng = new System.Random(3307);

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)count;
            float env = Mathf.Exp(-t * 28f);
            float click = Mathf.Sin(t * 1800f * Mathf.PI * 2f) * Mathf.Exp(-t * 60f) * 0.45f;
            float thud = Mathf.Sin(t * 90f * Mathf.PI * 2f) * 0.35f * Mathf.Exp(-t * 12f);
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.18f * Mathf.Exp(-t * 40f);
            data[i] = Mathf.Clamp((click + thud + noise) * env, -1f, 1f);
        }

        spikeClip = AudioClip.Create("TrapSpikePop", count, 1, SampleRate, false);
        spikeClip.SetData(data, 0);
        spikeClip.name = "TrapSpikePop";
        return spikeClip;
    }
}
