// DungeonSoul — Inject VFX vũ khí procedural vào WeaponVfxLibrary khi game khởi động.
// Chạy trước Resources.LoadAll; PNG thật (nếu có) sẽ tự override sau.

using UnityEngine;

public static class ProceduralWeaponVfxInjector
{
    private static bool injected;

    public static void InjectAll()
    {
        if (injected) return;
        injected = true;

        // ── Projectile — 12 loại vũ khí ─────────────────────────
        WeaponVfxLibrary.InjectProjectile(WeaponType.IronBow,      ProceduralWeaponVfxPainter.BuildArrow());
        WeaponVfxLibrary.InjectProjectile(WeaponType.StormBow,     ProceduralWeaponVfxPainter.BuildArrowStorm());
        WeaponVfxLibrary.InjectProjectile(WeaponType.FireStaff,    ProceduralWeaponVfxPainter.BuildFireball());
        WeaponVfxLibrary.InjectProjectile(WeaponType.DragonStaff,  ProceduralWeaponVfxPainter.BuildDragonOrb());
        WeaponVfxLibrary.InjectProjectile(WeaponType.FrostWand,    ProceduralWeaponVfxPainter.BuildIceShard());
        WeaponVfxLibrary.InjectProjectile(WeaponType.BlizzardWand, ProceduralWeaponVfxPainter.BuildBlizzardOrb());
        WeaponVfxLibrary.InjectProjectile(WeaponType.PoisonDagger, ProceduralWeaponVfxPainter.BuildPoisonBlade());
        WeaponVfxLibrary.InjectProjectile(WeaponType.DeathDagger,  ProceduralWeaponVfxPainter.BuildDeathBlade());
        WeaponVfxLibrary.InjectProjectile(WeaponType.HolyCross,    ProceduralWeaponVfxPainter.BuildHolyCross());
        WeaponVfxLibrary.InjectProjectile(WeaponType.HolyNova,     ProceduralWeaponVfxPainter.BuildHolyNova());
        WeaponVfxLibrary.InjectProjectile(WeaponType.ThunderRod,   ProceduralWeaponVfxPainter.BuildThunderBolt());
        WeaponVfxLibrary.InjectProjectile(WeaponType.ZeusRod,      ProceduralWeaponVfxPainter.BuildZeusBolt());

        // ── Muzzle flash — 6 element ─────────────────────────────
        WeaponVfxLibrary.InjectMuzzle("arrow",     ProceduralWeaponVfxPainter.BuildMuzzleArrow());
        WeaponVfxLibrary.InjectMuzzle("fire",      ProceduralWeaponVfxPainter.BuildMuzzleFire());
        WeaponVfxLibrary.InjectMuzzle("ice",       ProceduralWeaponVfxPainter.BuildMuzzleIce());
        WeaponVfxLibrary.InjectMuzzle("poison",    ProceduralWeaponVfxPainter.BuildMuzzlePoison());
        WeaponVfxLibrary.InjectMuzzle("holy",      ProceduralWeaponVfxPainter.BuildMuzzleHoly());
        WeaponVfxLibrary.InjectMuzzle("lightning", ProceduralWeaponVfxPainter.BuildMuzzleLightning());

        // ── Hit burst — 6 element ────────────────────────────────
        WeaponVfxLibrary.InjectHit("arrow",     ProceduralWeaponVfxPainter.BuildHitArrow());
        WeaponVfxLibrary.InjectHit("fire",      ProceduralWeaponVfxPainter.BuildHitFire());
        WeaponVfxLibrary.InjectHit("ice",       ProceduralWeaponVfxPainter.BuildHitIce());
        WeaponVfxLibrary.InjectHit("poison",    ProceduralWeaponVfxPainter.BuildHitPoison());
        WeaponVfxLibrary.InjectHit("holy",      ProceduralWeaponVfxPainter.BuildHitHoly());
        WeaponVfxLibrary.InjectHit("lightning", ProceduralWeaponVfxPainter.BuildHitLightning());
    }
}
