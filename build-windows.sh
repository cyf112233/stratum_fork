#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"

dotnet publish Stratum.Desktop/Stratum.Desktop.csproj -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -o dist/win-x64

rm -f dist/win-x64/*.pdb

echo ""
echo "Built: dist/win-x64/Stratum.exe"
