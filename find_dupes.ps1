$json = Get-Content 'Assets\Resources\Questions\kpss_onlisans_turkce.json' -Raw | ConvertFrom-Json
$groups = $json | Group-Object -Property questionText | Where-Object { $_.Count -gt 1 }
Write-Host "Total duplicate groups: $($groups.Count)"
Write-Host ""
foreach ($g in $groups) {
    $ids = ($g.Group | ForEach-Object { $_.id }) -join ', '
    $preview = $g.Name
    if ($preview.Length -gt 150) { $preview = $preview.Substring(0, 150) + "..." }
    Write-Host "--- DUPLICATE (x$($g.Count)) IDs: $ids"
    Write-Host $preview
    Write-Host ""
}
