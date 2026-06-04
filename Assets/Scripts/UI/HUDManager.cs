using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("Top Left")]
    [SerializeField] private Image hpFillImage;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private GameObject skillsPanel;

    [Header("Top Center")]
    [SerializeField] private TMP_Text floorText;

    [Header("Top Right")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text coinsText;

    [Header("Bottom Bar")]
    [SerializeField] private Image expFillImage;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text expText;

    [Header("Damage Numbers")]
    [SerializeField] private GameObject damageNumberPrefab;

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverCanvas;
    [SerializeField] private GameOverUI gameOverUI;

    private HealthSystem playerHealth;
    private int score;
    private int coins;
    private int currentFloor = 1;
    private bool runEnded;

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
        if (playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerHealth = player.GetComponent<HealthSystem>();
        }

        UpdateHp();
        RefreshTexts();

        if (playerHealth != null && playerHealth.CurrentHP <= 0f && !runEnded)
            ShowGameOver();
    }

    public void UpdateHp()
    {
        if (playerHealth == null || hpFillImage == null || hpText == null)
            return;

        float maxHp = Mathf.Max(1f, playerHealth.MaxHP);
        float ratio = Mathf.Clamp01(playerHealth.CurrentHP / maxHp);
        hpFillImage.fillAmount = ratio;
        hpText.text = Mathf.CeilToInt(playerHealth.CurrentHP) + " / " + Mathf.CeilToInt(maxHp);
    }

    public void UpdateExp(float current, float max, int level)
    {
        if (expFillImage != null) expFillImage.fillAmount = Mathf.Clamp01(current / max);
        if (levelText != null) levelText.text = "LV." + level;
        if (expText != null) expText.text = Mathf.FloorToInt(current) + " / " + Mathf.FloorToInt(max);
    }

    public void UpdateFloor(int floor)
    {
        currentFloor = floor;
    }

    public void AddScore(int amount)
    {
        score += amount;
    }

    public void AddCoins(int amount)
    {
        coins += amount;
    }

    private void RefreshTexts()
    {
        if (scoreText != null) scoreText.text = "Score: " + score;
        if (coinsText != null) coinsText.text = "Xu: " + coins;
        if (floorText != null) floorText.text = "TẦNG " + currentFloor + " / 10";
    }

    public void ShowGameOver()
    {
        if (runEnded) return;
        runEnded = true;

        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(true);
            gameOverUI?.Setup(score, currentFloor, coins);
        }
        else if (GameOverUI.Instance != null)
        {
            GameOverUI.Instance.Show(score, currentFloor, coins);
        }
        else
        {
            new GameObject("GameOverUI").AddComponent<GameOverUI>().Show(score, currentFloor, coins);
        }

        Time.timeScale = 0f;
    }

    public static void SpawnDamageNumber(Vector3 worldPosition, float amount, bool isCrit = false)
    {
        if (amount <= 0f)
            return;

        HUDManager hud = Instance;
        if (hud != null && hud.damageNumberPrefab != null)
        {
            hud.SpawnFromPrefab(worldPosition, amount, isCrit);
            return;
        }

        SpawnRuntimeDamageText(worldPosition, amount, isCrit);
    }

    private void SpawnFromPrefab(Vector3 worldPosition, float amount, bool isCrit)
    {
        if (damageNumberPrefab == null)
        {
            SpawnRuntimeDamageText(worldPosition, amount, isCrit);
            return;
        }

        Vector3 spawnPos = worldPosition + Vector3.up * 0.5f;
        GameObject go = Instantiate(damageNumberPrefab, spawnPos, Quaternion.identity);

        TextMeshPro tmp = go.GetComponent<TextMeshPro>();
        if (tmp == null)
            tmp = go.GetComponentInChildren<TextMeshPro>();

        if (tmp != null)
        {
            tmp.text = Mathf.RoundToInt(amount).ToString();
            tmp.color = isCrit ? Color.yellow : Color.white;
            if (isCrit)
                tmp.fontSize *= 1.25f;
        }

        DamageNumberFloat floater = go.GetComponent<DamageNumberFloat>();
        if (floater == null)
            floater = go.AddComponent<DamageNumberFloat>();
        floater.Initialize(0.8f);
    }

    private static void SpawnRuntimeDamageText(Vector3 worldPosition, float amount, bool isCrit)
    {
        GameObject go = new GameObject("DmgNum");
        go.transform.position = worldPosition + Vector3.up * 0.5f;

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = Mathf.RoundToInt(amount).ToString();
        tmp.fontSize = isCrit ? 6f : 4f;
        tmp.color = isCrit ? Color.yellow : Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        DamageNumberFloat floater = go.AddComponent<DamageNumberFloat>();
        floater.Initialize(0.8f);
    }

    // Legacy method stubs to avoid breaking other scripts if they exist
    public void RegisterDamageDealt(float amount) {}
    public void RegisterEnemyKill() {}
    public void UpdateWaveNumber(int wave) {}
    public void UpdateSurvivalTimer(float elapsed) {}
    public void ShowWaveAnnouncement(string message) {}
    public void UpdateWeaponSlots(List<WeaponType> weapons, int maxSlots) {}
    public void UpdatePassiveSlots(IReadOnlyList<PassiveItem> passives, int maxSlots) {}
    public void RegisterEnemyKilled(int s, int c) { AddScore(s); AddCoins(c); }
    public void ShowRunResult(bool victory, int finalScore, int coinsEarned) => ShowGameOver();
    public void AddScore(int amount, bool animateDelta) => AddScore(amount);
}
