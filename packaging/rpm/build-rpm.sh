#!/usr/bin/env bash
set -eu
if (set -o 2>/dev/null | grep -q pipefail); then
  set -o pipefail
fi

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
PKG_DIR="${ROOT_DIR}/packaging/rpm"

NAME="medproject"
VERSION="${1:-1.0.0}"

PUBLISH_DIR="${ROOT_DIR}/publish_linux"

if [[ ! -d "${PUBLISH_DIR}" ]]; then
  echo "ERROR: '${PUBLISH_DIR}' not found."
  echo "Build/publish for Linux first and ensure 'publish_linux' exists."
  exit 1
fi

if [[ ! -f "${PUBLISH_DIR}/MedProject" ]]; then
  echo "ERROR: '${PUBLISH_DIR}/MedProject' not found."
  echo "Expected a self-contained publish with an executable named 'MedProject'."
  exit 1
fi

if [[ ! -x "${PUBLISH_DIR}/MedProject" ]]; then
  chmod +x "${PUBLISH_DIR}/MedProject" || true
fi

if ! command -v rpmbuild >/dev/null 2>&1; then
  echo "ERROR: rpmbuild not found. Install rpm-build."
  echo "Example: sudo dnf install rpm-build"
  exit 1
fi

RPMTOP="${RPMTOP:-${HOME}/rpmbuild}"
for d in BUILD BUILDROOT RPMS SOURCES SPECS SRPMS; do
  mkdir -p "${RPMTOP}/${d}"
done

WORKDIR="$(mktemp -d)"
trap 'rm -rf "${WORKDIR}"' EXIT

STAGE="${WORKDIR}/${NAME}-${VERSION}"
mkdir -p "${STAGE}"

cp -a "${PUBLISH_DIR}" "${STAGE}/publish_linux"
cp -a "${PKG_DIR}/medproject.desktop" "${STAGE}/medproject.desktop"
cp -a "${PKG_DIR}/INSTALLATION_RedOS.md" "${STAGE}/INSTALLATION_RedOS.md"
cp -a "${PKG_DIR}/iconApp.png" "${STAGE}/medproject.png"
cp -a "${PKG_DIR}/create-desktop-shortcut.sh" "${STAGE}/create-desktop-shortcut.sh"

# Prefer project icon; build script copies a known file from repo.
ICON_SRC="${ROOT_DIR}/MedProject/Assets/iconApp.ico"
if [[ -f "${ICON_SRC}" ]]; then
  cp -a "${ICON_SRC}" "${STAGE}/medproject.ico"
else
  echo "WARN: icon not found at '${ICON_SRC}'."
  # Create an empty placeholder to keep spec happy.
  : > "${STAGE}/medproject.ico"
fi

tar -C "${WORKDIR}" -czf "${RPMTOP}/SOURCES/${NAME}-${VERSION}.tar.gz" "${NAME}-${VERSION}"
cp -a "${PKG_DIR}/medproject.spec" "${RPMTOP}/SPECS/${NAME}.spec"

rpmbuild -ba "${RPMTOP}/SPECS/${NAME}.spec" \
  --define "_topdir ${RPMTOP}" \
  --define "pkgver ${VERSION}"

echo
echo "Built RPMs:"
find "${RPMTOP}/RPMS" -name "${NAME}-${VERSION}-*.rpm" -print || true

