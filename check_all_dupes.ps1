$files = Get-ChildItem 'Assets\Resources\Questions\*.json'
foreach ($file in $files) {
    try {
        $json = Get-Content $file.FullName -Raw | ConvertFrom-Json
        $groups = $json | Group-Object -Property questionText | Where-Object { $_.Count -gt 1 }
        if ($groups.Count -gt 0) {
            Write-Host "$($file.Name): $($groups.Count) duplicate groups"
        } else {
            Write-Host "$($file.Name): OK (No duplicates)"
        }
    } catch {
        Write-Host "$($file.Name): Error reading or parsing file"
    }
}
