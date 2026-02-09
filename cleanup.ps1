$files = Get-ChildItem -Path "Pages" -Filter "*.cshtml" -Recurse
foreach ($file in $files) {
    $content = Get-Content $file.FullName
    $newContent = $content | Where-Object { $_ -notmatch 'ViewData\["Title"\]\s*=' }
    $newContent | Set-Content $file.FullName
    
    # Clean empty @{ } blocks
    $text = [System.IO.File]::ReadAllText($file.FullName)
    $cleanText = $text -replace '@\{[\s\n]*?\}', ''
    if ($text -ne $cleanText) {
        [System.IO.File]::WriteAllText($file.FullName, $cleanText)
    }
}
