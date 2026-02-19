@echo off
setlocal

REM Set these paths before running:
REM   set "LC_MANAGED_DIR=C:\Steam\steamapps\common\Lethal Company\Lethal Company_Data\Managed"
REM   set "BEPINEX_CORE_DIR=C:\path\to\BepInEx\core"

if "%LC_MANAGED_DIR%"=="" (
  echo [ERROR] LC_MANAGED_DIR is not set.
  exit /b 1
)
if "%BEPINEX_CORE_DIR%"=="" (
  echo [ERROR] BEPINEX_CORE_DIR is not set.
  exit /b 1
)

dotnet build "%~dp0..\AntiCheat.csproj" -c Release ^
  -p:Managed="%LC_MANAGED_DIR%" ^
  -p:BepInExCore="%BEPINEX_CORE_DIR%"