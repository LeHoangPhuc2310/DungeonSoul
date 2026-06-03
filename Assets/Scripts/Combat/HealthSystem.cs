using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float currentHP = 100f;

    public float MaxHP
    {
        get => maxHP;
        set
        {
            maxHP = Mathf.Max(1f, value);
            currentHP = Mathf.Clamp(currentHP, 0f, maxHP);
        }
    }

    public float CurrentHP
    {
        get => currentHP;
        set => currentHP = Mathf.Clamp(value, 0f, maxHP);
    }

    private void Awake()
    {
        maxHP = Mathf.Max(1f, maxHP);
        currentHP = Mathf.Clamp(currentHP, 0f, maxHP);
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f || currentHP <= 0f)
            return;

        currentHP = Mathf.Max(0f, currentHP - amount);
        if (currentHP <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || currentHP <= 0f)
            return;

        currentHP = Mathf.Min(maxHP, currentHP + amount);
    }

    private void Die()
    {
        if (CompareTag("Enemy") && ExpSystem.Instance != null)
        {
            float floorMultiplier = Mathf.Max(1, FloorManager.currentFloor);
            ExpSystem.Instance.AddExp(20f * floorMultiplier);
        }

        Destroy(gameObject);
    }
}
