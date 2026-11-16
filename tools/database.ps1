param([ValidateSet('check','upgrade')][string]$Operation='check')
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskRoot
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'Instala el SDK de .NET 10 para revisar este proyecto.' }
$taskProject = Join-Path $taskRoot 'src\Mega.Web'
# Compilar aparte evita reemplazar los archivos del servidor abierto.
$taskTempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$taskOutput = Join-Path $taskTempRoot ('MEGA-database-' + [Guid]::NewGuid().ToString('N'))
if ($Operation -eq 'check') {
    Write-Host 'MEGA - comprobacion de estructura, relaciones y datos. Esta consulta no modifica la base.'
} else {
    Write-Host 'MEGA - actualizacion de esquema con copia previa para bases existentes.'
}
$taskExitCode = 1
try {
    dotnet build (Join-Path $taskProject 'Mega.Web.csproj') --configuration Release --output $taskOutput -p:UseAppHost=false --nologo
    $taskExitCode = $LASTEXITCODE
    if ($taskExitCode -eq 0) {
        # Conservar configuracion y datos del perfil MEGA del proyecto original.
        $taskLaunch = Get-Content -LiteralPath (Join-Path $taskProject 'Properties\launchSettings.json') -Raw | ConvertFrom-Json
        $taskEnvironment = $taskLaunch.profiles.MEGA.environmentVariables.ASPNETCORE_ENVIRONMENT
        dotnet (Join-Path $taskOutput 'Mega.Web.dll') "--db-$Operation" --contentRoot $taskProject --environment $taskEnvironment
        $taskExitCode = $LASTEXITCODE
    }
} finally {
    $taskResolvedOutput = [IO.Path]::GetFullPath($taskOutput)
    if ((Split-Path -Parent $taskResolvedOutput).TrimEnd('\') -eq $taskTempRoot.TrimEnd('\') -and
        (Split-Path -Leaf $taskResolvedOutput) -match '^MEGA-database-[a-f0-9]{32}$' -and
        (Test-Path -LiteralPath $taskResolvedOutput)) {
        Remove-Item -LiteralPath $taskResolvedOutput -Recurse -Force
    }
}
exit $taskExitCode
