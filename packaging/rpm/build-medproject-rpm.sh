#!/usr/bin/env bash
set -eu
if (set -o 2>/dev/null | grep -q pipefail); then
  set -o pipefail
fi

usage() {
  cat <<EOF
Usage: bash packaging/rpm/build-medproject-rpm.sh [--publish] [--configuration Release] [--runtime linux-x64] [--output publish_linux] [VERSION]

By default only builds RPM from existing publish_linux/ (same as build-rpm.sh).

Options:
  --publish             Run 'dotnet publish' before rpmbuild
  --configuration NAME  dotnet -c (default: Release)
  --runtime RID         dotnet -r (default: linux-x64)
  --output DIR          publish output folder relative to repo root (default: publish_linux)

VERSION                 Package version passed to rpmbuild (default: 1.0.0)

Examples:
  bash packaging/rpm/build-medproject-rpm.sh 1.0.1
  bash packaging/rpm/build-medproject-rpm.sh --publish 1.0.1
EOF
}

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
DO_PUBLISH=0
DOTNET_CONFIGURATION="Release"
DOTNET_RUNTIME="linux-x64"
PUBLISH_OUTPUT="publish_linux"
VERSION="1.0.0"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --publish)
      DO_PUBLISH=1
      shift
      ;;
    --configuration|--config|-c)
      DOTNET_CONFIGURATION="${2:?missing value after $1}"
      shift 2
      ;;
    --runtime|-r)
      DOTNET_RUNTIME="${2:?missing value after $1}"
      shift 2
      ;;
    --output|-o)
      PUBLISH_OUTPUT="${2:?missing value after $1}"
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    -*)
      echo "Unknown option: $1"
      usage
      exit 2
      ;;
    *)
      VERSION="$1"
      shift
      ;;
  esac
done

if [[ "${DO_PUBLISH}" -eq 1 ]]; then
  if ! command -v dotnet >/dev/null 2>&1; then
    echo "ERROR: dotnet not found in PATH."
    exit 1
  fi

  OUT_ABS="${ROOT_DIR}/${PUBLISH_OUTPUT}"
  CSproj="${ROOT_DIR}/MedProject/MedProject.csproj"

  if [[ ! -f "${CSproj}" ]]; then
    echo "ERROR: project file not found: ${CSproj}"
    exit 1
  fi

  echo "Publishing MedProject → ${OUT_ABS}"
  dotnet publish "${CSproj}" \
    -c "${DOTNET_CONFIGURATION}" \
    -r "${DOTNET_RUNTIME}" \
    --self-contained true \
    -o "${OUT_ABS}"
fi

exec bash "${ROOT_DIR}/packaging/rpm/build-rpm.sh" "${VERSION}"
