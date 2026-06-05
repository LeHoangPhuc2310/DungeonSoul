#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UObject = UnityEngine.Object;

public static class PlayableCharacterBuilder
{
    private const string OutputPath = "Assets/Resources/PlayableCharacters/Database.asset";
    private const string TinyRpgRoot =
        "Assets/Art/EnemyAssets/Tiny RPG Character Asset Pack v1.03b -Full 20 Characters/Tiny RPG Character Asset Pack v1.03 -Full 20 Characters/Characters(100x100)";

    private struct CharDef
    {
        public string folder;
        public string displayName;
        public HeroType heroClass;
        public float hp;
        public float dmg;
        public float move;
        public float fireRate;
        public float crit;
        public string bonusPos;
        public string bonusNeg;
        public string ability;
        public string abilityDesc;
    }

    private static readonly CharDef[] Characters =
    {
        Def("Knight", "Hiệp sĩ", HeroType.Warrior, 150, 15, 4.5f, 1.2f, 0.05f,
            "+20% sát thương chém", "-10% tốc bắn", "Cuồng Chiến", "Trụ vững, sát thương ổn định."),
        Def("Swordsman", "Kiếm sĩ", HeroType.Warrior, 140, 16, 4.6f, 1.25f, 0.06f,
            "+18% sát thương", "-8% máu", "Phản Đòn", "Đánh nhanh hơn quái thường."),
        Def("Soldier", "Lính", HeroType.Warrior, 135, 14, 4.4f, 1.15f, 0.04f,
            "+15% giáp", "-12% tốc chạy", "Khiên Sắt", "Chậm nhưng bền."),
        Def("Armored Axeman", "Kỵ sĩ rìu", HeroType.Warrior, 160, 18, 4.0f, 1.0f, 0.04f,
            "+25% sát thương", "-15% tốc chạy", "Đập Mạnh", "Mỗi đòn nặng hơn."),
        Def("Armored Orc", "Orc giáp", HeroType.Warrior, 170, 17, 4.2f, 1.05f, 0.03f,
            "+22% máu", "-10% tốc bắn", "Máu Chiến", "Tank cận chiến."),
        Def("Elite Orc", "Orc tinh nhuệ", HeroType.Warrior, 155, 19, 4.3f, 1.1f, 0.05f,
            "+20% sát thương", "-8% máu", "Uy Hiếp", "Cân bằng sát thương/máu."),
        Def("Orc", "Orc", HeroType.Warrior, 145, 16, 4.5f, 1.15f, 0.05f,
            "+15% sát thương", "-5% tốc chạy", "Hung Hãn", "Lính cận chiến cơ bản."),
        Def("Knight Templar", "Kỵ sĩ thánh", HeroType.Warrior, 165, 14, 4.2f, 1.0f, 0.03f,
            "+30% máu", "-15% sát thương", "Thánh Hộ", "Siêu trụ, sát thương vừa."),
        Def("Werebear", "Gấu chiến", HeroType.Warrior, 180, 20, 3.8f, 0.95f, 0.04f,
            "+28% sát thương", "-20% tốc chạy", "Gầm Rú", "Chậm nhưng cực mạnh."),
        Def("Archer", "Cung thủ", HeroType.Ranger, 100, 20, 5.5f, 1.8f, 0.15f,
            "+25% tốc bắn", "-25% máu", "Mưa Tên", "Bắn nhanh, tầm xa."),
        Def("Skeleton Archer", "Xương cung", HeroType.Ranger, 95, 19, 5.3f, 1.75f, 0.14f,
            "+20% chí mạng", "-20% máu", "Xuyên Giáp", "Crit cao, thân mỏng."),
        Def("Lancer", "Kỵ sĩ thương", HeroType.Ranger, 110, 18, 5.6f, 1.6f, 0.12f,
            "+18% tốc chạy", "-15% máu", "Xung Kích", "Di chuyển nhanh."),
        Def("Orc rider", "Kỵ Orc", HeroType.Ranger, 120, 17, 5.4f, 1.55f, 0.10f,
            "+15% tốc chạy", "-10% sát thương", "Kỵ Binh", "Linh hoạt trên map."),
        Def("Werewolf", "Người sói", HeroType.Ranger, 105, 21, 5.8f, 1.7f, 0.13f,
            "+22% sát thương", "-18% máu", "Săn Mồi", "Nhanh và hung ác."),
        Def("Wizard", "Pháp sư", HeroType.Mage, 80, 28, 4.0f, 0.9f, 0.08f,
            "+30% sát thương phép", "-25% máu", "Hỏa Cầu", "Burst cao, rất mỏng."),
        Def("Priest", "Thầy tu", HeroType.Mage, 90, 22, 4.2f, 1.0f, 0.06f,
            "+20% hồi máu skill", "-15% sát thương", "Thánh Quang", "Sống sót tốt hơn mage."),
        Def("Skeleton", "Bộ xương", HeroType.Mage, 85, 24, 4.1f, 0.95f, 0.07f,
            "+18% sát thương", "-20% máu", "Lời Nguyền", "Sát thương phép ổn."),
        Def("Armored Skeleton", "Xương giáp", HeroType.Mage, 100, 20, 3.9f, 0.85f, 0.05f,
            "+15% máu", "-10% tốc bắn", "Khiên Xương", "Mage tanky hơn."),
        Def("Greatsword Skeleton", "Xương đại kiếm", HeroType.Mage, 95, 26, 4.0f, 0.88f, 0.06f,
            "+25% sát thương", "-22% máu", "Đại Kiếm", "Sát thương cực cao."),
        Def("Slime", "Slime", HeroType.Mage, 70, 30, 4.8f, 1.1f, 0.10f,
            "+35% sát thương", "-35% máu", "Nhầy Nhụa", "Glass cannon — cực mỏng.")
    };

    [MenuItem("DungeonSoul/Characters/Build Playable Character Database")]
    public static void BuildWithDialog() => BuildInternal(showDialog: true);

    public static void BuildSilent() => BuildInternal(showDialog: false);

    private static void BuildInternal(bool showDialog)
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/PlayableCharacters");

        PlayableCharacterDatabase db = AssetDatabase.LoadAssetAtPath<PlayableCharacterDatabase>(OutputPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<PlayableCharacterDatabase>();
            AssetDatabase.CreateAsset(db, OutputPath);
        }

        db.entries = new List<PlayableCharacterEntry>();
        int built = 0;
        for (int i = 0; i < Characters.Length; i++)
        {
            PlayableCharacterEntry entry = BuildEntry(Characters[i]);
            if (entry != null && entry.PreviewSprite != null)
            {
                db.entries.Add(entry);
                built++;
            }
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        PlayableCharacterCatalog.InvalidateCache();

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Dungeon Soul",
                $"Đã tạo {built}/20 nhân vật chơi được.\n\n" +
                "Mở CharacterSelectScene để xem lưới nhân vật.",
                "OK");
        }
        else
        {
            Debug.Log($"[PlayableCharacterBuilder] Built {built} playable characters.");
        }
    }

    private static PlayableCharacterEntry BuildEntry(CharDef def)
    {
        PlayableCharacterEntry entry = new PlayableCharacterEntry
        {
            id = Slug(def.folder),
            displayName = def.displayName,
            combatClass = def.heroClass,
            hp = def.hp,
            damage = def.dmg,
            moveSpeed = def.move,
            fireRate = def.fireRate,
            crit = def.crit,
            bonusPositive = def.bonusPos,
            bonusNegative = def.bonusNeg,
            abilityName = def.ability,
            abilityDescription = def.abilityDesc,
            idle = LoadTinyRpgSheet(def.folder, "Idle"),
            walk = FirstNonEmpty(LoadTinyRpgSheet(def.folder, "Walk"), LoadTinyRpgSheet(def.folder, "Idle"))
        };

        if (entry.PreviewSprite == null)
            Debug.LogWarning("[PlayableCharacterBuilder] Không có sprite: " + def.folder);

        return entry;
    }

    private static CharDef Def(string folder, string name, HeroType cls,
        float hp, float dmg, float move, float fireRate, float crit,
        string pos, string neg, string ability, string desc)
    {
        return new CharDef
        {
            folder = folder,
            displayName = name,
            heroClass = cls,
            hp = hp,
            dmg = dmg,
            move = move,
            fireRate = fireRate,
            crit = crit,
            bonusPos = pos,
            bonusNeg = neg,
            ability = ability,
            abilityDesc = desc
        };
    }

    private static Sprite[] LoadTinyRpgSheet(string characterName, string stateSuffix)
    {
        string folder = Path.Combine(TinyRpgRoot, characterName).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(folder))
            return Array.Empty<Sprite>();

        string fileName = characterName + "-" + stateSuffix + ".png";
        string[] guids = AssetDatabase.FindAssets(fileName + " t:Texture2D", new[] { folder });
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

    private static Sprite[] LoadSprites(string path)
    {
        UObject[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        List<Sprite> list = new List<Sprite>();
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
                list.Add(sprite);
        }

        return list.OrderBy(s => s.name).ToArray();
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

    private static string Slug(string value) =>
        value.ToLowerInvariant().Replace(" ", "_").Replace("-", "_");

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
#endif
