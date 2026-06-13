// DungeonSoul — Vũ khí quay quanh người chơi (kiểu VS whip / blade ring).

using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(WeaponManager))]
public class PlayerOrbitalWeapons : MonoBehaviour
{
    private class OrbitBlade
    {
        public GameObject root;
        public SpriteRenderer sr;
        public LoopingSpriteAnimator anim;
        public WeaponType weapon;
        public float angle;
        public float radius;
        public float damageTimer;
    }

    [SerializeField] private float baseRadius = 1.1f;
    [SerializeField] private float orbitSpeed = 120f;
    [SerializeField] private float damageInterval = 0.45f;
    [SerializeField] private float bladeDamageScale = 0.35f;

    private readonly List<OrbitBlade> blades = new List<OrbitBlade>(6);
    private WeaponManager weaponManager;
    private readonly Collider2D[] hitBuffer = new Collider2D[24];
    private Sprite[] orbitFrames;

    private void Awake()
    {
        weaponManager = GetComponent<WeaponManager>();
    }

    private void LateUpdate()
    {
        if (weaponManager == null || !weaponManager.enabled)
            return;

        SyncBlades();
        float dt = Time.deltaTime;
        for (int i = 0; i < blades.Count; i++)
        {
            OrbitBlade blade = blades[i];
            if (blade.root == null)
                continue;

            blade.angle += orbitSpeed * dt;
            float rad = blade.angle * Mathf.Deg2Rad;
            blade.root.transform.position = transform.position + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * blade.radius;
            blade.root.transform.rotation = Quaternion.Euler(0f, 0f, blade.angle + 90f);

            blade.damageTimer -= dt;
            if (blade.damageTimer <= 0f)
            {
                blade.damageTimer = damageInterval;
                TryDamageAt(blade);
            }
        }
    }

    private void SyncBlades()
    {
        IReadOnlyList<WeaponManager.WeaponSlot> weapons = weaponManager.ActiveWeapons;
        int want = Mathf.Min(weapons.Count, 6);
        while (blades.Count < want)
            blades.Add(CreateBlade(blades.Count));

        for (int i = 0; i < blades.Count; i++)
        {
            bool active = i < want;
            if (blades[i].root != null)
                blades[i].root.SetActive(active);

            if (!active)
                continue;

            WeaponManager.WeaponSlot slot = weapons[i];
            blades[i].weapon = slot.weaponType;
            blades[i].radius = baseRadius + i * 0.12f;
            if (blades[i].sr != null)
                blades[i].sr.color = WeaponVfxLibrary.GetTint(slot.weaponType);
        }
    }

    private OrbitBlade CreateBlade(int index)
    {
        orbitFrames ??= WeaponVfxLibrary.GetOrbitFrames();

        GameObject go = RuntimeSpawnGuard.Mark(new GameObject("OrbitBlade_" + index));
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 21;
        sr.color = Color.white;

        LoopingSpriteAnimator anim = null;
        if (orbitFrames != null && orbitFrames.Length > 0)
        {
            anim = go.AddComponent<LoopingSpriteAnimator>();
            anim.Begin(orbitFrames, 14f, 0.38f);
        }

        return new OrbitBlade
        {
            root = go,
            sr = sr,
            anim = anim,
            angle = index * (360f / 6f),
            radius = baseRadius + index * 0.12f,
            damageTimer = index * 0.08f
        };
    }

    private void TryDamageAt(OrbitBlade blade)
    {
        if (blade.root == null)
            return;

        int count = Physics2D.OverlapCircleNonAlloc(blade.root.transform.position, 0.45f, hitBuffer);
        float damage = GetBladeDamage(blade.weapon);
        for (int i = 0; i < count; i++)
        {
            Collider2D c = hitBuffer[i];
            if (c == null || !c.CompareTag("Enemy"))
                continue;

            HealthSystem hp = c.GetComponent<HealthSystem>();
            if (hp == null)
                continue;

            hp.TakeDamage(damage);
            WeaponVfxLibrary.PlayHit(blade.weapon, c.transform.position, false);
            SkillBehaviors behaviors = GetComponent<SkillBehaviors>();
            behaviors?.OnPlayerDealtDamage(damage, hp);
            if (HUDManager.Instance != null)
                HUDManager.Instance.RegisterDamageDealt(damage);
        }
    }

    private float GetBladeDamage(WeaponType type)
    {
        WeaponManager.WeaponSlot slot = null;
        var list = weaponManager.ActiveWeapons;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].weaponType == type)
            {
                slot = list[i];
                break;
            }
        }

        float baseDmg = slot != null ? slot.baseDamage : 8f;
        return baseDmg * bladeDamageScale * Mathf.Max(0.2f, weaponManager.DamageMultiplier);
    }

    private void OnDisable()
    {
        for (int i = 0; i < blades.Count; i++)
        {
            if (blades[i].root != null)
                blades[i].root.SetActive(false);
        }
    }
}
