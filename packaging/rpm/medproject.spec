Name:           medproject
Version:        %{?pkgver}%{!?pkgver:1.0.0}
Release:        1%{?dist}
Summary:        Приложение для ведения коечного учёта

License:        Proprietary
URL:            https://example.invalid/medproject
Source0:        %{name}-%{version}.tar.gz

BuildArch:      x86_64

Requires(pre):  /usr/bin/bash

# RedOS repos may not ship liblttng-ust.so.0 (needed only for .NET diagnostics).
# The app runs fine without diagnostics, so we exclude this auto-generated requirement.
%global __requires_exclude ^liblttng-ust\\.so\\..*\\(64bit\\)$

%description
MedProject is an Avalonia/.NET desktop application for hospital statistics.

%prep
%setup -q

%install
rm -rf %{buildroot}

# App files
install -d %{buildroot}/opt/medproject
cp -a publish_linux/. %{buildroot}/opt/medproject/

# Installation instructions (shipped with app files)
install -m 0644 INSTALLATION_RedOS.md %{buildroot}/opt/medproject/INSTALLATION_RedOS.md

# Helper script to create Desktop shortcut for current user
install -m 0755 create-desktop-shortcut.sh %{buildroot}/opt/medproject/create-desktop-shortcut.sh

# Also ship docs in standard location
install -d %{buildroot}%{_docdir}/%{name}
install -m 0644 INSTALLATION_RedOS.md %{buildroot}%{_docdir}/%{name}/INSTALLATION_RedOS.md

# Launcher
install -d %{buildroot}%{_bindir}
cat > %{buildroot}%{_bindir}/medproject <<'EOF'
#!/usr/bin/env bash
set -eu
exec /opt/medproject/MedProject "$@"
EOF
chmod 0755 %{buildroot}%{_bindir}/medproject

# Desktop entry
install -d %{buildroot}%{_datadir}/applications
install -m 0644 medproject.desktop %{buildroot}%{_datadir}/applications/medproject.desktop

# Icon for desktop environments (included in Source0 by build script)
install -d %{buildroot}%{_datadir}/icons/hicolor/256x256/apps
install -m 0644 medproject.png %{buildroot}%{_datadir}/icons/hicolor/256x256/apps/medproject.png

# Legacy icon location (some environments still look here)
install -d %{buildroot}%{_datadir}/pixmaps
install -m 0644 medproject.ico %{buildroot}%{_datadir}/pixmaps/medproject.ico

%files
/opt/medproject
%{_bindir}/medproject
%{_datadir}/applications/medproject.desktop
%{_datadir}/icons/hicolor/256x256/apps/medproject.png
%{_datadir}/pixmaps/medproject.ico
%{_docdir}/%{name}/INSTALLATION_RedOS.md

%changelog
* Mon Apr 20 2026 Angela <angela@example.invalid> - %{version}-%{release}
- Initial RPM packaging

%post
if command -v update-desktop-database >/dev/null 2>&1; then
  update-desktop-database %{_datadir}/applications >/dev/null 2>&1 || true
fi
if command -v gtk-update-icon-cache >/dev/null 2>&1; then
  gtk-update-icon-cache -f %{_datadir}/icons/hicolor >/dev/null 2>&1 || true
fi

%postun
if command -v update-desktop-database >/dev/null 2>&1; then
  update-desktop-database %{_datadir}/applications >/dev/null 2>&1 || true
fi
if command -v gtk-update-icon-cache >/dev/null 2>&1; then
  gtk-update-icon-cache -f %{_datadir}/icons/hicolor >/dev/null 2>&1 || true
fi

# Remove Desktop shortcut created by our helper script (only on full erase)
if [ "$1" -eq 0 ]; then
  for home in /home/*; do
    [ -d "$home" ] || continue
    for desk in "$home/Desktop" "$home/Рабочий стол"; do
      f="$desk/medproject.desktop"
      if [ -f "$f" ] && grep -q '^X-MedProject-Shortcut=true' "$f" 2>/dev/null; then
        rm -f "$f" 2>/dev/null || true
      fi
    done
  done
fi

