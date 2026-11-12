$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$taskVersion = (Get-Content -LiteralPath (Join-Path $taskRoot 'VERSION') -Raw).Trim()
$taskOutput = Join-Path (Split-Path -Parent $taskRoot) ("MEGA_Fuentes_{0}_{1}.zip" -f $taskVersion, [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff'))
$taskPaths = @(& git -C $taskRoot -c core.quotepath=false ls-files)
if ($LASTEXITCODE -ne 0 -or $taskPaths.Count -eq 0) { throw 'No se pudo obtener el listado de archivos versionados.' }
$taskForbidden = $taskPaths | Where-Object { $_ -match '(^|/)(bin|obj|App_Data|TestResults|\.git|__pycache__)/|(^|/)[^/]+\.archivos/|\.(db|sqlite|sqlite3|pfx|p12|key)$' }
if ($taskForbidden) { throw 'Hay archivos de ejecución o credenciales versionados; revisa el repositorio antes de empaquetar.' }
$taskStream = [IO.File]::Open($taskOutput, [IO.FileMode]::CreateNew)
$taskArchive = [IO.Compression.ZipArchive]::new($taskStream, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($taskPath in $taskPaths) {
        $taskRelative = $taskPath.Replace('\', '/')
        $taskFile = Join-Path $taskRoot $taskPath
        $taskEntry = $taskArchive.CreateEntry($taskRelative, [IO.Compression.CompressionLevel]::Optimal)
        $taskSource = [IO.File]::OpenRead($taskFile)
        $taskDestination = $taskEntry.Open()
        try { $taskSource.CopyTo($taskDestination) } finally { $taskDestination.Dispose(); $taskSource.Dispose() }
    }
} finally { $taskArchive.Dispose(); $taskStream.Dispose() }
Write-Output $taskOutput
