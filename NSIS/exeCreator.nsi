; Silent switch /S
; Pass in /Dver=0.00.0 before calling the script to set the version
;  ie: makensis /Dver=0.77.0 .\exeCreator.nsi
; Doc: https://nsis.sourceforge.io/Docs/Chapter4.html
!define EPT "EverythingPT"
!define PT "PowerToys.exe"

LoadLanguageFile "${NSISDIR}\Contrib\Language files\English.nlf"
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

BrandingText "${EPT} v${ver}"
CRCCheck force
FileErrorText "Can't write: $\r$\n$\r$\n$0$\r$\n$\r$\nPowerToys is probaly still running, please close it and retry."
Icon Everything.ico
InstallDir "$LOCALAPPDATA\Microsoft\PowerToys\PowerToys Run\Plugins\Everything"
Name "${EPT}"
OutFile ".\..\bin\${EPT}-${ver}-x64+ARM64.exe"
RequestExecutionLevel user
SetCompressor /SOLID /FINAL lzma

;--------------------------------

;Page directory
Page instfiles

;--------------------------------

Section ""

  ClearErrors
  SetOutPath $INSTDIR
  GetFullPathName $0 "$EXEDIR\"
  GetFullPathName $0 $0
  File /r "${direct}\*"

  IfErrors 0 +5
  SetErrorlevel 2
  IfSilent +2
  MessageBox MB_ICONEXCLAMATION "Unable to install, PowerToys is probaly still running, please close it manually before install."
  Abort
  
SectionEnd