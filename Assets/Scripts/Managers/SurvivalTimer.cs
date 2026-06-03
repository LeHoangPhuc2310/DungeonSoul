using System;
using UnityEngine;

public class SurvivalTimer : MonoBehaviour
{
    public static SurvivalTimer Instance { get; private set; }

    public event Action<int> OnMinutePassed;
    public event Action OnBossRushStarted;
    public event Action OnEliteModeStarted;

    private float elapsed;
    private int lastMinute;
    private bool bossRushStarted;
    private bool eliteModeStarted;

    public float ElapsedTime => elapsed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        int minute = Mathf.FloorToInt(elapsed / 60f);
        if (minute > lastMinute)
        {
            lastMinute = minute;
            OnMinutePassed?.Invoke(minute);
        }

        if (!bossRushStarted && elapsed >= 600f)
        {
            bossRushStarted = true;
            OnBossRushStarted?.Invoke();
        }

        if (!eliteModeStarted && elapsed >= 900f)
        {
            eliteModeStarted = true;
            OnEliteModeStarted?.Invoke();
        }

        if (HUDManager.Instance != null)
            HUDManager.Instance.UpdateSurvivalTimer(elapsed);
    }
}
