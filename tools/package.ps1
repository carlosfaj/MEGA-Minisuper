$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$taskOutput = Join-Path (Split-Path -Parent $taskRoot) 'MEGA_Avance01_20261111.zip'
$taskFiles = Get-ChildItem -LiteralPath $taskRoot -Recurse -File -Force | Where-Object { $_.FullName -notmatch '\\(bin|obj|App_Data|\.git|__pycache__)\\' }
$taskStream = [IO.File]::Open($taskOutput, [IO.FileMode]::Create)
$taskArchive = [IO.Compression.ZipArchive]::new($taskStream, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($taskFile in $taskFiles) {
        $taskRelative = [IO.Path]::GetRelativePath($taskRoot, $taskFile.FullName).Replace('\', '/')
        $taskEntry = $taskArchive.CreateEntry($taskRelative, [IO.Compression.CompressionLevel]::Optimal)
        $taskSource = [IO.File]::OpenRead($taskFile.FullName)
        $taskDestination = $taskEntry.Open()
        try { $taskSource.CopyTo($taskDestination) } finally { $taskDestination.Dispose(); $taskSource.Dispose() }
    }
} finally { $taskArchive.Dispose(); $taskStream.Dispose() }
Write-Output $taskOutput
