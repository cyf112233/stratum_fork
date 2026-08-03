#!/usr/bin/env bash
# Build an AppImage from a dotnet publish output (needs network to fetch linuxdeploy).
# Usage: build-appimage.sh <publish-dir> <appimage-arch> <out-dir>
#   appimage-arch: x86_64 or aarch64
set -euo pipefail

PUB_DIR="$(realpath "${1:?publish dir required}")"
APP_ARCH="${2:?appimage arch required}"
OUT_DIR="${3:?out dir required}"
mkdir -p "$OUT_DIR"
OUT_DIR="$(realpath "$OUT_DIR")"
VERSION="${VERSION:-1.0.0}"

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"

SIZE="$(stat -c%s "$PUB_DIR/Stratum" 2>/dev/null || echo 0)"
if [ "$SIZE" -lt 100000000 ]; then
  echo "Error: Stratum binary looks truncated ($SIZE bytes)" >&2
  exit 1
fi

TMP="$(mktemp -d)"

case "$APP_ARCH" in
  x86_64) LD_NAME="linuxdeploy-x86_64" ;;
  aarch64) LD_NAME="linuxdeploy-aarch64" ;;
  *) echo "AppImage not supported for arch: $APP_ARCH" >&2; exit 1 ;;
esac

cd "$TMP"
curl -fsSL -o linuxdeploy.AppImage \
  "https://github.com/linuxdeploy/linuxdeploy/releases/download/continuous/${LD_NAME}.AppImage"
curl -fsSL -o linuxdeploy-plugin-appimage.AppImage \
  "https://github.com/linuxdeploy/linuxdeploy-plugin-appimage/releases/download/continuous/linuxdeploy-plugin-appimage-${APP_ARCH}.AppImage"
chmod +x linuxdeploy.AppImage linuxdeploy-plugin-appimage.AppImage

# CI has no FUSE: extract instead of running the AppImage
./linuxdeploy.AppImage --appimage-extract >/dev/null

APP_DIR="$TMP/AppDir"
mkdir -p "$APP_DIR/usr/bin" \
         "$APP_DIR/usr/share/applications" \
         "$APP_DIR/usr/share/icons/hicolor/512x512/apps"

cp "$PUB_DIR/Stratum" "$APP_DIR/usr/bin/stratum"
chmod +x "$APP_DIR/usr/bin/stratum"
cp -r "$PUB_DIR/Assets" "$APP_DIR/usr/bin/Assets"
cp "$PUB_DIR"/*.so "$APP_DIR/usr/bin/" 2>/dev/null || true
cp "$ROOT/packaging/linux/stratum.desktop" "$APP_DIR/usr/share/applications/stratum.desktop"
cp "$ROOT/Stratum.Desktop/Resources/icon.png" "$APP_DIR/usr/share/icons/hicolor/512x512/apps/stratum.png"

export PATH="$TMP:$PATH"
# CI has no FUSE: force linuxdeploy to extract-and-run the plugin AppImage.
export APPIMAGE_EXTRACT_AND_RUN=1
mkdir -p "$OUT_DIR"
VERSION="$VERSION" ./squashfs-root/AppRun --appdir "$APP_DIR" --output appimage >/dev/null

mv Stratum-*.AppImage "$OUT_DIR/" 2>/dev/null || mv *.AppImage "$OUT_DIR/" 2>/dev/null || true
rm -rf "$TMP"

echo "Built: $OUT_DIR/Stratum-${VERSION}-${APP_ARCH}.AppImage"
