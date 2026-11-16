$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskRoot
$taskAddress = Get-NetIPConfiguration | Where-Object { $_.IPv4DefaultGateway -and $_.NetAdapter.Status -eq 'Up' } | Select-Object -First 1 -ExpandProperty IPv4Address
if (-not $taskAddress) { throw 'Conecta esta computadora al Wi-Fi antes de abrir MEGA.' }
$taskIp = $taskAddress.IPAddress
$taskBytes = ([Net.IPAddress]::Parse($taskIp)).GetAddressBytes()
$taskPrefix = [int]$taskAddress.PrefixLength
for ($taskIndex = 0; $taskIndex -lt 4; $taskIndex++) {
    $taskBits = [Math]::Max(0, [Math]::Min(8, $taskPrefix - 8 * $taskIndex))
    $taskBytes[$taskIndex] = $taskBytes[$taskIndex] -band (256 - [Math]::Pow(2, 8 - $taskBits))
}
$env:MEGA__AllowedNetwork = ($taskBytes -join '.') + '/' + $taskPrefix
$env:AllowedHosts = 'localhost;127.0.0.1;[::1];' + $taskIp
$env:ASPNETCORE_ENVIRONMENT = 'Development'
Write-Host ('En el celular conectado al mismo Wi-Fi abre https://' + $taskIp + ':7248/Acceso')
Write-Host 'El certificado de desarrollo requiere aceptar el aviso del navegador en esta demostracion.'
Write-Host 'Conserva esta ventana abierta. Ctrl+C detiene MEGA.'
dotnet run --project (Join-Path $taskRoot 'src/Mega.Web') --no-launch-profile --urls ('https://localhost:7248;https://' + $taskIp + ':7248')
exit $LASTEXITCODE
