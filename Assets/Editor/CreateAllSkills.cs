using UnityEditor;
using UnityEngine;

public static class CreateAllSkills
{
    private const string SkillsFolder = "Assets/Skills";

    private struct SkillDefinition
    {
        public string skillName;
        public string description;
        public SkillRarity rarity;
        public SkillType skillType;
        public float value;

        public SkillDefinition(string skillName, string description, SkillRarity rarity, SkillType skillType, float value)
        {
            this.skillName = skillName;
            this.description = description;
            this.rarity = rarity;
            this.skillType = skillType;
            this.value = value;
        }
    }

    [MenuItem("Tools/Create All Skills")]
    public static void CreateAll()
    {
        EnsureSkillsFolder();

        SkillDefinition[] definitions =
        {
            // COMMON
            new SkillDefinition("DoubleShot", "Ban 2 dan song song", SkillRarity.Common, SkillType.DoubleShot, 2f),
            new SkillDefinition("SpeedBoost", "Di chuyen nhanh hon +1.0", SkillRarity.Common, SkillType.SpeedBoost, 1.0f),
            new SkillDefinition("IronBody", "Tang HP toi da +20", SkillRarity.Common, SkillType.IronBody, 20f),
            new SkillDefinition("QuickReload", "Giam cooldown ban 20%", SkillRarity.Common, SkillType.QuickReload, 0.20f),
            new SkillDefinition("CoinMagnet", "Tang pham vi hut xu +3", SkillRarity.Common, SkillType.CoinMagnet, 3f),
            new SkillDefinition("ToughSkin", "Giam sat thuong nhan 8%", SkillRarity.Common, SkillType.ToughSkin, 0.08f),
            new SkillDefinition("FireArrow", "Dan gay lua 3 damage/giay", SkillRarity.Common, SkillType.FireArrow, 3f),
            new SkillDefinition("SteadyAim", "Tang damage co ban +5", SkillRarity.Common, SkillType.SteadyAim, 5f),

            // RARE
            new SkillDefinition("PiercingArrow", "Dan xuyen qua 2 enemy", SkillRarity.Rare, SkillType.PiercingArrow, 2f),
            new SkillDefinition("MultiTarget", "Ban 2 enemy cung luc", SkillRarity.Rare, SkillType.MultiTarget, 2f),
            new SkillDefinition("CriticalHit", "25% crit x2 damage", SkillRarity.Rare, SkillType.CriticalHit, 0.25f),
            new SkillDefinition("LifeSteal", "Hoi HP 10% damage gay ra", SkillRarity.Rare, SkillType.LifeSteal, 0.10f),
            new SkillDefinition("Boomerang", "Dan quay ve sau max range", SkillRarity.Rare, SkillType.Boomerang, 0.8f),
            new SkillDefinition("LightningChain", "Crit chain sang 1 enemy", SkillRarity.Rare, SkillType.LightningChain, 1f),
            new SkillDefinition("PoisonCloud", "Enemy chet de lai vung doc", SkillRarity.Rare, SkillType.PoisonCloud, 5f),
            new SkillDefinition("ExplosiveRounds", "Dan no radius 1.5", SkillRarity.Rare, SkillType.ExplosiveRounds, 1.5f),

            // EPIC
            new SkillDefinition("Explosion", "Enemy chet phat no radius 2.5", SkillRarity.Epic, SkillType.Explosion, 2.5f),
            new SkillDefinition("IceAura", "Lam cham enemy xung quanh 40%", SkillRarity.Epic, SkillType.IceAura, 0.4f),
            new SkillDefinition("GhostForm", "Vo dich 2.5s moi 15s", SkillRarity.Epic, SkillType.GhostForm, 2.5f),
            new SkillDefinition("QuadShot", "Ban them 2 dan vuong goc", SkillRarity.Epic, SkillType.QuadShot, 2f),
            new SkillDefinition("BladeStorm", "3 kiem quay quanh nguoi", SkillRarity.Epic, SkillType.BladeStorm, 3f),
            new SkillDefinition("Vampire", "Moi kill hoi 5% HP toi da", SkillRarity.Epic, SkillType.Vampire, 0.05f),
            new SkillDefinition("TwinArrows", "Ban 2 dan lien tiep", SkillRarity.Epic, SkillType.TwinArrows, 2f),

            // LEGENDARY
            new SkillDefinition("DeathMark", "Enemy bi danh 5 lan phat no", SkillRarity.Legendary, SkillType.DeathMark, 5f),
            new SkillDefinition("TimeFreeze", "Dong bang tat ca enemy 2.5s", SkillRarity.Legendary, SkillType.TimeFreeze, 2.5f),
            new SkillDefinition("DragonStrike", "Phong rong lua moi 8s", SkillRarity.Legendary, SkillType.DragonStrike, 150f),
            new SkillDefinition("SoulHarvest", "Kill enemy 30% spawn cau hoi HP", SkillRarity.Legendary, SkillType.SoulHarvest, 0.30f),
            new SkillDefinition("MirrorImage", "Tao phan than ban 40% damage", SkillRarity.Legendary, SkillType.MirrorImage, 0.4f)
        };

        int createdOrUpdatedCount = 0;
        for (int i = 0; i < definitions.Length; i++)
        {
            SkillDefinition def = definitions[i];
            string assetPath = $"{SkillsFolder}/{i + 1:00}_{def.skillName}.asset";

            SkillData data = AssetDatabase.LoadAssetAtPath<SkillData>(assetPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<SkillData>();
                AssetDatabase.CreateAsset(data, assetPath);
            }

            data.skillName = def.skillName;
            data.description = def.description;
            data.rarity = def.rarity;
            data.skillType = def.skillType;
            data.value = def.value;

            EditorUtility.SetDirty(data);
            createdOrUpdatedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"CreateAllSkills: Created/updated {createdOrUpdatedCount} SkillData assets in {SkillsFolder}.");
    }

    private static void EnsureSkillsFolder()
    {
        if (AssetDatabase.IsValidFolder(SkillsFolder))
            return;

        AssetDatabase.CreateFolder("Assets", "Skills");
    }
}
