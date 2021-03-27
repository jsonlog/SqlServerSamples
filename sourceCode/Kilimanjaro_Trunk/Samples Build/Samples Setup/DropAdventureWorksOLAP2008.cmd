@ECHO OFF

ECHO 0: Commandline : %0
ECHO 1: Instance    : %1
ECHO 2: Source Path : %2
ECHO.
ECHO Dropping AW 2008 OLAP databases.

ECHO TODO Use the environment variables, if no commandline arguments specified.

:: SQL Server 2008 instance name.
:: Remove quotes
SET _instancename=%1
SET _instancename=###%_instancename%###
SET _instancename=%_instancename:"###=%
SET _instancename=%_instancename:###"=%
SET _instancename=%_instancename:###=%
IF "%_instancename%"=="" GOTO Usage

:: Samples common source path, slash-terminated.
:: Remove quotes
SET _sourcepath=%2
SET _sourcepath=###%_sourcepath%###
SET _sourcepath=%_sourcepath:"###=%
SET _sourcepath=%_sourcepath:###"=%
SET _sourcepath=%_sourcepath:###=%
IF "%_sourcepath%"=="" GOTO Usage

ECHO Drop the enterprise version of the cube.
"%_sourcepath%ascmd.exe" -S "%_instancename%" -i "%_sourcepath%AdventureWorks 2008 Analysis Services Project\enterprise\Adventure Works DW 2008 Script Delete.xmla"

ECHO Drop the standard version of the cube.
"%_sourcepath%ascmd.exe" -S "%_instancename%" -i "%_sourcepath%AdventureWorks 2008 Analysis Services Project\standard\Adventure Works DW 2008 SE Script Delete.xmla"

GOTO Exit

:Usage
ECHO.
ECHO.
ECHO    USAGE
ECHO.
ECHO    Two (2) parameters are required:
ECHO.
ECHO    1. SQL Server 2008 Analysis Services instance name must be specified.
ECHO    2. The SQL Server samples root source data path must be specified.
ECHO.
ECHO    Example for a default instance installation:
ECHO.
ECHO       DropAdventureWorksOLAP2008.cmd "." "C:\Program Files\Microsoft SQL Server\100\Tools\Samples\"
ECHO.

:Exit
ECHO.
ECHO Script execution complete.
EXIT 0
