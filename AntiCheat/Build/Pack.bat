@echo off
setlocal enabledelayedexpansion

rem Usage:
rem   Pack.bat <ProjectDir> <TargetDir> <SolutionDir> <ProjectName>
rem Example (Visual Studio post-build event):
rem   "$(ProjectDir)Build\Pack.bat" "$(ProjectDir)" "$(TargetDir)" "$(SolutionDir)" "$(ProjectName)"

set "projectDir=%~1"
set "targetDir=%~2"
set "solutionDir=%~3"
set "projectName=%~4"

if "%projectName%"=="" (
  echo.
  echo [ERROR] Missing arguments.
  echo   Pack.bat ^<ProjectDir^> ^<TargetDir^> ^<SolutionDir^> ^<ProjectName^>
  echo.
  exit /b 1
)

if not exist "%targetDir%%projectName%.dll" (
  echo.
  echo [ERROR] DLL not found:
  echo   "%targetDir%%projectName%.dll"
  echo.
  exit /b 1
)

set "tempDir=%TEMP%\%projectName%_pack_%RANDOM%%RANDOM%"
if exist "%tempDir%" rmdir /s /q "%tempDir%"
mkdir "%tempDir%\BepInEx\plugins" >nul 2>&1

copy "%solutionDir%CHANGELOG.md" "%tempDir%\" >nul 2>&1
copy "%solutionDir%README.md" "%tempDir%\" >nul 2>&1
copy "%solutionDir%icon.png" "%tempDir%\" >nul 2>&1
copy "%solutionDir%manifest.json" "%tempDir%\" >nul 2>&1

copy "%targetDir%%projectName%.dll" "%tempDir%\BepInEx\plugins\" >nul 2>&1

set "langSourceDir=%projectDir%Lang"
set "localesDir=%tempDir%\BepInEx\plugins\locales"
if exist "%langSourceDir%" (
  mkdir "%localesDir%" >nul 2>&1
  for %%f in ("%langSourceDir%\*") do (
    copy "%%f" "%localesDir%\" >nul 2>&1
  )
)

set "zipPath=%solutionDir%%projectName%.zip"
if exist "%zipPath%" del /f "%zipPath%" >nul 2>&1

set "PWSH_EXE=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
if exist "%PWSH_EXE%" (
  "%PWSH_EXE%" -NoProfile -Command "Compress-Archive -Path '%tempDir%\*' -DestinationPath '%zipPath%' -Force" >nul
) else (
  pwsh -NoProfile -Command "Compress-Archive -Path '%tempDir%\*' -DestinationPath '%zipPath%' -Force" >nul
)
set "rc=%ERRORLEVEL%"

rmdir /s /q "%tempDir%" >nul 2>&1

if not "%rc%"=="0" (
  echo.
  echo [ERROR] Compress-Archive failed. (exit code: %rc%)
  exit /b %rc%
)

echo.
echo ZIP created: %zipPath%
