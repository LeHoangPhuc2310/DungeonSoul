using System.Text;
using TMPro;
using UnityEngine;

/// <summary>Combat stats readout under the HP bar / skill icons.</summary>
public class PlayerStatsUI : MonoBehaviour
{
    public static PlayerStatsUI Instance { get; private set; }

    [SerializeField] private TMP_Text statsText;
    [SerializeField] private float fontSize = 24f;

    private readonly StringBuilder sb = new StringBuilder(128);
    private float refreshTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureLabel();
        ApplyHudStyle();
    }

    private void LateUpdate()
    {
        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = 0.12f;
            Refresh();
        }
    }

    public static void NotifyChanged()
    {
        if (Instance != null)
            Instance.Refresh();
    }

    public void Refresh()
    {
        EnsureLabel();
        if (statsText == null)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            statsText.text = "";
            return;
        }

        AutoAttack atk = player.GetComponent<AutoAttack>();
        HealthSystem hp = player.GetComponent<HealthSystem>();
        PlayerController move = player.GetComponent<PlayerController>();
        PlayerSkillStats skills = player.GetComponent<PlayerSkillStats>();
        PlayerSkillHandler handler = PlayerSkillHandler.Instance;

        sb.Clear();
        if (atk != null)
        {
            float shots = atk.FireInterval > 0.001f ? 1f / atk.FireInterval : 0f;
            sb.Append("ATK ").Append(atk.ProjectileDamage.ToString("0"));
            sb.Append(" | ").Append(shots.ToString("0.##")).Append("/s");
            if (skills != null && skills.CritChance > 0.01f)
                sb.Append(" | CRIT ").Append((skills.CritChance * 100f).ToString("0")).Append('%');
            if (atk.ProjectileCount > 1)
                sb.Append(" | Đạn x").Append(atk.ProjectileCount);
            if (atk.MultiTargetCount > 1)
                sb.Append(" | Mục tiêu x").Append(atk.MultiTargetCount);
        }

        if (move != null || hp != null)
        {
            sb.Append('\n');
            if (move != null)
                sb.Append("Tốc ").Append(move.MoveSpeed.ToString("0.#"));
            if (hp != null)
            {
                if (move != null)
                    sb.Append(" | ");
                sb.Append("Máu ").Append(hp.CurrentHP.ToString("0")).Append('/').Append(hp.MaxHP.ToString("0"));
            }
        }

        string extras = BuildExtras(handler, skills);
        if (!string.IsNullOrEmpty(extras))
        {
            sb.Append('\n');
            sb.Append(extras);
        }

        statsText.text = sb.ToString();
    }

    private static string BuildExtras(PlayerSkillHandler handler, PlayerSkillStats skills)
    {
        if (handler == null && skills == null)
            return "";

        var parts = new System.Collections.Generic.List<string>(6);
        if (skills != null)
        {
            if (skills.PierceCount > 0)
                parts.Add("Xuyên " + skills.PierceCount);
            if (skills.FireDotDuration > 0f)
                parts.Add("Cháy " + skills.FireDotDamage.ToString("0") + "/s");
            if (skills.LifeStealPercent > 0f)
                parts.Add("Hút " + (skills.LifeStealPercent * 100f).ToString("0") + "%");
            if (skills.SlowAuraRadius > 0f)
                parts.Add("Băng");
            if (skills.ExplosionOnKillRadius > 0f)
                parts.Add("Nổ");
        }

        if (handler != null)
        {
            if (handler.HasSkill(SkillType.DeathMark))
                parts.Add("Dấu tử");
            if (handler.HasSkill(SkillType.GhostForm))
                parts.Add("Ảo ảnh");
            if (handler.HasSkill(SkillType.DragonStrike))
                parts.Add("Rồng");
            if (handler.HasSkill(SkillType.TimeFreeze))
                parts.Add("Đóng băng TG");
        }

        return parts.Count == 0 ? "" : string.Join(" · ", parts);
    }

    public void ApplyHudStyle()
    {
        EnsureLabel();
        if (statsText == null)
            return;

        statsText.fontSize = fontSize;
        statsText.fontSizeMin = fontSize;
        statsText.fontSizeMax = fontSize;
        statsText.enableAutoSizing = false;
        statsText.outlineWidth = 0.22f;
        statsText.outlineColor = new Color(0f, 0f, 0f, 0.9f);

        RectTransform rt = statsText.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(0f, -48f);
        rt.sizeDelta = new Vector2(0f, 78f);
    }

    private void EnsureLabel()
    {
        if (statsText != null)
            return;

        statsText = GetComponent<TMP_Text>();
        if (statsText == null)
            statsText = gameObject.AddComponent<TextMeshProUGUI>();

        ApplyHudStyle();
    }
}
