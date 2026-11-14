param([string]$OutputDirectory, [switch]$AllowDirty)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$taskVersion = (Get-Content -LiteralPath (Join-Path $taskRoot 'VERSION') -Raw).Trim()
if ($taskVersion -notmatch '^\d+\.\d+\.\d+$') { throw 'Version no valida.' }
$taskCommit = (& git -C $taskRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'No se pudo identificar la revision.' }
$taskDirty = @(& git -C $taskRoot status --porcelain).Count -gt 0
if ($taskDirty -and -not $AllowDirty) { throw 'Registra primero la version estable. Usa AllowDirty solo para ensayo privado.' }
if (-not $OutputDirectory) { $OutputDirectory = Split-Path -Parent $taskRoot }
$taskOutput = [IO.Path]::GetFullPath($OutputDirectory)
$taskTestRoot = [IO.Path]::GetFullPath((Join-Path $taskRoot 'TestResults'))
if ($AllowDirty -and -not ($taskOutput -eq $taskTestRoot -or $taskOutput.StartsWith($taskTestRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase))) { throw 'Un paquete de ensayo debe quedar dentro de TestResults.' }
[IO.Directory]::CreateDirectory($taskOutput) | Out-Null
$taskWork = Join-Path (Join-Path $taskTestRoot 'Distribucion') ([Guid]::NewGuid().ToString('N'))
$taskApp = Join-Path $taskWork 'App'
[IO.Directory]::CreateDirectory($taskWork) | Out-Null
Push-Location -LiteralPath $taskRoot
try {
    $taskSdk = (& dotnet --version).Trim()
    & dotnet publish (Join-Path $taskRoot 'src\Mega.Web\Mega.Web.csproj') -c Release -r win-x64 --self-contained true -p:RestoreLockedMode=true -o $taskApp --nologo
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo generar el programa con las dependencias fijadas.' }
    $taskPackageRoots = (& dotnet nuget locals global-packages --list) -replace '^global-packages:\s*',''
} finally { Pop-Location }
foreach ($taskName in @('Iniciar_MEGA.cmd','Iniciar_MEGA.ps1')) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot ('distribution\'+$taskName)) -Destination (Join-Path $taskWork $taskName) }
Copy-Item -LiteralPath (Join-Path $taskRoot 'docs\Distribucion.md') -Destination (Join-Path $taskWork 'LEEME.md')
$taskRuntime = Get-Content -LiteralPath (Join-Path $taskApp 'Mega.Web.runtimeconfig.json') -Raw | ConvertFrom-Json
if ($taskRuntime.runtimeOptions.framework -or $taskRuntime.runtimeOptions.frameworks -or -not $taskRuntime.runtimeOptions.includedFrameworks) { throw 'La publicacion depende de una instalacion externa de .NET.' }
foreach ($taskName in @('Mega.Web.exe','coreclr.dll','hostfxr.dll','hostpolicy.dll')) { if (-not (Test-Path -LiteralPath (Join-Path $taskApp $taskName))) { throw ('Falta un componente incluido: '+$taskName) } }
foreach ($taskFramework in $taskRuntime.runtimeOptions.includedFrameworks) {
    $taskRuntimePack = Join-Path $taskPackageRoots ($taskFramework.name.ToLowerInvariant()+'.runtime.win-x64\'+$taskFramework.version)
    foreach ($taskNotice in @('LICENSE.TXT','THIRD-PARTY-NOTICES.TXT')) {
        $taskNoticeSource = Join-Path $taskRuntimePack $taskNotice
        if (-not (Test-Path -LiteralPath $taskNoticeSource)) { throw ('Falta el aviso de redistribucion del entorno incluido: '+$taskNoticeSource) }
        $taskNoticeFolder = Join-Path (Join-Path $taskApp 'Licencias') $taskFramework.name
        [IO.Directory]::CreateDirectory($taskNoticeFolder) | Out-Null
        Copy-Item -LiteralPath $taskNoticeSource -Destination (Join-Path $taskNoticeFolder $taskNotice)
    }
}
$taskFiles = @(Get-ChildItem -LiteralPath $taskWork -Recurse -File)
$taskEntries = @()
foreach ($taskFile in $taskFiles) {
    $taskRelative = [IO.Path]::GetRelativePath($taskWork,$taskFile.FullName).Replace('\','/')
    if ($taskRelative -match '(^|/)(App_Data|TestResults|\.git|[^/]+\.archivos)/|\.(db|sqlite|sqlite3|pfx|p12|key|bin)$') { throw ('Archivo de operacion excluido: '+$taskRelative) }
    $taskEntries += @{name=$taskRelative;bytes=$taskFile.Length;sha256=(Get-FileHash -LiteralPath $taskFile.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}
}
$taskManifest = @{format=1;version=$taskVersion;sourceCommit=$taskCommit;workingTreeClean=(-not $taskDirty);rid='win-x64';selfContained=$true;sdk=$taskSdk;frameworks=$taskRuntime.runtimeOptions.includedFrameworks;files=$taskEntries;createdUtc=[DateTime]::UtcNow.ToString('O')}
$taskManifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $taskWork 'paquete.json') -Encoding utf8
$taskZip = Join-Path $taskOutput ('MEGA_Ejecucion_'+$taskVersion+'_'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff')+'.zip')
$taskStream = [IO.File]::Open($taskZip,[IO.FileMode]::CreateNew)
$taskArchive = [IO.Compression.ZipArchive]::new($taskStream,[IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($taskFile in Get-ChildItem -LiteralPath $taskWork -Recurse -File) {
        $taskRelative = [IO.Path]::GetRelativePath($taskWork,$taskFile.FullName).Replace('\','/')
        $taskEntry = $taskArchive.CreateEntry($taskRelative,[IO.Compression.CompressionLevel]::Optimal)
        $taskInput = [IO.File]::OpenRead($taskFile.FullName); $taskDestination = $taskEntry.Open()
        try { $taskInput.CopyTo($taskDestination) } finally { $taskDestination.Dispose();$taskInput.Dispose() }
    }
} finally { $taskArchive.Dispose();$taskStream.Dispose() }
$taskHash = (Get-FileHash -LiteralPath $taskZip -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText($taskZip+'.sha256',$taskHash+'  '+[IO.Path]::GetFileName($taskZip)+[Environment]::NewLine)
Write-Output ('PAQUETE='+$taskZip)
Write-Output ('CARPETA='+$taskWork)
Write-Output ('SHA256='+$taskHash)
