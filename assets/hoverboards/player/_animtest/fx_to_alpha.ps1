param(
    [Parameter(Mandatory=$true)][string]$FramesDir,
    [Parameter(Mandatory=$true)][string]$OutDir
)

Add-Type -AssemblyName System.Drawing

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$files = Get-ChildItem $FramesDir -Filter *.png | Sort-Object Name
$idx = 0
foreach ($f in $files) {
    $src = New-Object System.Drawing.Bitmap($f.FullName)
    $w = $src.Width
    $h = $src.Height
    $out = New-Object System.Drawing.Bitmap($w, $h, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $nonzero = 0
    for ($y = 0; $y -lt $h; $y++) {
        for ($x = 0; $x -lt $w; $x++) {
            $p = $src.GetPixel($x, $y)
            $lum = ($p.R + $p.G + $p.B) / 3.0
            $v = $p.A * $lum / 255.0
            $a = [int][Math]::Min(255.0, [Math]::Max(0.0, ($v - 95.0) * 255.0 / 140.0))
            $out.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($a, 255, 255, 255))
            if ($a -gt 0) { $nonzero++ }
        }
    }
    $src.Dispose()
    $dest = Join-Path $OutDir ("boardfx_{0:D2}.png" -f $idx)
    $out.Save($dest, [System.Drawing.Imaging.ImageFormat]::Png)
    $out.Dispose()
    Write-Output "$($f.Name) -> $(Split-Path $dest -Leaf) px=$nonzero"
    $idx++
}
