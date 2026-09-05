param(
    [Parameter(Mandatory=$true)][string]$JobId,
    [Parameter(Mandatory=$true)][string]$OutDir,
    [Parameter(Mandatory=$true)][int]$Count,
    [string]$UrlBase = "https://api.pixellab.ai/mcp/images"
)

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
for ($i = 0; $i -lt $Count; $i++) {
    $url = "$UrlBase/$JobId/download?index=$i"
    $dest = Join-Path $OutDir ("c_{0:D2}.png" -f $i)
    try {
        Invoke-WebRequest -Uri $url -OutFile $dest -ErrorAction Stop
    } catch {
        Write-Warning "failed index $i : $($_.Exception.Message)"
    }
}
Write-Output "downloaded $((Get-ChildItem $OutDir -Filter *.png).Count) / $Count to $OutDir"
