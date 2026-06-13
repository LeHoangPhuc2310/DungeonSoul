# Run from repo root to slice AI-generated icon sheets into Resources/GeneratedIcons/
# Usage: powershell -ExecutionPolicy Bypass -File Assets/Editor/SliceGeneratedPixelIcons.ps1

Add-Type -AssemblyName System.Drawing

$src = "C:\Users\ASUS\.cursor\projects\d-UNITY-DungeonSoul\assets"
$dst = Join-Path $PSScriptRoot "..\Resources\GeneratedIcons"

function Save-Crop($bitmap, $rect, $outPath) {
    $crop = New-Object System.Drawing.Rectangle($rect.X, $rect.Y, $rect.Width, $rect.Height)
    $piece = $bitmap.Clone($crop, $bitmap.PixelFormat)
    $dir = Split-Path $outPath -Parent
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    $piece.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $piece.Dispose()
}

function Slice-Grid($sheetPath, $cols, $rows, $names, $outDir) {
    $bmp = [System.Drawing.Bitmap]::FromFile($sheetPath)
    $cellW = [int]($bmp.Width / $cols)
    $cellH = [int]($bmp.Height / $rows)
    $i = 0
    for ($r = 0; $r -lt $rows; $r++) {
        for ($c = 0; $c -lt $cols; $c++) {
            if ($i -ge $names.Count) { break }
            $x = $c * $cellW
            $y = $r * $cellH
            $pad = [Math]::Min(12, [Math]::Min($cellW, $cellH) / 16)
            $inner = [Math]::Min($cellW, $cellH) - ($pad * 2)
            $cx = $x + [int](($cellW - $inner) / 2)
            $cy = $y + [int](($cellH - $inner) / 2)
            $out = Join-Path $outDir ($names[$i] + ".png")
            Save-Crop $bmp ([System.Drawing.Rectangle]::new($cx, $cy, $inner, $inner)) $out
            Write-Host "  $($names[$i]).png"
            $i++
        }
    }
    $bmp.Dispose()
}

$passives = @(
    "clover","tim_rong","vuong_mien_exp","manh_hon_rong",
    "canh_quat","dong_ho_cat","mong_vuot","hon_quy",
    "ao_giap_da","tui_tham_lam","luoi_lua","vuong_mien_vinh_cuu"
)

$skillsCommon = @(
    "DoubleShot","SpeedBoost","IronBody","QuickReload",
    "CoinMagnet","ToughSkin","FireArrow","SteadyAim"
)

$skillsRare = @(
    "PiercingArrow","MultiTarget","CriticalHit","LifeSteal",
    "Boomerang","LightningChain","PoisonCloud","ExplosiveRounds"
)

$skillsEpic = @(
    "Explosion","IceAura","GhostForm","QuadShot",
    "BladeStorm","Vampire","TwinArrows"
)

$skillsLegendary = @(
    "DeathMark","TimeFreeze","DragonStrike","SoulHarvest","MirrorImage"
)

$weapons = @(
    "IronBow","FireStaff","FrostWand","PoisonDagger","HolyCross","ThunderRod"
)

Write-Host "Slicing passives..."
Slice-Grid (Join-Path $src "passive-icons-sheet.png") 4 3 $passives (Join-Path $dst "Passives")

Write-Host "Slicing luoi_sac..."
$bmp = [System.Drawing.Bitmap]::FromFile((Join-Path $src "passive-luoi_sac.png"))
$size = [Math]::Min($bmp.Width, $bmp.Height)
$cx = [int](($bmp.Width - $size) / 2)
$cy = [int](($bmp.Height - $size) / 2)
Save-Crop $bmp ([System.Drawing.Rectangle]::new($cx, $cy, $size, $size)) (Join-Path $dst "Passives\luoi_sac.png")
$bmp.Dispose()

Write-Host "Slicing skills..."
Slice-Grid (Join-Path $src "skill-icons-common.png") 4 2 $skillsCommon (Join-Path $dst "Skills")
Slice-Grid (Join-Path $src "skill-icons-rare.png") 4 2 $skillsRare (Join-Path $dst "Skills")
Slice-Grid (Join-Path $src "skill-icons-epic.png") 4 2 $skillsEpic (Join-Path $dst "Skills")

$legBmp = [System.Drawing.Bitmap]::FromFile((Join-Path $src "skill-icons-legendary.png"))
$legCols = 5
$legCellW = [int]($legBmp.Width / $legCols)
$legCellH = $legBmp.Height
for ($i = 0; $i -lt $skillsLegendary.Count; $i++) {
    $x = $i * $legCellW
    $inner = [Math]::Min($legCellW, $legCellH) - 24
    $cx = $x + [int](($legCellW - $inner) / 2)
    $cy = [int](($legCellH - $inner) / 2)
    Save-Crop $legBmp ([System.Drawing.Rectangle]::new($cx, $cy, $inner, $inner)) (Join-Path $dst "Skills\$($skillsLegendary[$i]).png")
    Write-Host "  $($skillsLegendary[$i]).png"
}
$legBmp.Dispose()

Write-Host "Slicing weapons..."
Slice-Grid (Join-Path $src "weapon-icons-sheet.png") 3 2 $weapons (Join-Path $dst "Weapons")

Write-Host "Done -> $dst"
