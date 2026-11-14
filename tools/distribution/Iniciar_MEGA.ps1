param([string]$DataRoot = (Join-Path $env:LOCALAPPDATA 'MEGA\Demostracion'), [ValidateRange(1024,65535)][int]$Port = 7248)
$ErrorActionPreference = 'Stop'
if (-not [Environment]::Is64BitOperatingSystem) { throw 'Este paquete necesita Windows de 64 bits.' }
$taskApp = Join-Path $PSScriptRoot 'App'
$taskExecutable = Join-Path $taskApp 'Mega.Web.exe'
if (-not (Test-Path -LiteralPath $taskExecutable)) { throw 'Extrae el ZIP completo antes de iniciar MEGA.' }
$taskListeners = [Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners()
if (@($taskListeners | Where-Object { $_.Port -eq $Port -and ([Net.IPAddress]::IsLoopback($_.Address) -or $_.Address.Equals([Net.IPAddress]::Any) -or $_.Address.Equals([Net.IPAddress]::IPv6Any)) }).Count -gt 0) {
    throw 'El puerto local ya esta en uso. Deten la instancia anterior o selecciona otro puerto. No se modificaron tus datos.'
}
$taskData = [IO.Path]::GetFullPath($DataRoot)
if (-not [IO.Path]::IsPathRooted($DataRoot)) { throw 'La carpeta de datos debe ser una ruta completa.' }
[IO.Directory]::CreateDirectory($taskData) | Out-Null
$taskTls = Join-Path $taskData 'HTTPS'
[IO.Directory]::CreateDirectory($taskTls) | Out-Null
Add-Type -AssemblyName System.Security
$taskPurpose = [Text.Encoding]::UTF8.GetBytes('MEGA-HTTPS-local-v1')
$taskProtected = Join-Path $taskTls 'certificado.bin'
$taskCertificate = $null
if (Test-Path -LiteralPath $taskProtected) {
    try {
        $taskBytes = [Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($taskProtected), $taskPurpose, [Security.Cryptography.DataProtectionScope]::CurrentUser)
        $taskCertificate = [Text.Encoding]::UTF8.GetString($taskBytes) | ConvertFrom-Json
        if ([DateTimeOffset]::Parse($taskCertificate.Expires) -lt [DateTimeOffset]::UtcNow.AddDays(30)) { $taskCertificate = $null }
    } catch { throw 'No se pudo abrir el certificado de este perfil. Conserva la carpeta HTTPS y solicita revision tecnica; no se modificaron tus datos.' }
}
if ($null -eq $taskCertificate) {
    $taskRsa = [Security.Cryptography.RSACng]::new(2048)
    try {
        $taskRequest = [Security.Cryptography.X509Certificates.CertificateRequest]::new('CN=localhost', $taskRsa, [Security.Cryptography.HashAlgorithmName]::SHA256, [Security.Cryptography.RSASignaturePadding]::Pkcs1)
        $taskSan = [Security.Cryptography.X509Certificates.SubjectAlternativeNameBuilder]::new()
        $taskSan.AddDnsName('localhost'); $taskSan.AddIpAddress([Net.IPAddress]::Loopback); $taskSan.AddIpAddress([Net.IPAddress]::IPv6Loopback)
        $taskRequest.CertificateExtensions.Add($taskSan.Build())
        $taskRequest.CertificateExtensions.Add([Security.Cryptography.X509Certificates.X509BasicConstraintsExtension]::new($false,$false,0,$true))
        $taskOid = [Security.Cryptography.OidCollection]::new(); [void]$taskOid.Add([Security.Cryptography.Oid]::new('1.3.6.1.5.5.7.3.1'))
        $taskRequest.CertificateExtensions.Add([Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension]::new($taskOid,$false))
        $taskCert = $taskRequest.CreateSelfSigned([DateTimeOffset]::UtcNow.AddDays(-1), [DateTimeOffset]::UtcNow.AddYears(2))
        try {
            $taskRandom = New-Object byte[] 32; $taskGenerator = [Security.Cryptography.RandomNumberGenerator]::Create()
            try { $taskGenerator.GetBytes($taskRandom) } finally { $taskGenerator.Dispose() }
            $taskPassword = [Convert]::ToBase64String($taskRandom)
            $taskCertificate = @{ Expires=$taskCert.NotAfter.ToUniversalTime().ToString('O'); Password=$taskPassword; Pfx=[Convert]::ToBase64String($taskCert.Export([Security.Cryptography.X509Certificates.X509ContentType]::Pfx,$taskPassword)) }
            $taskPlain = [Text.Encoding]::UTF8.GetBytes(($taskCertificate | ConvertTo-Json -Compress))
            $taskEncrypted = [Security.Cryptography.ProtectedData]::Protect($taskPlain,$taskPurpose,[Security.Cryptography.DataProtectionScope]::CurrentUser)
            $taskTemporary = Join-Path $taskTls ([Guid]::NewGuid().ToString('N')+'.tmp')
            [IO.File]::WriteAllBytes($taskTemporary,$taskEncrypted)
            if (Test-Path -LiteralPath $taskProtected) { [IO.File]::Replace($taskTemporary,$taskProtected,$null) } else { [IO.File]::Move($taskTemporary,$taskProtected) }
        } finally { $taskCert.Dispose() }
    } finally { $taskRsa.Dispose() }
}
$taskActive = Join-Path $taskTls ([Guid]::NewGuid().ToString('N')+'.pfx')
try {
    [IO.File]::WriteAllBytes($taskActive,[Convert]::FromBase64String($taskCertificate.Pfx))
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:MEGA__DemoData = 'true'
    $env:MEGA__DatabasePath = Join-Path $taskData 'mega-demo.db'
    $env:MEGA__ProjectPlanPath = Join-Path $taskApp 'Tracking\proyecto.json'
    $env:Kestrel__Certificates__Default__Path = $taskActive
    $env:Kestrel__Certificates__Default__Password = $taskCertificate.Password
    Write-Host 'MEGA - demostracion local. No requiere instalar .NET.'
    Write-Host ('Abre https://localhost:{0} cuando aparezca el aviso de inicio.' -f $Port)
    Write-Host 'Usuario: cajero | Contrasena: MegaDemo2026!'
    Write-Host 'El navegador puede pedir aceptar el certificado local. No se cambia la confianza del equipo.'
    Write-Host ('Datos conservados en: '+$taskData)
    Write-Host 'Conserva esta ventana abierta. Pulsa Ctrl+C para detener.'
    & $taskExecutable --contentRoot $taskApp --urls ('https://localhost:'+ $Port)
    $taskExit = $LASTEXITCODE
} finally {
    Remove-Item Env:\Kestrel__Certificates__Default__Password -ErrorAction SilentlyContinue
    $taskResolved = [IO.Path]::GetFullPath($taskActive)
    if ([IO.Path]::GetDirectoryName($taskResolved) -eq [IO.Path]::GetFullPath($taskTls) -and [IO.Path]::GetExtension($taskResolved) -eq '.pfx' -and [IO.Path]::GetFileNameWithoutExtension($taskResolved) -match '^[0-9a-f]{32}$') {
        if (Test-Path -LiteralPath $taskResolved) { Remove-Item -LiteralPath $taskResolved }
    }
}
exit $taskExit
