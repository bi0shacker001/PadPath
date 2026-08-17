#!/usr/bin/env sh
set -eu

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
if [ -f "$SCRIPT_DIR/PadPath" ]; then SOURCE_DIR="$SCRIPT_DIR"; else SOURCE_DIR=$(CDPATH= cd -- "$SCRIPT_DIR/.." && pwd); fi
INSTALL_DIR="${XDG_DATA_HOME:-$HOME/.local/share}/padpath"
BIN_DIR="$HOME/.local/bin"

mkdir -p "$INSTALL_DIR" "$BIN_DIR"
for item in "$SOURCE_DIR"/*; do
  [ "$item" = "$INSTALL_DIR" ] || cp -R "$item" "$INSTALL_DIR"/
done
chmod +x "$INSTALL_DIR/PadPath"
ln -sf "$INSTALL_DIR/PadPath" "$BIN_DIR/padpath"

printf '%s\n' "PadPath installed. Run $BIN_DIR/padpath --setup"
