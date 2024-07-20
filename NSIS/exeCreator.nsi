; Silent switch /S
; to check silent mode `start /wait EverythingPT-0.81.0-x64.exe /S`
; and `echo %ERRORLEVEL%` to check if 0 or 2
; Pass in /Dver=0.00.0 /Ddirect=$(TargetDir) before calling the script to set the version
;  ie: makensis /Dver=0.77.0 /Ddirect=bin\x64\Release\Everything\ .\exeCreator.nsi
; Doc: https://nsis.sourceforge.io/Docs/Chapter4.html
!define EPT "EverythingPT"

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
FileErrorText "Can't write: $\r$\n$\r$\n$0$\r$\n$\r$\nPowerToys is probably still running, please close it and retry."
Icon Everything.ico
InstallDir "$LOCALAPPDATA\Microsoft\PowerToys\PowerToys Run\Plugins\Everything"
Name "${EPT}"
OutFile ".\..\bin\${EPT}-${ver}-${platform}.exe"
RequestExecutionLevel user
SetCompressor /SOLID /FINAL lzma
LicenseData "..\LICENSE"

;--------------------------------

Page license
Page instfiles

;--------------------------------

Section ""

  ClearErrors
  SetOutPath $INSTDIR
  GetFullPathName $0 "$EXEDIR\"
  GetFullPathName $0 $0
  File /r "${direct}\*"

  IfErrors 0 +5
  SetErrorlevel 1
  IfSilent +2
  MessageBox MB_ICONEXCLAMATION "Unable to install, PowerToys is probably still running, please close it manually before install."
  Abort
  
SectionEnd