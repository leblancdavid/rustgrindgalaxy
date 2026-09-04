param(
    [string]$FxDir = "assets\hoverboards\player\_animtest\fxbase",
    [string]$OutName = "fx_placement_preview.png"
)

Add-Type -AssemblyName System.Drawing

$board = New-Object System.Drawing.Bitmap((Resolve-Path "assets\hoverboards\player\anim\idle\board_00.png").Path)
$glow = New-Object System.Drawing.Bitmap((Resolve-Path "assets\hoverboards\player\board_glow.png").Path)
$dust = New-Object System.Drawing.Bitmap((Resolve-Path "$FxDir\dust.png").Path)
$sparks = New-Object System.Drawing.Bitmap((Resolve-Path "$FxDir\sparks.png").Path)
$wisps = New-Object System.Drawing.Bitmap((Resolve-Path "$FxDir\wisps.png").Path)

$bs = 6          # zoom for visibility
$world = 128     # canvas per row
$cell = $world * $bs
$rows = @(
    @{ name = "dust"; bmp = $dust; ox = -30.0; oy = 8.0; scale = 64.0 / 48.0 },
    @{ name = "sparks"; bmp = $sparks; ox = -12.0; oy = 26.0; scale = 64.0 / 48.0 },
    @{ name = "wisps"; bmp = $wisps; ox = -10.0; oy = -2.0; scale = 64.0 / 48.0 }
)

$sheetW = $cell * 2
$sheetH = $cell * $rows.Count
$sheet = New-Object System.Drawing.Bitmap($sheetW, $sheetH)
$g = [System.Drawing.Graphics]::FromImage($sheet)
$g.Clear([System.Drawing.Color]::FromArgb(255, 90, 94, 100))
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor

$cx = $world / 2.0
$cy = $world / 2.0

function Draw-At($bmp, $destSizePx, $centerX, $centerY) {
    $rect = [System.Drawing.Rectangle]::new(($centerX - $destSizePx / 2), ($centerY - $destSizePx / 2), $destSizePx, $destSizePx)
    $g.DrawImage($bmp, $rect, 0, 0, $bmp.Width, $bmp.Height, [System.Drawing.GraphicsUnit]::Pixel)
}

for ($r = 0; $r -lt $rows.Count; $r++) {
    $fx = $rows[$r]
    for ($col = 0; $col -lt 2; $col++) {
        $offX = $col * $cell + $cx * $bs
        $offY = $r * $cell + $cy * $bs
        # glow at world scale (192px covers same 48 units as board -> same dest as board)
        Draw-At $glow (48 * $bs) $offX ($offY + (2 * $bs))
        # board at 0.75 world scale
        Draw-At $board ([int]($board.Width * 0.75 * $bs)) $offX ($offY - (0 * $bs))
        # fx at 0.75 world scale of its 64px canvas, offset in board-local units
        $fxPx = [int]($fx.bmp.Width * 0.75 * $bs)
        Draw-At $fx.bmp $fxPx ($offX + $fx.ox * $bs * 0.75) ($offY + $fx.oy * $bs * 0.75)
    }
}

$g.Dispose()
$dest = Join-Path "assets\hoverboards\player\_animtest" $OutName
$sheet.Save($dest, [System.Drawing.Imaging.ImageFormat]::Png)
$sheet.Dispose()
foreach ($b in @($board, $glow, $dust, $sparks, $wisps)) { $b.Dispose() }
Write-Output "saved $dest"
