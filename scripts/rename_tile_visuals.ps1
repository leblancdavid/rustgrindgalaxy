#!/usr/bin/env pwsh
# Rename visual nodes in tile scenes from *Visual/*Trim to Foundation*Visual/Foundation*Trim

$tilesDir = "D:\Dev\Games\rustgrindgalaxy\scenes\world\tiles\industrial"
$files = Get-ChildItem "$tilesDir\*.tscn"

$replacements = @(
    @{ Old = 'GroundVisual'; New = 'FoundationGroundVisual' }
    @{ Old = 'GroundTrim'; New = 'FoundationGroundTrim' }
    @{ Old = 'FlatVisual'; New = 'FoundationFlatVisual' }
    @{ Old = 'FlatTrim'; New = 'FoundationFlatTrim' }
    @{ Old = 'RampVisual'; New = 'FoundationRampVisual' }
    @{ Old = 'RampTrim'; New = 'FoundationRampTrim' }
    @{ Old = 'RampRise'; New = 'FoundationRampRise' }
    @{ Old = 'LandingVisual'; New = 'FoundationLandingVisual' }
    @{ Old = 'LandingTrim'; New = 'FoundationLandingTrim' }
    @{ Old = 'StairVisual'; New = 'FoundationStairVisual' }
    @{ Old = 'StairTrim'; New = 'FoundationStairTrim' }
    @{ Old = 'StairRise'; New = 'FoundationStairRise' }
    @{ Old = 'CurveVisual'; New = 'FoundationCurveVisual' }
    @{ Old = 'CurveTrim'; New = 'FoundationCurveTrim' }
    @{ Old = 'GapVisual'; New = 'FoundationGapVisual' }
    @{ Old = 'GapTrim'; New = 'FoundationGapTrim' }
    @{ Old = 'UpperVisual'; New = 'FoundationUpperVisual' }
    @{ Old = 'UpperTrim'; New = 'FoundationUpperTrim' }
    @{ Old = 'LowerVisual'; New = 'FoundationLowerVisual' }
    @{ Old = 'LowerTrim'; New = 'FoundationLowerTrim' }
    @{ Old = 'RailVisual'; New = 'FoundationRailVisual' }
    @{ Old = 'RailTrim'; New = 'FoundationRailTrim' }
)

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $original = $content
    
    foreach ($r in $replacements) {
        # Simple string replacement (not regex)
        $content = $content.Replace('[node name="' + $r.Old + '"', '[node name="' + $r.New + '"')
        $content = $content.Replace('parent="' + $r.Old + '"', 'parent="' + $r.New + '"')
    }
    
    if ($content -ne $original) {
        Set-Content $file.FullName $content -NoNewline
        Write-Host "Updated: $($file.Name)"
    } else {
        Write-Host "No changes: $($file.Name)"
    }
}

Write-Host "Done!"