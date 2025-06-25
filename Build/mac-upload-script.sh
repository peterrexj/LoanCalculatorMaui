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