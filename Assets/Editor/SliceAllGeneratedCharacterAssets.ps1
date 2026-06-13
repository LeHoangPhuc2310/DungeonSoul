# Slice all AI-generated per-skill VFX, auras, held weapons
Add-Type -AssemblyName System.Drawing
$src = "C:\Users\ASUS\.cursor\projects\d-UNITY-DungeonSoul\assets"
$vfxRoot = Join-Path $PSScriptRoot "..\Resources\GeneratedSkillVfx"
$heldRoot = Join-Path $PSScriptRoot "..\Resources\GeneratedHeldWeapons"

function Save-Crop($bmp, $rect, $path) {
    $piece = $bmp.Clone([System.Drawing.Rectangle]::new($rect.X,$rect.Y,$rect.Width,$rect.Height), $bmp.PixelFormat)
    $dir = Split-Path $path -Parent
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    $piece.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $piece.Dispose()
}

function Slice-Rows($file, $names, $outTpl, $cols, $framesPerRow) {
    $bmp = [System.Drawing.Bitmap]::FromFile((Join-Path $src $file))
    $rows = $names.Count
    $cellW = [int]($bmp.Width / $cols)
    $cellH = [int]($bmp.Height / $rows)
    for ($r = 0; $r -lt $rows; $r++) {
        for ($c = 0; $c -lt $framesPerRow; $c++) {
            $pad = 6
            $iw = $cellW - $pad*2; $ih = $cellH - $pad*2
            $x = $c * $cellW + [int](($cellW-$iw)/2)
            $y = $r * $cellH + [int](($cellH-$ih)/2)
            $out = $outTpl -f $names[$r], $c
            Save-Crop $bmp ([System.Drawing.Rectangle]::new($x,$y,$iw,$ih)) $out
        }
    }
    $bmp.Dispose()
}

$vfxBatches = @(
    @("gen-per-skill-vfx-01.png", @("DoubleShot","SpeedBoost","IronBody","QuickReload")),
    @("gen-per-skill-vfx-02.png", @("CoinMagnet","ToughSkin","FireArrow","SteadyAim")),
    @("gen-per-skill-vfx-03.png", @("PiercingArrow","MultiTarget","CriticalHit","LifeSteal")),
    @("gen-per-skill-vfx-04.png", @("Boomerang","LightningChain","PoisonCloud","ExplosiveRounds")),
    @("gen-per-skill-vfx-05.png", @("Explosion","IceAura","GhostForm","QuadShot")),
    @("gen-per-skill-vfx-06.png", @("BladeStorm","Vampire","TwinArrows","DeathMark")),
    @("gen-per-skill-vfx-07.png", @("TimeFreeze","DragonStrike","SoulHarvest","MirrorImage"))
)
foreach ($b in $vfxBatches) {
    Slice-Rows $b[0] $b[1] (Join-Path $vfxRoot "PerSkill\{0}\frame_{1:D2}.png") 6 6
    Write-Host "VFX $($b[0])"
}

$auraBatches = @(
    @("gen-skill-aura-01.png", @("DoubleShot","SpeedBoost","IronBody","QuickReload")),
    @("gen-skill-aura-02.png", @("CoinMagnet","ToughSkin","FireArrow","SteadyAim")),
    @("gen-skill-aura-03.png", @("PiercingArrow","MultiTarget","CriticalHit","LifeSteal")),
    @("gen-skill-aura-04.png", @("Boomerang","LightningChain","PoisonCloud","ExplosiveRounds")),
    @("gen-skill-aura-05.png", @("Explosion","IceAura","GhostForm","QuadShot")),
    @("gen-skill-aura-06.png", @("BladeStorm","Vampire","TwinArrows","DeathMark")),
    @("gen-skill-aura-07.png", @("TimeFreeze","DragonStrike","SoulHarvest","MirrorImage"))
)
foreach ($b in $auraBatches) {
    Slice-Rows $b[0] $b[1] (Join-Path $vfxRoot "Auras\Skills\{0}\frame_{1:D2}.png") 4 4
    Write-Host "Aura $($b[0])"
}

$passives = @("clover","tim_rong","vuong_mien_exp","manh_hon_rong","canh_quat","dong_ho_cat","mong_vuot","hon_quy","ao_giap_da","tui_tham_lam","luoi_lua","vuong_mien_vinh_cuu","luoi_sac","clover","tim_rong","vuong_mien_exp")
Slice-Rows "gen-passive-aura.png" $passives (Join-Path $vfxRoot "Auras\Passives\{0}\frame_{1:D2}.png") 4 4

$weapons = @("IronBow","FireStaff","FrostWand","PoisonDagger","HolyCross","ThunderRod","StormBow","DragonStaff","BlizzardWand","DeathDagger","HolyNova","ZeusRod")
$wbmp = [System.Drawing.Bitmap]::FromFile((Join-Path $src "gen-held-weapons.png"))
$wCols = 4; $wRows = 3
$wCellW = [int]($wbmp.Width/$wCols); $wCellH = [int]($wbmp.Height/$wRows)
for ($i = 0; $i -lt $weapons.Count; $i++) {
    $c = $i % $wCols; $r = [int]($i / $wCols)
    $pad = 8; $iw = $wCellW-$pad*2; $ih = $wCellH-$pad*2
    $x = $c*$wCellW+[int](($wCellW-$iw)/2); $y = $r*$wCellH+[int](($wCellH-$ih)/2)
    Save-Crop $wbmp ([System.Drawing.Rectangle]::new($x,$y,$iw,$ih)) (Join-Path $heldRoot "$($weapons[$i]).png")
}
$wbmp.Dispose()
Write-Host "Done. PNG count:" (Get-ChildItem $vfxRoot,$heldRoot -Recurse -Filter *.png).Count
