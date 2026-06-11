@echo off
setlocal enabledelayedexpansion

set MODNAME=%~1
set TARGETDIR=%~2
set PROJECTDIR=%~3

if "%MODNAME%"=="" goto :usage
if "%TARGETDIR%"=="" goto :usage
if "%PROJECTDIR%"=="" goto :usage

set DLLPATH=%TARGETDIR%%MODNAME%.dll
set RESOURCESPATH=%PROJECTDIR%Resources
set OUTDIR=..\..\BepInEx\plugins\%MODNAME%
set BUILDOUTDIR=..\Build\BepInEx\plugins\%MODNAME%

if not exist "%OUTDIR%" mkdir "%OUTDIR%"
if not exist "%BUILDOUTDIR%" mkdir "%BUILDOUTDIR%"

copy /Y "%DLLPATH%" "%OUTDIR%\" >nul 2>&1
echo [PostBuild] Copied %MODNAME%.dll to %OUTDIR%

copy /Y "%DLLPATH%" "%BUILDOUTDIR%\" >nul 2>&1
echo [PostBuild] Copied %MODNAME%.dll to %BUILDOUTDIR%

if exist "%RESOURCESPATH%" (
    xcopy /E /I /Y "%RESOURCESPATH%" "%OUTDIR%\Resources\" >nul 2>&1
    echo [PostBuild] Copied Resources folder to %OUTDIR%

    xcopy /E /I /Y "%RESOURCESPATH%" "%BUILDOUTDIR%\Resources\" >nul 2>&1
    echo [PostBuild] Copied Resources folder to %BUILDOUTDIR%
) else (
    echo [PostBuild] Resources folder not found at %RESOURCESPATH%
)

exit /b 0

:usage
echo [PostBuild] Missing arguments. Usage: postbuild-copy.cmd MODNAME TARGETDIR PROJECTDIR
exit /b 1
