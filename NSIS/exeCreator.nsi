; Silent switch /S
; to check silent mode `start /wait EverythingPT-0.81.0-x64.exe /S`
; and `echo %ERRORLEVEL%` to check if 0 or 2
; Pass in /Dver=0.00.0 /Ddirect=$(TargetDir) before calling the script to set the version
;  ie: makensis /Dver=0.77.0 /Ddirect=bin\x64\Release\Everything\ .\exeCreator.nsi
; Doc: https://nsis.sourceforge.io/Docs/Chapter4.html
!include "MUI2.nsh" ; Doc: https://nsis.sourceforge.io/Docs/Modern%20UI/Readme.html
!define EPT "EverythingPT"
!define SMWCUE "Software\Microsoft\Windows\CurrentVersion\Uninstall\EverythingPowerToys3"
;--------------------------------
;General
  Name "${EPT}"
  BrandingText "v${ver} ${platform}"
  SetCompressor zlib
  OutFile ".\..\bin\${EPT}-${ver}-${platform}-SDK3.exe"
  Unicode True
  RequestExecutionLevel user
  SetOverwrite ifnewer
  InstallDir "$LOCALAPPDATA\Microsoft\PowerToys\PowerToys Run\Plugins\Everything3"
  FileErrorText "Can't access: $\r$\n$\r$\n$0$\r$\n$\r$\nPowerToys is probably still running, please close it and retry."
;--------------------------------
;Interface Settings
  !define MUI_ICON "Everything.ico"
  !define MUI_ABORTWARNING

  ;!define MUI_UNICON "Everything.ico"
  ;!define MUI_UNABORTWARNING
;--------------------------------
;Pages
  !insertmacro MUI_PAGE_LICENSE "..\LICENSE"
  !insertmacro MUI_PAGE_INSTFILES

  ;!insertmacro MUI_UNPAGE_CONFIRM
  ;!insertmacro MUI_UNPAGE_INSTFILES

  !insertmacro MUI_LANGUAGE "English"
;--------------------------------
;Version Information
  VIProductVersion "${ver}.0"
  VIAddVersionKey /LANG=${LANG_ENGLISH} "ProductName" "${EPT}3 Setup"
  VIAddVersionKey /LANG=${LANG_ENGLISH} "CompanyName" "lin-ycv"
  VIAddVersionKey /LANG=${LANG_ENGLISH} "LegalCopyright" "lin-ycv"
  VIAddVersionKey /LANG=${LANG_ENGLISH} "FileDescription" "Everything search plugin for PowerToys Run"
  VIAddVersionKey /LANG=${LANG_ENGLISH} "FileVersion" "${ver}"
  VIAddVersionKey /LANG=${LANG_ENGLISH} "ProductVersion" "${ver}"
;--------------------------------
Section "Installer"
  SetOutPath $INSTDIR
  File /r "${direct}\*"

  ;WriteUninstaller "$INSTDIR\uninstall.exe"

  ;WriteRegStr HKCU "${SMWCUE}" "DisplayIcon" "$INSTDIR\uninstall.exe"
  WriteRegStr HKCU "${SMWCUE}" "DisplayName" "${EPT}3 (${platform})"
  WriteRegStr HKCU "${SMWCUE}" "DisplayVersion" "${ver}.1"
  WriteRegStr HKCU "${SMWCUE}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU "${SMWCUE}" "Publisher" "Lin-ycv"
  ;WriteRegStr HKCU "${SMWCUE}" "UninstallString" "$INSTDIR\uninstall.exe"
  ;WriteRegDWORD HKCU "${SMWCUE}" "NoModify" 1
  ;WriteRegDWORD HKCU "${SMWCUE}" "NoRepair" 1

  IfErrors 0 +5
  SetErrorlevel 1
  IfSilent +2
  MessageBox MB_ICONEXCLAMATION "Unable to install, PowerToys is probably still running, please close it manually before install."
  Abort
  
  SetErrorlevel 0
SectionEnd
;--------------------------------
;Section "Uninstall"
    
    ;MessageBox MB_YESNO "This'll forcibly close PowerToys and remove all traces of EverythingPowerToys3$\r$\nAre you sure you want to continue?" /SD IDYES IDYES true IDNO false

    ;false:
    ;DetailPrint "Uninstall cancelled, all files remains intact"
    ;Abort

    ;true:
    ;ExecWait `taskkill /im PowerToys.exe /f /t`
    ;Sleep 5000
    ;rmdir /r "$INSTDIR"
    ;rmdir /r "$LOCALAPPDATA\Microsoft\PowerToys\PowerToys Run\Settings\Plugins\Community.PowerToys.Run.Plugin.Everything3"
    ;DeleteRegKey HKCU "${SMWCUE}"

    ;IfErrors 0 +4
    ;SetErrorlevel 0
    ;IfSilent +2
    ;MessageBox MB_ICONEXCLAMATION "Some file couldn't be removed, PowerToys is probably still running.$\r$\n$\r$\nCheck for leftover files in $INSTDIR"

    ;SetErrorlevel 0
;SectionEnd