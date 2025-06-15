#!/bin/bash

# Define variables
PROJECT_FILE="../src/LoanCalculator/LoanCalculatorMaui.csproj" # Updated path
CONFIGURATION="Release"
TARGET_FRAMEWORK="net9.0-ios"
RUNTIME_IDENTIFIER="ios-arm64"
OUTPUT_DIR="./bin/${CONFIGURATION}/${TARGET_FRAMEWORK}/publish/"

# --- IMPORTANT: Replace these with your actual signing details ---
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
# --- End of IMPORTANT section ---

echo "Starting .NET MAUI iOS app publishing process..."
echo "Project: ${PROJECT_FILE}"
echo "Configuration: ${CONFIGURATION}"
echo "Target Framework: ${TARGET_FRAMEWORK}"
echo "Runtime Identifier: ${RUNTIME_IDENTIFIER}"
echo "Output Directory: ${OUTPUT_DIR}"
echo "Signing Identity: ${IOS_SIGNING_IDENTITY}"
echo "Provisioning Profile: ${PROVISIONING_PROFILE_NAME}"
echo ""

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
if [ -d "${OUTPUT_DIR}" ]; then
    IPA_FILE=$(find "${OUTPUT_DIR}" -name "*.ipa" -print -quit)
    if [ -n "${IPA_FILE}" ]; then
        echo ""
        echo "Successfully published your .NET MAUI iOS app!"
        echo "IPA file created at: ${IPA_FILE}"
        echo "You can now upload this IPA to App Store Connect or distribute it via other means."
    else
        echo ""
        echo "Publish completed, but no IPA file was found in the output directory: ${OUTPUT_DIR}"
        echo "Please check the logs above for any errors during the packaging process."
    fi
else
    echo ""
    echo "Publish directory does not exist: ${OUTPUT_DIR}"
    echo "Please check the logs above for errors."
fi

echo "Publishing process finished."
