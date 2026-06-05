using UnityEngine;

/// <summary>Smooth EXP bar fill — animates displayed EXP toward target.</summary>
public class ExpBarAnimator
{
    private float displayExp;
    private float displayMax = 100f;
    private int displayLevel = 1;

    private float targetExp;
    private float targetMax = 100f;
    private int targetLevel = 1;

    /// <summary>Seconds to fill an empty bar to 100% at the current level max.</summary>
    private float fillDurationSeconds = 0.9f;
    private float levelUpHold;
    private const float LevelUpHoldDuration = 0.25f;
    private const float ExpEpsilon = 0.05f;

    public bool IsAnimating =>
        levelUpHold > 0f
        || displayLevel != targetLevel
        || Mathf.Abs(displayExp - targetExp) > ExpEpsilon
        || Mathf.Abs(displayMax - targetMax) > 0.5f;

    public void Configure(float secondsToFillFullBar)
    {
        fillDurationSeconds = Mathf.Clamp(secondsToFillFullBar, 0.35f, 3f);
    }

    public void SnapTo(float current, float max, int level)
    {
        targetExp = Mathf.Max(0f, current);
        targetMax = Mathf.Max(1f, max);
        targetLevel = Mathf.Max(1, level);
        displayExp = targetExp;
        displayMax = targetMax;
        displayLevel = targetLevel;
        levelUpHold = 0f;
    }

    public void SetTarget(float current, float max, int level)
    {
        targetExp = Mathf.Max(0f, current);
        targetMax = Mathf.Max(1f, max);
        targetLevel = Mathf.Max(1, level);
    }

    /// <summary>Instantly match level/max when game is paused or system leveled ahead of the bar.</summary>
    public void SyncLevelDisplay(int level, float currentExp, float maxExp)
    {
        int lv = Mathf.Max(1, level);
        targetLevel = lv;
        displayLevel = lv;
        targetMax = Mathf.Max(1f, maxExp);
        displayMax = targetMax;
        targetExp = Mathf.Max(0f, currentExp);
        displayExp = targetExp;
        levelUpHold = 0f;
    }

    public bool Tick(float deltaTime, out bool leveledUpThisFrame)
    {
        leveledUpThisFrame = false;
        if (deltaTime <= 0f)
            return IsAnimating;

        if (levelUpHold > 0f)
        {
            levelUpHold -= deltaTime;
            return true;
        }

        float expPerSecond = displayMax / fillDurationSeconds;

        if (displayLevel < targetLevel)
        {
            displayExp = Mathf.MoveTowards(displayExp, displayMax, expPerSecond * deltaTime);

            if (displayExp >= displayMax - ExpEpsilon)
            {
                displayLevel++;
                displayExp = 0f;
                displayMax = ExpSystem.CalculateExpToNextLevel(displayLevel);
                levelUpHold = LevelUpHoldDuration;
                leveledUpThisFrame = true;
            }

            return true;
        }

        displayLevel = targetLevel;
        displayMax = targetMax;

        if (Mathf.Abs(displayExp - targetExp) > ExpEpsilon)
        {
            displayExp = Mathf.MoveTowards(displayExp, targetExp, expPerSecond * deltaTime);
            return true;
        }

        displayExp = targetExp;
        return false;
    }

    public float DisplayExp => displayExp;
    public float DisplayMax => displayMax;
    public int DisplayLevel => displayLevel;
    public float DisplayFill => displayMax > 0f ? Mathf.Clamp01(displayExp / displayMax) : 0f;
}
