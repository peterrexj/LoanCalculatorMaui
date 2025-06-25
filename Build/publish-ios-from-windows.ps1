# Windows PowerShell Script for .NET MAUI iOS Publishing and Remote Upload
#
# This script builds the IPA on Windows, copies it to the Mac, and then
# remotely executes a *separate* Bash script on the Mac for the App Store Connect upload.
#
# Usage examples:
# ./publish-ios-windows.ps1 -MacUsername "Peter Rex" -MacIP "192.168.133.129" -AppleIDUsername "rex_de_devil@yahoo.com"
# ./publish-ios-windows.ps1 -MacUsername "Peter Rex" -MacIP "192.168.133.129" -AppleIDUsername "rex_de_devil@yahoo.com" -Stages "Build" # Build only
# ./publish-ios-windows.ps1 -MacUsername "Peter Rex" -MacIP "192.168.133.129" -AppleIDUsername "rex_de_devil@yahoo.com" -Stages "Copy", "Deploy" # Skip build, just copy and deploy
#
# IMPORTANT:
# 1. Ensure OpenSSH Client is installed on Windows (Settings > Apps > Optional features > Add an optional feature)
# 2. Ensure Remote Login is enabled on your Mac (System Settings > General > Sharing > Remote Login)
# 3. For passwordless SSH, set up SSH keys between your Windows PC and your Mac.
#    (Refer to previous instructions on generating keys and manually copying them to Mac's authorized_keys).
# 4. **You MUST save the 'mac-upload-script.sh' (provided below) on your Mac before running this Windows script.**
#    Make sure it's executable: chmod +x /path/to/your/mac-upload-script.sh
# 5. Replace placeholder default values or pass them as parameters.

# Windows PowerShell Script for .NET MAUI iOS Publishing and Remote Upload
# PowerShell Script to Build .NET MAUI iOS App, Transfer to Mac, and Upload to App Store Connect

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$ProjectFile = "..\\src\\LoanCalculator\\LoanCalculatorMaui.csproj",

    [Parameter(Mandatory = $false)]
    [string]$Configuration = "Release",

    [Parameter(Mandatory = $false)]
    [string]$TargetFramework = "net9.0-ios",

    [Parameter(Mandatory = $false)]
    [string]$RuntimeIdentifier = "ios-arm64",

    [Parameter(Mandatory = $false)]
    [string]$OutputDir = ".\\bin\\${Configuration}\\${TargetFramework}\\publish\\",

    [Parameter(Mandatory = $true)]
    [string]$MacUsername,

    [Parameter(Mandatory = $true)]
    [string]$MacIP,

    [Parameter(Mandatory = $false)]
    [string]$MacRemoteTempPath = "/tmp/",

    [Parameter(Mandatory = $false)]
    [string]$MacUploadScriptPath = "/Users/$MacUsername/scripts/upload_ipa.sh",

    [Parameter(Mandatory = $true)]
    [string]$AppleIDUsername,

    [Parameter(Mandatory = $false)]
    [System.Security.SecureString]$AppleIDPassword,

    [Parameter(Mandatory = $false)]
    [ValidateSet("Build", "Copy", "Deploy")]
    [string[]]$Stages = @("Build", "Copy", "Deploy")
)

# Prompt for password if not provided
if (-not $AppleIDPassword) {
    $AppleIDPassword = Read-Host -AsSecureString -Prompt "Enter App-Specific Password for '$AppleIDUsername'"
}
$PlainAppleIDPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($AppleIDPassword)
)

Write-Host "`n=== .NET MAUI iOS Publishing Script ===`n"
Write-Host "Project: $ProjectFile"
Write-Host "Target: $TargetFramework"
Write-Host "Runtime: $RuntimeIdentifier"
Write-Host "Mac: $MacUsername@$MacIP"
Write-Host "Upload Script: $MacUploadScriptPath"
Write-Host "Stages: $($Stages -join ', ')"
Write-Host ""

# Ensure SSH tools are installed
if (-not (Get-Command ssh -ErrorAction SilentlyContinue)) {
    Write-Error "OpenSSH client (ssh.exe) not found. Install it via Windows Optional Features."
    exit 1
}
if (-not (Get-Command scp -ErrorAction SilentlyContinue)) {
    Write-Error "OpenSSH client (scp.exe) not found. Install it via Windows Optional Features."
    exit 1
}

$IPA_FILE_PATH = ""

# === Build Stage ===
if ($Stages -contains "Build") {
    Write-Host "`n--- Building IPA ---"
    dotnet clean $ProjectFile -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Clean failed."
        exit 1
    }

    dotnet restore $ProjectFile
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Restore failed."
        exit 1
    }

    dotnet publish $ProjectFile `
        -c $Configuration `
        -f $TargetFramework `
        -r $RuntimeIdentifier `
        -o $OutputDir `
        -p:MtouchLink=SdkOnly `
        -p:MtouchExtraArgs="--optimize=force-rejected-types-removal" `
        -p:CreatePackage=true
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Publish failed."
        exit 1
    }

    $IpaFile = Get-ChildItem -Path $OutputDir -Filter "*.ipa" -Recurse | Select-Object -First 1
    if (-not $IpaFile) {
        Write-Error "IPA file not found after build."
        exit 1
    }
    $IPA_FILE_PATH = $IpaFile.FullName
    Write-Host "Build successful. IPA: $IPA_FILE_PATH"
} else {
    Write-Host "Skipping Build. Searching for existing IPA in: $OutputDir"
    $IpaFile = Get-ChildItem -Path $OutputDir -Filter "*.ipa" -Recurse | Select-Object -First 1
    if (-not $IpaFile) {
        Write-Error "No IPA found. Cannot continue."
        exit 1
    }
    $IPA_FILE_PATH = $IpaFile.FullName
    Write-Host "Found IPA: $IPA_FILE_PATH"
}

# === Copy Stage ===
$RemoteIpaFileName = [System.IO.Path]::GetFileName($IPA_FILE_PATH)
$RemoteIpaTempPath = "$MacRemoteTempPath$RemoteIpaFileName"

if ($Stages -contains "Copy") {
    Write-Host "`n--- Copying IPA to Mac (${MacUsername}@${MacIP}:${RemoteIpaTempPath}) ---"
    & scp "$IPA_FILE_PATH" "${MacUsername}@${MacIP}:${RemoteIpaTempPath}"
    if ($LASTEXITCODE -ne 0) {
        Write-Error "SCP failed. Check connection and path."
        exit 1
    }
    Write-Host "IPA copied to: $RemoteIpaTempPath"
} else {
    Write-Host "Skipping Copy."
}

# === Deploy Stage ===
if ($Stages -contains "Deploy") {
    Write-Host "`n--- Deploying via remote script ---"
    $remoteCommand = "$MacUploadScriptPath '$RemoteIpaTempPath' '$AppleIDUsername' '$PlainAppleIDPassword'"
    & ssh "${MacUsername}@${MacIP}" $remoteCommand
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Remote deployment failed. See above for errors."
        exit 1
    }
    Write-Host "Deploy complete."
} else {
    Write-Host "Skipping Deploy."
}

Write-Host "`n✅ Publishing process completed."
