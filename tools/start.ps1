$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskRoot
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'Instala el SDK de .NET 10 para abrir este proyecto.' }
Write-Host 'MEGA - demostracion local del cuarto corte'
Write-Host 'Usuario: cajero | Contrasena: MegaDemo2026!'
Write-Host 'Abre https://localhost:7248 cuando la aplicacion termine de iniciar.'
Write-Host 'La primera vez, el navegador puede pedir que aceptes el certificado local de desarrollo.'
Write-Host 'Conserva esta ventana abierta. Pulsa Ctrl+C para detener la aplicacion.'
dotnet run --project (Join-Path $taskRoot 'src\Mega.Web') --launch-profile MEGA
exit $LASTEXITCODE
