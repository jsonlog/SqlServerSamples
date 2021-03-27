Option Explicit

Const ERROR_SUCCESS = 0
Const ERROR_INSTALL_FAILURE = 1603
Const HKEY_LOCAL_MACHINE = &H80000002
Const msiMessageTypeInfo = &H04000000
Const msiMessageTypeError = &H01000000
Const msiViewModifyInsertTemporary = 7
Const strKeyPath = "SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL"
Const strComputer = "."

' Call ListKatmaiInstances()
' Call ListServiceData()
'Call ListServiceDataProperty("DATAPATH")
MsgBox FetchServiceDataProperty("MSSQLSERVER", "DATAPATH")
' Call ListServices

'MsgBox "MSSQLSERVER FTS: " & CStr(IsFullTextSearchAvailable("MSSQLSERVER"))
'MsgBox "SQLSERVER2008 FTS: " & CStr(IsFullTextSearchAvailable("SQLSERVER2008"))

'MsgBox "Start: " & CStr(Timer)
'Sleep(10)
'MsgBox "Stop: " & CStr(Timer)

' Call DropAdventureWorks()

Function DropAdventureWorks()
    Dim msg, proceed, servername
    servername = InputBox("What is the instance name?")
    ' vbYesNo
    proceed = MsgBox("Remove the installed AdventureWorks databases?" & vbCrLf & vbCrLf & "If SQL Server 2008 is not running, click No." & vbCrLf, 4, "Drop AdventureWorks?")
    If proceed = 6 Then ' vbYes
        MsgBox DropCmdString(servername, "AdventureWorks")
        MsgBox ExecSqlCmd(DropCmdString(servername, "AdventureWorks"))

        MsgBox DropCmdString(servername, "AdventureWorks2008")
        MsgBox ExecSqlCmd(DropCmdString(servername, "AdventureWorks2008"))

        MsgBox DropCmdString(servername, "AdventureWorksLT")
        MsgBox ExecSqlCmd(DropCmdString(servername, "AdventureWorksLT"))

        MsgBox DropCmdString(servername, "AdventureWorksLT2008")
        MsgBox ExecSqlCmd(DropCmdString(servername, "AdventureWorksLT2008"))

        MsgBox DropCmdString(servername, "AdventureWorksDW")
        MsgBox ExecSqlCmd(DropCmdString(servername, "AdventureWorksDW"))

        MsgBox DropCmdString(servername, "AdventureWorksDW2008")
        MsgBox ExecSqlCmd(DropCmdString(servername, "AdventureWorksDW2008"))
    End If ' vbYes
End Function ' DropAdventureWorks

Function DropCmdString(serverName, databaseName)
    DropCmdString = "sqlcmd -S " _
        & serverName _
        & " -E -Q ""IF EXISTS(SELECT * FROM sys.databases WHERE [name] = '" _
        & databaseName _
        & "') BEGIN EXECUTE (N'ALTER DATABASE [" _
        & databaseName _
        & "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;'); EXECUTE (N'DROP DATABASE [" _
        & databaseName _
        & "];'); END"""
End Function ' DropCmdString

Function ExecSqlCmd(cmdstr)
    Dim shell, exec, stdout
    Set shell = CreateObject("WScript.Shell")
    Set exec = shell.Exec(cmdstr)
    Set stdout = exec.StdOut
    ExecSqlCmd = stdout.ReadAll
    Set shell = Nothing
    Set exec = Nothing
    Set stdout = Nothing
End Function ' ExecSqlCmd

Function IsFileStreamEnabled(instanceName)
    Dim wmifs, method, inParam, outParam
    On Error Resume Next ' This WMI call bombs if ComputerManagement10 is AWOL or if FilestreamSettings is missing (such as x86 under WOW64).
    Set wmifs = GetObject("WINMGMTS:\\.\root\Microsoft\SqlServer\ComputerManagement10:FilestreamSettings='" & instanceName & "'")
    If Err.number = 0 Then
        Err.Clear
        On Error Goto 0
        If wmifs.AccessLevel = 0 Then
            Set method = wmifs.Methods_("EnableFilestream")
            Set inParam = method.inParameters.SpawnInstance_()
            inParam.AccessLevel = 2
            inParam.ShareName = ""
            Set outParam = wmifs.ExecMethod_("EnableFilestream", inParam)
            If outParam.returnValue = 0 Then
                IsFileStreamEnabled =  True
            Else
                IsFileStreamEnabled =  False
            End If
        Else
            IsFileStreamEnabled = True
        End If
    Else
        IsFileStreamEnabled = False
        Err.Clear
        On Error Goto 0
    End If ' Err

    Set wmifs = Nothing
    Set method = Nothing
    Set inParam = Nothing
    Set outParam = Nothing
End Function ' IsFileStreamEnabled

Function IsInstanceRunning(serviceName)
    Dim wmi, list, svc, msg

    Set wmi = GetObject("winmgmts:\\.\root\CIMV2")
    Set list = wmi.ExecQuery("SELECT * FROM Win32_Service WHERE Name = '" & serviceName & "'")

    If IsNull(list) Then
        IsInstanceRunning = False
        Exit Function
    End If
    If list.Count = 0 Then
        IsInstanceRunning = False
    End If
    If list.Count > 1 Then
        Err.Raise-1, "IsInstanceRunning", "More than one service found by the name: " & serviceName, Null, Null
    End If
    For Each svc in list
        If svc.State = "Running" Then
            IsInstanceRunning = True
            Exit Function
        Else
            IsInstanceRunning = False
            Exit Function
        End If
    Next
End Function ' IsInstanceRunning

Function ListKatmaiInstances()
    Dim record, combolist, x, y, msg, wmi, list, instance, name, addi, wmifs

    Set wmi = GetObject("WINMGMTS:\\.\root\Microsoft\SqlServer\ComputerManagement10")
    If IsNull(wmi) Then
        ListKatmaiInstances = ERROR_INSTALL_FAILURE
        Exit Function
    End If

    Set list = wmi.ExecQuery("SELECT * FROM SqlServiceAdvancedProperty WHERE SQLServiceType = 1 AND PropertyName = 'InstanceId'")
    ' Bail if we don't get a list of values, since Katmai is missing.
    If IsNull(list) Then
        ListKatmaiInstances = ERROR_SUCCESS
        Exit Function
    End If

    x = 0
    y = 0
    For Each instance In list
        name = Replace(instance.PropertyStrValue, "MSSQL10.", "")
        addi = false

        If IsInstanceRunning(instance.ServiceName) Then
            If IsFileStreamEnabled(name) Then
                msg = "   Added " & name & " (" & x & ") to combobox KATMAI_INSTANCE_LIST. " & instance.ServiceName
                y = y + 1
            Else
                msg = "   SKIPPED " & name & " (" & x & ") because FILESTREAM was not enabled. " & instance.ServiceName
            End If
        Else
            msg = "   SKIPPED " & name & " (" & x & ") because the service was not running. " & instance.ServiceName
        End If
        x = x + 1

        MsgBox msg
    Next

    ' Bail if we don't get any FILESTREAM-enabled instances.
    If y = 0 Then
        MsgBox "!!! No eligible/running Katmai instances were found, including with FILESTREAM enabled for AdventureWorks2008. Make KATMAI_READY_INSTANCES = 0 and continue."
        ListKatmaiInstances = ERROR_SUCCESS
        Exit Function
    End If
    ListKatmaiInstances = ERROR_SUCCESS

End Function ' ListKatmaiInstances()

Function FetchServiceDataProperty(instanceName, propertyName)
    Dim wmi, list, instance
    Set wmi = GetObject("WINMGMTS:\\.\root\Microsoft\SqlServer\ComputerManagement10")
    Set list = wmi.ExecQuery("SELECT * FROM SqlServiceAdvancedProperty WHERE ServiceName = '" & instanceName & "' AND PropertyName ='" & propertyName & "'")
    For Each instance In list
        If instance.PropertyName = propertyName Then
            FetchServiceDataProperty = instance.PropertyStrValue & "\DATA\"
        End If
    Next ' Each
End Function ' ListServiceData

Function ListServiceDataProperty(propertyName)
    Dim wmi, list, instance, msg, version
    Set wmi = GetObject("WINMGMTS:\\.\root\Microsoft\SqlServer\ComputerManagement10")
'    Set list = wmi.ExecQuery("SELECT * FROM SqlServiceAdvancedProperty WHERE ServiceName LIKE '%SQLSERVER2008%' AND SQLServiceType = 1 AND PropertyName LIKE 'Version'")
    Set list = wmi.ExecQuery("SELECT * FROM SqlServiceAdvancedProperty WHERE ServiceName LIKE '%SQL%'")
    msg = "Records: " & CStr(list.Count) & vbCrLf & vbCrLf
    For Each instance In list
        version = instance.PropertyStrValue
        If instance.PropertyName = propertyName Then
            msg = instance.ServiceName & " [" & instance.SQLServiceType & "] : " & instance.PropertyName & " (" & version & ")"
            msg = msg & vbCrLf
            MsgBox msg
        End If
    Next ' Each
End Function ' ListServiceData

Function ListServiceData()
    Dim wmi, list, instance, msg, version
    Set wmi = GetObject("WINMGMTS:\\.\root\Microsoft\SqlServer\ComputerManagement10")
'    Set list = wmi.ExecQuery("SELECT * FROM SqlServiceAdvancedProperty WHERE ServiceName LIKE '%SQLSERVER2008%' AND SQLServiceType = 1 AND PropertyName LIKE 'Version'")
    Set list = wmi.ExecQuery("SELECT * FROM SqlServiceAdvancedProperty WHERE ServiceName LIKE '%SQL%'")
    msg = "Records: " & CStr(list.Count) & vbCrLf & vbCrLf
    For Each instance In list
        version = instance.PropertyStrValue
        msg = instance.ServiceName & " [" & instance.SQLServiceType & "] : " & instance.PropertyName & " (" & version & ")"
        If IsVersion10(instance.ServiceName) Then
            msg = msg & " ***"
        End If
        msg = msg & vbCrLf
        MsgBox msg
    Next ' Each
End Function ' ListServiceData

Function IsVersion10(serviceName)
    Dim wmi, list, svc, version
    On Error Resume Next
    Set wmi = GetObject("WINMGMTS:\\.\root\Microsoft\SqlServer\ComputerManagement10")
    If Err.number <> 0 Then
        Err.Clear
        On Error Goto 0
        IsVersion10 = False
        Exit Function
    End If
    Err.Clear
    On Error Goto 0

    Set list = wmi.ExecQuery("SELECT * FROM SqlServiceAdvancedProperty WHERE ServiceName = '" & serviceName & "' AND SQLServiceType = 1 AND PropertyName LIKE 'Version'")
    If list.Count <> 1 Then
        IsVersion10 = False
        Exit Function
    End If

    For Each svc in list
        version = svc.PropertyStrValue
        If IsNull(version) Then
            IsVersion10 = False
            Exit Function
        End If
        If Len(version) < 2 Then
            IsVersion10 = False
            Exit Function
        End If
        If Left(version, 2) = "10" Then
            IsVersion10 = True
            Exit Function
        End If
    Next ' Each
End Function ' IsVersion10

Function IsFullTextSearchAvailable(instanceName)
    Dim servicename

    If IsNull(instanceName) Then
        IsFullTextSearchAvailable = False
        Exit Function
    End If

    servicename = Replace(LCase(instanceName), "mssql$", "")

    If servicename = "mssqlserver" Then
        servicename = "MSSQLFDLauncher"
    Else
        servicename = "MSSQLFDLauncher$" & servicename
    End If

    MsgBox "Starting service: " & servicename
    IsFullTextSearchAvailable = StartInstance(servicename)
End Function ' IsFullTextSearchAvailable

Sub ListServices()
    Dim wmi, list, svc, msg

    Set wmi = GetObject("winmgmts:\\.\root\CIMV2")
    Set list = wmi.ExecQuery("SELECT * FROM Win32_Service WHERE Name LIKE '%sql%'")

    For Each svc in list
        MsgBox svc.Name & " " & svc.State
    Next
End Sub ' ListServices

Function StartInstance(serviceName)
    Dim wmi, list, svc, msg, record, e

    On Error Resume Next
    Set wmi = GetObject("winmgmts:\\.\root\CIMV2")
    If Err.number <> 0 Then
        Err.Clear
        On Error Goto 0
        StartInstance = False
        Exit Function
    End If ' Err
    Err.Clear
    On Error Goto 0

    Set list = wmi.ExecQuery("SELECT * FROM Win32_Service WHERE Name = '" & serviceName & "'")
    If IsNull(list) Then
        StartInstance = False
        Exit Function
    End If
    If list.Count = 0 Then
        StartInstance = False
    End If
    If list.Count > 1 Then
        Err.Raise-1, "StartInstance", "More than one service found by the name: " & serviceName, Null, Null
    End If

    For Each svc in list
        If svc.State = "Running" Then
            msgbox "Running? " & serviceName
            StartInstance = True
            Exit Function
        Else
'            On Error Resume Next
            svc.StartService()
            e = Err.number
            Err.Clear
            On Error Goto 0
            If e = 0 Then
                msgbox "e = 0? " & serviceName
                StartInstance = IsInstanceRunning(serviceName)
            Else
                StartInstance = False
            End If
            Exit Function
        End If
    Next
End Function ' StartInstance

Function Sleep(seconds)
    Dim starttime, stoptime
    starttime = Timer()
    stoptime = starttime + seconds
    While Timer <= stoptime
        ' Sleeping...
    Wend
End Function ' Sleep
