; Silent switch /S/
; Pass in /Dver=0.00.0 before calling the script to set the version
;  ie: makensis /Dver=0.77.0 .\exeCreator.nsi
!define EPT "EverythingPT"
!define PT "PowerToys.exe"

LoadLanguageFile "${NSISDIR}\Contrib\Language files\English.nlf"
;--------------------------------
;Version Information
  VIProductVersion "${ver}.0"
  VIAddVersionKey /LANG=${LANG_ENGLISH} "ProductName" "${EPT} Setup"
  VIAddVersionKey /LANG=${LANG_ENGLISH} "CompanyName" "lin-ycv"
  VIAddVersionKey /LANG=${LANG_ENGLISH} "LegalCopyright" "© lin-ycv"
  VIAddVersionKey /LANG=${LANG_ENGLISH} "FileDescription" "Everything search plugin for PowerToys Run"
  VIAddVersionKey /LANG=${LANG_ENGLISH} "FileVersion" "${ver}"
  VIAddVersionKey /LANG=${LANG_ENGLISH} "ProductVersion" "${ver}"
;--------------------------------

BrandingText "${EPT} v${ver}"
CRCCheck force
Icon Everything.ico
InstallDir "$LOCALAPPDATA\Microsoft\PowerToys\PowerToys Run\Plugins\Everything"
Name "${EPT}"
OutFile ".\..\bin\${EPT}-${ver}-x64.exe"
RequestExecutionLevel user
SetCompressor /SOLID /FINAL lzma
Unicode True

;--------------------------------

; Pages

Page directory
Page instfiles

;--------------------------------

Section ""
  SetAutoClose True

  ; Execute TaskKill to terminate the specified process
  ExecWait '"$%SystemRoot%\system32\TaskKill.exe" /F /IM ${PT}'
  Sleep 1000

  SetOutPath $INSTDIR
  GetFullPathName $0 "$EXEDIR\"
  GetFullPathName $0 $0
  File /r ".\..\..\..\..\..\..\x64\Release\RunPlugins\Everything\*"

IfSilent +2
  MessageBox MB_OK|MB_ICONINFORMATION|MB_TOPMOST "${EPT} installed, please restart ${PT}"
  
SectionEnd

;--------------------------------

 Function .onInit

    System::Call 'kernel32::CreateMutex(p 0, i 0, t "ACFEF7F6-7856-4BB3-82E3-0877CBB4E9C7") p .r1 ?e'
 Pop $R0
 
 StrCmp $R0 0 +3
   MessageBox MB_OK|MB_ICONEXCLAMATION "The installer is already running."
   Abort

 FunctionEnd