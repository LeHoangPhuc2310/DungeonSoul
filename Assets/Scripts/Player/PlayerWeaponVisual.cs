// DungeonSoul — PlayerWeaponVisual.cs — Vũ khí tile trên tay theo WeaponManager.

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AutoAttack))]
public class PlayerWeaponVisual : MonoBehaviour
{
    [SerializeField] private float aimSmooth = 12f;
    [SerializeField] private float recoilDuration = 0.08f;
    [SerializeField] private float recoilScale = 1.25f;
    [Tooltip("Weapon length as fraction of player world height (e.g. 0.7 ≈ a large, clearly visible weapon).")]
    [SerializeField] private float weaponHeightRatio = 0.7f;
    [SerializeField] private Vector2 handOffsetWorld = new Vector2(0.5f, 0.08f);

    private Transform weaponPivot;
    private SpriteRenderer weaponSprite;
    private SpriteRenderer muzzleFlash;
    private AutoAttack autoAttack;
    private SpriteRenderer bodySprite;
    private float aimAngle;
    private Coroutine recoilRoutine;

    /// <summary>Vị trí thế giới của đầu mũi vũ khí — điểm xuất phát đạn.</summary>
    public Vector3 MuzzleWorldPosition
    {
        get
        {
            if (weaponPivot == null)
                return transform.position;
            // Mũi vũ khí = pivot tay + hướng nhắm * độ dài vũ khí.
            Vector2 aimDir = autoAttack != null ? autoAttack.LastAimDirection : Vector2.right;
            if (aimDir.sqrMagnitude < 0.01f)
                aimDir = Vector2.right;
            float reach = 0.45f * Mathf.Max(0.01f, transform.lossyScale.x);
            return weaponPivot.position + (Vector3)(aimDir.normalized * reach);
        }
    }

    private void Awake()
    {
        autoAttack = GetComponent<AutoAttack>();
        bodySprite = GetComponent<SpriteRenderer>();
        if (bodySprite == null)
            bodySprite = GetComponentInChildren<SpriteRenderer>();
        BuildWeaponHierarchy();
        autoAttack.OnProjectileFired += HandleFired;
    }

    private void OnDestroy()
    {
        if (autoAttack != null)
            autoAttack.OnProjectileFired -= HandleFired;
    }

    private void Start()
    {
        RefreshFromLoadout();
    }

    private void LateUpdate()
    {
        if (weaponPivot == null)
            return;

        bool facingLeft = bodySprite != null && bodySprite.flipX;
        float side = facingLeft ? -1f : 1f;
        float invParent = 1f / Mathf.Max(0.01f, transform.lossyScale.x);
        weaponPivot.localPosition = new Vector3(
            handOffsetWorld.x * side * invParent,
            handOffsetWorld.y * invParent,
            0f);

        Vector2 aimDir = autoAttack != null ? autoAttack.LastAimDirection : Vector2.right * side;
        if (aimDir.sqrMagnitude < 0.01f)
            aimDir = Vector2.right * side;

        float targetAngle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        aimAngle = Mathf.LerpAngle(aimAngle, targetAngle, aimSmooth * Time.deltaTime);
        weaponPivot.localRotation = Quaternion.Euler(0f, 0f, aimAngle);

        if (weaponSprite != null)
            weaponSprite.flipY = aimDir.x < 0f && Mathf.Abs(aimDir.y) < 0.5f;

        ApplyWeaponLocalScale();
    }

    public void RefreshWeaponSprite(HeroType hero)
    {
        RefreshWeaponSprite(ArtSpriteLibrary.GetWeaponSprite(hero), Color.white);
    }

    public void RefreshWeaponSprite(WeaponType type)
    {
        RefreshWeaponSprite(ArtSpriteLibrary.GetWeaponSprite(type), ArtSpriteLibrary.GetWeaponTint(type));
    }

    public void RefreshFromLoadout()
    {
        if (WeaponManager.Instance != null && WeaponManager.Instance.ActiveWeapons.Count > 0)
        {
            RefreshWeaponSprite(WeaponManager.Instance.ActiveWeapons[0].weaponType);
            return;
        }

        HeroType hero = HeroRunStats.Instance != null
            ? HeroRunStats.Instance.SelectedHero
            : HeroType.Warrior;
        RefreshWeaponSprite(hero);
    }

    private void RefreshWeaponSprite(Sprite sprite, Color tint)
    {
        if (weaponSprite == null)
            return;

        weaponSprite.sprite = sprite;
        weaponSprite.color = tint;
        weaponSprite.sortingOrder = 26;
        ApplyWeaponLocalScale();
    }

    private void ApplyWeaponLocalScale()
    {
        if (weaponSprite == null || weaponSprite.sprite == null)
            return;

        // Vũ khí = ~45% chiều cao player (world units). Tính độc lập, bù cho parentScale
        // (vì vũ khí là con của player đã bị phóng to).
        float parentScale = Mathf.Max(0.01f, transform.lossyScale.x);
        float targetWeaponWorldHeight = GameScale.PlayerHeight * 0.45f;

        float spriteHeight = Mathf.Max(0.02f, weaponSprite.sprite.bounds.size.y);
        // localScale cần để sprite cao đúng targetWeaponWorldHeight TRÊN MÀN (đã nhân parentScale).
        float local = targetWeaponWorldHeight / (spriteHeight * parentScale);
        weaponSprite.transform.localScale = Vector3.one * Mathf.Clamp(local, 0.05f, 8f);
    }

    private void HandleFired(Vector2 direction)
    {
        if (recoilRoutine != null)
            StopCoroutine(recoilRoutine);
        recoilRoutine = StartCoroutine(RecoilPulse());
        ShowMuzzleFlash(direction);
    }

    private IEnumerator RecoilPulse()
    {
        if (weaponPivot == null)
            yield break;

        Vector3 baseScale = Vector3.one;
        weaponPivot.localScale = baseScale * recoilScale;
        float t = 0f;
        while (t < recoilDuration)
        {
            t += Time.deltaTime;
            float k = 1f - t / recoilDuration;
            weaponPivot.localScale = Vector3.Lerp(baseScale, baseScale * recoilScale, k);
            yield return null;
        }

        weaponPivot.localScale = baseScale;
    }

    private void ShowMuzzleFlash(Vector2 direction)
    {
        if (muzzleFlash == null || weaponPivot == null)
            return;

        muzzleFlash.transform.localPosition = new Vector3(0.55f, 0f, 0f);
        muzzleFlash.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        muzzleFlash.gameObject.SetActive(true);
        muzzleFlash.color = new Color(1f, 0.9f, 0.5f, 0.95f);
        CancelInvoke(nameof(HideMuzzle));
        Invoke(nameof(HideMuzzle), 0.06f);
    }

    private void HideMuzzle()
    {
        if (muzzleFlash != null)
            muzzleFlash.gameObject.SetActive(false);
    }

    private void BuildWeaponHierarchy()
    {
        GameObject pivotGo = new GameObject("WeaponPivot");
        pivotGo.transform.SetParent(transform, false);
        weaponPivot = pivotGo.transform;

        GameObject weaponGo = new GameObject("Weapon");
        weaponGo.transform.SetParent(weaponPivot, false);
        weaponSprite = weaponGo.AddComponent<SpriteRenderer>();
        weaponSprite.sortingOrder = 26;
        weaponSprite.transform.localPosition = Vector3.zero;

        GameObject flashGo = new GameObject("MuzzleFlash");
        flashGo.transform.SetParent(weaponPivot, false);
        muzzleFlash = flashGo.AddComponent<SpriteRenderer>();
        muzzleFlash.sprite = CreateFlashSprite();
        muzzleFlash.sortingOrder = 27;
        muzzleFlash.transform.localScale = Vector3.one * 0.12f;
        flashGo.SetActive(false);
    }

    private static Sprite CreateFlashSprite()
    {
        const int s = 8;
        Texture2D tex = new Texture2D(s, s);
        Color c = Color.white;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
                tex.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), new Vector2(3.5f, 3.5f)) < 3.5f ? c : Color.clear);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16f);
    }
}
