Name:           stratum
Version:        1.0.0
Release:        1
Summary:        Two-factor authenticator (TOTP/HOTP/Steam/mOTP/Yandex)
License:        GPL-3.0-or-later
URL:            https://github.com/stratumauth/app

%description
Desktop client for Stratum, supporting encrypted backups, categories and
QR code import.

%install
mkdir -p %{buildroot}/usr/bin
mkdir -p %{buildroot}/usr/lib/stratum/Assets
mkdir -p %{buildroot}/usr/share/applications
mkdir -p %{buildroot}/usr/share/icons/hicolor/512x512/apps

cp %{_sourcedir}/Stratum %{buildroot}/usr/lib/stratum/stratum
chmod +x %{buildroot}/usr/lib/stratum/stratum
cp -r %{_sourcedir}/Assets/. %{buildroot}/usr/lib/stratum/Assets/
cp %{_sourcedir}/*.so %{buildroot}/usr/lib/stratum/ 2>/dev/null || true

cat > %{buildroot}/usr/bin/stratum <<'EOF'
#!/bin/sh
exec /usr/lib/stratum/stratum "$@"
EOF
chmod +x %{buildroot}/usr/bin/stratum

cp %{_sourcedir}/stratum.desktop %{buildroot}/usr/share/applications/stratum.desktop
cp %{_sourcedir}/stratum.png %{buildroot}/usr/share/icons/hicolor/512x512/apps/stratum.png

%files
/usr/bin/stratum
/usr/lib/stratum/stratum
/usr/lib/stratum/Assets
/usr/share/applications/stratum.desktop
/usr/share/icons/hicolor/512x512/apps/stratum.png
