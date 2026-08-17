#!/usr/bin/env bash
set -euo pipefail

script_directory="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repository_root="$(cd -- "${script_directory}/.." && pwd)"

cd "${repository_root}"
dotnet restore tests/MultiBloxy.Core.Tests/MultiBloxy.Core.Tests.csproj
dotnet test tests/MultiBloxy.Core.Tests/MultiBloxy.Core.Tests.csproj \
  --configuration Release \
  --no-restore
