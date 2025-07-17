@echo off
setlocal enabledelayedexpansion

:: ��ȡ����
set projectDir=%~1
set targetDir=%~2
set solutionDir=%~3
set projectName=%~4

:: ������ʱĿ¼
set tempDir=%TEMP%\%projectName%
if exist "%tempDir%" (
    rmdir /s /q "%tempDir%"
)
mkdir "%tempDir%"

:: �����ĵ��ļ�
copy "%solutionDir%CHANGELOG.md" "%tempDir%\" >nul 2>&1
copy "%solutionDir%README.md" "%tempDir%\" >nul 2>&1

:: �������Ŀ¼�ṹ
mkdir "%tempDir%\BepInEx\plugins"
copy "%targetDir%%projectName%.dll" "%tempDir%\BepInEx\plugins\" >nul 2>&1

:: �Զ��������������ļ�
set langSourceDir=%projectDir%Lang
set localesDir=%tempDir%\BepInEx\plugins\locales

if exist "%langSourceDir%" (
    mkdir "%localesDir%"
    echo ���ڸ��������ļ�:
    for %%f in ("%langSourceDir%\*") do (
        copy "%%f" "%localesDir%\" >nul 2>&1
        echo    - %%~nxf
    )
)

:: ����������Դ�ļ�
copy "%projectDir%icon.png" "%tempDir%\" >nul 2>&1
copy "%projectDir%manifest.json" "%tempDir%\" >nul 2>&1

:: ����ZIP�ļ�
set zipPath=%solutionDir%%projectName%.zip
if exist "%zipPath%" (
    del /f "%zipPath%"
)

:: ʹ������ѹ������ (Windows 10+)
powershell -Command "Compress-Archive -Path '%tempDir%\*' -DestinationPath '%zipPath%' -Force"

:: ������ʱĿ¼
rmdir /s /q "%tempDir%"

:: ������
echo.
echo ZIP������: %zipPath%