$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path

$assetRoot = Join-Path $root "Assets\Scripts\ClassicUO"

$placeholders = @()

Write-Host "Scanning project..."

Get-ChildItem $assetRoot -Recurse -Filter *.cs | ForEach-Object {

    # Skip symbolic links
    if ($_.LinkType) {
        return
    }

    $content = Get-Content $_.FullName -Raw

    # Placeholder files only contain a relative path
    if ($content.Trim() -match '^\.\./') {

        $resolved = Resolve-Path (
            Join-Path $_.DirectoryName $content.Trim()
        ) -ErrorAction SilentlyContinue

        if (-not $resolved) {
            throw "Missing target for $($_.FullName)"
        }

        $placeholders += [PSCustomObject]@{
            File   = $_.FullName
            Target = $resolved.Path
        }
    }
}

Write-Host ""
Write-Host "Found $($placeholders.Count) placeholder files."
Write-Host ""

if ($placeholders.Count -eq 0) {
    Write-Host "Nothing to do."
    exit
}

$answer = Read-Host "Convert all placeholder files? (Y/N)"

if ($answer -ne "Y") {
    Write-Host "Cancelled."
    exit
}

$count = 0

foreach ($item in $placeholders) {

    $count++

    Write-Host ("[{0}/{1}] {2}" -f `
        $count,
        $placeholders.Count,
        (Split-Path $item.File -Leaf)
    )

    $backup = "$($item.File).placeholder"

    if (!(Test-Path $backup)) {
        Rename-Item $item.File $backup
    }

    New-Item `
        -ItemType SymbolicLink `
        -Path $item.File `
        -Target $item.Target `
        -Force | Out-Null
}

Write-Host ""
Write-Host "======================================="
Write-Host "Completed!"
Write-Host "$count files converted."
Write-Host "======================================="