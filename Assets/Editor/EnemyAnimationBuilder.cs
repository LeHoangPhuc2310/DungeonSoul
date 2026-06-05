// DungeonSoul — EnemyAnimationBuilder.cs — Import toàn bộ quái từ EnemyAssets.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UObject = UnityEngine.Object;

public static class EnemyAnimationBuilder
{
    private const string KenneyFolder = "Assets/Enemy_Animations_Set";
    private const string EnemyAssetsRoot = "Assets/Art/EnemyAssets";
    private const string OutputFolder = "Assets/Resources/EnemyAnimations";
    private const string TinyRpgRoot =
        "Assets/Art/EnemyAssets/Tiny RPG Character Asset Pack v1.03b -Full 20 Characters/Tiny RPG Character Asset Pack v1.03 -Full 20 Characters/Characters(100x100)";
    private const string OrcRoot = "Assets/Art/EnemyAssets/Free-Top-Down-Orc-Game-Character-Pixel-Art/PNG";
    private const string GolemRoot = "Assets/Art/EnemyAssets/Golems/PNG";
    private const string VampireRoot = "Assets/Art/EnemyAssets/Vampires/PNG";
    private const string MedusaRoot = "Assets/Art/EnemyAssets/Medusa V2.0/Medusa V2.0/Sprite";

    [MenuItem("DungeonSoul/Enemy/Build Animation Database")]
    public static void BuildAll() => BuildAllInternal(showDialog: true);

    [MenuItem("DungeonSoul/Enemy/Build All Enemy Animations")]
    public static void BuildAllExtended() => BuildAllInternal(showDialog: true);

    public static void BuildAllSilent() => BuildAllInternal(showDialog: false);

    private static void BuildAllInternal(bool showDialog)
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder(OutputFolder);

        List<EnemyAnimationSet> built = new List<EnemyAnimationSet>();

        built.Add(Save(BuildKenney("skeleton1", "Skeleton Grunt", EnemyArchetype.Grunt,
            "Xương khô lang thang.", "Cuồng Xương", "Tốc đánh nhanh hơn khi máu thấp.", "+15% tốc chạy", "-10% máu", 2f)));
        built.Add(Save(BuildKenney("skeleton2", "Skeleton Elite", EnemyArchetype.Elite,
            "Xương nặng đội giáp.", "Khiên Xương", "Giảm sát thương đầu tiên mỗi wave.", "+25% giáp", "-15% tốc chạy", 2.1f)));
        built.Add(Save(BuildKenney("vampire", "Vampire", EnemyArchetype.Brute,
            "Ma cà rồng đói máu.", "Hút Máu", "Hồi một ít máu khi đánh trúng.", "+20% sát thương", "-10% tốc chạy", 2.3f)));

        built.Add(Save(BuildMedusa()));
        built.Add(Save(BuildGolem(1, EnemyArchetype.Brute)));
        built.Add(Save(BuildGolem(2, EnemyArchetype.Brute)));
        built.Add(Save(BuildGolem(3, EnemyArchetype.Elite)));

        for (int i = 1; i <= 3; i++)
            built.Add(Save(BuildTopDownOrc(i)));

        for (int i = 1; i <= 3; i++)
            built.Add(Save(BuildVampirePack(i)));

        foreach (string charName in TinyRpgCharacterNames())
        {
            EnemyAnimationSet set = BuildTinyRpg(charName);
            if (set != null && set.PreviewSprite != null)
                built.Add(Save(set));
        }

        built.RemoveAll(s => s == null);

        string dbPath = OutputFolder + "/Database.asset";
        EnemyAnimationDatabase db = AssetDatabase.LoadAssetAtPath<EnemyAnimationDatabase>(dbPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<EnemyAnimationDatabase>();
            AssetDatabase.CreateAsset(db, dbPath);
        }

        db.enemies = built.Where(s => s != null).ToList();
        db.skeleton1 = db.GetById("skeleton1");
        db.skeleton2 = db.GetById("skeleton2");
        db.vampire = db.GetById("vampire");
        EditorUtility.SetDirty(db);

        UpdateEnemyPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EnemyVisualLibrary.InvalidateCache();

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Dungeon Soul",
                $"Đã tạo {built.Count} bộ animation quái.\n\n" +
                "• Medusa, Golem, Orc, Vampire\n" +
                "• 20 nhân vật Tiny RPG\n" +
                "• Kenney skeleton/vampire\n\n" +
                "Vào Character Select để xem bảng quái.",
                "OK");
        }
        else
        {
            Debug.Log($"[EnemyAnimationBuilder] Built {built.Count} enemy animation sets.");
        }
    }

    private static EnemyAnimationSet BuildMedusa()
    {
        EnemyAnimationSet set = CreateSet("medusa", "Medusa", EnemyArchetype.Elite,
            "Nữ hoàng rắn hóa đá kẻ địch.", "Ánh Mắt Đá",
            "Làm chậm người chơi khi ở gần.", "+30% sát thương", "-20% tốc chạy", 2.4f);
        set.idle = LoadSprites($"{MedusaRoot}/IDLE.png");
        set.move = LoadSprites($"{MedusaRoot}/MOVE.png");
        set.hurt = LoadSprites($"{MedusaRoot}/HURT.png");
        set.death = LoadSprites($"{MedusaRoot}/DEATH.png");
        set.attack = Merge(LoadSprites($"{MedusaRoot}/ATTACK1.png"), LoadSprites($"{MedusaRoot}/ATTACK2.png"));
        return set;
    }

    private static EnemyAnimationSet BuildGolem(int index, EnemyArchetype archetype)
    {
        string id = "golem" + index;
        string name = "Golem " + index;
        EnemyAnimationSet set = CreateSet(id, name, archetype,
            "Golem đá khổng lồ.", "Đập Mặt Đất",
            "Gây sát thương vùng khi tấn công.", "+35% máu", "-25% tốc chạy", 2.2f + index * 0.1f);
        string root = $"{GolemRoot}/Golem{index}";
        set.idle = LoadSprites($"{root}/Idle/Golem{index}_Idle_full.png");
        set.move = FirstNonEmpty(
            LoadSprites($"{root}/Walk/Golem{index}_Walk_full.png"),
            LoadSprites($"{root}/Run/Golem{index}_Run_full.png"));
        set.hurt = LoadSprites($"{root}/Hurt/Golem{index}_Hurt_full.png");
        set.death = LoadSprites($"{root}/Death/Golem{index}_Death_full.png");
        set.attack = LoadSprites($"{root}/Attack/Golem{index}_Attack_full.png");
        return set;
    }

    private static EnemyAnimationSet BuildTopDownOrc(int index)
    {
        EnemyArchetype archetype = index == 1 ? EnemyArchetype.Grunt
            : index == 2 ? EnemyArchetype.Runner : EnemyArchetype.Brute;
        string id = "orc" + index;
        EnemyAnimationSet set = CreateSet(id, "Orc " + index, archetype,
            "Orc hung hăng từ pack top-down.", "Cuồng Chiến",
            "Tăng sát thương khi máu thấp.", "+20% sát thương cận chiến", "-15% tầm đánh", 2f);
        string prefix = index == 3 ? "orc3" : "Orc" + index;
        string root = $"{OrcRoot}/Orc{index}";
        set.idle = LoadOrcState(root, prefix, "idle");
        set.move = FirstNonEmpty(LoadOrcState(root, prefix, "walk"), LoadOrcState(root, prefix, "run"));
        set.hurt = LoadOrcState(root, prefix, "hurt");
        set.death = LoadOrcState(root, prefix, "death");
        set.attack = FirstNonEmpty(LoadOrcState(root, prefix, "attack"), LoadOrcState(root, prefix, "run_attack"));
        return set;
    }

    private static EnemyAnimationSet BuildVampirePack(int index)
    {
        EnemyArchetype archetype = index == 1 ? EnemyArchetype.Runner
            : index == 2 ? EnemyArchetype.Brute : EnemyArchetype.Elite;
        string id = "vampire_pack" + index;
        EnemyAnimationSet set = CreateSet(id, "Vampire " + index, archetype,
            "Ma cà rồng bay lượn.", "Dơi Đêm",
            "Di chuyển nhanh hơn ban đêm.", "+18% tốc chạy", "-12% máu", 2.1f);
        string root = $"{VampireRoot}/Vampires{index}/Parts";
        set.idle = LoadSprites($"{root}/Vampires{index}_Idle_body.png");
        set.move = FirstNonEmpty(
            LoadSprites($"{root}/Vampires{index}_Walk_body.png"),
            LoadSprites($"{root}/Vampires{index}_Run_body.png"));
        set.hurt = LoadSprites($"{root}/Vampires{index}_Hurt_body.png");
        set.death = LoadSprites($"{root}/Vampires{index}_Death_body.png");
        set.attack = LoadSprites($"{root}/Vampires{index}_Attack_body.png");
        return set;
    }

    private static EnemyAnimationSet BuildKenney(string prefix, string displayName, EnemyArchetype archetype,
        string desc, string ability, string abilityDesc, string bonusPos, string bonusNeg, float scale)
    {
        EnemyAnimationSet set = CreateSet(prefix, displayName, archetype, desc, ability, abilityDesc, bonusPos, bonusNeg, scale);
        set.idle = LoadSprites($"{KenneyFolder}/enemies-{prefix}_idle.png");
        set.move = FirstNonEmpty(
            LoadSprites($"{KenneyFolder}/enemies-{prefix}_movement.png"),
            LoadSprites($"{KenneyFolder}/enemies-{prefix}_movemen.png"));
        set.hurt = LoadSprites($"{KenneyFolder}/enemies-{prefix}_take_damage.png");
        set.death = FirstNonEmpty(
            LoadSprites($"{KenneyFolder}/enemies-{prefix}_death2.png"),
            LoadSprites($"{KenneyFolder}/enemies-{prefix}_death.png"));
        set.attack = LoadSprites($"{KenneyFolder}/enemies-{prefix}_attack.png");
        return set;
    }

    private static EnemyAnimationSet BuildTinyRpg(string characterName)
    {
        EnemyArchetype archetype = GuessArchetype(characterName);
        string id = "trpg_" + Slug(characterName);
        EnemyAnimationSet set = CreateSet(id, characterName, archetype,
            "Quái từ Tiny RPG pack.", DefaultAbility(characterName),
            "Kỹ năng đặc trưng theo loại quái.", DefaultBonusPos(archetype), DefaultBonusNeg(archetype), 2f);
        set.idle = LoadTinyRpgSheet(characterName, "Idle");
        set.move = LoadTinyRpgSheet(characterName, "Walk");
        set.hurt = LoadTinyRpgSheet(characterName, "Hurt");
        set.death = LoadTinyRpgSheet(characterName, "Death");
        set.attack = Merge(
            LoadTinyRpgSheet(characterName, "Attack01"),
            LoadTinyRpgSheet(characterName, "Attack02"));
        return set;
    }

    private static EnemyAnimationSet CreateSet(string id, string displayName, EnemyArchetype archetype,
        string desc, string abilityName, string abilityDesc, string bonusPos, string bonusNeg, float scale)
    {
        EnemyAnimationSet set = ScriptableObject.CreateInstance<EnemyAnimationSet>();
        set.id = id;
        set.displayName = displayName;
        set.defaultArchetype = archetype;
        set.description = desc;
        set.abilityName = abilityName;
        set.abilityDescription = abilityDesc;
        set.bonusPositive = bonusPos;
        set.bonusNegative = bonusNeg;
        set.visualScale = scale;
        set.idleFps = 8f;
        set.moveFps = 12f;
        set.hurtFps = 12f;
        set.deathFps = 10f;
        return set;
    }

    private static Sprite[] LoadOrcState(string orcRoot, string prefix, string stateFolderSuffix)
    {
        string folderName = prefix + "_" + stateFolderSuffix;
        string folderPath = Path.Combine(orcRoot, folderName).Replace('\\', '/');
        if (!Directory.Exists(folderPath))
            return Array.Empty<Sprite>();

        string[] pngs = Directory.GetFiles(folderPath, "*.png", SearchOption.TopDirectoryOnly);
        List<Sprite> sprites = new List<Sprite>();
        for (int i = 0; i < pngs.Length; i++)
        {
            string fileName = Path.GetFileName(pngs[i]);
            if (!fileName.Contains("front", StringComparison.OrdinalIgnoreCase))
                continue;
            sprites.AddRange(LoadSprites(pngs[i].Replace('\\', '/')));
        }

        return sprites.OrderBy(s => s.name).ToArray();
    }

    private static Sprite[] LoadTinyRpgSheet(string characterName, string stateSuffix)
    {
        string folder = Path.Combine(TinyRpgRoot, characterName).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(folder))
            return Array.Empty<Sprite>();

        string fileName = characterName + "-" + stateSuffix + ".png";
        string[] guids = AssetDatabase.FindAssets(fileName + " t:Sprite", new[] { folder });
        string bestPath = null;
        int bestScore = int.MaxValue;
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (path.Contains("Split Effects", StringComparison.OrdinalIgnoreCase))
                continue;
            int score = path.Contains("with shadows", StringComparison.OrdinalIgnoreCase) ? 2 : 1;
            if (score < bestScore)
            {
                bestScore = score;
                bestPath = path;
            }
        }

        if (string.IsNullOrEmpty(bestPath))
        {
            string[] files = Directory.GetFiles(folder, fileName, SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i].Replace('\\', '/');
                if (path.Contains("Split Effects", StringComparison.OrdinalIgnoreCase))
                    continue;
                int score = path.Contains("with shadows", StringComparison.OrdinalIgnoreCase) ? 2 : 1;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestPath = path;
                }
            }
        }

        return string.IsNullOrEmpty(bestPath) ? Array.Empty<Sprite>() : LoadSprites(bestPath);
    }

    private static IEnumerable<string> TinyRpgCharacterNames()
    {
        if (!AssetDatabase.IsValidFolder(TinyRpgRoot))
            yield break;

        string full = Path.GetFullPath(TinyRpgRoot);
        if (!Directory.Exists(full))
            yield break;

        foreach (string dir in Directory.GetDirectories(full))
            yield return Path.GetFileName(dir);
    }

    private static EnemyArchetype GuessArchetype(string name)
    {
        string n = name.ToLowerInvariant();
        if (n.Contains("elite") || n.Contains("greatsword") || n.Contains("knight templar") || n.Contains("wizard"))
            return EnemyArchetype.Elite;
        if (n.Contains("slime") || n.Contains("archer") || n.Contains("skeleton archer") || n.Contains("lancer"))
            return EnemyArchetype.Runner;
        if (n.Contains("armored") || n.Contains("werebear") || n.Contains("orc rider") || n.Contains("soldier"))
            return EnemyArchetype.Brute;
        return EnemyArchetype.Grunt;
    }

    private static string DefaultAbility(string name)
    {
        string n = name.ToLowerInvariant();
        if (n.Contains("slime")) return "Nhầy Nhụa";
        if (n.Contains("wizard")) return "Phép Thuật";
        if (n.Contains("werewolf") || n.Contains("werebear")) return "Hoang Dã";
        return "Cuồng Chiến";
    }

    private static string DefaultBonusPos(EnemyArchetype a)
    {
        return a switch
        {
            EnemyArchetype.Runner => "+25% tốc chạy",
            EnemyArchetype.Brute => "+30% máu",
            EnemyArchetype.Elite => "+20% sát thương",
            _ => "+15% sát thương"
        };
    }

    private static string DefaultBonusNeg(EnemyArchetype a)
    {
        return a switch
        {
            EnemyArchetype.Runner => "-20% máu",
            EnemyArchetype.Brute => "-20% tốc chạy",
            EnemyArchetype.Elite => "-10% tốc chạy",
            _ => "-10% tốc chạy"
        };
    }

    private static string Slug(string value)
    {
        return value.ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
    }

    private static Sprite[] LoadSprites(string path)
    {
        if (string.IsNullOrEmpty(path))
            return Array.Empty<Sprite>();

        UObject[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        List<Sprite> list = new List<Sprite>();
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
                list.Add(sprite);
        }

        return list.OrderBy(s => s.name).ToArray();
    }

    private static Sprite[] Merge(params Sprite[][] arrays)
    {
        List<Sprite> merged = new List<Sprite>();
        for (int a = 0; a < arrays.Length; a++)
        {
            if (arrays[a] == null)
                continue;
            merged.AddRange(arrays[a]);
        }

        return merged.Count > 0 ? merged.ToArray() : Array.Empty<Sprite>();
    }

    private static Sprite[] FirstNonEmpty(params Sprite[][] arrays)
    {
        for (int i = 0; i < arrays.Length; i++)
        {
            if (arrays[i] != null && arrays[i].Length > 0)
                return arrays[i];
        }

        return Array.Empty<Sprite>();
    }

    private static EnemyAnimationSet Save(EnemyAnimationSet set)
    {
        if (set == null || set.PreviewSprite == null)
        {
            if (set != null)
                Debug.LogWarning("[EnemyAnimationBuilder] Bỏ qua (không có sprite): " + set.id);
            return null;
        }

        string path = OutputFolder + "/" + set.id + ".asset";
        EnemyAnimationSet existing = AssetDatabase.LoadAssetAtPath<EnemyAnimationSet>(path);
        if (existing != null)
            AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(set, path);
        return set;
    }

    private static void UpdateEnemyPrefab()
    {
        const string prefabPath = "Assets/Prefabs/Enemy.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
            return;

        if (prefab.GetComponent<EnemySpriteAnimator>() == null)
            prefab.AddComponent<EnemySpriteAnimator>();

        EditorUtility.SetDirty(prefab);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
            AssetDatabase.CreateFolder(parent, name);
    }
}
