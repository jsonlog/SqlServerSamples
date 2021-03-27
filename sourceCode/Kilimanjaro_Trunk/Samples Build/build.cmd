@echo off
cls
REM %windir%\Microsoft.NET\Framework\v3.5\msbuild.exe /? | more

REM Note the 32-bit assumptions.
%windir%\Microsoft.NET\Framework\v3.5\msbuild.exe build.proj /property:SyncSourceYN=%1 /property:InstallerPlatform=%2 /property:InstallerTag=%3 /verbosity:normal 
REM /filelogger
REM /verbosity:diag
