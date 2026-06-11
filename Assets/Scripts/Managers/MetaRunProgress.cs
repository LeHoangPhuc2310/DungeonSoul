using UnityEngine;

/// <summary>Meta nhẹ giữa các run — Soul Points, kỷ lục, mở khóa nhân vật (không có shop trong run).</summary>
public static class MetaRunProgress
{
    private const string KeySouls = "ds_meta_souls";
    private const string KeyBestTime = "ds_meta_best_survival_sec";
    private const string KeyBestKills = "ds_meta_best_kills";
    private const string KeyBestScore = "ds_meta_best_score";
    private const string KeyUnlockPrefix = "ds_unlock_char_";

    private static readonly string[] FreeCharacterIds =
    {
        "swordsman_lvl1",
        "orc_ase_1",
        "slime_ase_2"
    };

    private static readonly (string id, int cost)[] UnlockCosts =
    {
        ("swordsman_lvl2", 300),
        ("swordsman_lvl3", 600),
        ("orc_ase_2", 400),
        ("orc_ase_3", 700),
        ("slime_ase_3", 500)
    };

    public static int SoulPoints
    {
        get => PlayerPrefs.GetInt(KeySouls, 0);
        private set
        {
            PlayerPrefs.SetInt(KeySouls, Mathf.Max(0, value));
            PlayerPrefs.Save();
        }
    }

    public static float BestSurvivalSeconds => PlayerPrefs.GetFloat(KeyBestTime, 0f);
    public static int BestKills => PlayerPrefs.GetInt(KeyBestKills, 0);
    public static int BestScore => PlayerPrefs.GetInt(KeyBestScore, 0);

    public static bool IsCharacterUnlocked(string characterId)
    {
        if (string.IsNullOrEmpty(characterId))
            return false;

        for (int i = 0; i < FreeCharacterIds.Length; i++)
        {
            if (FreeCharacterIds[i] == characterId)
                return true;
        }

        return PlayerPrefs.GetInt(KeyUnlockPrefix + characterId, 0) == 1;
    }

    public static int GetUnlockCost(string characterId)
    {
        for (int i = 0; i < UnlockCosts.Length; i++)
        {
            if (UnlockCosts[i].id == characterId)
                return UnlockCosts[i].cost;
        }

        return 0;
    }

    public static bool TryUnlockCharacter(string characterId)
    {
        if (IsCharacterUnlocked(characterId))
            return true;

        int cost = GetUnlockCost(characterId);
        if (cost <= 0 || SoulPoints < cost)
            return false;

        SoulPoints -= cost;
        PlayerPrefs.SetInt(KeyUnlockPrefix + characterId, 1);
        PlayerPrefs.Save();
        return true;
    }

    /// <summary>Ghi nhận kết quả run — gọi từ RunManager.EndRun.</summary>
    public static int RecordRun(bool victory, int score, int kills, float survivalSeconds)
    {
        int soulsEarned = CalculateSoulsEarned(victory, score, kills, survivalSeconds);
        SoulPoints += soulsEarned;

        if (survivalSeconds > BestSurvivalSeconds)
            PlayerPrefs.SetFloat(KeyBestTime, survivalSeconds);

        if (kills > BestKills)
            PlayerPrefs.SetInt(KeyBestKills, kills);

        if (score > BestScore)
            PlayerPrefs.SetInt(KeyBestScore, score);

        PlayerPrefs.Save();
        return soulsEarned;
    }

    private static int CalculateSoulsEarned(bool victory, int score, int kills, float survivalSeconds)
    {
        int souls = Mathf.Max(1, kills / 25);
        souls += score / 500;
        souls += Mathf.FloorToInt(survivalSeconds / 120f);

        if (victory)
            souls += SurvivalRunManager.IsSurvivalMode() ? 50 : 25;

        return Mathf.Clamp(souls, 1, 500);
    }
}
