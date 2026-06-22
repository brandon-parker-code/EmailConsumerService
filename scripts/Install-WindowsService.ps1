#Requires -Version 5.1
#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Installs (or reinstalls) the Email Consumer Service as a Windows Service.

.DESCRIPTION
    Registers the published EmailConsumerService executable with the Windows
    Service Control Manager. Validates the binary, optionally replaces an
    existing service, configures automatic restart on failure, and can start
    the service when done.

    Must be run from an elevated (Administrator) PowerShell session.

.PARAMETER ServiceName
    The SCM service name. Must match the name the app registers via
    AddWindowsService (default: "EmailConsumerService").

.PARAMETER DisplayName
    Friendly name shown in services.msc.

.PARAMETER Description
    Service description shown in services.msc.

.PARAMETER BinaryPath
    Full path to the published EmailConsumerService.exe.

.PARAMETER StartupType
    Service start mode: Automatic, AutomaticDelayedStart, Manual, or Disabled.

.PARAMETER Credential
    Optional service log-on account. The account must hold the
    "Log on as a service" right; this script does not grant it.

.PARAMETER Force
    Replace an existing service with the same name instead of failing.

.PARAMETER Start
    Start the service after a successful install.

.EXAMPLE
    .\Install-WindowsService.ps1 -BinaryPath 'C:\Services\EmailConsumerService\EmailConsumerService.exe' -Start

.EXAMPLE
    $cred = Get-Credential
    .\Install-WindowsService.ps1 -BinaryPath 'C:\Services\EmailConsumerService\EmailConsumerService.exe' -Credential $cred -Force -Start
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateNotNullOrEmpty()]
    [string]$ServiceName = 'EmailConsumerService',

    [ValidateNotNullOrEmpty()]
    [string]$DisplayName = 'Email Consumer Service',

    [string]$Description = 'Consumes email requests from a Kafka topic and sends them via SendGrid with SMTP fallback.',

    [ValidateNotNullOrEmpty()]
    [string]$BinaryPath = 'C:\services\ecs\EmailConsumerService.exe',

    [ValidateSet('Automatic', 'AutomaticDelayedStart', 'Manual', 'Disabled')]
    [string]$StartupType = 'Automatic',

    [System.Management.Automation.PSCredential]$Credential,

    [switch]$Force,

    [switch]$Start
)

$ErrorActionPreference = 'Stop'

function Remove-ExistingService {
    param([string]$Name)

    Write-Host "Stopping and removing existing service '$Name'..." -ForegroundColor Yellow

    $service = Get-Service -Name $Name -ErrorAction SilentlyContinue
    if ($service.Status -ne 'Stopped') {
        Stop-Service -Name $Name -Force -ErrorAction SilentlyContinue
    }

    # sc.exe delete works on Windows PowerShell 5.1 (Remove-Service is 6.0+).
    $null = & sc.exe delete $Name
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to delete existing service '$Name' (sc.exe exit code $LASTEXITCODE)."
    }

    # Deletion is asynchronous; wait for the SCM to drop the entry.
    for ($i = 0; $i -lt 10; $i++) {
        Start-Sleep -Milliseconds 500
        if (-not (Get-Service -Name $Name -ErrorAction SilentlyContinue)) {
            return
        }
    }
    throw "Service '$Name' was not removed in time. Close any open Services consoles and retry."
}

# --- Validate inputs -------------------------------------------------------
if (-not (Test-Path -LiteralPath $BinaryPath -PathType Leaf)) {
    throw "Binary not found at '$BinaryPath'. Publish the app first, e.g.:`n" +
          "  dotnet publish EmailConsumerService\EmailConsumerService.csproj -c Release -o C:\services\ecs"
}

$resolvedPath = (Resolve-Path -LiteralPath $BinaryPath).Path

# --- Handle an existing service -------------------------------------------
$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existing) {
    if (-not $Force) {
        throw "Service '$ServiceName' already exists. Re-run with -Force to replace it."
    }
    if ($PSCmdlet.ShouldProcess($ServiceName, 'Remove existing service')) {
        Remove-ExistingService -Name $ServiceName
    }
}

# --- Create the service ----------------------------------------------------
# Quote the image path when it contains spaces so the SCM parses it correctly.
$imagePath = if ($resolvedPath -match '\s') { '"' + $resolvedPath + '"' } else { $resolvedPath }

# New-Service (5.1) accepts Automatic/Manual/Disabled; delayed-auto is applied
# via sc.exe after creation for broad compatibility.
$newServiceStartupType = if ($StartupType -eq 'AutomaticDelayedStart') { 'Automatic' } else { $StartupType }

$newServiceArgs = @{
    Name           = $ServiceName
    BinaryPathName = $imagePath
    DisplayName    = $DisplayName
    Description    = $Description
    StartupType    = $newServiceStartupType
}
if ($Credential) {
    $newServiceArgs['Credential'] = $Credential
}

if ($PSCmdlet.ShouldProcess($ServiceName, 'Create Windows service')) {
    Write-Host "Creating service '$ServiceName' -> $imagePath" -ForegroundColor Cyan
    $null = New-Service @newServiceArgs

    if ($StartupType -eq 'AutomaticDelayedStart') {
        $null = & sc.exe config $ServiceName start= delayed-auto
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Could not set delayed-auto start (sc.exe exit code $LASTEXITCODE)."
        }
    }

    # Auto-restart on failure: restart after 5s for the first two failures,
    # reset the failure counter after one day.
    $null = & sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/5000/restart/5000
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Could not configure failure recovery (sc.exe exit code $LASTEXITCODE)."
    }

    Write-Host "Service '$ServiceName' installed." -ForegroundColor Green
}

# --- Optionally start ------------------------------------------------------
if ($Start -and $PSCmdlet.ShouldProcess($ServiceName, 'Start service')) {
    Write-Host "Starting service '$ServiceName'..." -ForegroundColor Cyan
    Start-Service -Name $ServiceName
    Get-Service -Name $ServiceName | Select-Object Name, Status, StartType | Format-Table -AutoSize
}

if ($Credential) {
    Write-Host "Note: ensure '$($Credential.UserName)' has the 'Log on as a service' right." -ForegroundColor Yellow
}
