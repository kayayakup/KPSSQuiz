param([string]$FilePath)

$json = Get-Content $FilePath -Raw | ConvertFrom-Json
$uniqueQuestions = @()
$seen = @{}

foreach ($q in $json) {
    if (-not $seen.ContainsKey($q.questionText)) {
        $seen[$q.questionText] = $true
        $uniqueQuestions += $q
    }
}

$uniqueQuestions | ConvertTo-Json -Depth 10 | Set-Content $FilePath
Write-Host "Processed $FilePath. Original count: $($json.Count), New count: $($uniqueQuestions.Count)"
