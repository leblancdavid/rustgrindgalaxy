param(
    [Parameter(Mandatory=$true)][string[]]$Sets,
    [string]$OutName = "sim_sheet.png",
    [string]$GlowPath = "assets\hoverboards\player\board_glow.png",
    [double]$BoardAlpha = 0.4,
    [double]$GlowAlpha = 0.6,
    [int]$Scale = 4
)

Add-Type -AssemblyName System.Drawing

$cell = 48 * $Scale
$cols = 5
$frameCount = 9
$rowsTotal = $Sets.Count * 2
$sheetW = $cols * $cell
$sheetH = $rowsTotal * $cell

$sheet = New-Object System.Drawing.Bitmap($sheetW, $sheetH)
$g = [System.Drawing.Graphics]::FromImage($sheet)
$g.Clear([System.Drawing.Color]::FromArgb(255, 90, 94, 100))
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor

function New-AlphaAttr([double]$a) {
    $m = New-Object System.Drawing.Imaging.ColorMatrix
    $m.Matrix33 = [single]$a
    $attr = New-Object System.Drawing.Imaging.ImageAttributes
    $attr.SetColorMatrix($m)
    return $attr
}

$boardAttr = New-AlphaAttr $BoardAlpha
$glowAttr = New-AlphaAttr $GlowAlpha
$glow = New-Object System.Drawing.Bitmap((Resolve-Path $GlowPath).Path)
$glowDest = $cell

$band = 0; $drawn = 0
foreach ($set in $Sets) {
    for ($i = 0; $i -lt $frameCount; $i++) {
        $file = "assets\hoverboards\player\_animtest\$set\board_$($i.ToString('D2')).png"
        if (-not (Test-Path $file)) { Write-Output "MISS $file"; continue }
        $bmp = New-Object System.Drawing.Bitmap((Resolve-Path $file).Path)
        $col = $i % $cols
        $row = $band * 2 + [int][Math]::Floor($i / $cols)
        $x = $col * $cell
        $y = $row * $cell
        $gx = $x + [int](($cell - $glowDest) / 2)
        $gy = $y + [int]($cell * 0.22)
        $g.DrawImage($glow, ([System.Drawing.Rectangle]::new($gx, $gy, $glowDest, $glowDest)), 0, 0, $glow.Width, $glow.Height, [System.Drawing.GraphicsUnit]::Pixel, $glowAttr)
        $dest = [System.Drawing.Rectangle]::new($x, $y, $cell, $cell)
        $g.DrawImage($bmp, $dest, 0, 0, $bmp.Width, $bmp.Height, [System.Drawing.GraphicsUnit]::Pixel, $boardAttr)
        $bmp.Dispose(); $drawn++
    }
    $band++
}

$glow.Dispose()
$g.Dispose()
$destPath = Join-Path "assets\hoverboards\player\_animtest" $OutName
$sheet.Save($destPath, [System.Drawing.Imaging.ImageFormat]::Png)
$sheet.Dispose()
Write-Output "drawn=$drawn saved=$destPath"
