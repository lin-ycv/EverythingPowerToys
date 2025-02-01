!include "MUI2.nsh"
!define EPT "EverythingPT"
!define SMWCUE "Software\Microsoft\Windows\CurrentVersion\Uninstall\EverythingPowerToys"
;--------------------------------
;General
  Name "${EPT}"
  BrandingText "v${ver} ${platform}"
  SetCompressor zlib
  OutFile ".\..\bin\${EPT}-${ver}-${platform}.exe"
  Unicode True
  RequestExecutionLevel user
  SetOverwrite ifnewer
  InstallDir "$LOCALAPPDATA\Microsoft\PowerToys\PowerToys Run\Plugins\Everything"
  FileErrorText "Can't access: $\r$\n$\r$\n$0$\r$\n$\r$\nPowerToys is probably still running, please close it and retry."
;--------------------------------
;Interface Settings
  !define MUI_ICON "Everything.ico"
  !define MUI_ABORTWARNING
;--------------------------------
;Pages
  !insertmacro MUI_PAGE_LICENSE "..\LICENSE"
  !insertmacro MUI_PAGE_INSTFILES

  !insertmacro MUI_LANGUAGE "English"
;--------------------------------
;Version Information
  VIProductVersion "${ver}.0"
  VIAddVersionKey /LANG=${LANG_ENGLISH} "ProductName" "${EPT} Setup"
  VIAddVersionKey /LANG=${LANG_ENGLISH} "CompanyName" "lin-ycv"
  VIAddVersionKey /LANG=${LANG_ENGLISH} "LegalCopyright" "lin-ycv"
  VIAddVersionKey /LANG=${LANG_ENGLISH} "FileDescription" "Everything search plugin for PowerToys Run"
  VIAddVersionKey /LANG=${LANG_ENGLISH} "FileVersion" "${ver}"
  VIAddVersionKey /LANG=${LANG_ENGLISH} "ProductVersion" "${ver}"
;--------------------------------
Section ""
  SetOutPath $INSTDIR
  File /r "${direct}\*"

  WriteRegStr HKCU "${SMWCUE}" "DisplayName" "${EPT} (${platform})"
  WriteRegStr HKCU "${SMWCUE}" "DisplayVersion" "${ver}"
  WriteRegStr HKCU "${SMWCUE}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU "${SMWCUE}" "Publisher" "Lin-ycv"

  IfErrors 0 +5
  SetErrorlevel 1
  IfSilent +2
  MessageBox MB_ICONEXCLAMATION "Unable to install, PowerToys is probably still running, please close it manually before install."
  Abort
  
  SetErrorlevel 0
SectionEnd