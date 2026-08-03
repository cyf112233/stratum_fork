#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"

dotnet publish Stratum.Desktop/Stratum.Desktop.csproj -c Release -r linux-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist/linux-x64

echo ""
echo "Built: dist/linux-x64/Stratum"
echo "Run it with: ./dist/linux-x64/Stratum"
rm -f dist/linux-x64/*.pdb
