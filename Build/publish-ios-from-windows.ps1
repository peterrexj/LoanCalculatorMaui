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

[CmdletBinding()]
param(
    # --- Project and Build Details (Windows Side) ---
    [Parameter(Mandatory=$false)]
    [string]$ProjectFile = "..\\src\\LoanCalculator\\LoanCalculatorMaui.csproj",

    [Parameter(Mandatory=$false)]
    [string]$Configuration = "Release",

    [Parameter(Mandatory=$false)]
    [string]$TargetFramework = "net9.0-ios",

    [Parameter(Mandatory=$false)]
    [string]$RuntimeIdentifier = "ios-arm64",

    [Parameter(Mandatory=$false)]
    [string]$OutputDir = ".\\bin\\${Configuration}\\${TargetFramework}\\publish\\",

    # --- Mac Connection Details ---
    [Parameter(Mandatory=$true)]
    [string]$MacUsername,

    [Parameter(Mandletory=$true)]
    [string]$MacIP,

    [Parameter(Mandatory=$false)]
    [string]$MacRemoteTempPath = "/tmp/", # Temporary directory on Mac to store the IPA for upload

    # Path to the Bash script on your Mac that handles the upload
    [Parameter(Mandatory=$false)]
    [string]$MacUploadScriptPath = "/Users/$(Convert-Path $MacUsername)./scripts/upload_ipa.sh", # <--- Adjust this path to where you save the Mac script

    # --- Apple ID for App Store Connect Upload (Used on Mac) ---
    [Parameter(Mandatory=$true)]
    [string]$AppleIDUsername,

    [Parameter(Mandatory=$false)]
    [System.Security.SecureString]$AppleIDPassword # Securely stores the password
)

# --- Control Stages to Run ---
# Valid stages: "Build", "Copy", "Deploy"
[Parameter(Mandatory=$false)]
[ValidateSet("Build", "Copy", "Deploy")]
[string[]]$Stages = @("Build", "Copy", "Deploy") # Default: run all stages

# Convert SecureString password to plain text only when needed for the remote script
# This is done just before sending it via SSH.
if (-not $AppleIDPassword) {
    $AppleIDPassword = Read-Host -AsSecureString -Prompt "Enter App-Specific Password for '$AppleIDUsername'"
}
# Convert SecureString to plain string for passing to remote script (PowerShell to Bash)
$PlainAppleIDPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($AppleIDPassword))

Write-Host "Starting .NET MAUI iOS app publishing and remote upload process..."
Write-Host "Project (Windows): $ProjectFile"
Write-Host "Configuration: $Configuration"
Write-Host "Target Framework: $TargetFramework"
Write-Host "Runtime Identifier: $RuntimeIdentifier"
Write-Host "Output Directory (Windows): $OutputDir"
Write-Host "Connecting to Mac: $MacUsername@$MacIP"
Write-Host "Mac Upload Script Path: $MacUploadScriptPath"
Write-Host "Stages to run: $($Stages -join ', ')"
Write-Host ""

# On Windows, ensure OpenSSH is available
if (-not (Get-Command ssh -ErrorAction SilentlyContinue)) {
    Write-Error "Error: OpenSSH client (ssh.exe) not found. Please install it via Windows Optional features."
    exit 1
}
if (-not (Get-Command scp -ErrorAction SilentlyContinue)) {
    Write-Error "Error: OpenSSH client (scp.exe) not found. Please install it via Windows Optional features."
    exit 1
}

$IPA_FILE_PATH = "" # To store the path to the generated IPA

# --- Step 1: Clean, Restore, and Build/Publish the iOS app on Windows ---
if ($Stages -contains "Build") {
    Write-Host "Cleaning previous build artifacts on Windows..."
    dotnet clean $ProjectFile -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Clean failed. Exiting."
        exit 1
    }
    Write-Host "Clean complete."
    Write-Host ""

    Write-Host "Restoring NuGet packages on Windows..."
    dotnet restore $ProjectFile
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Restore failed. Exiting."
        exit 1
    }
    Write-Host "Restore complete."
    Write-Host ""

    Write-Host "Publishing .NET MAUI iOS app on Windows (this generates the IPA)..."
    dotnet publish $ProjectFile `
        -c $Configuration `
        -f $TargetFramework `
        -r $RuntimeIdentifier `
        -o $OutputDir `
        -p:MtouchLink=SdkOnly `
        -p:MtouchExtraArgs="--optimize=force-rejected-types-removal" `
        -p:CreatePackage=true
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Publish failed on Windows. Exiting."
        exit 1
    }

    # Find the generated IPA file
    $IpaFile = Get-ChildItem -Path $OutputDir -Filter "*.ipa" -Recurse | Select-Object -First 1
    if (-not $IpaFile) {
        Write-Error "Publish completed, but no IPA file was found in the output directory: $OutputDir. Exiting."
        exit 1
    }
    $IPA_FILE_PATH = $IpaFile.FullName
    Write-Host ""
    Write-Host "Successfully published your .NET MAUI iOS app on Windows!"
    Write-Host "IPA file created at: $IPA_FILE_PATH"
    Write-Host ""
} else {
    # If not building, we assume IPA already exists at the output directory
    Write-Host "Skipping Build stage. Attempting to find existing IPA in $OutputDir..."
    $IpaFile = Get-ChildItem -Path $OutputDir -Filter "*.ipa" -Recurse | Select-Object -First 1
    if (-not $IpaFile) {
        Write-Error "No IPA found to copy/deploy. If skipping build, ensure IPA exists. Exiting."
        exit 1
    }
    $IPA_FILE_PATH = $IpaFile.FullName
    Write-Host "Found existing IPA: $IPA_FILE_PATH"
    Write-Host ""
}

# --- Step 2: Transfer IPA to Mac using SCP ---
$RemoteIpaFileName = [System.IO.Path]::GetFileName($IPA_FILE_PATH)
$RemoteIpaTempPath = "$($MacRemoteTempPath)$($RemoteIpaFileName)" # Full path on Mac for the temporary IPA file

if ($Stages -contains "Copy") {
    if (-not $IPA_FILE_PATH) {
        Write-Error "IPA file path not set. Cannot proceed with Copy stage. Exiting."
        exit 1
    }
    Write-Host "Transferring IPA to Mac ($MacUsername@$MacIP:$RemoteIpaTempPath)..."
    # Note the use of backticks (`) to escape internal double quotes for scp
    $SshCommand = "scp `"$IPA_FILE_PATH`" `"$($MacUsername)@$($MacIP):$($RemoteIpaTempPath)`""
    Invoke-Expression $SshCommand
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to transfer IPA to Mac via SCP. Check SSH connection, IP, and paths. Exiting."
        exit 1
    }
    Write-Host "IPA transferred successfully to Mac to: $RemoteIpaTempPath"
    Write-Host ""
} else {
    Write-Host "Skipping Copy stage."
    Write-Host ""
}

# --- Step 3: Execute upload on Mac via SSH ---
if ($Stages -contains "Deploy") {
    if (-not $IPA_FILE_PATH) {
        Write-Error "IPA file path not set. Cannot proceed with Deploy stage. Exiting."
        exit 1
    }

    Write-Host "Executing remote upload command on Mac via SSH (streaming output for progress)..."
    # Execute the pre-saved Bash script on the Mac, passing parameters to it.
    # The output from the remote script (including altool's verbose output) will be streamed
    # back to your Windows console, providing progress feedback.
    $SshCommand = "ssh $MacUsername@$MacIP ""'$MacUploadScriptPath' '$RemoteIpaTempPath' '$AppleIDUsername' '$PlainAppleIDPassword'"""
    Invoke-Expression $SshCommand
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Remote upload command failed on Mac. Please check the SSH connection and the Mac script's output for errors."
        exit 1
    }

    Write-Host "Remote upload process finished."
} else {
    Write-Host "Skipping Deploy stage."
}

Write-Host "Overall publishing process finished."
```bash
# mac-upload-script.sh
# This script MUST be saved on your Mac and made executable (e.g., chmod +x /path/to/mac-upload-script.sh)
#
# It is executed remotely by the Windows PowerShell script, which passes arguments.
#
# Arguments:
# $1: Full path to the IPA file on the Mac (e.g., /tmp/your_app.ipa)
# $2: Apple ID Username (e.g., your-apple-id@example.com)
# $3: Apple ID App-Specific Password

IPA_TO_UPLOAD="$1"
APPLE_ID_USERNAME="$2"
APPLE_ID_PASSWORD="$3"

echo "Starting IPA upload on Mac (via remote script)..."

# Check for Xcode command-line tools
if ! command -v xcrun &> /dev/null
then
    echo "Error: Xcode command-line tools are not installed or not found in PATH."
    echo "Please install them by running: xcode-select --install"
    exit 1
fi

if [ ! -f "$IPA_TO_UPLOAD" ]; then
    echo "Error: IPA file not found on Mac at: $IPA_TO_UPLOAD. Exiting."
    exit 1
fi

echo "Attempting to upload IPA to App Store Connect using altool..."
# Using -u for username and -p for password.
# --verbose provides detailed output, acting as progress feedback.
xcrun altool --upload-app \
             --file "$IPA_TO_UPLOAD" \
             --username "$APPLE_ID_USERNAME" \
             --password "$APPLE_ID_PASSWORD" \
             --verbose

UPLOAD_EXIT_CODE=$?
if [ $UPLOAD_EXIT_CODE -ne 0 ]; then
    echo "IPA upload failed on Mac. Please check the altool output above for errors."
else
    echo "IPA upload process initiated on Mac. Check App Store Connect for build processing status."
    echo "You can now manage this build in App Store Connect."
fi

# Clean up the transferred IPA file on Mac
echo "Cleaning up transferred IPA file on Mac..."
rm -f "$IPA_TO_UPLOAD"
echo "Mac cleanup complete."
exit $UPLOAD_EXIT_CODE
