@ECHO OFF
ECHO 0: Commandline : %0
ECHO 1: Instance    : %1
ECHO 2: Source Path : %2
ECHO 3: Data Path   : %3
ECHO.
ECHO Creating AW databases.

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

:: SQL Server 2008 common root path, slash-terminated.
:: Remove quotes
SET _datapath=%3
SET _datapath=###%_datapath%###
SET _datapath=%_datapath:"###=%
SET _datapath=%_datapath:###"=%
SET _datapath=%_datapath:###=%
IF "%_datapath%"=="" GOTO Usage

:: AdventureWorks2008 ::
sqlcmd -E -S %_instancename% -d master -i "%_sourcepath%AdventureWorks 2008 OLTP\instawdb.sql" -b -v SqlSamplesSourceDataPath = "%_sourcepath%" -v SqlSamplesDatabasePath = "%_datapath%"

GOTO Exit

:Usage
ECHO.
ECHO.
ECHO    USAGE
ECHO.
ECHO    Three (3) parameters are required:
ECHO.
ECHO    1. SQL Server 2008 instance name must be specified.
ECHO    2. The SQL Server samples root source data path must be specified.
ECHO    3. SQL Server 2008 common data path for that instance.
ECHO.
ECHO    Example for a default instance installation:
ECHO.
ECHO       CreateAdventureWorks.cmd "." "C:\Program Files\Microsoft SQL Server\100\Tools\Samples\" "C:\Program Files\Microsoft SQL Server\MSSQL10.MSSQLSERVER\MSSQL\DATA\"
ECHO.

:Exit
ECHO.
ECHO Script execution complete.
EXIT 0
