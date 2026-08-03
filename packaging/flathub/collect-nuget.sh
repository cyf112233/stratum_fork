#!/usr/bin/env bash
# Collect all NuGet packages into a reproducible offline tarball for Flathub offline builds.
set -euo pipefail
cd "$(dirname "$0")/../.."

# Pin the runtime version to match the Flathub dotnet10 SDK extension
# (currently SDK 10.0.300 shipping Microsoft.NETCore.App 10.0.8).
RUNTIME_VERSION="${RUNTIME_VERSION:-10.0.8}"

dotnet restore Stratum.Desktop/Stratum.Desktop.csproj -p:NuGetAudit=false 2>&1 | tail -1

OUT="/tmp/nuget-offline"
rm -rf "$OUT"
mkdir -p "$OUT"

# RID-specific runtime/app/host packs are implicit SDK dependencies of a
# self-contained publish; they are not always listed in project.assets.json
# (older SDKs, e.g. 10.0.110, skip them during restore). Take them from the
# local cache when available, otherwise fetch them straight from nuget.org.
RID_PACKS=(
  microsoft.netcore.app.runtime.linux-x64
  microsoft.netcore.app.runtime.linux-arm64
  microsoft.aspnetcore.app.runtime.linux-x64
  microsoft.aspnetcore.app.runtime.linux-arm64
  microsoft.netcore.app.host.linux-x64
  microsoft.netcore.app.host.linux-arm64
)
for p in "${RID_PACKS[@]}"; do
  nupkg="$HOME/.nuget/packages/$p/$RUNTIME_VERSION/$p.$RUNTIME_VERSION.nupkg"
  if [ -f "$nupkg" ]; then
    cp -f "$nupkg" "$OUT/"
  else
    curl -fsSL -o "$OUT/$p.$RUNTIME_VERSION.nupkg" \
      "https://api.nuget.org/v3-flatcontainer/$p/$RUNTIME_VERSION/$p.$RUNTIME_VERSION.nupkg"
  fi
done

python3 - <<'EOF'
import json, os, shutil, sys

want = []

for proj in ["Stratum.Desktop/obj/project.assets.json", "Stratum.Core/obj/project.assets.json"]:
    with open(proj) as f:
        data = json.load(f)
    want.extend(sorted(data.get("libraries", {})))

missing = []
for pkg in sorted(set(want)):
    name, _, version = pkg.rpartition("/")
    src = os.path.expanduser(f"~/.nuget/packages/{name.lower()}/{version}")
    # NuGet's global cache stores the .nupkg under the lower-cased package id.
    nupkg = os.path.join(src, f"{name.lower()}.{version}.nupkg")

    if os.path.exists(nupkg):
        shutil.copy(nupkg, "/tmp/nuget-offline")
    else:
        missing.append(pkg)

if missing:
    print("WARNING: nupkgs missing from local NuGet cache:", file=sys.stderr)
    for m in missing:
        print("  " + m, file=sys.stderr)

print(f"collected {len(os.listdir('/tmp/nuget-offline'))} nupkgs")
EOF

mkdir -p dist
tar --sort=name -cf - -C /tmp/nuget-offline . | gzip -n > dist/nuget-offline.tar.gz
echo "Built: dist/nuget-offline.tar.gz ($(du -h dist/nuget-offline.tar.gz | cut -f1))"
