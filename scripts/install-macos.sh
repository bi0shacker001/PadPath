#!/usr/bin/env sh
set -eu

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
if [ -f "$SCRIPT_DIR/PadPath" ]; then SOURCE_DIR="$SCRIPT_DIR"; else SOURCE_DIR=$(CDPATH= cd -- "$SCRIPT_DIR/.." && pwd); fi
APP_DIR="$HOME/Applications/PadPath.app"
MACOS_DIR="$APP_DIR/Contents/MacOS"

mkdir -p "$MACOS_DIR"
for item in "$SOURCE_DIR"/*; do cp -R "$item" "$MACOS_DIR"/; done
chmod +x "$MACOS_DIR/PadPath"
cat > "$APP_DIR/Contents/Info.plist" <<'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
<key>CFBundleExecutable</key><string>PadPath</string>
<key>CFBundleIdentifier</key><string>io.github.bi0shacker001.padpath</string>
<key>CFBundleName</key><string>PadPath</string>
<key>CFBundlePackageType</key><string>APPL</string>
<key>CFBundleShortVersionString</key><string>0.3.0</string>
</dict></plist>
PLIST

printf '%s\n' "PadPath installed at $APP_DIR"
