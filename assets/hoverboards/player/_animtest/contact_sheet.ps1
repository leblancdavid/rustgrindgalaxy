param(
    [Parameter(Mandatory=$true)][string[]]$Sets,
    [string]$OutName = "sheet.png",
    [string]$Root = "assets\hoverboards\player\_animtest"
)

Add-Type -AssemblyName System.Drawing

$cell = 192
$cols = 5
$frameCount = 9
$rowsTotal = $Sets.Count * 2

$sheetW = $cols * $cell
$sheetH = $rowsTotal * $cell
$sheet = New-Object System.Drawing.Bitmap($sheetW, $sheetH)
$g = [System.Drawing.Graphics]::FromImage($sheet)
$g.Clear([System.Drawing.Color]::FromArgb(255, 40, 44, 50))
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor

$band = 0
foreach ($set in $Sets) {
    for ($i = 0; $i -lt $frameCount; $i++) {
        $file = Join-Path (Join-Path $Root $set) ("frame_{0:D2}.png" -f $i)
        if (-not (Test-Path $file)) { continue }
        $bmp = New-Object System.Drawing.Bitmap((Resolve-Path $file).Path)
        $col = $i % $cols
        $row = $band * 2 + [int][Math]::Floor($i / $cols)
        $g.DrawImage($bmp, $col * $cell, $row * $cell, $cell, $cell)
        $bmp.Dispose()
    }
    $band++
}

$g.Dispose()
$dest = Join-Path $Root $OutName
$sheet.Save($dest, [System.Drawing.Imaging.ImageFormat]::Png)
$sheet.Dispose()
Write-Output "saved $dest"
