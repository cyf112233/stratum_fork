#!/usr/bin/env bash
# Build a .deb package from a dotnet publish output.
# Usage: build-deb.sh <publish-dir> <rid> <out-dir>
#   rid: linux-x64 -> amd64, linux-arm64 -> arm64
set -euo pipefail

PUB_DIR="${1:?publish dir required}"
RID="${2:?rid required}"
OUT_DIR="${3:?out dir required}"
VERSION="${VERSION:-1.0.0}"

case "$RID" in
  linux-x64) ARCH="amd64" ;;
  linux-arm64) ARCH="arm64" ;;
  *) echo "Unsupported rid: $RID" >&2; exit 1 ;;
esac

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"

SIZE="$(stat -c%s "$PUB_DIR/Stratum" 2>/dev/null || echo 0)"
if [ "$SIZE" -lt 100000000 ]; then
  echo "Error: Stratum binary looks truncated ($SIZE bytes)" >&2
  exit 1
fi

PKG_DIR="$(mktemp -d)"

mkdir -p "$PKG_DIR/usr/bin" \
         "$PKG_DIR/usr/lib/stratum/Assets" \
         "$PKG_DIR/usr/share/applications" \
         "$PKG_DIR/usr/share/icons/hicolor/512x512/apps" \
         "$PKG_DIR/DEBIAN"

cp -r "$PUB_DIR/Assets/." "$PKG_DIR/usr/lib/stratum/Assets/"
cp "$PUB_DIR/Stratum" "$PKG_DIR/usr/lib/stratum/stratum"
chmod +x "$PKG_DIR/usr/lib/stratum/stratum"
cp "$PUB_DIR"/*.so "$PKG_DIR/usr/lib/stratum/" 2>/dev/null || true

cat > "$PKG_DIR/usr/bin/stratum" <<'EOF'
#!/bin/sh
exec /usr/lib/stratum/stratum "$@"
EOF
chmod +x "$PKG_DIR/usr/bin/stratum"

cp "$ROOT/packaging/linux/stratum.desktop" "$PKG_DIR/usr/share/applications/stratum.desktop"
cp "$ROOT/Stratum.Desktop/Resources/icon.png" "$PKG_DIR/usr/share/icons/hicolor/512x512/apps/stratum.png"

cat > "$PKG_DIR/DEBIAN/control" <<EOF
Package: stratum
Version: $VERSION
Section: utils
Priority: optional
Architecture: $ARCH
Maintainer: Stratum Desktop <noreply@localhost>
Description: Two-factor authenticator (TOTP/HOTP/Steam/mOTP/Yandex)
 Desktop client for Stratum, supporting encrypted backups, categories
 and QR code import.
EOF

mkdir -p "$OUT_DIR"
dpkg-deb --build --root-owner-group "$PKG_DIR" "$OUT_DIR/stratum-${VERSION}_${ARCH}.deb" >/dev/null
rm -rf "$PKG_DIR"

echo "Built: $OUT_DIR/stratum-${VERSION}_${ARCH}.deb"
