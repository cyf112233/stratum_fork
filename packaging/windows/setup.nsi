!include "MUI2.nsh"

!define APPNAME "Stratum"
!define APPFILE "Stratum.exe"
!ifndef VERSION
  !define VERSION "1.0.0"
!endif
!ifndef SOURCEDIR
  !define SOURCEDIR "publish\win-x64"
!endif
!ifndef RID
  !define RID "win-x64"
!endif
!ifndef OUTDIR
  !define OUTDIR "dist"
!endif

Name "${APPNAME}"
OutFile "${OUTDIR}\Stratum-setup-${VERSION}-${RID}.exe"
InstallDir "$PROGRAMFILES64\${APPNAME}"
RequestExecutionLevel admin

!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_LANGUAGE "SimpChinese"
!insertmacro MUI_LANGUAGE "English"

VIProductVersion "1.0.0.0"
VIAddVersionKey "ProductName" "Stratum"
VIAddVersionKey "CompanyName" "cyf112233"
VIAddVersionKey "FileDescription" "Stratum"
VIAddVersionKey "FileVersion" "1.0.0.0"
VIAddVersionKey "ProductVersion" "1.0.0"

Section "Install"
  SetOutPath "$INSTDIR"
  File /r "${SOURCEDIR}\*"

  CreateDirectory "$SMPROGRAMS\${APPNAME}"
  CreateShortCut "$SMPROGRAMS\${APPNAME}\${APPNAME}.lnk" "$INSTDIR\${APPFILE}"
  CreateShortCut "$DESKTOP\${APPNAME}.lnk" "$INSTDIR\${APPFILE}"

  WriteUninstaller "$INSTDIR\uninstall.exe"
SectionEnd

Section "Uninstall"
  Delete "$SMPROGRAMS\${APPNAME}\${APPNAME}.lnk"
  RMDir "$SMPROGRAMS\${APPNAME}"
  Delete "$DESKTOP\${APPNAME}.lnk"
  RMDir /r "$INSTDIR"
SectionEnd
