#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>Import nhân vật từ pack ASEPRITE (4 hướng) vào PlayableCharacterDatabase.</summary>
public static class AsepriteCharacterBuilder
{
    private const string DatabasePath = "Assets/Resources/PlayableCharacters/Database.asset";

    private enum AnimState
    {
        Idle,
        Walk,
        Attack,
        Hurt,
        Death
    }

    private enum FacingDir
    {
        Front,
        Back,
        SideRight,
        SideLeft
    }

    private struct AseCharDef
    {
        public string id;
        public string displayName;
        public HeroType heroClass;
        public string searchRoot;
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

    private static readonly AseCharDef[] Characters =
    {
        Def("swordsman_lvl1", "Kiếm sĩ I", HeroType.Warrior,
            "Assets/ASEPRITE/ASEPRITE_swordsman/ASEPRITE/Swordsman_lvl1",
            140, 16, 4.6f, 1.25f, 0.06f, "+18% sát thương", "-8% máu", "Phản Đòn", "Skin ASEPRITE cấp 1."),
        Def("swordsman_lvl2", "Kiếm sĩ II", HeroType.Warrior,
            "Assets/ASEPRITE/ASEPRITE_swordsman/ASEPRITE/Swordsman_lvl2",
            150, 17, 4.5f, 1.28f, 0.07f, "+20% sát thương", "-6% máu", "Liên Kích", "Skin ASEPRITE cấp 2."),
        Def("swordsman_lvl3", "Kiếm sĩ III", HeroType.Warrior,
            "Assets/ASEPRITE/ASEPRITE_swordsman/ASEPRITE/Swordsman_lvl3",
            160, 18, 4.4f, 1.3f, 0.08f, "+22% sát thương", "-5% máu", "Tối Thượng", "Skin ASEPRITE cấp 3."),
        Def("orc_ase_1", "Orc I", HeroType.Warrior,
            "Assets/ASEPRITE/ASEPRITE_ORC/ASEPRITE/Orc1",
            145, 16, 4.5f, 1.15f, 0.05f, "+15% sát thương", "-5% tốc chạy", "Hung Hãn", "Orc ASEPRITE — biến thể 1."),
        Def("orc_ase_2", "Orc II", HeroType.Warrior,
            "Assets/ASEPRITE/ASEPRITE_ORC/ASEPRITE/Orc2",
            155, 17, 4.4f, 1.12f, 0.05f, "+18% sát thương", "-8% máu", "Uy Hiếp", "Orc ASEPRITE — biến thể 2."),
        Def("orc_ase_3", "Orc III", HeroType.Warrior,
            "Assets/ASEPRITE/ASEPRITE_ORC/ASEPRITE/Orc3",
            165, 18, 4.3f, 1.1f, 0.04f, "+20% máu", "-10% tốc bắn", "Máu Chiến", "Orc ASEPRITE — biến thể 3."),
        Def("slime_ase_2", "Slime II", HeroType.Mage,
            "Assets/ASEPRITE/ASEPRITE_slime/Slime2",
            75, 28, 4.7f, 1.05f, 0.1f, "+30% sát thương", "-30% máu", "Nhầy Độc", "Slime ASEPRITE — biến thể 2."),
        Def("slime_ase_3", "Slime III", HeroType.Mage,
            "Assets/ASEPRITE/ASEPRITE_slime/Slime3",
            80, 30, 4.6f, 1.08f, 0.11f, "+32% sát thương", "-28% máu", "Phân Tách", "Slime ASEPRITE — biến thể 3.")
    };

    [MenuItem("DungeonSoul/Characters/Import ASEPRITE Characters")]
    public static void ImportWithDialog() => ImportInternal(showDialog: true, rebuildTinyRpg: false);

    [MenuItem("DungeonSoul/Characters/Quick Play — Kiếm sĩ 4 hướng (bỏ chọn nhân vật)")]
    public static void EnableQuickPlayBundle()
    {
        PlayableCharacterCatalog.ApplyQuickPlayBundle();
        EditorUtility.DisplayDialog("Quick Play",
            "Đã bật: luôn dùng Kiếm sĩ ASEPRITE I (4 hướng, cầm kiếm sẵn).\n" +
            "Menu Play → thẳng chọn vũ khí → vào game.\n\n" +
            "Tắt: DungeonSoul → Characters → Tắt Quick Play",
            "OK");
    }

    [MenuItem("DungeonSoul/Characters/Tắt Quick Play")]
    public static void DisableQuickPlayBundle()
    {
        PlayableCharacterCatalog.UseQuickPlayBundle = false;
        EditorUtility.DisplayDialog("Quick Play", "Đã tắt — quay lại màn chọn nhân vật.", "OK");
    }

    [MenuItem("DungeonSoul/Characters/Rebuild All + Import ASEPRITE")]
    public static void RebuildAllWithAseprite()
    {
        PlayableCharacterBuilder.BuildSilent();
        ImportInternal(showDialog: true, rebuildTinyRpg: false);
    }

    public static void ImportSilent() => ImportInternal(showDialog: false, rebuildTinyRpg: false);

    private static void ImportInternal(bool showDialog, bool rebuildTinyRpg)
    {
        if (rebuildTinyRpg)
            PlayableCharacterBuilder.BuildSilent();

        EnsureFolder("Assets/Resources/PlayableCharacters");

        PlayableCharacterDatabase db = AssetDatabase.LoadAssetAtPath<PlayableCharacterDatabase>(DatabasePath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<PlayableCharacterDatabase>();
            AssetDatabase.CreateAsset(db, DatabasePath);
        }

        if (db.entries == null)
            db.entries = new List<PlayableCharacterEntry>();

        HashSet<string> aseIds = new HashSet<string>(Characters.Select(c => c.id));
        db.entries.RemoveAll(e => e != null && aseIds.Contains(e.id));

        int built = 0;
        List<string> failures = new List<string>();

        for (int i = 0; i < Characters.Length; i++)
        {
            PlayableCharacterEntry entry = BuildEntry(Characters[i], failures);
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

        string msg = $"Đã import {built}/{Characters.Length} nhân vật ASEPRITE vào Database.\n" +
                     $"Tổng trong DB: {db.entries.Count} nhân vật.";
        if (failures.Count > 0)
            msg += "\n\nThiếu sprite:\n" + string.Join("\n", failures);

        Debug.Log("[AsepriteCharacterBuilder] " + msg.Replace("\n", " "));

        if (showDialog)
            EditorUtility.DisplayDialog("Import ASEPRITE", msg, "OK");
    }

    private static PlayableCharacterEntry BuildEntry(AseCharDef def, List<string> failures)
    {
        string root = def.searchRoot.Replace('\\', '/');
        if (!Directory.Exists(root))
        {
            failures.Add(def.id + ": không tìm thấy " + root);
            return null;
        }

        bool useOrcPng = def.id.StartsWith("orc_ase_");
        Sprite[] idle = LoadStateSprites(root, AnimState.Idle, FacingDir.Front, useOrcPng);
        Sprite[] walk = LoadStateSprites(root, AnimState.Walk, FacingDir.Front, useOrcPng);
        Sprite[] idleBack = LoadStateSprites(root, AnimState.Idle, FacingDir.Back, useOrcPng);
        Sprite[] walkBack = LoadStateSprites(root, AnimState.Walk, FacingDir.Back, useOrcPng);
        Sprite[] idleSide = LoadStateSprites(root, AnimState.Idle, FacingDir.SideRight, useOrcPng);
        Sprite[] walkSide = LoadStateSprites(root, AnimState.Walk, FacingDir.SideRight, useOrcPng);
        Sprite[] idleSideLeft = LoadStateSprites(root, AnimState.Idle, FacingDir.SideLeft, useOrcPng);
        Sprite[] walkSideLeft = LoadStateSprites(root, AnimState.Walk, FacingDir.SideLeft, useOrcPng);
        Sprite[] attack = LoadStateSprites(root, AnimState.Attack, FacingDir.Front, useOrcPng);
        Sprite[] attackBack = LoadStateSprites(root, AnimState.Attack, FacingDir.Back, useOrcPng);
        Sprite[] attackSide = LoadStateSprites(root, AnimState.Attack, FacingDir.SideRight, useOrcPng);
        Sprite[] attackSideLeft = LoadStateSprites(root, AnimState.Attack, FacingDir.SideLeft, useOrcPng);
        Sprite[] hurt = LoadStateSprites(root, AnimState.Hurt, FacingDir.Front, useOrcPng);
        Sprite[] death = LoadStateSprites(root, AnimState.Death, FacingDir.Front, useOrcPng);

        if (idle.Length == 0 && walk.Length == 0)
        {
            failures.Add(def.id + ": không load được idle/walk");
            return null;
        }

        if (idle.Length == 0)
            idle = walk;
        if (walk.Length == 0)
            walk = idle;

        bool fourWay = idleBack.Length > 0 && walkBack.Length > 0
            && idleSide.Length > 0 && walkSide.Length > 0;

        bool heldInSprite = attack.Length > 0 || fourWay;
        bool rangedOverlay = def.heroClass != HeroType.Warrior && !heldInSprite;

        PlayableCharacterEntry entry = new PlayableCharacterEntry
        {
            id = def.id,
            displayName = def.displayName,
            combatClass = def.heroClass,
            idle = idle,
            walk = walk,
            weaponHeldInSprite = heldInSprite,
            keepRangedWeaponOverlay = rangedOverlay,
            useFourDirections = fourWay,
            idleBack = idleBack,
            walkBack = walkBack,
            idleSideRight = idleSide,
            walkSideRight = walkSide,
            idleSideLeft = idleSideLeft,
            walkSideLeft = walkSideLeft,
            attack = attack,
            attackBack = attackBack,
            attackSideRight = attackSide,
            attackSideLeft = attackSideLeft,
            hurt = hurt,
            death = death,
            attackFps = 12f,
            hp = def.hp,
            damage = def.dmg,
            moveSpeed = def.move,
            fireRate = def.fireRate,
            crit = def.crit,
            bonusPositive = def.bonusPos,
            bonusNegative = def.bonusNeg,
            abilityName = def.ability,
            abilityDescription = def.abilityDesc
        };

        if (attack.Length == 0)
            Debug.LogWarning("[AsepriteCharacterBuilder] " + def.id + ": thiếu attack front — dùng idle.");
        if (fourWay && attackBack.Length == 0)
            Debug.LogWarning("[AsepriteCharacterBuilder] " + def.id + ": thiếu attack back.");
        if (fourWay && attackSide.Length == 0)
            Debug.LogWarning("[AsepriteCharacterBuilder] " + def.id + ": thiếu attack side.");

        return entry;
    }

    private static Sprite[] LoadStateSprites(string searchRoot, AnimState state, FacingDir facing, bool useOrcPng)
    {
        if (useOrcPng)
            return LoadOrcPngSprites(searchRoot, state, facing);

        string path = FindDirectionAseprite(searchRoot, state, facing);
        if (string.IsNullOrEmpty(path))
            return Array.Empty<Sprite>();

        return LoadAsepriteSprites(path);
    }

    private static bool TryGetOrcPngFolder(string asepriteRoot, out string pngFolder, out int orcNum)
    {
        pngFolder = null;
        orcNum = 0;
        string folder = Path.GetFileName(asepriteRoot.Replace('\\', '/'));
        Match match = Regex.Match(folder, @"Orc(\d+)", RegexOptions.IgnoreCase);
        if (!match.Success)
            return false;

        orcNum = int.Parse(match.Groups[1].Value);
        pngFolder = $"Assets/ASEPRITE/ASEPRITE_ORC/PNG/Orc{orcNum}/With_shadow";
        return Directory.Exists(pngFolder);
    }

    private static string GetOrcPngFileName(int orcNum, AnimState state)
    {
        string token = state switch
        {
            AnimState.Walk => "walk",
            AnimState.Attack => "attack",
            AnimState.Hurt => "hurt",
            AnimState.Death => "death",
            _ => "idle"
        };

        return $"orc{orcNum}_{token}_with_shadow.png";
    }

    private static Sprite[] LoadOrcPngSprites(string asepriteRoot, AnimState state, FacingDir facing)
    {
        if (!TryGetOrcPngFolder(asepriteRoot, out string pngFolder, out int orcNum))
            return Array.Empty<Sprite>();

        string path = Path.Combine(pngFolder, GetOrcPngFileName(orcNum, state)).Replace('\\', '/');
        if (!File.Exists(path))
            return Array.Empty<Sprite>();

        Sprite[] all = LoadPngSprites(path);
        if (all.Length == 0)
            return Array.Empty<Sprite>();

        List<List<Sprite>> rows = GroupSpritesIntoRows(all);
        int rowIndex = facing switch
        {
            FacingDir.Back => 1,
            FacingDir.SideRight => 3,
            FacingDir.SideLeft => 2,
            _ => 0
        };

        if (rowIndex >= rows.Count)
            return Array.Empty<Sprite>();

        return rows[rowIndex].ToArray();
    }

    private static List<List<Sprite>> GroupSpritesIntoRows(Sprite[] sprites)
    {
        List<Sprite> ordered = sprites.OrderBy(s => FrameSortKey(s.name)).ToList();
        List<List<Sprite>> rows = new List<List<Sprite>>();
        if (ordered.Count == 0)
            return rows;

        List<Sprite> currentRow = new List<Sprite> { ordered[0] };
        float rowY = ordered[0].rect.y;

        for (int i = 1; i < ordered.Count; i++)
        {
            if (Mathf.Abs(ordered[i].rect.y - rowY) > 8f)
            {
                rows.Add(currentRow);
                currentRow = new List<Sprite>();
                rowY = ordered[i].rect.y;
            }

            currentRow.Add(ordered[i]);
        }

        rows.Add(currentRow);
        rows.Sort((a, b) => b[0].rect.y.CompareTo(a[0].rect.y));
        return rows;
    }

    private static Sprite[] LoadPngSprites(string assetPath)
    {
        assetPath = assetPath.Replace('\\', '/');
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        List<Sprite> sprites = new List<Sprite>();

        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
                sprites.Add(sprite);
        }

        if (sprites.Count == 0)
            return Array.Empty<Sprite>();

        return sprites
            .OrderBy(s => FrameSortKey(s.name))
            .ThenBy(s => s.name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string FindDirectionAseprite(string searchRoot, AnimState state, FacingDir facing)
    {
        string[] files = Directory.GetFiles(searchRoot, "*.aseprite", SearchOption.AllDirectories);
        List<string> matches = new List<string>();

        for (int i = 0; i < files.Length; i++)
        {
            string path = files[i].Replace('\\', '/');
            if (path.Contains("__MACOSX"))
                continue;

            string name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            if (!MatchesDirection(name, facing))
                continue;

            if (!MatchesState(name, state))
                continue;

            matches.Add(path);
        }

        if (matches.Count == 0)
            return null;

        matches.Sort(StringComparer.OrdinalIgnoreCase);
        return PickBestMatch(matches, state);
    }

    private static bool MatchesDirection(string nameLower, FacingDir facing)
    {
        bool hasLeft = nameLower.Contains("side_left") || nameLower.Contains("_left");
        bool hasRight = nameLower.Contains("side_right") || nameLower.Contains("_right");
        bool hasBack = nameLower.Contains("_back") || nameLower.Contains("back_");
        bool hasFront = nameLower.Contains("_front") || nameLower.Contains("front_");

        switch (facing)
        {
            case FacingDir.Back:
                return hasBack;
            case FacingDir.SideRight:
                return hasRight && !hasLeft;
            case FacingDir.SideLeft:
                return hasLeft && !hasRight;
            default:
                return hasFront && !hasBack && !hasRight && !hasLeft
                    && !nameLower.Contains("side_");
        }
    }

    private static bool MatchesState(string nameLower, AnimState state)
    {
        switch (state)
        {
            case AnimState.Idle:
                return nameLower.Contains("idle")
                    && !nameLower.Contains("walk")
                    && !nameLower.Contains("run");
            case AnimState.Walk:
                return nameLower.Contains("walk")
                    && !nameLower.Contains("attack");
            case AnimState.Attack:
                return nameLower.Contains("attack")
                    && !nameLower.Contains("walk")
                    && !nameLower.Contains("run");
            case AnimState.Hurt:
                return nameLower.Contains("hurt");
            case AnimState.Death:
                return nameLower.Contains("death");
            default:
                return false;
        }
    }

    private static string PickBestMatch(List<string> matches, AnimState state)
    {
        if (state == AnimState.Attack)
        {
            string pure = matches.Find(p =>
            {
                string n = Path.GetFileNameWithoutExtension(p).ToLowerInvariant();
                return n.Contains("attack") && !n.Contains("walk") && !n.Contains("run");
            });
            if (!string.IsNullOrEmpty(pure))
                return pure;
        }

        return matches[0];
    }

    private static Sprite[] LoadAsepriteSprites(string assetPath)
    {
        assetPath = assetPath.Replace('\\', '/');
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        List<Sprite> sprites = new List<Sprite>();

        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite
                && sprite.name.StartsWith("Frame_", StringComparison.OrdinalIgnoreCase))
                sprites.Add(sprite);
        }

        if (sprites.Count == 0)
            return Array.Empty<Sprite>();

        return sprites
            .OrderBy(s => FrameSortKey(s.name))
            .ThenBy(s => s.name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static int FrameSortKey(string spriteName)
    {
        Match frame = Regex.Match(spriteName, @"Frame_(\d+)", RegexOptions.IgnoreCase);
        if (frame.Success && int.TryParse(frame.Groups[1].Value, out int frameIndex))
            return frameIndex;

        Match tail = Regex.Match(spriteName, @"_(\d+)$");
        if (tail.Success && int.TryParse(tail.Groups[1].Value, out int tailIndex))
            return tailIndex;

        return 0;
    }

    private static AseCharDef Def(string id, string name, HeroType cls, string root,
        float hp, float dmg, float move, float fireRate, float crit,
        string pos, string neg, string ability, string desc)
    {
        return new AseCharDef
        {
            id = id,
            displayName = name,
            heroClass = cls,
            searchRoot = root,
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

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string folderName = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folderName))
            AssetDatabase.CreateFolder(parent, folderName);
    }
}
#endif
