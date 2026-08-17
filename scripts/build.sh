#!/usr/bin/env bash
set -euo pipefail

script_directory="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repository_root="$(cd -- "${script_directory}/.." && pwd)"
configuration="${1:-Release}"

cd "${repository_root}"
dotnet restore MultiBloxy.sln
dotnet build MultiBloxy.sln --configuration "${configuration}" --no-restore
