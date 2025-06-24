#!/bin/bash

# Define variables
PROJECT_FILE="../src/LoanCalculator/LoanCalculatorMaui.csproj" # Updated path
CONFIGURATION="Release"
TARGET_FRAMEWORK="net9.0-ios"
RUNTIME_IDENTIFIER="ios-arm64"
OUTPUT_DIR="./bin/${CONFIGURATION}/${TARGET_FRAMEWORK}/publish/"

# --- IMPORTANT: Replace these with your actual signing and Apple ID details ---
# To find your iOS Signing Identity:
# Open Xcode, go to Xcode -> Settings (or Preferences) -> Accounts.
# Select your Apple ID, then 'Manage Certificates...'.
# The identity typically looks like "Apple Distribution: Your Name (XXXXXXXXXX)"
IOS_SIGNING_IDENTITY="Apple Distribution: Peter Joseph (5PNCUV7LZ5)" # e.g., "Apple Distribution: John Doe (ABCDEFGHIJ)"

# To find your Provisioning Profile Name:
# Open Xcode, go to Xcode -> Settings (or Preferences) -> Accounts.
# Select your Apple ID, then 'Download Manual Profiles'.
# Note the name of the provisioning profile you intend to use for distribution.
PROVISIONING_PROFILE_NAME="LoanAffordabilityCalculator" # e.g., "LoanCalculator App Store Profile"

# For App Store Connect Upload:
# Use your Apple ID email address
APPLE_ID_USERNAME="your-apple-id@example.com" # <--- REPLACE WITH YOUR APPLE ID EMAIL
# Use an App-Specific Password, NOT your main Apple ID password for automation
# Generate one at appleid.apple.com -> Security -> Generate Password
APPLE_ID_PASSWORD="your-app-specific-password" # <--- REPLACE WITH YOUR APP-SPECIFIC PASSWORD
# --- End of IMPORTANT section ---

echo "Starting .NET MAUI iOS app publishing and upload process..."
echo "Project: ${PROJECT_FILE}"
echo "Configuration: ${CONFIGURATION}"
echo "Target Framework: ${TARGET_FRAMEWORK}"
echo "Runtime Identifier: ${RUNTIME_IDENTIFIER}"
echo "Output Directory: ${OUTPUT_DIR}"
echo "Signing Identity: ${IOS_SIGNING_IDENTITY}"
echo "Provisioning Profile: ${PROVISIONING_PROFILE_NAME}"
echo "Apple ID Username (for upload): ${APPLE_ID_USERNAME}"
echo ""

# Check for Xcode command-line tools
if ! command -v xcrun &> /dev/null
then
    echo "Error: Xcode command-line tools are not installed or not found in PATH."
    echo "Please install them by running: xcode-select --install"
    exit 1
fi

# Clean previous build artifacts
echo "Cleaning previous build artifacts..."
dotnet clean "${PROJECT_FILE}" -c "${CONFIGURATION}" || { echo "Clean failed. Exiting."; exit 1; }
echo "Clean complete."
echo ""

# Restore NuGet packages
echo "Restoring NuGet packages..."
dotnet restore "${PROJECT_FILE}" || { echo "Restore failed. Exiting."; exit 1; }
echo "Restore complete."
echo ""

# Publish the iOS app
# -c: Configuration (Release for publishing)
# -f: Target Framework
# -r: Runtime Identifier (specific to architecture)
# -o: Output directory
# -p:MtouchLink=SdkOnly: Links only the SDK assemblies, reducing app size.
# -p:MtouchExtraArgs=--optimize=force-rejected-types-removal: Further optimizes by removing unused types.
# -p:CreatePackage=true: Tells MSBuild to create an IPA package.
# -p:CodesignKey: Your iOS Signing Identity for code signing.
# -p:ProvisioningProfile: The name of your provisioning profile.
# For App Store Connect upload, you'll typically need an 'App Store' or 'Ad Hoc' distribution profile.
echo "Publishing .NET MAUI iOS app..."
dotnet publish "${PROJECT_FILE}" \
    -c "${CONFIGURATION}" \
    -f "${TARGET_FRAMEWORK}" \
    -r "${RUNTIME_IDENTIFIER}" \
    -o "${OUTPUT_DIR}" \
    -p:MtouchLink=SdkOnly \
    -p:MtouchExtraArgs="--optimize=force-rejected-types-removal" \
    -p:CreatePackage=true \
    -p:CodesignKey="${IOS_SIGNING_IDENTITY}" \
    -p:ProvisioningProfile="${PROVISIONING_PROFILE_NAME}" \
    || { echo "Publish failed. Exiting."; exit 1; }

# Check if the IPA was created
IPA_FILE=""
if [ -d "${OUTPUT_DIR}" ]; then
    IPA_FILE=$(find "${OUTPUT_DIR}" -name "*.ipa" -print -quit)
fi

if [ -n "${IPA_FILE}" ]; then
    echo ""
    echo "Successfully published your .NET MAUI iOS app!"
    echo "IPA file created at: ${IPA_FILE}"
    echo ""

    # --- Upload to App Store Connect ---
    echo "Attempting to upload IPA to App Store Connect using altool..."
    # You might need to accept Xcode's license agreement if this is the first time using altool
    # sudo xcodebuild -license accept

    # Using -u for username and -p for password.
    # --verbose can be added for more detailed output.
    xcrun altool --upload-app \
                 --file "${IPA_FILE}" \
                 --username "${APPLE_ID_USERNAME}" \
                 --password "${APPLE_ID_PASSWORD}" \
                 --verbose \
                 || { echo "IPA upload failed. Please check the altool output for errors."; exit 1; }

    echo ""
    echo "IPA upload process initiated. Check App Store Connect for build processing status."
    echo "You can now manage this build in App Store Connect."

else
    echo ""
    echo "Publish completed, but no IPA file was found in the output directory: ${OUTPUT_DIR}"
    echo "Please check the dotnet publish logs above for any errors during the packaging process."
fi

echo "Publishing and upload process finished."
