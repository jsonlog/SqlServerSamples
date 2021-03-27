@ECHO OFF
ECHO 0: Commandline : %0
ECHO 1: Instance    : %1
ECHO.
ECHO Dropping AW databases.

ECHO TODO Use the environment variables, if no commandline arguments specified.

:: SQL Server 2008 instance name.
:: Remove quotes
SET _instancename=%1
SET _instancename=###%_instancename%###
SET _instancename=%_instancename:"###=%
SET _instancename=%_instancename:###"=%
SET _instancename=%_instancename:###=%
IF "%_instancename%"=="" GOTO Usage

ECHO DROP DATABASE AdventureWorksLT
sqlcmd -S %_instancename% -E -Q "IF EXISTS(SELECT * FROM sys.databases WHERE [name] = 'AdventureWorksLT') BEGIN EXECUTE (N'ALTER DATABASE [AdventureWorksLT] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;'); EXECUTE (N'DROP DATABASE [AdventureWorksLT];'); END"

GOTO Exit

:Usage
ECHO.
ECHO.
ECHO    USAGE
ECHO.
ECHO    One (1) parameters are required:
ECHO.
ECHO    1. SQL Server 2008 instance name must be specified.
ECHO.
ECHO    For example:
ECHO.
ECHO       dropadventureworks.cmd ".\SQLSERVER2008" "%PROGRAMFILES%\Microsoft SQL Server\100\" "%PROGRAMFILES%\Microsoft SQL Server\MSSQL10.MSSQLSERVER\"
ECHO.

:Exit
ECHO.
ECHO Script execution complete.
EXIT 0
