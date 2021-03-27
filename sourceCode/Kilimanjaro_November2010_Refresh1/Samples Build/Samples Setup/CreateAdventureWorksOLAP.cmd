@ECHO OFF

ECHO 0: Commandline : %0
ECHO 1: Instance    : %1
ECHO 2: Source Path : %2
ECHO 3: Service ID  : %3
ECHO.
ECHO Creating AW OLAP databases.

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

:: SQL Engine Service Identity, slash-terminated.
:: Remove quotes
SET _serviceid=%3
SET _serviceid=###%_serviceid%###
SET _serviceid=%_serviceid:"###=%
SET _serviceid=%_serviceid:###"=%
SET _serviceid=%_serviceid:###=%
IF "%_serviceid%"=="" GOTO Usage

ECHO Grant data reader permssions for the service account to our DW database.
sqlcmd -S %_instancename% -E -Q "USE AdventureWorksDW; EXEC sp_grantdbaccess N'%_serviceid%'; EXEC sp_addrolemember 'db_datareader', N'%_serviceid%';"

ECHO Deploy and process the enterprise version of the cube.
"%_sourcepath%ascmd.exe" -S "%_instancename%" -i "%_sourcepath%AdventureWorks Analysis Services Project\enterprise\Adventure Works DW Script.xmla"

ECHO Deploy and process the standard version of the cube.
"%_sourcepath%ascmd.exe" -S "%_instancename%" -i "%_sourcepath%AdventureWorks Analysis Services Project\standard\Adventure Works DW Standard Edition Script.xmla"

GOTO Exit

:Usage
ECHO.
ECHO.
ECHO    USAGE
ECHO.
ECHO    Three (3) parameters are required:
ECHO.
ECHO    1. SQL Server 2008 Analysis Services instance name must be specified.
ECHO    2. The SQL Server samples root source data path must be specified.
ECHO    3. The SQL Server Engine service identity.
ECHO.
ECHO    Example for a default instance installation:
ECHO.
ECHO       CreateAdventureWorksOLAP.cmd "." "C:\Program Files\Microsoft SQL Server\100\Tools\Samples\" "NT AUTHORITY\NETWORK SERVICE"
ECHO.

:Exit
ECHO.
ECHO Script execution complete.
EXIT 0
