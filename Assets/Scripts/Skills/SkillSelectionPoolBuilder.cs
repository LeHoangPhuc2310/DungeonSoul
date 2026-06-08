using System.Collections.Generic;
using UnityEngine;

/// <summary>Pool 3 nguồn có trọng số — skill / passive / vũ khí.</summary>
public static class SkillSelectionPoolBuilder
{
    private struct WeightedCandidate
    {
        public SkillSelectionChoice choice;
        public float weight;
        public SkillSelectionChoiceKind sourceKind;
    }

    public static List<SkillSelectionChoice> Build(
        SkillSelectionContext context,
        List<SkillData> allSkills,
        int playerLevel)
    {
        SkillSelectionConfig cfg = SkillSelectionConfig.Get();
        List<SkillSelectionChoice> result = new List<SkillSelectionChoice>(3);
        HashSet<string> usedKeys = new HashSet<string>();

        // Tutorial: level-up đầu tiên chỉ skill
        if (context == SkillSelectionContext.LevelUp && playerLevel <= 2)
        {
            FillSkillsOnly(result, usedKeys, allSkills, context, cfg, 3);
            LogResult(context, result, "first-level-up-skills-only");
            return PadOrFallback(result, usedKeys, allSkills, context, cfg);
        }

        GetSourceWeights(context, out float wSkill, out float wPassive, out float wWeapon);

        for (int slot = 0; slot < 3; slot++)
        {
            SkillSelectionChoice pick = PickOne(context, allSkills, usedKeys, wSkill, wPassive, wWeapon, cfg);
            if (pick == null)
                continue;

            result.Add(pick);
            usedKeys.Add(pick.GetUniqueKey());
        }

        LogResult(context, result, $"weights skill={wSkill:P0} passive={wPassive:P0} weapon={wWeapon:P0}");
        return PadOrFallback(result, usedKeys, allSkills, context, cfg);
    }

    private static void GetSourceWeights(SkillSelectionContext ctx,
        out float wSkill, out float wPassive, out float wWeapon)
    {
        switch (ctx)
        {
            case SkillSelectionContext.LevelUp:
                wSkill = 0.5f; wPassive = 0.3f; wWeapon = 0.2f;
                break;
            case SkillSelectionContext.NormalChest:
                wSkill = 0.7f; wPassive = 0.3f; wWeapon = 0f;
                break;
            case SkillSelectionContext.EliteChest:
                wSkill = 0.7f; wPassive = 0.3f; wWeapon = 0f;
                break;
            case SkillSelectionContext.BossChest:
                wSkill = 0.4f; wPassive = 0.4f; wWeapon = 0.2f;
                break;
            default:
                wSkill = 0.5f; wPassive = 0.3f; wWeapon = 0.2f;
                break;
        }
    }

    private static SkillSelectionChoice PickOne(
        SkillSelectionContext context,
        List<SkillData> allSkills,
        HashSet<string> usedKeys,
        float wSkill, float wPassive, float wWeapon,
        SkillSelectionConfig cfg)
    {
        List<WeightedCandidate> pool = new List<WeightedCandidate>(48);

        AddSkillCandidates(pool, allSkills, usedKeys, context, cfg, wSkill);
        AddPassiveCandidates(pool, usedKeys, context, wPassive);
        AddWeaponCandidates(pool, usedKeys, context, wWeapon);

        if (pool.Count == 0)
            return null;

        float total = 0f;
        for (int i = 0; i < pool.Count; i++)
            total += pool[i].weight;

        float roll = Random.value * total;
        float acc = 0f;
        for (int i = 0; i < pool.Count; i++)
        {
            acc += pool[i].weight;
            if (roll <= acc)
                return pool[i].choice;
        }

        return pool[pool.Count - 1].choice;
    }

    private static void AddSkillCandidates(
        List<WeightedCandidate> pool,
        List<SkillData> allSkills,
        HashSet<string> usedKeys,
        SkillSelectionContext context,
        SkillSelectionConfig cfg,
        float sourceWeight)
    {
        if (allSkills == null || sourceWeight <= 0f)
            return;

        SkillRarity rolled = RollRarity(context);
        List<SkillData> matching = new List<SkillData>();

        for (int i = 0; i < allSkills.Count; i++)
        {
            SkillData s = allSkills[i];
            if (s == null || usedKeys.Contains("skill:" + s.skillType))
                continue;
            if (IsSkillMaxed(s, cfg))
                continue;
            if (s.rarity == rolled)
                matching.Add(s);
        }

        if (matching.Count == 0)
        {
            for (int i = 0; i < allSkills.Count; i++)
            {
                SkillData s = allSkills[i];
                if (s == null || usedKeys.Contains("skill:" + s.skillType) || IsSkillMaxed(s, cfg))
                    continue;
                matching.Add(s);
            }
        }

        for (int i = 0; i < matching.Count; i++)
        {
            SkillData s = matching[i];
            pool.Add(new WeightedCandidate
            {
                sourceKind = SkillSelectionChoiceKind.SkillUpgrade,
                weight = sourceWeight / matching.Count,
                choice = new SkillSelectionChoice
                {
                    kind = SkillSelectionChoiceKind.SkillUpgrade,
                    skill = s
                }
            });
        }
    }

    private static void AddPassiveCandidates(
        List<WeightedCandidate> pool,
        HashSet<string> usedKeys,
        SkillSelectionContext context,
        float sourceWeight)
    {
        if (sourceWeight <= 0f || PassiveItemManager.Instance == null)
            return;

        bool boss = context == SkillSelectionContext.BossChest;
        SkillRarity rolled = RollRarity(context);

        List<PassiveItemData> eligibles = BuildPassivePool(usedKeys);
        List<PassiveItemData> matched = eligibles.FindAll(p => p != null && p.rarity == rolled);
        if (matched.Count == 0)
            matched = eligibles;

        for (int i = 0; i < matched.Count; i++)
        {
            PassiveItemData p = matched[i];
            pool.Add(new WeightedCandidate
            {
                sourceKind = SkillSelectionChoiceKind.PassiveItem,
                weight = sourceWeight / matched.Count,
                choice = new SkillSelectionChoice
                {
                    kind = SkillSelectionChoiceKind.PassiveItem,
                    passiveItem = p
                }
            });
        }
    }

    private static List<PassiveItemData> BuildPassivePool(HashSet<string> usedKeys)
    {
        List<PassiveItemData> result = new List<PassiveItemData>();
        if (PassiveItemManager.Instance == null)
            return result;

        List<PassiveItemData> eligibles = PassiveItemManager.Instance.GetEligibleForSelection();
        for (int i = 0; i < eligibles.Count; i++)
        {
            PassiveItemData p = eligibles[i];
            if (p == null || usedKeys.Contains("passive:" + p.id))
                continue;
            result.Add(p);
        }

        return result;
    }

    private static void AddWeaponCandidates(
        List<WeightedCandidate> pool,
        HashSet<string> usedKeys,
        SkillSelectionContext context,
        float sourceWeight)
    {
        if (sourceWeight <= 0f || WeaponManager.Instance == null)
            return;

        List<WeaponType> offers = BuildWeaponPool(context, usedKeys);
        for (int i = 0; i < offers.Count; i++)
        {
            pool.Add(new WeightedCandidate
            {
                sourceKind = SkillSelectionChoiceKind.WeaponPickup,
                weight = sourceWeight / offers.Count,
                choice = new SkillSelectionChoice
                {
                    kind = SkillSelectionChoiceKind.WeaponPickup,
                    weaponType = offers[i]
                }
            });
        }
    }

    private static List<WeaponType> BuildWeaponPool(SkillSelectionContext context, HashSet<string> usedKeys)
    {
        List<WeaponType> result = new List<WeaponType>();
        WeaponManager wm = WeaponManager.Instance;

        WeaponType[] bases =
        {
            WeaponType.IronBow, WeaponType.FireStaff, WeaponType.FrostWand,
            WeaponType.PoisonDagger, WeaponType.HolyCross, WeaponType.ThunderRod
        };

        for (int i = 0; i < bases.Length; i++)
        {
            WeaponType t = bases[i];
            string key = "weapon:" + t;
            if (usedKeys.Contains(key))
                continue;

            if (!wm.HasWeapon(t))
            {
                result.Add(t);
                continue;
            }

            if (wm.GetWeaponCopies(t) < 8)
                result.Add(t);
        }

        // Boss: thêm vũ khí tiến hóa nếu đủ điều kiện
        if (context == SkillSelectionContext.BossChest)
        {
            TryAddEvolved(ref result, usedKeys, WeaponType.IronBow, WeaponType.StormBow, wm);
            TryAddEvolved(ref result, usedKeys, WeaponType.FireStaff, WeaponType.DragonStaff, wm);
            TryAddEvolved(ref result, usedKeys, WeaponType.PoisonDagger, WeaponType.DeathDagger, wm);
            TryAddEvolved(ref result, usedKeys, WeaponType.FrostWand, WeaponType.BlizzardWand, wm);
            TryAddEvolved(ref result, usedKeys, WeaponType.HolyCross, WeaponType.HolyNova, wm);
            TryAddEvolved(ref result, usedKeys, WeaponType.ThunderRod, WeaponType.ZeusRod, wm);
        }

        return result;
    }

    private static void TryAddEvolved(ref List<WeaponType> result, HashSet<string> usedKeys,
        WeaponType baseType, WeaponType evolved, WeaponManager wm)
    {
        string key = "weapon:" + evolved;
        if (usedKeys.Contains(key) || wm.HasWeapon(evolved))
            return;
        if (wm.HasWeapon(baseType) && wm.GetWeaponCopies(baseType) >= 6)
            result.Add(evolved);
    }

    private static bool IsSkillMaxed(SkillData skill, SkillSelectionConfig cfg)
    {
        if (skill == null || PlayerSkillHandler.Instance == null)
            return false;

        int stack = PlayerSkillHandler.Instance.GetStack(skill.skillType);
        if (skill.rarity == SkillRarity.Legendary)
            return stack >= 1;

        return stack >= cfg.maxSkillStack;
    }

    public static SkillRarity RollRarity(SkillSelectionContext context)
    {
        float roll = Random.value;
        if (MetaRunModifiers.Instance != null)
            roll -= MetaRunModifiers.Instance.SkillRarityBonus * 0.05f;
        roll = Mathf.Clamp01(roll);

        if (context == SkillSelectionContext.BossChest)
        {
            if (roll < 0.4f) return SkillRarity.Rare;
            if (roll < 0.9f) return SkillRarity.Epic;
            return SkillRarity.Legendary;
        }

        if (roll < 0.55f) return SkillRarity.Common;
        if (roll < 0.85f) return SkillRarity.Rare;
        if (roll < 0.97f) return SkillRarity.Epic;
        return SkillRarity.Legendary;
    }

    private static void FillSkillsOnly(
        List<SkillSelectionChoice> result,
        HashSet<string> usedKeys,
        List<SkillData> allSkills,
        SkillSelectionContext context,
        SkillSelectionConfig cfg,
        int count)
    {
        if (allSkills == null)
            return;

        for (int i = 0; i < count; i++)
        {
            List<SkillData> pool = new List<SkillData>();
            for (int j = 0; j < allSkills.Count; j++)
            {
                SkillData s = allSkills[j];
                if (s == null || usedKeys.Contains("skill:" + s.skillType) || IsSkillMaxed(s, cfg))
                    continue;
                pool.Add(s);
            }

            if (pool.Count == 0)
                break;

            SkillData pick = pool[Random.Range(0, pool.Count)];
            var choice = new SkillSelectionChoice { kind = SkillSelectionChoiceKind.SkillUpgrade, skill = pick };
            result.Add(choice);
            usedKeys.Add(choice.GetUniqueKey());
        }
    }

    private static List<SkillSelectionChoice> PadOrFallback(
        List<SkillSelectionChoice> result,
        HashSet<string> usedKeys,
        List<SkillData> allSkills,
        SkillSelectionContext context,
        SkillSelectionConfig cfg)
    {
        // Mọi thứ max → một thẻ hồi máu
        if (result.Count == 0)
        {
            result.Add(new SkillSelectionChoice
            {
                kind = SkillSelectionChoiceKind.HealFallback,
                bonusHp = cfg.allMaxedHealHp,
                note = "Mọi skill đã max"
            });
            LogResult(context, result, "all-maxed-fallback");
            return result;
        }

        int safety = 0;
        while (result.Count < 3 && safety++ < 8)
        {
            if (result.Count % 2 == 0)
            {
                string key = "bonus_hp";
                if (!usedKeys.Contains(key))
                {
                    result.Add(new SkillSelectionChoice
                    {
                        kind = SkillSelectionChoiceKind.BonusHp,
                        bonusHp = cfg.fallbackBonusHp
                    });
                    usedKeys.Add(key);
                    continue;
                }
            }

            string coinKey = "bonus_coin";
            if (!usedKeys.Contains(coinKey))
            {
                result.Add(new SkillSelectionChoice
                {
                    kind = SkillSelectionChoiceKind.BonusCoin,
                    bonusCoins = cfg.fallbackBonusCoins
                });
                usedKeys.Add(coinKey);
                continue;
            }

            FillSkillsOnly(result, usedKeys, allSkills, context, cfg, 1);
        }

        return result;
    }

    private static void LogResult(SkillSelectionContext ctx, List<SkillSelectionChoice> choices, string tag)
    {
        if (!SkillSelectionConfig.Get().logPoolWeights)
            return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("[SkillPool] ").Append(ctx).Append(" (").Append(tag).Append("): ");
        for (int i = 0; i < choices.Count; i++)
            sb.Append(choices[i].kind).Append(' ');
        Debug.Log(sb.ToString());
    }
}
