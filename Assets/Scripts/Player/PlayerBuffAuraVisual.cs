using System.Collections.Generic;
using UnityEngine;

/// <summary>Aura mềm quanh nhân vật — tối đa vài vòng, chỉ skill/passive "có aura", không spam icon AI.</summary>
public class PlayerBuffAuraVisual : MonoBehaviour
{
    private class AuraSlot
    {
        public GameObject root;
        public SpriteRenderer sr;
        public string key;
        public float angle;
        public float radius;
        public float pulse;
        public Sprite[] frames;   // aura PNG loop (GeneratedAuraLibrary); null = fallback SoftRing orbit
        public float frameTimer;
        public int frameIndex;
    }

    [SerializeField] private float baseRadius = 0.62f;
    [SerializeField] private float orbitSpeed = 28f;
    [SerializeField] private int maxAuras = 3;

    private static Sprite SoftRing;
    private readonly List<AuraSlot> slots = new List<AuraSlot>(4);
    private readonly List<string> desiredKeys = new List<string>(8);

    private void Awake()
    {
        if (SoftRing == null)
            SoftRing = SoftRingSprite.Get(64, 0.58f);
    }

    private void OnEnable()
    {
        PassiveItemManager passives = PassiveItemManager.Instance;
        if (passives != null)
            passives.OnPassivesChanged += Refresh;
    }

    private void OnDisable()
    {
        PassiveItemManager passives = PassiveItemManager.Instance;
        if (passives != null)
            passives.OnPassivesChanged -= Refresh;

        ClearAll();
    }

    private void LateUpdate()
    {
        BuildDesiredList();
        SyncSlots();

        float dt = Time.deltaTime;
        for (int i = 0; i < slots.Count; i++)
        {
            AuraSlot s = slots[i];
            if (s.root == null || !s.root.activeSelf)
                continue;

            s.pulse += dt * (1.6f + i * 0.2f);

            if (s.frames != null && s.frames.Length > 0)
            {
                // Aura PNG: vòng sáng đặt giữa chân player, loop frame, thở nhẹ alpha.
                s.root.transform.position = transform.position;
                s.frameTimer += dt;
                if (s.frameTimer >= 0.14f && s.frames.Length > 1)
                {
                    s.frameTimer = 0f;
                    s.frameIndex = (s.frameIndex + 1) % s.frames.Length;
                    if (s.sr != null)
                        s.sr.sprite = s.frames[s.frameIndex];
                }

                if (s.sr != null)
                {
                    Color c = s.sr.color;
                    c.a = 0.38f + Mathf.Sin(s.pulse * 1.3f) * 0.08f;
                    s.sr.color = c;
                }
                continue;
            }

            s.angle += orbitSpeed * dt * (0.85f + i * 0.12f);
            float breathe = 1f + Mathf.Sin(s.pulse) * 0.08f;
            float rad = s.radius * breathe;
            float a = s.angle * Mathf.Deg2Rad;
            s.root.transform.position = transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * rad;

            if (s.sr != null)
            {
                Color c = s.sr.color;
                c.a = 0.22f + Mathf.Sin(s.pulse * 1.3f) * 0.06f;
                s.sr.color = c;
            }
        }
    }

    private void BuildDesiredList()
    {
        desiredKeys.Clear();

        PlayerSkillHandler skills = PlayerSkillHandler.Instance;
        if (skills != null)
        {
            for (int i = 0; i < skills.activeSkills.Count && desiredKeys.Count < maxAuras; i++)
            {
                SkillData sd = skills.activeSkills[i];
                if (sd == null || !ShouldShowSkillAura(sd.skillType))
                    continue;

                string key = "S:" + sd.skillType;
                if (!desiredKeys.Contains(key))
                    desiredKeys.Add(key);
            }
        }

        PassiveItemManager passives = PassiveItemManager.Instance;
        if (passives != null)
        {
            IReadOnlyList<PassivePick> picks = passives.PickedItems;
            for (int i = 0; i < picks.Count && desiredKeys.Count < maxAuras; i++)
            {
                PassivePick pick = picks[i];
                if (pick?.data == null)
                    continue;

                string key = "P:" + pick.data.id;
                if (!desiredKeys.Contains(key))
                    desiredKeys.Add(key);
            }
        }
    }

    private static bool ShouldShowSkillAura(SkillType type)
    {
        switch (type)
        {
            case SkillType.IceAura:
            case SkillType.GhostForm:
            case SkillType.BladeStorm:
            case SkillType.Vampire:
            case SkillType.PoisonCloud:
            case SkillType.LightningChain:
            case SkillType.DeathMark:
            case SkillType.TimeFreeze:
            case SkillType.DragonStrike:
            case SkillType.SoulHarvest:
            case SkillType.MirrorImage:
                return true;
            default:
                return false;
        }
    }

    private void SyncSlots()
    {
        for (int i = slots.Count - 1; i >= 0; i--)
        {
            if (!desiredKeys.Contains(slots[i].key))
            {
                if (slots[i].root != null)
                    Destroy(slots[i].root);
                slots.RemoveAt(i);
            }
        }

        for (int i = 0; i < desiredKeys.Count; i++)
        {
            string key = desiredKeys[i];
            AuraSlot existing = FindSlot(key);
            if (existing != null)
            {
                existing.radius = baseRadius + i * 0.1f;
                continue;
            }

            if (slots.Count >= maxAuras)
                break;

            AuraSlot created = CreateSlot(key, i);
            if (created != null)
                slots.Add(created);
        }
    }

    private AuraSlot FindSlot(string key)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].key == key)
                return slots[i];
        }

        return null;
    }

    private AuraSlot CreateSlot(string key, int index)
    {
        Color tint = Color.white;
        Sprite[] frames = null;

        if (key.StartsWith("S:"))
        {
            if (!System.Enum.TryParse(key.Substring(2), out SkillType type))
                return null;
            frames = GeneratedAuraLibrary.GetSkillAura(type);
            tint = GeneratedAuraLibrary.SkillAuraTint(type);
        }
        else if (key.StartsWith("P:"))
        {
            PassiveItemData data = FindPassiveData(key.Substring(2));
            frames = GeneratedAuraLibrary.GetPassiveAura(data != null ? data.id : null);
            tint = GeneratedAuraLibrary.PassiveAuraTint(data);
            if (tint.a > 0.9f)
                tint.a = 0.75f;
        }
        else
            return null;

        GameObject go = RuntimeSpawnGuard.Mark(new GameObject("SoftAura_" + key));
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 18;

        if (frames != null && frames.Length > 0)
        {
            // Aura PNG đã vẽ đủ màu — không tint, đặt vòng quanh chân player to dần theo index.
            sr.sprite = frames[0];
            sr.color = new Color(1f, 1f, 1f, 0.4f);
            float h = frames[0].bounds.size.y;
            float desired = 2f + index * 0.35f; // đường kính canvas (world units)
            go.transform.localScale = Vector3.one * (h > 0.001f ? Mathf.Clamp(desired / h, 0.1f, 3f) : 0.8f);
        }
        else
        {
            frames = null;
            sr.sprite = SoftRing;
            sr.color = new Color(tint.r, tint.g, tint.b, 0.25f);
            go.transform.localScale = Vector3.one * (0.42f + index * 0.06f);
        }

        return new AuraSlot
        {
            root = go,
            sr = sr,
            key = key,
            frames = frames,
            angle = index * 120f,
            radius = baseRadius + index * 0.1f,
            pulse = index * 0.7f
        };
    }

    private static PassiveItemData FindPassiveData(string id)
    {
        PassiveItemManager pm = PassiveItemManager.Instance;
        if (pm == null)
            return null;

        IReadOnlyList<PassivePick> picks = pm.PickedItems;
        for (int i = 0; i < picks.Count; i++)
        {
            if (picks[i]?.data != null && picks[i].data.id == id)
                return picks[i].data;
        }

        return null;
    }

    private void ClearAll()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].root != null)
                Destroy(slots[i].root);
        }

        slots.Clear();
    }

    public void Refresh() { }
}
