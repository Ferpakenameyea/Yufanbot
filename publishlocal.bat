@echo off
REM usage: publishlocal.bat NUGET_DIR
REM example: publishlocal.bat C:\LocalNuget

SET NUGET_DIR=%1

IF "%NUGET_DIR%"=="" (
    ECHO please input NUGET_DIR
    EXIT /B 1
)

SET PROJECT_PATH=./Yufanbot.Plugin.Common/Yufanbot.Plugin.Common.csproj

ECHO building: %PROJECT_PATH%
dotnet build "%PROJECT_PATH%" -c Release

IF %ERRORLEVEL% NEQ 0 (
    ECHO build failed!
    EXIT /B 1
)

SET RELEASE_DIR=%~dp0Yufanbot.Plugin.Common\bin\Release

FOR %%f IN ("%RELEASE_DIR%\*.nupkg") DO (
    ECHO copying %%f to %NUGET_DIR%
    COPY /Y "%%f" "%NUGET_DIR%"
)

ECHO done
