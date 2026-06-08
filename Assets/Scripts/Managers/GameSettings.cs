// DungeonSoul — GameSettings.cs — Cài đặt âm thanh + ngôn ngữ, lưu PlayerPrefs.

using UnityEngine;

public enum GameLanguage
{
    Vietnamese = 0,
    English = 1
}

public static class GameSettings
{
    private const string KeyMusic = "ds_music_on";
    private const string KeySfx = "ds_sfx_on";
    private const string KeyLang = "ds_language";

    public static bool MusicOn
    {
        get => PlayerPrefs.GetInt(KeyMusic, 1) == 1;
        set { PlayerPrefs.SetInt(KeyMusic, value ? 1 : 0); PlayerPrefs.Save(); ApplyAudio(); }
    }

    public static bool SfxOn
    {
        get => PlayerPrefs.GetInt(KeySfx, 1) == 1;
        set { PlayerPrefs.SetInt(KeySfx, value ? 1 : 0); PlayerPrefs.Save(); ApplyAudio(); }
    }

    public static GameLanguage Language
    {
        get => (GameLanguage)Mathf.Clamp(PlayerPrefs.GetInt(KeyLang, 0), 0, 1);
        set { PlayerPrefs.SetInt(KeyLang, (int)value); PlayerPrefs.Save(); }
    }

    public static void ApplyAudio()
    {
        // Tắt nhạc = giảm âm lượng tổng (đơn giản, không cần tách kênh).
        AudioListener.volume = MusicOn || SfxOn ? 1f : 0f;
    }

    /// <summary>Chọn chuỗi theo ngôn ngữ hiện tại.</summary>
    public static string T(string vi, string en)
    {
        return Language == GameLanguage.English ? en : vi;
    }
}
