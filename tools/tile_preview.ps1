param(
    [Parameter(Mandatory=$true)][string]$InFile,
    [Parameter(Mandatory=$true)][string]$OutFile,
    [int]$Scale = 4
)

Add-Type -AssemblyName System.Drawing

# Seam check: roll the tile by half its size so the outer edges meet in the
# middle, then lay a 2x2 grid of the original next to a 2x2 grid of the roll.
# A visible line at the roll's center cross (or on the plain grid's internal
# boundaries) means the tile is not seamless.

$src = New-Object System.Drawing.Bitmap((Resolve-Path $InFile).Path)
$w = $src.Width; $h = $src.Height

function Roll([System.Drawing.Bitmap]$bmp, [int]$dx, [int]$dy) {
    $out = New-Object System.Drawing.Bitmap($bmp.Width, $bmp.Height)
    $g = [System.Drawing.Graphics]::FromImage($out)
    $g.DrawImage($bmp, -($dx % $bmp.Width), -($dy % $bmp.Height), $bmp.Width, $bmp.Height)
    $g.DrawImage($bmp, -($dx % $bmp.Width) + $bmp.Width, -($dy % $bmp.Height), $bmp.Width, $bmp.Height)
    $g.DrawImage($bmp, -($dx % $bmp.Width), -($dy % $bmp.Height) + $bmp.Height, $bmp.Width, $bmp.Height)
    $g.DrawImage($bmp, -($dx % $bmp.Width) + $bmp.Width, -($dy % $bmp.Height) + $bmp.Height, $bmp.Width, $bmp.Height)
    $g.Dispose()
    return $out
}

$rolled = Roll $src ([int]($w / 2)) ([int]($h / 2))

$cellW = $w * $Scale
$cellH = $h * $Scale
$gap = 16
$gridW = 2 * $cellW + $gap
$gridH = 2 * $cellH + $gap
$sheetH = $gridH + $cellH + $gap * 2
$sheetW = $gridW

$sheet = New-Object System.Drawing.Bitmap($sheetW, $sheetH)
$g = [System.Drawing.Graphics]::FromImage($sheet)
$g.Clear([System.Drawing.Color]::FromArgb(255, 24, 26, 32))
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor

for ($r = 0; $r -lt 2; $r++) {
    for ($c = 0; $c -lt 2; $c++) {
        $g.DrawImage($src, $c * $cellW, $r * $cellH, $cellW, $cellH)
    }
}
$gy = $gridH + $gap
$g.DrawImage($rolled, 0, $gy, $cellW, $cellH)

$g.Dispose()
$sheet.Save($OutFile, [System.Drawing.Imaging.ImageFormat]::Png)
$sheet.Dispose()
foreach ($b in @($src, $rolled)) { $b.Dispose() }
Write-Output "saved $OutFile"
