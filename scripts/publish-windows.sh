#!/usr/bin/env bash
set -euo pipefail

script_directory="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repository_root="$(cd -- "${script_directory}/.." && pwd)"
output_directory="${repository_root}/dist/windows-x64"
version="${MULTIBLOXY_VERSION:-2.0.0}"

if [[ ! "${version}" =~ ^[0-9A-Za-z][0-9A-Za-z.+-]*$ ]]; then
  printf '%s\n' "MULTIBLOXY_VERSION contains unsupported characters: ${version}" >&2
  exit 2
fi

cd "${repository_root}"
if [[ "${output_directory}" != "${repository_root}/dist/windows-x64" ]]; then
  printf '%s\n' "Refusing to clean an unexpected output path: ${output_directory}" >&2
  exit 2
fi

rm -rf -- "${output_directory}"
mkdir -p "${output_directory}"

dotnet restore MultiBloxy/MultiBloxy.csproj --runtime win-x64
dotnet publish MultiBloxy/MultiBloxy.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  --no-restore \
  --output "${output_directory}" \
  -p:Version="${version}" \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:PublishTrimmed=false \
  -p:DebugType=None \
  -p:DebugSymbols=false

cp LICENSE "${output_directory}/LICENSE.txt"
cp docs/PORTABLE-README.txt "${output_directory}/README.txt"

cd "${output_directory}"
hash_value="$(sha256sum MultiBloxy.exe | awk '{print $1}')"
git_commit="$(git -C "${repository_root}" rev-parse --verify HEAD)"
built_at_utc="$(date -u +"%Y-%m-%dT%H:%M:%SZ")"

printf '%s  %s\n' "${hash_value}" "MultiBloxy.exe" > MultiBloxy.exe.sha256
printf '{\n  "name": "MultiBloxy",\n  "version": "%s",\n  "gitCommit": "%s",\n  "targetFramework": "net10.0-windows",\n  "runtimeIdentifier": "win-x64",\n  "selfContained": true,\n  "singleFile": true,\n  "sha256": "%s",\n  "builtAtUtc": "%s"\n}\n' \
  "${version}" \
  "${git_commit}" \
  "${hash_value}" \
  "${built_at_utc}" \
  > release-manifest.json

printf '%s\n' "Windows package created in: ${output_directory}"
printf '%s\n' "Executable: ${output_directory}/MultiBloxy.exe"
