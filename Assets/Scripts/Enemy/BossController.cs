using UnityEngine;

public class BossController : MonoBehaviour
{
    [SerializeField] private string bossName = "Boss";
    [SerializeField] private int coinReward = 100;
    [SerializeField] private int scoreReward = 500;

    private HealthSystem health;
    private EnemyAI ai;
    private int phase = 1;
    private float baseSpeed;
    private float baseDamage;

    private void Awake()
    {
        health = GetComponent<HealthSystem>();
        ai = GetComponent<EnemyAI>();
        EnemyReward reward = GetComponent<EnemyReward>();
        if (reward == null)
            reward = gameObject.AddComponent<EnemyReward>();

        if (ai != null)
        {
            baseSpeed = ai.MoveSpeed;
            baseDamage = ai.ContactDamage;
        }
    }

    private void Update()
    {
        if (health == null)
            return;

        float ratio = health.CurrentHP / Mathf.Max(1f, health.MaxHP);
        int newPhase = ratio > 0.66f ? 1 : ratio > 0.33f ? 2 : 3;
        if (newPhase == phase)
            return;

        phase = newPhase;
        ApplyPhase(phase);
    }

    private void ApplyPhase(int p)
    {
        if (ai == null)
            return;

        switch (p)
        {
            case 2:
                ai.MoveSpeed = baseSpeed * 1.4f;
                ai.ContactDamage = baseDamage * 1.2f;
                break;
            case 3:
                ai.MoveSpeed = baseSpeed * 1.8f;
                ai.ContactDamage = baseDamage * 1.5f;
                break;
        }

        if (HUDManager.Instance != null)
            HUDManager.Instance.ShowWaveAnnouncement(bossName + " Phase " + p);
    }

    public void OnBossDefeated()
    {
        if (HUDManager.Instance != null)
            HUDManager.Instance.ShowWaveAnnouncement(bossName + " Defeated!");

        RunManager.Instance?.OnBossDefeated();
        RoomManager.Instance?.OnRoomCleared();
    }
}
