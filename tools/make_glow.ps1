param(
    [Parameter(Mandatory=$true)][string]$SrcDir,
    [Parameter(Mandatory=$true)][string]$OutDir,
    [int]$Factor = 4,
    [int]$Sigma = 9,
    [int]$AlphaThreshold = 128,
    [int]$Pad = 0
)

# Baked-Gaussian glow per docs/GLOW_SYSTEM.md: binarize alpha, upscale,
# separable Gaussian (clamp edges), peak-normalize, white RGB + alpha PNG.

Add-Type -AssemblyName System.Drawing
$code = @"
using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;

public static class GlowGen
{
    public static void Make(string srcPath, string dstPath, int factor, int sigma, int alphaThreshold, int pad)
    {
        byte[] src;
        int srcW, srcH, srcStride;
        using (var bmp = new Bitmap(srcPath))
        {
            srcW = bmp.Width; srcH = bmp.Height;
            var rect = new Rectangle(0, 0, srcW, srcH);
            var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            srcStride = data.Stride;
            src = new byte[srcStride * srcH];
            Marshal.Copy(data.Scan0, src, 0, src.Length);
            bmp.UnlockBits(data);
        }

        int cw = srcW * factor, ch = srcH * factor;
        int w = cw + 2 * pad, h = ch + 2 * pad;
        var bin = new double[w * h];
        for (int y = 0; y < h; y++)
        {
            int sy0 = y - pad;
            if (sy0 < 0 || sy0 >= ch) continue;
            int sy = sy0 / factor;
            for (int x = 0; x < w; x++)
            {
                int sx0 = x - pad;
                if (sx0 < 0 || sx0 >= cw) continue;
                int sx = sx0 / factor;
                byte a = src[sy * srcStride + sx * 4 + 3];
                if (a > alphaThreshold) bin[y * w + x] = 255.0;
            }
        }

        int radius = (int)Math.Ceiling(3.0 * sigma);
        var kernel = new double[2 * radius + 1];
        double sum = 0.0;
        for (int i = -radius; i <= radius; i++)
        {
            kernel[i + radius] = Math.Exp(-(i * (double)i) / (2.0 * sigma * sigma));
            sum += kernel[i + radius];
        }
        for (int i = 0; i < kernel.Length; i++) kernel[i] /= sum;

        var tmp = new double[w * h];
        var blurred = new double[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                double acc = 0.0;
                for (int k = -radius; k <= radius; k++)
                {
                    int sxp = x + k;
                    if (sxp < 0) sxp = 0; else if (sxp >= w) sxp = w - 1;
                    acc += bin[y * w + sxp] * kernel[k + radius];
                }
                tmp[y * w + x] = acc;
            }
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                double acc = 0.0;
                for (int k = -radius; k <= radius; k++)
                {
                    int syp = y + k;
                    if (syp < 0) syp = 0; else if (syp >= h) syp = h - 1;
                    acc += tmp[syp * w + x] * kernel[k + radius];
                }
                blurred[y * w + x] = acc;
            }

        double peak = 0.0;
        foreach (var v in blurred) if (v > peak) peak = v;
        double norm = peak > 0.0 ? 255.0 / peak : 0.0;

        using (var outBmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
        {
            var rect = new Rectangle(0, 0, w, h);
            var data = outBmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            var buf = new byte[data.Stride * h];
            for (int y = 0; y < h; y++)
            {
                int row = y * data.Stride;
                for (int x = 0; x < w; x++)
                {
                    int o = row + x * 4;
                    int a = (int)Math.Round(Math.Min(255.0, blurred[y * w + x] * norm));
                    buf[o] = 255; buf[o + 1] = 255; buf[o + 2] = 255; buf[o + 3] = (byte)a;
                }
            }
            Marshal.Copy(buf, 0, data.Scan0, buf.Length);
            outBmp.UnlockBits(data);
            outBmp.Save(dstPath, ImageFormat.Png);
        }
    }
}
"@

$drawingRefs = [System.AppDomain]::CurrentDomain.GetAssemblies() |
    Where-Object { -not $_.IsDynamic -and $_.Location } |
    Select-Object -ExpandProperty Location
Add-Type -TypeDefinition $code -ReferencedAssemblies $drawingRefs

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
if ($Pad -le 0) { $Pad = 3 * $Sigma }
$files = Get-ChildItem -Path $SrcDir -Filter "*.png" | Where-Object { $_.DirectoryName -ne (Resolve-Path $OutDir).Path } | Sort-Object Name
foreach ($f in $files) {
    [GlowGen]::Make($f.FullName, (Join-Path $OutDir $f.Name), $Factor, $Sigma, $AlphaThreshold, $Pad)
}
Write-Output "glow: $($files.Count) -> $OutDir"
