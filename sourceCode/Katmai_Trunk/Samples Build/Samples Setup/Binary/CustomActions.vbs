''''' ***** THIS SCRIPT IS OBSOLETE AND UNUSED. FOR REFERENCE PURPOSES ONLY. ***** '''''


'' http://www.microsoft.com/technet/scriptcenter/resources/qanda/jun05/hey0603.mspx
'' http://blogs.technet.com/alexshev/archive/2008/02/21/from-msi-to-wix-part-5-custom-actions.aspx
'' http://www.indigorose.com/forums/archive/index.php/t-22481.html
'' http://msdn.microsoft.com/en-us/library/xc8bz3y5(VS.71).aspx          Error Handling in Custom Actions
'' http://msdn.microsoft.com/en-us/library/aa371672.aspx                 Session.Message
'' http://msdn.microsoft.com/en-us/library/aa390445(VS.85).aspx          Registry

'' http://blogs.msdn.com/sqlexpress/archive/2006/07/29/682254.aspx
'' http://www.eggheadcafe.com/software/aspnet/31943819/wmi-to-obtain-edition-of.aspx

'' http://www.codeplex.com/Wiki/View.aspx?ProjectName=SQLSrvEngine&title=FileStreamEnable&referringTitle=Home

'' http://msdn.microsoft.com/en-us/library/ms179591(SQL.100).aspx        Service Types for SQL Server
'' http://msdn.microsoft.com/en-us/library/aa394602(VS.85).aspx          WMI Tasks, Services
'' http://msdn.microsoft.com/en-us/library/ms974592.aspx
'' http://www.microsoft.com/technet/scriptcenter/default.mspx

'' Deferred Custom Actions
'' http://blogs.claritycon.com/blogs/sajo_jacob/archive/2008/02/28/customactiondata-in-wix-with-deferred-custom-actions.aspx
'' http://msdn.microsoft.com/en-us/library/aa370543(VS.85).aspx
'' http://juice.altiris.com/tip/4432/property-passing-custom-action

'Option Explicit

'' Installer Error Codes: http://msdn.microsoft.com/en-us/library/aa368542.aspx
'Const ERROR_SUCCESS = 0
'Const ERROR_INSTALL_FAILURE = 1603
'Const HKEY_LOCAL_MACHINE = &H80000002
'Const MSIMESSAGETYPEINFO = &H04000000
'Const MSIMESSAGETYPEERROR = &H01000000
'Const MSIVIEWMODIFYINSERTTEMPORARY = 7

'' HACK This is way overdue for a refactor.
'Function ListKatmaiInstances()
'    Dim record, combolist, x, y, msg, wmi, list, instance, name, wmifs

'    Set record = Session.Installer.CreateRecord(0)
'    record.StringData(0) = "*** Begin ListKatmaiInstances()"
'    Session.Message MSIMESSAGETYPEINFO, record

'    On Error Resume Next ' This WMI call bombs if ComputerManagement10 is AWOL or if FilestreamSettings is missing (such as x86 under WOW64).
'    Set wmi = GetObject("WINMGMTS:\\.\root\Microsoft\SqlServer\ComputerManagement10")
'    If Err.number <> 0 Then
'        On Error Goto 0
'        Err.Clear
'        Set record = Session.Installer.CreateRecord(0)
'        record.StringData(0) = "!!! Unable to open WMI for the local machine. Probably SQL Server 2008 is not installed. Make KATMAI_READY_INSTANCES = 0 and continue."
'        Session.Message MSIMESSAGETYPEINFO, record
'        Session.Property("KATMAI_READY_INSTANCES") = "0" ' Using strings to avoid type mismatch error cross-platform.
'        ListKatmaiInstances = ERROR_SUCCESS
'        Exit Function
'    End If ' Err
'    If IsNull(wmi) Then
'        Set record = Session.Installer.CreateRecord(0)
'        record.StringData(0) = "!!! Unable to open WMI for the local machine. Possibly elevation, permissions or WMI are the problem. Make KATMAI_READY_INSTANCES = 0 and continue."
'        Session.Message MSIMESSAGETYPEINFO, record
'        ListKatmaiInstances = ERROR_SUCCESS
'        Exit Function
'    End If

'    Set list = wmi.ExecQuery("SELECT * FROM SqlServiceAdvancedProperty WHERE SQLServiceType = 1 AND PropertyName = 'InstanceId'")
'    ' Bail if we don't get a list of values, since Katmai is missing.
'    If IsNull(list) Then
'        Set record = Session.Installer.CreateRecord(0)
'        record.StringData(0) = "!!! No records were found in the registry for Katmai instances. Make KATMAI_READY_INSTANCES = 0 and continue."
'        Session.Message MSIMESSAGETYPEINFO, record
'        Session.Property("KATMAI_READY_INSTANCES") = "0" ' Using strings to avoid type mismatch error cross-platform.
'        ListKatmaiInstances = ERROR_SUCCESS
'        Exit Function
'    End If
'    Set record = Session.Installer.CreateRecord(0)
'    record.StringData(0) = "   Instances found in WMI: " & list.Count
'    Session.Message MSIMESSAGETYPEINFO, record

'    Set record = Session.Installer.CreateRecord(0)
'    record.StringData(0) = "   Opening ComboBox table."
'    Session.Message MSIMESSAGETYPEINFO, record
'    Set combolist = Session.Database.OpenView("SELECT * FROM `ComboBox`")
'    If IsNull(combolist) Then
'        Set record = Session.Installer.CreateRecord(0)
'        record.StringData(0) = "!!! ComboBox table was empty or missing."
'        Session.Message MSIMESSAGETYPEERROR, record
'        ListKatmaiInstances = ERROR_INSTALL_FAILURE
'        Exit Function
'    End If

'    x = 0
'    y = 0
'    For Each instance In list
'        name = Replace(instance.PropertyStrValue, "MSSQL10.", "")

'        If IsVersion10(instance.ServiceName) Then
'            msg = "   Added " & name & " (" & x & ") to combobox KATMAI_INSTANCE_LIST."
'                ' ComboBox record fields are Property, Order, Value, Text
'                Set record = Session.Installer.CreateRecord(4)
'                record.StringData(1) = "KATMAI_INSTANCE_LIST"
'                record.IntegerData(2) = x
'                record.StringData(3) = instance.ServiceName
'                record.StringData(4) = name
'                combolist.Modify MSIVIEWMODIFYINSERTTEMPORARY, record
'            y = y + 1
'        Else
'            msg = "   SKIPPED " & name & " (" & x & ") because the service was not Katmai (version 10)."
'        End If
'        x = x + 1

'        Set record = Session.Installer.CreateRecord(0)
'        record.StringData(0) = msg
'        Session.Message MSIMESSAGETYPEINFO, record
'    Next

'    combolist.Close
'    Set combolist = Nothing

'    ' Bail if we don't get any instances.
'    If y = 0 Then
'        Set record = Session.Installer.CreateRecord(0)
'        record.StringData(0) = "!!! No Katmai instances with FILESTREAM enabled were found. Make KATMAI_READY_INSTANCES = 0 and continue."
'        Session.Message MSIMESSAGETYPEINFO, record
'        Session.Property("KATMAI_READY_INSTANCES") = "0" ' Using strings to avoid type mismatch error cross-platform.
'        ListKatmaiInstances = ERROR_SUCCESS
'    Else
'        Session.Property("KATMAI_READY_INSTANCES") = CStr(y)
'        Set record = Session.Installer.CreateRecord(0)
'        record.StringData(0) = "  Instances eligible: " & CStr(y)
'        Session.Message MSIMESSAGETYPEINFO, record
'        ListKatmaiInstances = ERROR_SUCCESS
'    End If

'    Set record = Session.Installer.CreateRecord(0)
'    record.StringData(0) = "*** End ListKatmaiInstances()"
'    Session.Message MSIMESSAGETYPEINFO, record
'End Function ' ListKatmaiInstances()

'Function SetDatabaseInstanceName()
'    Dim name
'    name = Session.Property("KATMAI_INSTANCE_LIST")
'    If name = "" Then
'        Exit Function
'    End If

'    Session.Property("DATABASESERVICENAME") = Session.Property("KATMAI_INSTANCE_LIST")
'    Session.Property("SQLSAMPLESDATABASEPATH") = FetchServiceDataProperty(Session.Property("KATMAI_INSTANCE_LIST"), "DATAPATH")

'    name = UCase(Replace(name, "MSSQL$", "", 1, 1))
'    If name = "MSSQLSERVER" Then
'        Session.Property("DATABASEINSTANCENAME") = "."
'    Else
'        Session.Property("DATABASEINSTANCENAME") = ".\" & name
'    End If
'End Function ' SetDatabaseInstanceName

'Function StartInstance(serviceName)
'    Dim wmi, list, svc, msg, record, e

'    On Error Resume Next
'    Set wmi = GetObject("winmgmts:\\.\root\CIMV2")
'    If Err.number <> 0 Then
'        Err.Clear
'        On Error Goto 0
'        Set record = Session.Installer.CreateRecord(0)
'        record.StringData(0) = "!!! Error getting WMI object while trying to start " & serviceName & "."
'        Session.Message MSIMESSAGETYPEINFO, record
'        StartInstance = False
'        Exit Function
'    End If ' Err
'    Err.Clear
'    On Error Goto 0

'    Set list = wmi.ExecQuery("SELECT * FROM Win32_Service WHERE Name = '" & serviceName & "'")
'    If IsNull(list) Then
'        Set record = Session.Installer.CreateRecord(0)
'        record.StringData(0) = "!!! Failure to return records for " & serviceName & " from Win32_Service."
'        Session.Message MSIMESSAGETYPEINFO, record
'        StartInstance = False
'        Exit Function
'    End If
'    If list.Count = 0 Then
'        Set record = Session.Installer.CreateRecord(0)
'        record.StringData(0) = "!!! Error: No records found in Win32_Service for " & serviceName & "."
'        Session.Message MSIMESSAGETYPEINFO, record
'        StartInstance = False
'    End If
'    If list.Count > 1 Then
'        Err.Raise-1, "StartInstance", "More than one service found by the name: " & serviceName, Null, Null
'    End If

'    For Each svc in list
'        If svc.State = "Running" Then
'            Set record = Session.Installer.CreateRecord(0)
'            record.StringData(0) = "   Instance " & serviceName & " was found to already be running."
'            Session.Message MSIMESSAGETYPEINFO, record
'            StartInstance = True
'            Exit Function
'        Else
'            On Error Resume Next
'            svc.StartService()
'            e = Err.number
'            Err.Clear
'            On Error Goto 0
'            If e = 0 Then
'                Sleep(30) ' Give it some time to finish starting.
'                If IsInstanceRunning(serviceName) Then
'                    Set record = Session.Installer.CreateRecord(0)
'                    record.StringData(0) = "   Instance " & serviceName & " successfully started. Proceeding normally."
'                    Session.Message MSIMESSAGETYPEINFO, record
'                    StartInstance = True
'                Else
'                    Set record = Session.Installer.CreateRecord(0)
'                    record.StringData(0) = "!!! Failure to start " & serviceName & ". It may be disabled or missing."
'                    Session.Message MSIMESSAGETYPEINFO, record
'                    StartInstance = False
'                End If
'            Else
'                Set record = Session.Installer.CreateRecord(0)
'                record.StringData(0) = "!!! Failure to start " & serviceName & ". Err.Number = " & CStr(e)
'                Session.Message MSIMESSAGETYPEINFO, record
'                StartInstance = False
'            End If
'            Exit Function
'        End If
'    Next
'End Function ' StartInstance

'Function StopInstance(serviceName)
'    Dim wmi, list, svc, msg, record, e

'    On Error Resume Next
'    Set wmi = GetObject("winmgmts:\\.\root\CIMV2")
'    If Err.number <> 0 Then
'        Err.Clear
'        On Error Goto 0
'        Set record = Session.Installer.CreateRecord(0)
'        record.StringData(0) = "!!! Error getting WMI object while trying to stop " & serviceName & "."
'        Session.Message MSIMESSAGETYPEINFO, record
'        StopInstance = False
'        Exit Function
'    End If ' Err
'    Err.Clear
'    On Error Goto 0

'    Set list = wmi.ExecQuery("SELECT * FROM Win32_Service WHERE Name = '" & serviceName & "'")
'    If IsNull(list) Then
'        Set record = Session.Installer.CreateRecord(0)
'        record.StringData(0) = "!!! Failure to return records for " & serviceName & " from Win32_Service."
'        Session.Message MSIMESSAGETYPEINFO, record
'        StopInstance = False
'        Exit Function
'    End If
'    If list.Count = 0 Then
'        Set record = Session.Installer.CreateRecord(0)
'        record.StringData(0) = "!!! Error: No records found in Win32_Service for " & serviceName & "."
'        Session.Message MSIMESSAGETYPEINFO, record
'        StopInstance = False
'    End If
'    If list.Count > 1 Then
'        Err.Raise-1, "StopInstance", "More than one service found by the name: " & serviceName, Null, Null
'    End If

'    For Each svc in list
'        If svc.State = "Stopped" Then
'            Set record = Session.Installer.CreateRecord(0)
'            record.StringData(0) = "   Instance " & serviceName & " was found to already be stopped."
'            Session.Message MSIMESSAGETYPEINFO, record
'            StopInstance = True
'            Exit Function
'        Else
'            On Error Resume Next
'            svc.StopService()
'            e = Err.number
'            Err.Clear
'            On Error Goto 0
'            If e = 0 Then
'                Set record = Session.Installer.CreateRecord(0)
'                record.StringData(0) = "   Instance " & serviceName & " successfully stopped. Proceeding normally."
'                Session.Message MSIMESSAGETYPEINFO, record
'                StopInstance = True
'            Else
'                Set record = Session.Installer.CreateRecord(0)
'                record.StringData(0) = "!!! Failure to stop " & serviceName & ". Err.Number = " & CStr(e)
'                Session.Message MSIMESSAGETYPEINFO, record
'                StopInstance = False
'            End If
'            Exit Function
'        End If
'    Next
'End Function ' StopInstance

'Function IsVersion10(serviceName)
'    Dim wmi, list, svc, version
'    On Error Resume Next
'    Set wmi = GetObject("WINMGMTS:\\.\root\Microsoft\SqlServer\ComputerManagement10")
'    If Err.number <> 0 Then
'        Err.Clear
'        On Error Goto 0
'        IsVersion10 = False
'        Exit Function
'    End If
'    Err.Clear
'    On Error Goto 0

'    Set list = wmi.ExecQuery("SELECT * FROM SqlServiceAdvancedProperty WHERE ServiceName = '" & serviceName & "' AND SQLServiceType = 1 AND PropertyName LIKE 'Version'")
'    If list.Count <> 1 Then
'        IsVersion10 = False
'        Exit Function
'    End If

'    For Each svc in list
'        version = svc.PropertyStrValue
'        If IsNull(version) Then
'            IsVersion10 = False
'            Exit Function
'        End If
'        If Len(version) < 2 Then
'            IsVersion10 = False
'            Exit Function
'        End If
'        If Left(version, 2) = "10" Then
'            IsVersion10 = True
'            Exit Function
'        End If
'    Next ' Each
'End Function ' IsVersion10

'Function MakeInstanceReady()
'    Dim args, name, record, isfsrequired, toleratefailure
'    ' Split on |
'    LogInstallerInfo("CustomActionData: " & Session.Property("CustomActionData"))
'    args = Split(Session.Property("CustomActionData"), "|")
'    name = args(0)
'    isfsrequired = LCase(args(1))
'    toleratefailure = LCase(args(2))
'    LogInstallerInfo("   Service Name: " & name)
'    LogInstallerInfo("   Is FILESTREAM Required: " & isfsrequired)
'    LogInstallerInfo("   Tolerate Failure: " & toleratefailure)

'    If toleratefailure = "yes" Then
'        ' Uninstallation must be more fault tolerant; we want successful uninstall even if this CA fails.
'        On Error Resume Next
'        MakeInstanceReady = PrepInstance(name, isfsrequired, true)
'        MakeInstanceReady = ERROR_SUCCESS ' Ignore any potential failure.
'        On Error Goto 0
'    Else
'        MakeInstanceReady = PrepInstance(name, isfsrequired, false)
'    End If ' toleratefailure
'End Function ' MakeInstanceReady

'Function PrepInstance(serviceName, isFSRequired, tolerateFailure)
'    ' Uninstallation must be more fault tolerant; we want successful uninstall even if this CA fails.

'    LogInstallerInfo("   PrepInstance: Getting ready to start " & serviceName & ".")

'    If isFSRequired = "yes" Then
'        Dim iserr, errmsg
'        iserr = False
'        errmsg = ""

'        If Not EnableFileStream(serviceName) Then
'            iserr = True
'            errmsg = errmsg & "FILESTREAM"
'        End If

'        If Not EnableFullTextSearch(serviceName) Then
'            iserr = True
'            If errmsg <> "" Then
'                errmsg = errmsg & " and "
'            End If
'            errmsg = errmsg & "Full Text Search"
'        End If

'        If iserr Then
'            errmsg = "PrepInstance() failed for " & serviceName _
'                & "." & vbCrLf & vbCrLf & "The following features are missing: " _
'                & errmsg & vbCrLf & vbCrLf _
'                & "Fix the problems and re-run setup."
'            If Not tolerateFailure Then
'                LogInstallerError(errmsg)
'                PrepInstance = ERROR_INSTALL_FAILURE
'            Else
'                LogInstallerInfo(errmsg)
'                PrepInstance = ERROR_SUCCESS
'            End If ' tolerateFailure
'            Exit Function
'        End If ' iserr

'        LogInstallerInfo("*** FILESTREAM and Full Text Search were enabled for instance " & serviceName & ". Proceeding normally.")
'        PrepInstance = ERROR_SUCCESS
'    Else
'        LogInstallerInfo("*** FILESTREAM was not required and was not enabled for instance " & serviceName & ". Proceeding normally.")
'        PrepInstance = ERROR_SUCCESS
'    End If

'    If Not StartInstance(serviceName) Then
'        If Not tolerateFailure Then
'            LogInstallerError("PrepInstance() cannot proceed because the selected instance was not running and could not be started successfully.")
'            PrepInstance = ERROR_INSTALL_FAILURE
'        Else
'            LogInstallerInfo("PrepInstance() cannot proceed because the selected instance was not running and could not be started successfully.")
'            PrepInstance = ERROR_SUCCESS
'        End If ' tolerateFailure
'        Exit Function
'    Else
'        LogInstallerInfo("   Service for the instance " & serviceName & " was running or started successfully. Proceeding normally.")
'    End If
'End Function ' PrepInstance

'Function EnableFileStream(serviceName)
'    Dim wmifs, method, inParam, outParam, record, fsname, sql
'    fsname = Replace(serviceName, "MSSQL$", "", 1, 1)
'    On Error Resume Next ' This WMI call bombs if ComputerManagement10 is AWOL or if FilestreamSettings is missing (such as x86 under WOW64).
'    Set wmifs = GetObject("WINMGMTS:\\.\root\Microsoft\SqlServer\ComputerManagement10:FilestreamSettings='" & fsname & "'")
'    If Err.number = 0 Then
'        Err.Clear
'        On Error Goto 0
'        If wmifs.AccessLevel = 0 Then
'            Set record = Session.Installer.CreateRecord(0)
'            record.StringData(0) = "   FILESTREAM is NOT enabled for instance " & serviceName & ". Attempting to enable it."
'            Session.Message MSIMESSAGETYPEINFO, record
'            Set method = wmifs.Methods_("EnableFilestream")
'            Set inParam = method.inParameters.SpawnInstance_()
'            inParam.AccessLevel = 2
'            inParam.ShareName = ""
'            Set outParam = wmifs.ExecMethod_("EnableFilestream", inParam)
'            If outParam.returnValue = 0 Then

'                ' Enable FS @ the SQL Server instance, just in case it was not during initial setup.
'                sql = EnableFSCmdString(fsname)
'                LogInstallerInfo("Executing: " + sql)
'                LogInstallerInfo(ExecSqlCmd(sql))

'                LogInstallerInfo("   FILESTREAM was enabled successfully for instance " & serviceName & ". Restarting the instance to enable FILESTREAM.")
'                If StopInstance(serviceName) Then
'                    Sleep(30)
'                    If StartInstance(serviceName) Then
'                        EnableFileStream = True
'                    Else
'                        Set record = Session.Installer.CreateRecord(0)
'                        record.StringData(0) = "   Attempting to restart the instance " & serviceName & " FAILED."
'                        Session.Message MSIMESSAGETYPEINFO, record
'                        EnableFileStream = False
'                    End If
'                Else
'                    Set record = Session.Installer.CreateRecord(0)
'                    record.StringData(0) = "   Attempting to stop the instance " & serviceName & " FAILED."
'                    Session.Message MSIMESSAGETYPEINFO, record
'                    EnableFileStream =  False
'                End If
'            Else
'                Set record = Session.Installer.CreateRecord(0)
'                record.StringData(0) = "   Attempting to enable FILESTREAM failed for instance " & serviceName & "."
'                Session.Message MSIMESSAGETYPEINFO, record
'                EnableFileStream =  False
'            End If
'        Else
'            Set record = Session.Installer.CreateRecord(0)
'            record.StringData(0) = "   FILESTREAM was already enabled for instance " & serviceName & "."
'            Session.Message MSIMESSAGETYPEINFO, record
'            EnableFileStream = True
'        End If
'    Else
'        Set record = Session.Installer.CreateRecord(0)
'        record.StringData(0) = "!!! A WMI error occurred while attempting to enable FILESTREAM failed for instance " & serviceName & "."
'        Session.Message MSIMESSAGETYPEINFO, record
'        EnableFileStream = False
'        Err.Clear
'        On Error Goto 0
'    End If ' Err

'    Set wmifs = Nothing
'    Set method = Nothing
'    Set inParam = Nothing
'    Set outParam = Nothing
'End Function ' EnableFileStream

'Function IsInstanceRunning(serviceName)
'    Dim wmi, list, svc, msg

'    Set wmi = GetObject("winmgmts:\\.\root\CIMV2")
'    Set list = wmi.ExecQuery("SELECT * FROM Win32_Service WHERE Name = '" & serviceName & "'")

'    If IsNull(list) Then
'        IsInstanceRunning = False
'        Exit Function
'    End If
'    If list.Count = 0 Then
'        IsInstanceRunning = False
'    End If
'    If list.Count > 1 Then
'        Err.Raise-1, "IsInstanceRunning", "More than one service found by the name: " & serviceName, Null, Null
'    End If
'    For Each svc in list
'        If svc.State = "Running" Then
'            IsInstanceRunning = True
'            Exit Function
'        Else
'            IsInstanceRunning = False
'            Exit Function
'        End If
'    Next
'End Function ' IsInstanceRunning

'Function EnableFullTextSearch(instanceName)
'    Dim servicename

'    If IsNull(instanceName) Then
'        EnableFullTextSearch = False
'        Exit Function
'    End If

'    servicename = Replace(LCase(instanceName), "mssql$", "")

'    If servicename = "mssqlserver" Then
'        servicename = "MSSQLFDLauncher"
'    Else
'        servicename = "MSSQLFDLauncher$" & servicename
'    End If

'    EnableFullTextSearch = StartInstance(servicename)
'End Function ' EnableFullTextSearch

'Function Sleep(seconds)
'    Dim starttime, stoptime
'    starttime = Timer()
'    stoptime = starttime + seconds
'    While Timer <= stoptime
'        ' Sleeping...
'    Wend
'End Function ' Sleep

'Function LogInstallerInfo(strmsg)
'    Dim record
'    Set record = Session.Installer.CreateRecord(0)
'    record.StringData(0) = strmsg
'    Session.Message MSIMESSAGETYPEINFO, record
'End Function ' LogInstallerInfo

'Function LogInstallerError(strmsg)
'    Dim record
'    Set record = Session.Installer.CreateRecord(0)
'    record.StringData(0) = strmsg
'    Session.Message MSIMESSAGETYPEERROR, record
'End Function ' LogInstallerError

'Function DropAdventureWorks()
'    DropAdventureWorks = ERROR_SUCCESS ' We want this to succeed during uninstall, regardless.
'    On Error Resume Next
'    Dim args, servername, databasetarget, msg, proceed, record, cmd
'    ' Split on |
'    args = Split(Session.Property("CustomActionData"), "|")
'    servername = LCase(args(0))
'    LogInstallerInfo("   Incoming server name: " & servername)
'    If servername = "mssqlserver" Then
'        servername = "."
'        LogInstallerInfo("   Server name updated: " & servername)
'    Else
'        servername = ".\" & servername
'        LogInstallerInfo("   Server name updated: " & servername)
'    End If
'    databasetarget = LCase(args(1))

'    ' vbYesNo
'    proceed = MsgBox("Remove the installed AdventureWorks databases?" & vbCrLf & vbCrLf & "If SQL Server 2008 is not running, click No." & vbCrLf, 4, "Drop AdventureWorks?")
'    If proceed = 6 Then ' vbYes
'        LogInstallerInfo("*** Begin drop of AdventureWorks database(s).")

'        If databasetarget = "all" Or databasetarget = "AdventureWorks" Then
'            cmd = DropCmdString(servername, "AdventureWorks")
'            LogInstallerInfo("    Command: " & cmd)
'            LogInstallerInfo("    --> " & ExecSqlCmd(cmd))
'        End If

'        If databasetarget = "all" Or databasetarget = "AdventureWorks2008" Then
'            cmd = DropCmdString(servername, "AdventureWorks2008")
'            LogInstallerInfo("    Command: " & cmd)
'            LogInstallerInfo("    --> " & ExecSqlCmd(cmd))
'        End If

'        If databasetarget = "all" Or databasetarget = "AdventureWorksLT" Then
'            cmd = DropCmdString(servername, "AdventureWorksLT")
'            LogInstallerInfo("    Command: " & cmd)
'            LogInstallerInfo("    --> " & ExecSqlCmd(cmd))
'        End If

'        If databasetarget = "all" Or databasetarget = "AdventureWorksLT2008" Then
'            cmd = DropCmdString(servername, "AdventureWorksLT2008")
'            LogInstallerInfo("    Command: " & cmd)
'            LogInstallerInfo("    --> " & ExecSqlCmd(cmd))
'        End If

'        If databasetarget = "all" Or databasetarget = "AdventureWorksDW" Then
'            cmd = DropCmdString(servername, "AdventureWorksDW")
'            LogInstallerInfo("    Command: " & cmd)
'            LogInstallerInfo("    --> " & ExecSqlCmd(cmd))
'        End If

'        If databasetarget = "all" Or databasetarget = "AdventureWorksDW2008" Then
'            cmd = DropCmdString(servername, "AdventureWorksDW2008")
'            LogInstallerInfo("    Command: " & cmd)
'            LogInstallerInfo("    --> " & ExecSqlCmd(cmd))
'        End If
'    End If ' vbYes
'    On Error Goto 0
'End Function ' DropAdventureWorks

'Function DropCmdString(serverName, databaseName)
'    DropCmdString = "sqlcmd -S " _
'        & serverName _
'        & " -E -Q ""IF EXISTS(SELECT * FROM sys.databases WHERE [name] = '" _
'        & databaseName _
'        & "') BEGIN EXECUTE (N'ALTER DATABASE [" _
'        & databaseName _
'        & "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;'); EXECUTE (N'DROP DATABASE [" _
'        & databaseName _
'        & "];'); END"""
'End Function ' DropCmdString

'Function EnableFSCmdString(instanceName)
'    Dim target
'    target = LCase(instanceName)
'    If target = "mssqlserver" Then
'        target = "."
'    Else
'        target = ".\" + target
'    End If
'    EnableFSCmdString = "sqlcmd -S " _
'        & target _
'        & " -d master -E -Q ""EXECUTE(N'EXEC sp_configure ''filestream access level'', ''2'';'); EXECUTE(N'RECONFIGURE WITH OVERRIDE;');"""
'End Function ' EnableFSCmdString

'Function ExecSqlCmd(cmdstr)
'    Dim shell, exec, stdout
'    Set shell = CreateObject("WScript.Shell")
'    Set exec = shell.Exec(cmdstr)
'    Set stdout = exec.StdOut
'    ExecSqlCmd = stdout.ReadAll
'    Set shell = Nothing
'    Set exec = Nothing
'    Set stdout = Nothing
'End Function ' ExecSqlCmd

'Function FetchServiceDataProperty(instanceName, propertyName)
'    Dim wmi, list, instance
'    Set wmi = GetObject("WINMGMTS:\\.\root\Microsoft\SqlServer\ComputerManagement10")
'    Set list = wmi.ExecQuery("SELECT * FROM SqlServiceAdvancedProperty WHERE ServiceName = '" & instanceName & "' AND PropertyName ='" & propertyName & "'")
'    For Each instance In list
'        If instance.PropertyName = propertyName Then
'            FetchServiceDataProperty = instance.PropertyStrValue & "\DATA\"
'        End If
'    Next ' Each
'End Function ' ListServiceData
