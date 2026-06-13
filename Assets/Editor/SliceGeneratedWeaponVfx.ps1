# Slice weapon animation sheets -> Assets/Resources/GeneratedWeaponVfx/
Add-Type -AssemblyName System.Drawing

$src = "C:\Users\ASUS\.cursor\projects\d-UNITY-DungeonSoul\assets"
$dst = Join-Path $PSScriptRoot "..\Resources\GeneratedWeaponVfx"

function Save-Crop($bitmap, $rect, $outPath) {
    $crop = New-Object System.Drawing.Rectangle($rect.X, $rect.Y, $rect.Width, $rect.Height)
    $piece = $bitmap.Clone($crop, $bitmap.PixelFormat)
    $dir = Split-Path $outPath -Parent
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    $piece.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $piece.Dispose()
}

function Slice-WeaponSheet($sheetPath, $weaponRows) {
    $bmp = [System.Drawing.Bitmap]::FromFile($sheetPath)
    $cols = 6
    $rows = $weaponRows.Count
    $cellW = [int]($bmp.Width / $cols)
    $cellH = [int]($bmp.Height / $rows)
    for ($r = 0; $r -lt $rows; $r++) {
        $weapon = $weaponRows[$r]
        $outDir = Join-Path $dst "Projectiles\$weapon"
        for ($c = 0; $c -lt $cols; $c++) {
            $x = $c * $cellW
            $y = $r * $cellH
            $pad = [Math]::Min(10, [Math]::Min($cellW, $cellH) / 18)
            $innerW = $cellW - ($pad * 2)
            $innerH = $cellH - ($pad * 2)
            $cx = $x + [int](($cellW - $innerW) / 2)
            $cy = $y + [int](($cellH - $innerH) / 2)
            $name = "frame_{0:D2}" -f $c
            Save-Crop $bmp ([System.Drawing.Rectangle]::new($cx, $cy, $innerW, $innerH)) (Join-Path $outDir "$name.png")
        }
        Write-Host "  $weapon (6 frames)"
    }
    $bmp.Dispose()
}

Write-Host "Projectile sheets..."
Slice-WeaponSheet (Join-Path $src "weapon-proj-sheet-a.png") @("IronBow","FireStaff","FrostWand")
Slice-WeaponSheet (Join-Path $src "weapon-proj-sheet-b.png") @("PoisonDagger","HolyCross","ThunderRod")
Slice-WeaponSheet (Join-Path $src "weapon-proj-sheet-c.png") @("StormBow","DragonStaff","BlizzardWand")
Slice-WeaponSheet (Join-Path $src "weapon-proj-sheet-d.png") @("DeathDagger","HolyNova","ZeusRod")

Write-Host "Orbit blade..."
$orbit = [System.Drawing.Bitmap]::FromFile((Join-Path $src "weapon-orbit-blade.png"))
$orbitCols = 8
$orbitCellW = [int]($orbit.Width / $orbitCols)
$orbitOut = Join-Path $dst "Orbit"
for ($c = 0; $c -lt $orbitCols; $c++) {
    $pad = 8
    $inner = $orbitCellW - ($pad * 2)
    $cx = $c * $orbitCellW + [int](($orbitCellW - $inner) / 2)
    $cy = [int](($orbit.Height - $inner) / 2)
    Save-Crop $orbit ([System.Drawing.Rectangle]::new($cx, $cy, $inner, $inner)) (Join-Path $orbitOut ("frame_{0:D2}" -f $c) + ".png")
}
$orbit.Dispose()

Write-Host "Muzzle + Hit..."
$mh = [System.Drawing.Bitmap]::FromFile((Join-Path $src "weapon-muzzle-hit.png"))
$mhCols = 6
$mhRows = 2
$mhCellW = [int]($mh.Width / $mhCols)
$mhCellH = [int]($mh.Height / $mhRows)
for ($c = 0; $c -lt $mhCols; $c++) {
    $pad = 6
    $innerW = $mhCellW - ($pad * 2)
    $innerH = $mhCellH - ($pad * 2)
    $cx = $c * $mhCellW + [int](($mhCellW - $innerW) / 2)
    for ($r = 0; $r -lt $mhRows; $r++) {
        $folder = if ($r -eq 0) { "Muzzle" } else { "Hit" }
        $cy = $r * $mhCellH + [int](($mhCellH - $innerH) / 2)
        $fileName = "frame_{0:D2}.png" -f $c
        Save-Crop $mh ([System.Drawing.Rectangle]::new($cx, $cy, $innerW, $innerH)) (Join-Path $dst $folder $fileName)
    }
}
$mh.Dispose()

Write-Host "Done -> $dst"
