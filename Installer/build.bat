@echo off
set version=%1
echo Building %version%
"c:\Program Files (x86)\Inno Setup 5\ISCC.exe" /dBUNDLED "/o.\dist" ".\OpenIZInstall.iss" /d"MyAppVersion=%version%"
"c:\Program Files (x86)\Inno Setup 5\ISCC.exe" /dx64 /dBUNDLED "/o.\dist" ".\OpenIZInstall.iss" /d"MyAppVersion=%version%"
"c:\Program Files (x86)\Inno Setup 5\ISCC.exe" "/o.\dist" ".\OpenIZInstall.iss" /d"MyAppVersion=%version%"
exit /b