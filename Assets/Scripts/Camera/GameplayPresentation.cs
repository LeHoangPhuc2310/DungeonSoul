using UnityEngine;

/// <summary>
/// Keeps camera zoom and character sizes consistent (GDD: ortho size ~6 for portrait).
/// </summary>
[ExecuteAlways]
public class GameplayPresentation : MonoBehaviour
{
    public static GameplayPresentation Instance { get; private set; }

    [Tooltip("Camera zoom — nhỏ = gần hơn. 2.85 ≈ nhân vật chiếm ~28% chiều cao màn hình.")]
    [SerializeField] private float orthographicSize = 2.85f;
    [SerializeField] private float playerVisualScale = 2f;
    [Tooltip("Chiều cao nhân vật trên map (world units). Đồng bộ GameScale.PlayerHeight.")]
    [SerializeField] private float playerTargetHeight = GameScale.PlayerHeight;
    [SerializeField] private float enemyVisualScale = 4.5f;
    [Tooltip("Kích thước đạn (world units đường kính).")]
    [SerializeField] private float projectileVisualScale = GameScale.ProjectileSize;

    public float EnemyVisualScale => enemyVisualScale;
    public float ProjectileVisualScale => projectileVisualScale;
    public float PlayerTargetHeight => playerTargetHeight;

    private void OnEnable()
    {
        Instance = this;
        ApplyCamera();
    }

    private void Awake()
    {
        Instance = this;
        ApplyCamera();
    }

    private void Start()
    {
        ApplyPlayerScale();
        ApplyScaleToExistingEnemies();
    }

    public void ApplyCamera()
    {
        Camera cam = GetComponent<Camera>();
        if (cam == null)
            cam = Camera.main;
        if (cam == null)
            return;

        cam.orthographic = true;
        cam.orthographicSize = orthographicSize;
        cam.orthographicSize = Mathf.Round(cam.orthographicSize * 100f) / 100f;
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
            return;

        Camera cam = GetComponent<Camera>();
        if (cam == null)
            cam = Camera.main;
        if (cam == null || !cam.orthographic)
            return;

        // Ép orthographicSize mỗi frame (scene có thể lưu giá trị cũ 5.5 → đè về giá trị chuẩn).
        if (!Mathf.Approximately(cam.orthographicSize, orthographicSize))
            cam.orthographicSize = orthographicSize;

        Vector3 p = cam.transform.position;
        cam.transform.position = new Vector3(
            Mathf.Round(p.x * 100f) / 100f,
            Mathf.Round(p.y * 100f) / 100f,
            p.z);
    }

    public void ApplyPlayerScale()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        PlayerController move = player.GetComponent<PlayerController>();
        if (move != null)
        {
            PlayableCharacterEntry entry = PlayableCharacterCatalog.GetSelected();
            if (entry != null)
                move.ApplyPlayableCharacter(entry);
            else
            {
                HeroType hero = HeroRunStats.Instance != null
                    ? HeroRunStats.Instance.SelectedHero
                    : HeroType.Warrior;
                move.ApplyHeroVisual(hero);
            }
        }
        else
        {
            SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
            float scale = HeroVisualLibrary.ResolveDisplayScale(sr != null ? sr.sprite : null, playerVisualScale);
            player.transform.localScale = Vector3.one * scale;
        }

        AutoAttack attack = player.GetComponent<AutoAttack>();
        if (attack != null)
            attack.SetProjectileVisualScale(projectileVisualScale);
    }

    [Tooltip("Chiều cao quái thường trên map (world units). Đồng bộ GameScale.EnemyHeight.")]
    [SerializeField] private float enemyTargetHeight = GameScale.EnemyHeight;

    public float EnemyTargetHeight => enemyTargetHeight;

    public void ApplyEnemyScale(Transform enemyTransform)
    {
        if (enemyTransform == null)
            return;

        // Quái có animator (Kenney hoặc HeroKnight): scale do EnemyArchetypeUtility đặt.
        if (enemyTransform.GetComponent<EnemySpriteAnimator>() != null
            || enemyTransform.GetComponent<SimpleSpriteAnimator>() != null)
            return;

        // Chuẩn hoá theo chiều cao thế giới dựa trên sprite thật — nhất quán mọi nguồn.
        SpriteRenderer sr = enemyTransform.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = enemyTransform.GetComponentInChildren<SpriteRenderer>();

        if (sr != null && sr.sprite != null)
        {
            float scale = SpriteScaleUtil.ScaleForHeight(sr.sprite, enemyTargetHeight);
            enemyTransform.localScale = new Vector3(scale, scale, 1f);
        }
        else
        {
            enemyTransform.localScale = Vector3.one * enemyVisualScale;
        }
    }

    private void ApplyScaleToExistingEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        for (int i = 0; i < enemies.Length; i++)
            ApplyEnemyScale(enemies[i].transform);
    }
}
