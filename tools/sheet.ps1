param(
    [Parameter(Mandatory=$true)][string]$InDir,
    [Parameter(Mandatory=$true)][string]$OutFile,
    [int]$Scale = 4,
    [int]$Cols = 8
)

Add-Type -AssemblyName System.Drawing

$files = Get-ChildItem -Path $InDir -Filter "*.png" | Sort-Object Name
if ($files.Count -eq 0) { Write-Error "no pngs in $InDir"; exit 1 }

$bmps = foreach ($f in $files) { New-Object System.Drawing.Bitmap((Resolve-Path $f.FullName).Path) }
$srcW = ($bmps | Measure-Object -Property Width -Maximum).Maximum
$srcH = ($bmps | Measure-Object -Property Height -Maximum).Maximum
$cellW = [int]($srcW * $Scale)
$cellH = [int](($srcH + 12) * $Scale)
$rows = [int][Math]::Ceiling($bmps.Count / $Cols)

$sheetW = [int]($Cols * $cellW)
$sheetH = [int]($rows * $cellH)
$sheet = New-Object System.Drawing.Bitmap($sheetW, $sheetH)
$g = [System.Drawing.Graphics]::FromImage($sheet)
$g.Clear([System.Drawing.Color]::FromArgb(255, 36, 38, 44))
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$fontSize = [int]($Scale * 8)
$font = New-Object System.Drawing.Font("Consolas", $fontSize, [System.Drawing.FontStyle]::Bold)

for ($i = 0; $i -lt $bmps.Count; $i++) {
    $col = $i % $Cols
    $row = [int][Math]::Floor($i / $Cols)
    $x = $col * $cellW
    $y = $row * $cellH
    $bmp = $bmps[$i]
    $w = [int]($bmp.Width * $Scale)
    $h = [int]($bmp.Height * $Scale)
    $ox = [int]($x + ($cellW - $w) / 2)
    $oy = [int]($y + ($cellH - $h) / 2)
    $g.DrawImage($bmp, $ox, $oy, $w, $h)
    $g.DrawString("$i", $font, [System.Drawing.Brushes]::Gold, $x + 4, $y + 2)
    $g.DrawRectangle([System.Drawing.Pens]::DimGray, $x, $y, $cellW - 1, $cellH - 1)
}

$g.Dispose()
$sheet.Save($OutFile, [System.Drawing.Imaging.ImageFormat]::Png)
$sheet.Dispose()
foreach ($b in $bmps) { $b.Dispose() }
Write-Output "saved $OutFile ($($bmps.Count) tiles)"
