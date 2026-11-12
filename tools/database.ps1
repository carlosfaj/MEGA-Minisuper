param([ValidateSet('check','upgrade')][string]$Operation='check')
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskRoot
if ($Operation -eq 'check') {
    Write-Host 'MEGA - comprobacion de estructura, relaciones y datos. Esta consulta no modifica la base.'
    dotnet run --project (Join-Path $taskRoot 'src\Mega.Web') --configuration Release -- --db-check
} else {
    Write-Host 'MEGA - actualizacion de esquema con copia previa para bases existentes.'
    dotnet run --project (Join-Path $taskRoot 'src\Mega.Web') --configuration Release -- --db-upgrade
}
exit $LASTEXITCODE
