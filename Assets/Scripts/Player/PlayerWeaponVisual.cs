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

    private Coroutine swingRoutine;
    private bool overlayEnabled = true;

    public bool IsOverlayVisible => overlayEnabled && weaponSprite != null && weaponSprite.enabled;

    public void SetOverlayEnabled(bool enabled)
    {
        overlayEnabled = enabled;
        if (weaponPivot != null)
            weaponPivot.gameObject.SetActive(enabled);
        if (weaponSprite != null)
            weaponSprite.enabled = enabled;
    }

    private void Awake()
    {
        autoAttack = GetComponent<AutoAttack>();
        ResolveBodySprite();
        BuildWeaponHierarchy();
        autoAttack.OnProjectileFired += HandleFired;
        autoAttack.OnMeleeSwing += HandleMeleeSwing;
    }

    private void ResolveBodySprite()
    {
        Transform heroBody = transform.Find("HeroBody");
        if (heroBody != null)
        {
            SpriteRenderer heroSr = heroBody.GetComponent<SpriteRenderer>();
            if (heroSr != null)
            {
                bodySprite = heroSr;
                return;
            }
        }

        bodySprite = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnDestroy()
    {
        if (autoAttack != null)
        {
            autoAttack.OnProjectileFired -= HandleFired;
            autoAttack.OnMeleeSwing -= HandleMeleeSwing;
        }
    }

    private void HandleMeleeSwing(Vector2 direction)
    {
        if (weaponPivot == null)
            return;
        if (swingRoutine != null)
            StopCoroutine(swingRoutine);
        swingRoutine = StartCoroutine(SwingArc());
    }

    private System.Collections.IEnumerator SwingArc()
    {
        // Vung vũ khí: xoay nhanh một cung quanh hướng nhắm rồi trả về.
        float dur = 0.18f;
        float t = 0f;
        float baseAngle = aimAngle;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            // Cung từ -55° tới +55° rồi về.
            float arc = Mathf.Sin(k * Mathf.PI) * 55f;
            weaponPivot.localRotation = Quaternion.Euler(0f, 0f, baseAngle - 55f + arc + 55f);
            yield return null;
        }
        swingRoutine = null;
    }

    private void Start()
    {
        RefreshFromLoadout();
    }

    private void LateUpdate()
    {
        if (!overlayEnabled || weaponPivot == null)
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
        // Khi đang vung cận chiến, để SwingArc điều khiển góc — không ghi đè.
        if (swingRoutine == null)
            weaponPivot.localRotation = Quaternion.Euler(0f, 0f, aimAngle);

        if (weaponSprite != null)
            weaponSprite.flipY = aimDir.x < 0f && Mathf.Abs(aimDir.y) < 0.5f;

        ApplyWeaponLocalScale();
    }

    public void RefreshWeaponSprite(HeroType hero)
    {
        // Cầm tay: dùng sprite không nền; UI vẫn dùng Icons_background qua GetWeapon().
        Sprite s = WeaponIconLibrary.GetHeroWeaponHeld(hero);
        if (s != null)
            RefreshWeaponSprite(s, Color.white);
        else
            RefreshWeaponSprite(ArtSpriteLibrary.GetWeaponSprite(hero), Color.white);
    }

    public void RefreshWeaponSprite(WeaponType type)
    {
        Sprite s = WeaponIconLibrary.GetWeaponHeld(type);
        if (s != null)
            RefreshWeaponSprite(s, WeaponIconLibrary.Tint(type));
        else
            RefreshWeaponSprite(ArtSpriteLibrary.GetWeaponSprite(type), ArtSpriteLibrary.GetWeaponTint(type));
    }

    public void RefreshFromLoadout()
    {
        PlayableCharacterEntry entry = PlayableCharacterCatalog.GetSelected();
        if (entry != null && entry.HasAttackAnimation)
        {
            SetOverlayEnabled(false);
            return;
        }

        SetOverlayEnabled(true);

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

        if (sprite == null)
        {
            Debug.LogWarning("[PlayerWeaponVisual] Không load được sprite vũ khí — kiểm tra Icons_no_background.");
            return;
        }

        weaponSprite.sprite = sprite;
        weaponSprite.color = tint;
        weaponSprite.sortingOrder = 27;
        ApplyGripPivotOffset(sprite);
        ApplyWeaponLocalScale();
    }

    /// <summary>Bù pivot góc dưới-trái của icon pack → tay cầm nằm đúng trên WeaponPivot.</summary>
    private void ApplyGripPivotOffset(Sprite sprite)
    {
        if (weaponSprite == null || sprite == null)
            return;

        Vector2 normPivot = new Vector2(
            sprite.pivot.x / Mathf.Max(1f, sprite.rect.width),
            sprite.pivot.y / Mathf.Max(1f, sprite.rect.height));
        Vector2 grip = new Vector2(0.12f, 0.38f);
        Vector2 delta = grip - normPivot;
        weaponSprite.transform.localPosition = new Vector3(
            delta.x * sprite.bounds.size.x,
            delta.y * sprite.bounds.size.y,
            0f);
    }

    private void ApplyWeaponLocalScale()
    {
        if (weaponSprite == null || weaponSprite.sprite == null)
            return;

        // Vũ khí = ~45% chiều cao player (world units). Tính độc lập, bù cho parentScale
        // (vì vũ khí là con của player đã bị phóng to).
        float parentScale = Mathf.Max(0.01f, transform.lossyScale.x);
        float targetWeaponWorldHeight = GameScale.PlayerHeight * 0.55f;

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
        Transform attach = transform;
        Transform heroBody = transform.Find("HeroBody");
        if (heroBody != null)
            attach = heroBody;

        GameObject pivotGo = new GameObject("WeaponPivot");
        pivotGo.transform.SetParent(attach, false);
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
