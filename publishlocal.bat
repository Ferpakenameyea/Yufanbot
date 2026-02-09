@echo off
REM usage: publishlocal.bat NUGET_DIR
REM example: publishlocal.bat C:\LocalNuget

SET NUGET_DIR=%1
SET PROJECT_NAME=%2

IF "%NUGET_DIR%"=="" (
    ECHO please input NUGET_DIR
    EXIT /B 1
)

IF "%PROJECT_NAME%"=="" (
    ECHO please input PROJECT_NAME
    EXIT /B 1
)

SET PROJECT_PATH=./%PROJECT_NAME%/%PROJECT_NAME%.csproj

ECHO building: %PROJECT_PATH%
dotnet build "%PROJECT_PATH%" -c Release

IF %ERRORLEVEL% NEQ 0 (
    ECHO build failed!
    EXIT /B 1
)

SET RELEASE_DIR=%~dp0%PROJECT_NAME%\bin\Release

FOR %%f IN ("%RELEASE_DIR%\*.nupkg") DO (
    ECHO copying %%f to %NUGET_DIR%
    COPY /Y "%%f" "%NUGET_DIR%"
)

ECHO done
