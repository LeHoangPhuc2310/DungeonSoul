// DungeonSoul — BossSpawnManager.cs — Spawn bosses by wave/floor gate.

using UnityEngine;

public class BossSpawnManager : MonoBehaviour
{
    public static BossSpawnManager Instance { get; private set; }

    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform spawnCenter;
    [SerializeField] private BossData goblinKing;
    [SerializeField] private BossData stoneGolem;
    [SerializeField] private BossData shadowWitch;
    [SerializeField] private BossData dragonLord;

    private static readonly BossData[] RuntimeBossTable = new BossData[4];

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        LoadBossAssets();
    }

    private void LoadBossAssets()
    {
        if (goblinKing == null) goblinKing = Resources.Load<BossData>("Boss/GoblinKing");
        if (stoneGolem == null) stoneGolem = Resources.Load<BossData>("Boss/StoneGolem");
        if (shadowWitch == null) shadowWitch = Resources.Load<BossData>("Boss/ShadowWitch");
        if (dragonLord == null) dragonLord = Resources.Load<BossData>("Boss/DragonLord");

        RuntimeBossTable[0] = goblinKing ?? BossDataFactory.CreateGoblinKing();
        RuntimeBossTable[1] = stoneGolem ?? BossDataFactory.CreateStoneGolem();
        RuntimeBossTable[2] = shadowWitch ?? BossDataFactory.CreateShadowWitch();
        RuntimeBossTable[3] = dragonLord ?? BossDataFactory.CreateDragonLord();
    }

    public static bool IsBossWave(int wave) => wave == 3 || wave == 6 || wave == 9 || wave == 10;

    public static void SpawnForWave(int wave, Vector3 position, GameObject prefabFallback)
    {
        BossSpawnManager mgr = Instance;
        if (mgr == null)
        {
            GameObject go = new GameObject("BossSpawnManager");
            mgr = go.AddComponent<BossSpawnManager>();
        }

        mgr.SpawnBoss(wave, position, prefabFallback);
    }

    public void SpawnBoss(int wave, Vector3 position, GameObject prefabFallback)
    {
        BossData data = GetDataForWave(wave);
        if (data == null)
            return;

        Vector3 pos = spawnCenter != null ? spawnCenter.position : position;
        GameObject go = prefabFallback != null
            ? Instantiate(prefabFallback, pos, Quaternion.identity)
            : CreateRuntimeBossBody(pos);

        go.tag = "Enemy";
        BossController boss = go.GetComponent<BossController>();
        if (boss == null)
            boss = go.AddComponent<BossController>();
        boss.Initialize(data);

        // Gán hình riêng cho từng boss theo tên (mỗi tầng một loại quái).
        ApplyBossSprite(go, data.bossName);

        // Boss luôn to hơn quái thường — áp scale boss SAU ApplyEnemyScale để không bị ghi đè.
        if (GameplayPresentation.Instance != null)
            GameplayPresentation.Instance.ApplyEnemyScale(go.transform);
        ApplyBossScale(go.transform, wave);

        // VFX triệu hồi boss — vòng to + nổ xanh.
        EffectLibrary.Play(EffectKind.SpawnPoint, pos, 2.4f, new Color(1f, 0.4f, 0.4f, 1f), 14f, 5);
        EffectLibrary.Play(EffectKind.BlueExplosion, pos, 2.8f, Color.white, 18f, 24);

        Debug.Log("[BossSpawn] " + data.bossName + " at wave " + wave);
    }

    public static void SpawnMinionsNear(Vector3 center, int count)
    {
        EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>();
        for (int i = 0; i < count; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 2.5f;
            if (spawner != null)
                spawner.SpawnEnemy();
            else
                Debug.Log("[Boss] Minion spawn at " + (center + (Vector3)offset));
        }
    }

    private BossData GetDataForWave(int wave)
    {
        return wave switch
        {
            3 => RuntimeBossTable[0],
            6 => RuntimeBossTable[1],
            9 => RuntimeBossTable[2],
            10 => RuntimeBossTable[3],
            _ => null
        };
    }

    /// <summary>Phóng to boss tới chiều cao thế giới mong muốn (bất kể PPU/nguồn sprite).</summary>
    private static void ApplyBossScale(Transform bossTransform, int wave)
    {
        SpriteRenderer sr = bossTransform.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = bossTransform.GetComponentInChildren<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
        {
            bossTransform.localScale = Vector3.one * 3.5f;
            return;
        }

        // Boss cao theo bảng chuẩn GameScale (đã tính theo wave).
        float scale = GameScale.ScaleFor(sr.sprite, GameScale.BossHeightFor(wave));
        bossTransform.localScale = new Vector3(scale, scale, 1f);

        EnemyPhysicsSetup physics = bossTransform.GetComponent<EnemyPhysicsSetup>();
        physics?.FitColliderToSprite();
        EnemyAI ai = bossTransform.GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.StopDistance = 0.62f;
            ai.MeleeRange = 0.68f;
            ai.RefreshBaseScale();
        }
    }

    /// <summary>Đặt sprite/animation riêng cho boss theo tên.</summary>
    private static void ApplyBossSprite(GameObject bossGo, string bossName)
    {
        SpriteRenderer sr = bossGo.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = bossGo.GetComponentInChildren<SpriteRenderer>();
        if (sr == null)
            sr = bossGo.AddComponent<SpriteRenderer>();

        // Ưu tiên animation HeroKnight (Minotaur/Orc/Skeleton); fallback sprite tĩnh.
        Sprite[] frames = HeroKnightLibrary.GetBossIdleFrames(bossName);
        if (frames != null && frames.Length > 0)
        {
            sr.sprite = frames[0];
            sr.color = Color.white;

            SimpleSpriteAnimator anim = sr.GetComponent<SimpleSpriteAnimator>();
            if (anim == null)
                anim = sr.gameObject.AddComponent<SimpleSpriteAnimator>();
            Sprite[] walk = HeroKnightLibrary.GetBossWalkFrames(bossName);
            anim.PlayWithWalk(frames, walk, 7f);
            anim.SetCombatFrames(
                HeroKnightLibrary.GetBossHurtFrames(bossName),
                HeroKnightLibrary.GetBossDeathFrames(bossName));
            anim.SetAttackFrames(HeroKnightLibrary.GetBossAttackFrames(bossName));
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                anim.SetFaceTarget(player.transform);
        }
        else
        {
            Sprite sprite = CharacterArtLibrary.GetBossSprite(bossName);
            if (sprite != null)
                sr.sprite = sprite;
            sr.color = Color.white;
        }

        if (sr.sortingOrder < 6)
            sr.sortingOrder = 6;
    }

    private static GameObject CreateRuntimeBossBody(Vector3 pos)
    {
        GameObject enemy = new GameObject("Boss");
        enemy.transform.position = pos;
        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        sr.sprite = ArtSpriteLibrary.GetEnemySprite(EnemyArtKind.Elite);
        sr.color = new Color(1f, 0.35f, 0.35f);
        sr.sortingOrder = 6;
        enemy.transform.localScale = Vector3.one * 2.2f;

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        CircleCollider2D col = enemy.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;

        enemy.AddComponent<EnemyPhysicsSetup>();
        enemy.AddComponent<HealthSystem>();
        EnemyAI ai = enemy.AddComponent<EnemyAI>();
        ai.MoveSpeed = 2f;
        ai.ContactDamage = 12f;
        ai.StopDistance = 1.4f;
        enemy.AddComponent<EnemyReward>();
        return enemy;
    }
}
