param(
    [Parameter(Mandatory=$true)][string]$FramesDir,
    [Parameter(Mandatory=$true)][string]$OutDir,
    [string]$BasePath = "$PSScriptRoot\..\anim\idle\board_00.png",
    [int]$SparkBand = 0
)

Add-Type -AssemblyName System.Drawing

$base = New-Object System.Drawing.Bitmap((Resolve-Path $BasePath).Path)
$W = $base.Width; $H = $base.Height

$baseAlpha = New-Object 'int[,]' $W, $H
$maskMinY = $H; $maskMaxY = -1; $maskMinX = $W; $maskMaxX = -1
for ($y = 0; $y -lt $H; $y++) {
    for ($x = 0; $x -lt $W; $x++) {
        $a = $base.GetPixel($x, $y).A
        $baseAlpha[$x, $y] = $a
        if ($a -gt 0) {
            if ($y -lt $maskMinY) { $maskMinY = $y }; if ($y -gt $maskMaxY) { $maskMaxY = $y }
            if ($x -lt $maskMinX) { $maskMinX = $x }; if ($x -gt $maskMaxX) { $maskMaxX = $x }
        }
    }
}
$base.Dispose()
Write-Output "mask: $maskMinX,$maskMinY .. $maskMaxX,$maskMaxY  (band=$SparkBand)"

function Ramp([double]$v) {
    if ($v -lt 150) { return 0 }
    return [int][Math]::Min(255, [Math]::Max(60, 60 + ($v - 150) * 195.0 / 105.0))
}

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$files = Get-ChildItem $FramesDir -Filter *.png | Sort-Object Name
$idx = 0
foreach ($f in $files) {
    $src = New-Object System.Drawing.Bitmap($f.FullName)
    if ($src.Width -ne $W -or $src.Height -ne $H) { Write-Error "size mismatch $($f.Name)"; $src.Dispose(); continue }
    $out = New-Object System.Drawing.Bitmap($W, $H, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $kept = 0; $outside = 0
    for ($y = 0; $y -lt $H; $y++) {
        for ($x = 0; $x -lt $W; $x++) {
            $p = $src.GetPixel($x, $y)
            $lum = ($p.R + $p.G + $p.B) / 3.0
            $v = $p.A * $lum / 255.0
            if ($baseAlpha[$x, $y] -gt 0) {
                $a = Ramp $v
                if ($a -eq 0 -and $baseAlpha[$x, $y] -gt 200) { $a = 140 }
                $out.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($a, 255, 255, 255))
                if ($a -gt 0) { $kept++ }
            }
            elseif ($SparkBand -gt 0 -and $v -ge 200 `
                    -and $x -ge ($maskMinX - $SparkBand) -and $x -le ($maskMaxX + $SparkBand) `
                    -and $y -ge ($maskMinY - $SparkBand) -and $y -le ($maskMaxY + $SparkBand)) {
                $out.SetPixel($x, $y, [System.Drawing.Color]::FromArgb((Ramp $v), 255, 255, 255))
                $outside++
            }
            else {
                $out.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 255, 255, 255))
            }
        }
    }
    $src.Dispose()
    $dest = Join-Path $OutDir ("board_{0:D2}.png" -f $idx)
    $out.Save($dest, [System.Drawing.Imaging.ImageFormat]::Png)
    $out.Dispose()
    Write-Output "$($f.Name) -> $(Split-Path $dest -Leaf) kept=$kept outside=$outside"
    $idx++
}
