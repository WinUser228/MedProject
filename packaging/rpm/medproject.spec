Name:           medproject
Version:        %{?pkgver}%{!?pkgver:1.0.0}
Release:        1%{?dist}
Summary:        MedProject hospital statistics desktop application

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

# Launcher symlink
install -d %{buildroot}%{_bindir}
ln -sf /opt/medproject/MedProject %{buildroot}%{_bindir}/medproject

# Desktop entry
install -d %{buildroot}%{_datadir}/applications
install -m 0644 medproject.desktop %{buildroot}%{_datadir}/applications/medproject.desktop

# Icon (optional, but included in Source0 by build script)
install -d %{buildroot}%{_datadir}/pixmaps
install -m 0644 medproject.ico %{buildroot}%{_datadir}/pixmaps/medproject.ico

%files
/opt/medproject
%{_bindir}/medproject
%{_datadir}/applications/medproject.desktop
%{_datadir}/pixmaps/medproject.ico

%post
if command -v update-desktop-database >/dev/null 2>&1; then
  update-desktop-database %{_datadir}/applications >/dev/null 2>&1 || true
fi

%postun
if command -v update-desktop-database >/dev/null 2>&1; then
  update-desktop-database %{_datadir}/applications >/dev/null 2>&1 || true
fi

