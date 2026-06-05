// DungeonSoul — HeroKnightVfx.cs — Hiệu ứng sprite từ pack TheHeroKnight (heal, nổ, đòn boss).

using System.Collections;
using UnityEngine;

public static class HeroKnightVfx
{
    private static Sprite whiteSprite;

    public static void PlayHeal(Vector3 worldPos, float scale = 1.2f)
    {
        Sprite[] frames = HeroKnightLibrary.GetHealFxFrames();
        if (frames == null || frames.Length == 0)
            return;

        AudioManager.PlayHeal();
        SpawnBurst(worldPos, frames, 14f, scale, new Color(0.55f, 1f, 0.65f, 1f), 22);
    }

    public static void PlayExplosion(Vector3 worldPos, float radiusWorld = 1.4f)
    {
        AudioManager.PlayCombatHit();
        float scale = Mathf.Max(0.9f, radiusWorld * 0.95f);
        if (SkillVfxLibrary.GetFrames(SkillVfxStyle.Fire).Length > 0)
        {
            SkillVfxLibrary.Play(SkillVfxStyle.Fire, worldPos, scale, new Color(1f, 0.55f, 0.25f, 1f), 24);
            return;
        }

        Sprite[] frames = HeroKnightLibrary.GetBossAttackEffectFrames();
        if (frames == null || frames.Length == 0)
            return;

        SpawnBurst(worldPos, frames, 16f, scale, new Color(1f, 0.55f, 0.25f, 1f), 24);
    }

    public static void PlayDragonStrike(Vector3 worldPos)
    {
        AudioManager.PlayBossAttack();
        if (SkillVfxLibrary.GetFrames(SkillVfxStyle.Fire).Length > 0)
        {
            SkillVfxLibrary.Play(SkillVfxStyle.Fire, worldPos, 2.1f, new Color(1f, 0.35f, 0.2f, 1f), 26);
            return;
        }

        Sprite[] frames = HeroKnightLibrary.GetBossAttackEffectFrames();
        if (frames == null || frames.Length == 0)
            return;

        SpawnBurst(worldPos, frames, 12f, 2.2f, new Color(1f, 0.35f, 0.2f, 1f), 26);
    }

    private static void SpawnBurst(Vector3 worldPos, Sprite[] frames, float fps, float scale, Color tint, int sortingOrder)
    {
        GameObject go = new GameObject("HeroKnightVfx");
        go.transform.position = worldPos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = frames[0];
        sr.color = tint;
        sr.sortingOrder = sortingOrder;
        go.AddComponent<HeroKnightVfxRunner>().Begin(frames, fps, scale);
    }

    internal static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null)
            return whiteSprite;

        const int size = 4;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();
        whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        return whiteSprite;
    }
}

public class HeroKnightVfxRunner : MonoBehaviour
{
    private SpriteRenderer sr;
    private Sprite[] frames;
    private float frameDuration;
    private float scale = 1f;
    private int index;
    private float timer;

    public void Begin(Sprite[] spriteFrames, float fps, float worldScale)
    {
        frames = spriteFrames;
        scale = worldScale;
        frameDuration = 1f / Mathf.Max(1f, fps);
        sr = GetComponent<SpriteRenderer>();
        if (sr != null && frames != null && frames.Length > 0)
        {
            sr.sprite = frames[0];
            float h = frames[0].bounds.size.y;
            float s = h > 0.001f ? scale / h : scale;
            // Giới hạn để VFX sprite nhỏ không phình thành vòng tròn khổng lồ.
            s = Mathf.Clamp(s, 0.05f, 3f);
            transform.localScale = Vector3.one * s;
        }

        StartCoroutine(DestroyAfterPlay());
    }

    private void Update()
    {
        if (frames == null || frames.Length <= 1 || sr == null)
            return;

        timer += Time.deltaTime;
        if (timer < frameDuration)
            return;

        timer -= frameDuration;
        index++;
        if (index >= frames.Length)
            return;

        sr.sprite = frames[index];
    }

    private IEnumerator DestroyAfterPlay()
    {
        float life = frames != null ? frames.Length * frameDuration + 0.05f : 0.35f;
        yield return new WaitForSeconds(life);
        Destroy(gameObject);
    }
}
