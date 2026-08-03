#!/usr/bin/env bash
# Build an .rpm package from a dotnet publish output.
# Usage: build-rpm.sh <publish-dir> <rpm-arch> <out-dir>
#   rpm-arch: x86_64 or aarch64
set -euo pipefail

PUB_DIR="${1:?publish dir required}"
RPM_ARCH="${2:?rpm arch required}"
OUT_DIR="${3:?out dir required}"
VERSION="${VERSION:-1.0.0}"

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"

SIZE="$(stat -c%s "$PUB_DIR/Stratum" 2>/dev/null || echo 0)"
if [ "$SIZE" -lt 100000000 ]; then
  echo "Error: Stratum binary looks truncated ($SIZE bytes)" >&2
  exit 1
fi

TOPDIR="$(mktemp -d)/rpmbuild"
mkdir -p "$TOPDIR"/{BUILD,RPMS,SOURCES,SPECS,SRPMS}

cp "$PUB_DIR/Stratum" "$TOPDIR/SOURCES/Stratum"
chmod +x "$TOPDIR/SOURCES/Stratum"
cp -r "$PUB_DIR/Assets" "$TOPDIR/SOURCES/Assets"
cp "$PUB_DIR"/*.so "$TOPDIR/SOURCES/" 2>/dev/null || true
cp "$ROOT/packaging/linux/stratum.desktop" "$TOPDIR/SOURCES/stratum.desktop"
cp "$ROOT/Stratum.Desktop/Resources/icon.png" "$TOPDIR/SOURCES/stratum.png"
cp "$ROOT/packaging/linux/stratum.spec" "$TOPDIR/SPECS/stratum.spec"

mkdir -p "$OUT_DIR"

# Do NOT strip the .NET single-file binary: strip destroys the embedded bundle
# (137MB binary would shrink to ~11MB and stop working).
rpmbuild --define "_topdir $TOPDIR" \
  --define "version $VERSION" \
  --define "__strip /bin/true" \
  --define "__objdump /bin/true" \
  --target "$RPM_ARCH-redhat-linux" \
  -bb "$TOPDIR/SPECS/stratum.spec" >/dev/null

find "$TOPDIR/RPMS" -name "*.rpm" -exec cp {} "$OUT_DIR/" \;
rm -rf "$(dirname "$TOPDIR")"

echo "Built: $OUT_DIR/stratum-${VERSION}-*.${RPM_ARCH}.rpm"
