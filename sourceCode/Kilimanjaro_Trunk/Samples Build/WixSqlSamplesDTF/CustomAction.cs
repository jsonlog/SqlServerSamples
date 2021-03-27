using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Deployment.WindowsInstaller;

namespace WixSqlSamplesDTF // http://blog.torresdal.net/2008/10/24/WiXAndDTFUsingACustomActionToListAvailableWebSitesOnIIS.aspx
{
    public class CustomActions
    {
        // If we don't disable pooling, we'll get an error if/when we bounce the service later.
        private const string SQL_CONNECTION_STRING = "Data Source={0}; Initial Catalog=master; Trusted_Connection=True; Connection Timeout = 5; Pooling = false;";
#if DEBUG
        private const bool DEBUG_NONLOGGED_CA_METHODS = true;
#else
        private const bool DEBUG_NONLOGGED_CA_METHODS = false;
#endif

        [CustomAction]
        public static ActionResult MakeInstanceReady(Session session)
        {
            ActionResult rtn = ActionResult.Failure;
            try
            {
                PopDebugWindow(session, "MakeInstanceReady");
                session.Log("*** Begin MakeInstanceReady()");

                string[] args;
                string name, isfsrequired, toleratefailure;
                // Split on |
                session.Log("*** CustomActionData: " + session["CustomActionData"]);
                args = session["CustomActionData"].Split('|');

                name = args[0];
                session.Log("   Service Name: " + name);

                isfsrequired = args[1].ToLower();
                session.Log("***   Is FILESTREAM Required: " + isfsrequired);

                toleratefailure = args[2].ToLower();
                session.Log("***   Tolerate Failure: " + toleratefailure);

                if (toleratefailure.ToLower() == "yes")
                {
                    //    Uninstallation must be more fault tolerant; we want successful uninstall even if this CA fails.
                    try
                    {
                        PrepInstance(session, name, isfsrequired, true);
                        rtn = ActionResult.Success;
                    }
                    catch
                    {
                        // We really don't care what happens at this point.
                    }
                    rtn = ActionResult.Success;
                }
                else
                {
                    rtn = PrepInstance(session, name, isfsrequired, false);
                }

                session.Log("*** End MakeInstanceReady()");
            }
            catch (Exception ex)
            {
                session.Log("!!! CustomActionException::MakeInstanceReady() \n {0}", ex.ToString());
                session.Log("*** End MakeInstanceReady()");
                rtn = ActionResult.Failure;
            }

            return rtn;
        }

        [CustomAction]
        public static ActionResult SetDatabaseInstanceName(Session session)
        {
            try
            {
                PopDebugWindow(session, "SetDatabaseInstanceName");
                session.Log("*** Begin SetDatabaseInstanceName()");

                string targetEngineService = session["KATMAI_ENGINE_INSTANCE_LIST"]; // The value expected is the service name.
                if (targetEngineService == null || targetEngineService.Length == 0 || targetEngineService == string.Empty)
                {
                    session.Log("!!! Error: Invalid/null/empty value in KATMAI_ENGINE_INSTANCE_LIST passed to SetDatabaseInstanceName().");
                    session.Log("*** End SetDatabaseInstanceName()");
                    return ActionResult.Failure;
                }
                session.Log("*** Engine service name received: {0}" + targetEngineService);

                string targetOlapService = session["KATMAI_OLAP_INSTANCE_LIST"]; // The value expected is the service name.
                if (targetOlapService == null || targetOlapService.Length == 0 || targetOlapService == string.Empty)
                {
                    session.Log("!!! Error: Invalid/null/empty value in KATMAI_OLAP_INSTANCE_LIST passed to SetDatabaseInstanceName().");
                    session.Log("*** End SetDatabaseInstanceName()");
                    return ActionResult.Failure;
                }
                session.Log("*** OLAP service name received: {0}" + targetOlapService);

                string targetEngineInstance = targetEngineService.Replace("MSSQL$", string.Empty).ToUpper();
                if (targetEngineInstance == "MSSQLSERVER")
                {
                    targetEngineInstance = ".";
                }
                else
                {
                    targetEngineInstance = @".\" + targetEngineInstance;
                }
                session.Log("*** Engine instance name calculated: {0}", targetEngineInstance);

                string targetOlapInstance = targetEngineService.Replace("MSSQL$", string.Empty).ToUpper();
                if (targetOlapInstance == "MSSQLSERVER")
                {
                    targetOlapInstance = ".";
                }
                else
                {
                    targetOlapInstance = @".\" + targetOlapInstance;
                }
                session.Log("*** Engine instance name calculated: {0}", targetOlapInstance);

                // Metadata that's required for creating databases.
                session["DATABASESERVICENAME"] = targetEngineService;
                session["DATABASEINSTANCENAME"] = targetEngineInstance;
                session["OLAPSERVICENAME"] = targetOlapService;
                session["OLAPINSTANCENAME"] = targetOlapInstance;
                session["SQLSAMPLESDATABASEPATH"] = FetchAdvancedServiceDataProperty(session, targetEngineService, "DATAPATH") + @"\DATA\"; // Fetch database path.
                string svcID = FetchServiceProperty(session, targetEngineService, "STARTNAME");
                if (svcID.ToUpper() == "LOCALSYSTEM")
                {
                    svcID = @"NT AUTHORITY\SYSTEM";
                }
                session["DATABASESERVICEIDENTITY"] = svcID;

                // Default to failure mode.
                bool isEngineRunning = false, isOlapRunning = false, hasFS = false, hasFTS = false;
                session["IS_ENGINE_INSTANCE_RUNNING"] = "No";
                session["IS_OLAP_INSTANCE_RUNNING"] = "No";
                session["IS_FILESTREAM_AVAILABLE"] = "No";
                session["IS_FTS_AVAILABLE"] = "No";

                // Test Engine instance for 10-ness, if necessary.
                if (
                       ((session.Features["F_CreateAdventureWorks"].RequestState == InstallState.Default || session.Features["F_CreateAdventureWorks"].RequestState == InstallState.Local))
                    || ((session.Features["F_CreateAdventureWorksDW"].RequestState == InstallState.Default || session.Features["F_CreateAdventureWorksDW"].RequestState == InstallState.Local))
                    || ((session.Features["F_CreateAdventureWorksLT"].RequestState == InstallState.Default || session.Features["F_CreateAdventureWorksLT"].RequestState == InstallState.Local))
                    || ((session.Features["F_CreateAdventureWorks2008"].RequestState == InstallState.Default || session.Features["F_CreateAdventureWorks2008"].RequestState == InstallState.Local))
                    || ((session.Features["F_CreateAdventureWorksDW2008"].RequestState == InstallState.Default || session.Features["F_CreateAdventureWorksDW2008"].RequestState == InstallState.Local))
                    || ((session.Features["F_CreateAdventureWorksLT2008"].RequestState == InstallState.Default || session.Features["F_CreateAdventureWorksLT2008"].RequestState == InstallState.Local))
                    )
                {
                    if (IsVersion10(session, targetEngineService))
                    {
                        session.Log("*** Instance is Katmai.");

                        // Is the selected instance running?
                        if (CheckServiceState(session, targetEngineService, "Running"))
                        {
                            session.Log("*** Instance is running.");
                            isEngineRunning = true;

                            session["IS_ENGINE_INSTANCE_RUNNING"] = "Yes";
                        }
                        else
                        {
                            session.Log("*** Instance is NOT running. Show warning dialog.");
                            isEngineRunning = false;

                            session["IS_ENGINE_INSTANCE_RUNNING"] = "No";
                        }
                    }
                    else // This should never happen, but...
                    {
                        session.Log("!!! Error: Non-Katmai (version 10) instance passed in KATMAI_ENGINE_INSTANCE_LIST passed to SetDatabaseInstanceName().");
                        session.Log("*** End SetDatabaseInstanceName()");
                        return ActionResult.Failure;
                    }

                    // Are advanced features required (i.e. is AW2k8 requested)?
                    if (session.Features["F_CreateAdventureWorks2008"].RequestState == InstallState.Default
                        || session.Features["F_CreateAdventureWorks2008"].RequestState == InstallState.Local)
                    {
                        session["ARE_FILESTREAM_FTS_REQUIRED"] = "Yes";

                        // Test instance for FILESTREAM.
                        if (IsFilestreamEnabled(session, targetEngineService, targetEngineInstance))
                        {
                            session["IS_FILESTREAM_AVAILABLE"] = "Yes";
                            hasFS = true;
                        }
                        else
                        {
                            session["IS_FILESTREAM_AVAILABLE"] = "No";
                            hasFS = false;
                        }

                        // Test instance for FTS.
                        if (IsFTSEnabled(session, targetEngineService))
                        {
                            session["IS_FTS_AVAILABLE"] = "Yes";
                            hasFTS = true;
                        }
                        else
                        {
                            session["IS_FTS_AVAILABLE"] = "No";
                            hasFTS = false;
                        }
                    }
                    else
                    {
                        session["ARE_FILESTREAM_FTS_REQUIRED"] = "No";
                        hasFS = true;
                        hasFTS = true;
                    }
                }
                else
                {
                    isEngineRunning = true;
                    hasFS = true;
                    hasFTS = true;
                }

                // Test OLAP instance for 10-ness, if necessary.
                if (
                       ((session.Features["F_CreateAdventureWorksOLAP"].RequestState == InstallState.Default || session.Features["F_CreateAdventureWorksOLAP"].RequestState == InstallState.Local))
                    || ((session.Features["F_CreateAdventureWorksOLAP2008"].RequestState == InstallState.Default || session.Features["F_CreateAdventureWorksOLAP2008"].RequestState == InstallState.Local))
                    )
                {
                    if (IsVersion10(session, targetOlapService))
                    {
                        session.Log("*** Instance is Katmai.");

                        // Is the selected instance running?
                        if (CheckServiceState(session, targetOlapService, "Running"))
                        {
                            session.Log("*** Instance is running.");
                            isOlapRunning = true;

                            session["IS_OLAP_INSTANCE_RUNNING"] = "Yes";
                        }
                        else
                        {
                            session.Log("*** Instance is NOT running. Show warning dialog.");
                            isOlapRunning = false;

                            session["IS_OLAP_INSTANCE_RUNNING"] = "No";
                        }
                    }
                    else // This should never happen, but...
                    {
                        session.Log("!!! Error: Non-Katmai (version 10) instance passed in KATMAI_OLAP_INSTANCE_LIST passed to SetDatabaseInstanceName().");
                        session.Log("*** End SetDatabaseInstanceName()");
                        return ActionResult.Failure;
                    }
                }
                else
                {
                    isOlapRunning = true;
                }

                session["SHOW_WARNING_DIALOG"] = (isEngineRunning && hasFS && hasFTS && isOlapRunning ? "No" : "Yes");
                session.Log("*** End SetDatabaseInstanceName()");
            }
            catch (Exception ex)
            {
                session.Log("!!! CustomActionException::SetDatabaseInstanceName() \n {0}" + ex.ToString());
                session.Log("*** End SetDatabaseInstanceName()");
                return ActionResult.Failure;
            }

            return ActionResult.Success;
        }

        private static string FetchServiceProperty(Session session, string serviceName, string propertyName)
        {
            session.Log("=== Begin FetchServiceProperty({0}, {1})", serviceName, propertyName);
            if (serviceName == null || serviceName == string.Empty)
            {
                session.Log("!!! ArgumentNullException for serviceName.");
                throw new ArgumentNullException("serviceName may not be null or empty.");
            }
            if (propertyName == null || propertyName == string.Empty)
            {
                session.Log("!!! ArgumentNullException for propertyName.");
                throw new ArgumentNullException("propertyName may not be null or empty.");
            }
            ManagementObjectSearcher mos = new ManagementObjectSearcher( // http://blogs.msdn.com/sqlexpress/archive/2006/07/29/faq-detecting-sql-server-2005-using-wmi.aspx
                @"ROOT\CIMV2",
                string.Format("SELECT * FROM Win32_Service WHERE Name = '{0}'", serviceName));
            ManagementObjectCollection moc = mos.Get();
            session.Log("--- ManagementObjectCollection.Count == {0}", moc.Count);
            foreach (ManagementObject mo in moc)
            {
                string propertyValue = Convert.ToString(mo[propertyName]);
                session.Log("=== {0} has property {1} with value {2}", serviceName, propertyName, propertyValue);
                session.Log("=== End FetchServiceProperty({0}, {1})", serviceName, propertyName);
                return propertyValue;
            }
            session.Log("!!! {0} does not have a property named {1}", serviceName, propertyName);
            session.Log("!!! End FetchServiceProperty({0}, {1})", serviceName, propertyName);
            return string.Empty;
        }

        [CustomAction]
        public static ActionResult ListKatmaiInstances(Session session)
        {
            try
            {
                session.Log("*** Begin ListKatmaiInstances()");
                PopDebugWindow(session, "ListKatmaiInstances");

                ManagementObjectSearcher mos = new ManagementObjectSearcher( // http://blogs.msdn.com/sqlexpress/archive/2006/07/29/faq-detecting-sql-server-2005-using-wmi.aspx
                    @"ROOT\Microsoft\SqlServer\ComputerManagement10",
                    //"SELECT * FROM SqlServiceAdvancedProperty WHERE SQLServiceType = 1 AND PropertyName = 'InstanceId'");
                    "SELECT * FROM SqlServiceAdvancedProperty WHERE PropertyName = 'InstanceId'");

                View comboBox = session.Database.OpenView("SELECT * FROM `ComboBox`");
                Record record;
                int engine = 0, olap = 0;
                string serviceName = string.Empty
                    , instanceName = string.Empty
                    , serviceType = string.Empty;
                foreach (ManagementObject mo in mos.Get()) // Iterate the list. Sort order doesn't have to be sequential.
                {
                    serviceName = mo["ServiceName"].ToString();
                    instanceName = mo["PropertyStrValue"].ToString();
                    serviceType = mo["SQLServiceType"].ToString();

                    // Test each instance for 10-ish-ness.
                    if (IsVersion10(session, serviceName))
                    {
                        if (serviceType == "1")
                        {
                            session.Log("*** Engine Instance Found:  ServiceName ({0}) PropertyStrValue ({1})", serviceName, instanceName);

                            // Append to combo box table.
                            record = new Record(4); // ComboBox record fields are Property, Order, Value, Text
                            record[1] = "KATMAI_ENGINE_INSTANCE_LIST";
                            record[2] = engine; // Order
                            record[3] = serviceName;
                            record[4] = instanceName.Replace("MSSQL10.", "") + " (Engine)";
                            comboBox.InsertTemporary(record);

                            // Bump the counter.
                            engine++;
                        }
                        else if (serviceType == "5")
                        {
                            session.Log("*** AS Instance Found:  ServiceName ({0}) PropertyStrValue ({1})", serviceName, instanceName);

                            // Append to combo box table.
                            record = new Record(4); // ComboBox record fields are Property, Order, Value, Text
                            record[1] = "KATMAI_OLAP_INSTANCE_LIST";
                            record[2] = olap; // Order
                            record[3] = serviceName;
                            record[4] = instanceName.Replace("MSAS10.", "") + " (OLAP)";
                            comboBox.InsertTemporary(record);

                            // Bump the counter.
                            olap++;
                        }
                        else
                        {
                            session.Log("*** Other Instance Found:  ServiceName ({0}) PropertyStrValue ({1}) SQLServerType ({2})", serviceName, instanceName, serviceType);
                        }
                    }
                    else
                    {
                        session.Log("*** Skipped {0} because it's not a Katmai (version 10) instance of the data engine or AS.", serviceName);
                    }
                }
                mos.Dispose();
                comboBox.Dispose();

                // Set number of Katmai instances found. Log lots.
                session.Log("*** Setting KATMAI_READY_ENGINE_INSTANCES to {0}", engine.ToString());
                session["KATMAI_READY_ENGINE_INSTANCES"] = engine.ToString();
                session.Log("*** Setting KATMAI_READY_OLAP_INSTANCES to {0}", olap.ToString());
                session["KATMAI_READY_OLAP_INSTANCES"] = olap.ToString();

                session.Log("*** End ListKatmaiInstances()");
            }
            catch (ManagementException mex)
            {
                session.Log("!!! Katmai appears to NOT be installed. Set KATMAI_READY_ENGINE_INSTANCES and KATMAI_READY_OLAP_INSTANCES to 0 and continue.");
                session["KATMAI_READY_ENGINE_INSTANCES"] = "0";
                session["KATMAI_READY_OLAP_INSTANCES"] = "0";
                session.Log("!!! CustomActionException::ListKatmaiInstances() \n {0}", mex.ToString());
                session.Log("*** End ListKatmaiInstances()");
                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                session.Log("!!! CustomActionException::ListKatmaiInstances() \n {0}", ex.ToString());
                session.Log("*** End ListKatmaiInstances()");
                return ActionResult.Failure;
            }

            return ActionResult.Success;
        }

        private static bool IsVersion10(Session session, string serviceName)
        {
            session.Log("--- Begin IsVersion10({0})", serviceName);
            if (serviceName == null || serviceName == string.Empty)
            {
                session.Log("!!! ArgumentNullException for serviceName.");
                throw new ArgumentNullException("serviceName may not be null or empty.");
            }
            session.Log("--- Fetch the ManagementObjectSearcher.");
            string query = string.Format("SELECT * FROM SqlServiceAdvancedProperty WHERE ServiceName = '{0}'", serviceName);
            session.Log("--- {0}", query);
            ManagementObjectSearcher mos = new ManagementObjectSearcher( // http://blogs.msdn.com/sqlexpress/archive/2006/07/29/faq-detecting-sql-server-2005-using-wmi.aspx
                @"ROOT\Microsoft\SqlServer\ComputerManagement10",
                query);
            if (mos != null)
            {
                session.Log("--- Fetch the ManagementObjectCollection.");
                ManagementObjectCollection moc = mos.Get();
                session.Log("--- ManagementObjectCollection count == {0}", moc.Count);

                session.Log("--- Iterate the ManagementObjectCollection.");
                foreach (ManagementObject mo in moc)
                {
                    if (Convert.ToString(mo["PropertyName"]) == "VERSION")
                    {
                        string version = Convert.ToString(mo["PropertyStrValue"]);
                        session.Log("--- {0} is version {1}", serviceName, version);
                        session.Log("--- End IsVersion10()");
                        return version.Substring(0, 2) == "10" ? true : false;
                    }
                    else if (serviceName.Contains("MSSQLServerOLAPService")
                        && Convert.ToString(mo["PropertyName"]) == "INSTANCEID"
                        && Convert.ToString(mo["PropertyStrValue"]).Contains("MSAS10."))
                    {
                        session.Log("--- {0} is a version 10 OLAP instance.", serviceName);
                        session.Log("--- End IsVersion10()");
                        return true;
                    }
                    else if (serviceName.Contains("MSOLAP$")
                        && Convert.ToString(mo["PropertyName"]) == "INSTANCEID"
                        && Convert.ToString(mo["PropertyStrValue"]).Contains("MSAS10."))
                    {
                        session.Log("--- {0} is a version 10 OLAP instance.", serviceName);
                        session.Log("--- End IsVersion10()");
                        return true;
                    }
#if DEBUG
                    else
                    {
                        session.Log("--- {0} has {1} with the value {2}", serviceName, Convert.ToString(mo["PropertyName"]), Convert.ToString(mo["PropertyStrValue"]));
                    }
#endif
                }
            }

            session.Log("--- End IsVersion10()");
            return false;
        }

        private static string FetchAdvancedServiceDataProperty(Session session, string serviceName, string propertyName)
        {
            session.Log("--- Begin FetchServiceDataProperty()");
            if (serviceName == null || serviceName == string.Empty)
            {
                session.Log("!!! ArgumentNullException for serviceName.");
                throw new ArgumentNullException("serviceName may not be null or empty.");
            }
            if (propertyName == null || propertyName == string.Empty)
            {
                session.Log("!!! ArgumentNullException for propertyName.");
                throw new ArgumentNullException("propertyName may not be null or empty.");
            }
            ManagementObjectSearcher mos = new ManagementObjectSearcher( // http://blogs.msdn.com/sqlexpress/archive/2006/07/29/faq-detecting-sql-server-2005-using-wmi.aspx
                @"ROOT\Microsoft\SqlServer\ComputerManagement10",
                //string.Format("SELECT * FROM SqlServiceAdvancedProperty WHERE ServiceName = '{0}' AND PropertyName ='{1}'"
                string.Format("SELECT * FROM SqlServiceAdvancedProperty WHERE ServiceName = '{0}'"
                , serviceName
                , propertyName));
            ManagementObjectCollection moc = mos.Get();
            session.Log("--- ManagementObjectCollection.Count == {0}", moc.Count);
            foreach (ManagementObject mo in moc)
            {
                if (mo["PropertyName"].ToString().ToUpper() == propertyName.ToUpper())
                {
                    string property = Convert.ToString(mo["PropertyStrValue"]);
                    session.Log("--- {0} has {1} with the value {2}", serviceName, propertyName, property);
                    session.Log("--- End FetchServiceDataProperty()");
                    return property;
                }
#if DEBUG
                else
                {
                    string property = Convert.ToString(mo["PropertyStrValue"]);
                    session.Log("--- {0} has {1} with the value {2}", serviceName, propertyName, property);
                }
#endif
            }
            session.Log("!!! Failed to find a value for {0}", propertyName);

            session.Log("--- End FetchServiceDataProperty()");
            return string.Empty;
        }

        private static bool IsFilestreamEnabled(Session session, string serviceName, string instanceName)
        {
            session.Log("--- Begin IsFilestreamEnabled({0}, {1}).", serviceName, instanceName);
            if (serviceName == null || serviceName == string.Empty)
            {
                session.Log("!!! ArgumentNullException for serviceName.");
                throw new ArgumentNullException("serviceName may not be null or empty.");
            }
            if (instanceName == null || instanceName == string.Empty)
            {
                session.Log("!!! ArgumentNullException for instanceName.");
                throw new ArgumentNullException("instanceName may not be null or empty.");
            }

            // Check both.
            bool isWmiEnabled = false, isTsqlEnabled = false;

            // Of course, this couldn't be the service name or the instance name from the connection string.
            string filestreamInstanceName = serviceName.Replace("MSSQL$", string.Empty).ToUpper();
            session.Log("--- filestreamInstanceName calculated to be {0}.", filestreamInstanceName);

            ManagementObjectSearcher mos = new ManagementObjectSearcher(
                @"ROOT\Microsoft\SqlServer\ComputerManagement10",
                string.Format("SELECT * FROM FilestreamSettings WHERE InstanceName = '{0}'", filestreamInstanceName));
            ManagementObjectCollection moc = mos.Get();
            session.Log("--- ManagementObjectCollection.Count == {0}", moc.Count);
            foreach (ManagementObject mo in moc)
            {
                string accessLevel = Convert.ToString(mo["AccessLevel"]);
                session.Log("--- FilestreamSettings.AccessLevel for {0} is {1}.", filestreamInstanceName, accessLevel);
                isWmiEnabled = (accessLevel == "0" ? false : true);
                session.Log("--- isWmiEnabled == {0}", isWmiEnabled);
            }

            // Check via T-SQL to ensure filestream is enabled.
            session.Log("--- ExecuteScalar on ({0}) for SELECT ServerProperty('FilestreamEffectiveLevel');", instanceName);
            SqlConnection cnx = new SqlConnection(string.Format(SQL_CONNECTION_STRING, instanceName));
            try
            {
                cnx.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT ServerProperty('FilestreamEffectiveLevel');"
                    , cnx);
                isTsqlEnabled = ((int)cmd.ExecuteScalar() >= 1);
                session.Log("--- isTsqlEnabled == {0}", isTsqlEnabled);
            }
            catch (Exception ex)
            {
                session.Log("!!! SqlException: {0}\n{1}", ex.Message, ex.StackTrace);
            }
            finally
            {
                cnx.Close();
            }

            session.Log("--- End IsFilestreamEnabled({0}).", serviceName);
            return isWmiEnabled && isTsqlEnabled;
        }

        private static bool IsFTSEnabled(Session session, string serviceName)
        {
            session.Log("--- Begin IsFTSEnabled({0})", serviceName);
            if (serviceName == null || serviceName == string.Empty)
            {
                session.Log("!!! ArgumentNullException for serviceName.");
                throw new ArgumentNullException("serviceName may not be null or empty.");
            }
            session.Log("--- Begin IsFTSEnabled({0})", serviceName);
            return CheckServiceState(
                session,
                "MSSQLFDLauncher"
                + (serviceName.ToUpper() == "MSSQLSERVER" ? string.Empty : "$" + serviceName.Replace("MSSQL$", string.Empty)),
                "Running");
        }

        private static bool CheckServiceState(Session session, string serviceName, string targetState)
        {
            session.Log("=== Begin CheckServiceState({0}, {1})", serviceName, targetState);
            if (serviceName == null || serviceName == string.Empty)
            {
                session.Log("!!! ArgumentNullException for serviceName.");
                throw new ArgumentNullException("serviceName may not be null or empty.");
            }
            if (targetState == null || targetState == string.Empty)
            {
                session.Log("!!! ArgumentNullException for targetState.");
                throw new ArgumentNullException("targetState may not be null or empty.");
            }
            ManagementObjectSearcher mos = new ManagementObjectSearcher( // http://blogs.msdn.com/sqlexpress/archive/2006/07/29/faq-detecting-sql-server-2005-using-wmi.aspx
                @"ROOT\CIMV2",
                string.Format("SELECT * FROM Win32_Service WHERE Name = '{0}'", serviceName));
            ManagementObjectCollection moc = mos.Get();
            session.Log("--- ManagementObjectCollection.Count == {0}", moc.Count);
            foreach (ManagementObject mo in moc)
            {
                string state = mo["State"].ToString();
                session.Log("=== Service State == {0}", serviceName);
                if (state.ToLower() == targetState.ToLower())
                {
                    session.Log("=== End CheckServiceState({0}, {1})", serviceName, targetState);
                    return true;
                }
            }
            session.Log("=== End CheckServiceState({0}, {1})", serviceName, targetState);
            return false;
        }

        private static void PopDebugWindow(Session session, string methodName)
        {
#if DEBUG
            if (methodName == null || methodName == string.Empty)
            {
                session.Log("!!! ArgumentNullException for methodName.");
                throw new ArgumentNullException("methodName may not be null or empty.");
            }
            if (DEBUG_NONLOGGED_CA_METHODS)
            {
                // Warning: This is necessary because Session.Log() doesn't work for a CA that is invoked by a DoAction ControlEvent.
                // http://n2.nabble.com/DTF-Show-warning-and-log-information-from-a-CA-that-is-invoked-by-a-DoAction-ControlEvent-td2123872.html
                // Don't fully qualify System.Windows.Forms in a using statement or it'll break all the DTFness above due to name collisions.
                System.Windows.Forms.MessageBox.Show(
                      "Attach the debugger here."
                    , "Debugging :: " + methodName
                    , System.Windows.Forms.MessageBoxButtons.OK
                    , System.Windows.Forms.MessageBoxIcon.Warning);
            }
#endif
        }

        private static ActionResult PrepInstance(Session session, string serviceName, string isFSRequired, bool tolerateFailure)
        {
            session.Log("--- Begin PrepInstance({0})", serviceName);
            if (serviceName == null || serviceName == string.Empty)
            {
                session.Log("!!! ArgumentNullException for serviceName.");
                throw new ArgumentNullException("serviceName may not be null or empty.");
            }
            if (isFSRequired == null || isFSRequired == string.Empty)
            {
                session.Log("!!! ArgumentNullException for isFSRequired.");
                throw new ArgumentNullException("isFSRequired may not be null or empty.");
            }
            ActionResult rtn = ActionResult.Failure;
            // Uninstallation must be more fault tolerant; we want successful uninstall even if this CA fails.
            session.Log("--- PrepInstance: Getting ready to start {0}.", serviceName);

            if (!StartInstance(session, serviceName))
            {
                if (!tolerateFailure)
                {
                    session.Log("!!! PrepInstance() cannot proceed because the selected instance was not running and could not be started successfully.");
                    rtn = ActionResult.Failure;
                }
                else
                {
                    session.Log("!!! PrepInstance() cannot proceed because the selected instance was not running and could not be started successfully.");
                    rtn = ActionResult.Success;
                }
            }
            else
            {
                session.Log("--- Service for the instance {0} was running or started successfully. Proceeding normally.", serviceName);
            }

            if (isFSRequired.ToLower() == "yes")
            {
                bool iserr = false;
                string errmsg = string.Empty;

                if (!EnableFileStream(session, serviceName))
                {
                    iserr = false;
                    errmsg = errmsg + "FILESTREAM";
                }

                if (!EnableFullTextSearch(session, serviceName))
                {
                    iserr = true;
                    if (errmsg != string.Empty)
                    {
                        errmsg += " and ";
                    }
                    errmsg += "Full Text Search";
                }

                if (iserr)
                {
                    errmsg = "PrepInstance() failed for " + serviceName
                        + ".\n\nThe following features are missing: "
                        + errmsg
                        + "\n\nFix the problems and re-run setup.";
                    if (!tolerateFailure)
                    {
                        session.Log("!!! " + errmsg);
                        return ActionResult.Failure;
                    }
                    else
                    {
                        session.Log("--- " + errmsg);
                        rtn = ActionResult.Success;
                    }
                }

                session.Log("--- FILESTREAM and Full Text Search were enabled for instance {0}. Proceeding normally.", serviceName);
                rtn = ActionResult.Success;
            }
            else
            {
                session.Log("--- FILESTREAM was not required and was not enabled for instance {0}. Proceeding normally.", serviceName);
                rtn = ActionResult.Success;
            }
            session.Log("--- End PrepInstance({0})", serviceName);
            return rtn;
        }

        private static bool EnableFullTextSearch(Session session, string serviceName)
        {
            session.Log("--- Begin EnableFullTextSearch({0})", serviceName);
            if (serviceName == null || serviceName == string.Empty)
            {
                session.Log("!!! ArgumentNullException for serviceName.");
                throw new ArgumentNullException("serviceName may not be null or empty.");
            }
            bool status = StartInstance(
                session,
                "MSSQLFDLauncher"
                + (serviceName.ToUpper() == "MSSQLSERVER" ? string.Empty : "$" + serviceName.Replace("MSSQL$", string.Empty)));
            session.Log("--- End EnableFullTextSearch({0})", serviceName);
            return status;
        }

        private static bool EnableFileStream(Session session, string serviceName)
        {
            session.Log("--- Begin EnableFileStream({0})", serviceName);
            if (serviceName == null || serviceName == string.Empty)
            {
                session.Log("!!! ArgumentNullException for serviceName.");
                throw new ArgumentNullException("serviceName may not be null or empty.");
            }
            string instanceName = serviceName.Replace("MSSQL$", string.Empty).ToUpper();
            if (instanceName == "MSSQLSERVER")
            {
                instanceName = ".";
            }
            else
            {
                instanceName = @".\" + instanceName;
            }
            session.Log("--- Instance name calculated: {0}", instanceName);

            if (IsFilestreamEnabled(session, serviceName, instanceName))
            {
                return true;
            }

            // Of course, this couldn't be the service name or the instance name from the connection string.
            string filestreamInstanceName = serviceName.Replace("MSSQL$", string.Empty).ToUpper();
            session.Log("--- filestreamInstanceName calculated to be {0}.", filestreamInstanceName);

            ManagementObjectSearcher mos = new ManagementObjectSearcher(
                @"ROOT\Microsoft\SqlServer\ComputerManagement10",
                string.Format("SELECT * FROM FilestreamSettings WHERE InstanceName = '{0}'", filestreamInstanceName));
            ManagementObjectCollection moc = mos.Get();
            session.Log("--- ManagementObjectCollection.Count == {0}", moc.Count);
            foreach (ManagementObject mo in moc)
            {
                string accessLevel = Convert.ToString(mo["AccessLevel"]);
                session.Log("--- FilestreamSettings.AccessLevel for {0} is {1}.", filestreamInstanceName, accessLevel);
                if (accessLevel == "0")
                {
                    session.Log("--- AccessLevel == 0");
                    session.Log("--- Get the EnableFilestream method and set AccessLevel to 1.");
                    ManagementBaseObject efsArgs = mo.GetMethodParameters("EnableFilestream");
                    efsArgs.SetPropertyValue("AccessLevel", 1);
                    efsArgs.SetPropertyValue("ShareName", string.Empty);
                    session.Log("--- Invoke EnableFilestream");
                    ManagementBaseObject efsReturn = mo.InvokeMethod("EnableFilestream", efsArgs, null);
                    object efsValue = efsReturn.GetPropertyValue("ReturnValue");
                    long efsCode = Convert.ToInt64(efsValue);
                    session.Log("--- Return value == {0}", efsCode);
                    if (efsCode != 0)
                    {
                        session.Log("!!! Failure to set filestream access level in WMI.");
                        return false;
                    }

                    session.Log("--- Setting filestream via T-SQL.");
                    if (!EnableFileStreamTSQL(session, instanceName))
                    {
                        session.Log("!!! Failed to enable filestream via T-SQL.");
                        session.Log("--- End EnableFileStream({0}) for ", serviceName);
                        return false;
                    }

                    // Restart the service in lieu of reconfigure.
                    session.Log("--- Restarting the service.");
                    if (StopInstance(session, serviceName)
                        && StartInstance(session, serviceName))
                    {
                        session.Log("--- Restarted successfully.");
                        session.Log("--- End EnableFileStream({0}) for ", serviceName);
                        return IsFilestreamEnabled(session, serviceName, instanceName);
                    }
                    else
                    {
                        session.Log("!!! Failed to restart service.");
                        session.Log("!!! End EnableFileStream({0}) for ", serviceName);
                        return false;
                    }
                }
                else
                {
                    session.Log("--- Setting filestream via T-SQL.");
                    if (!EnableFileStreamTSQL(session, instanceName))
                    {
                        session.Log("!!! Failed to enable filestream via T-SQL.");
                        session.Log("--- End EnableFileStream({0}) for ", serviceName);
                        return false;
                    }

                    session.Log("--- End EnableFileStream({0}) for ", serviceName);
                    return IsFilestreamEnabled(session, serviceName, instanceName);
                }
            }

            session.Log("--- End EnableFileStream({0}).", serviceName);
            return false;
        }

        private static bool EnableFileStreamTSQL(Session session, string instanceName)
        {
            session.Log("--- Begin EnableFileStreamTSQL({0})", instanceName);
            if (instanceName == null || instanceName == string.Empty)
            {
                session.Log("!!! ArgumentNullException for instanceName.");
                throw new ArgumentNullException("instanceName may not be null or empty.");
            }

            // Enable via T-SQL to ensure filestream is enabled.
            session.Log("--- ExecuteNonQuery on ({0}) for EXEC sp_configure 'filestream access level', 1;", instanceName);
            SqlConnection cnx = new SqlConnection(string.Format(SQL_CONNECTION_STRING, instanceName));
            try
            {
                cnx.Open();
                SqlCommand cmd = new SqlCommand(
                    "EXEC sp_configure 'filestream access level', 1;"
                    , cnx);
                cmd.ExecuteNonQuery();
                cmd = new SqlCommand(
                                    "RECONFIGURE;"
                                    , cnx);
                cmd.ExecuteNonQuery();
                session.Log("--- End EnableFileStreamTSQL({0})", instanceName);
                return true;
            }
            catch (Exception ex)
            {
                session.Log("!!! SqlException: {0}\n{1}", ex.Message, ex.StackTrace);
            }
            finally
            {
                cnx.Close();
            }
            session.Log("--- End EnableFileStreamTSQL({0})", instanceName);
            return false;
        }

        private static bool StartInstance(Session session, string serviceName)
        {
            session.Log("--- Begin StartInstance({0})", serviceName);
            if (serviceName == null || serviceName == string.Empty)
            {
                session.Log("!!! ArgumentNullException for serviceName.");
                throw new ArgumentNullException("serviceName may not be null or empty.");
            }
            if (CheckServiceState(session, serviceName, "Running"))
            {
                return true;
            }

            ManagementObjectSearcher mos = new ManagementObjectSearcher(
                @"ROOT\CIMV2",
                string.Format("SELECT * FROM Win32_Service WHERE Name = '{0}'", serviceName));
            ManagementObjectCollection moc = mos.Get();
            session.Log("--- ManagementObjectCollection.Count == {0}", moc.Count);
            foreach (ManagementObject mo in moc)
            {
                session.Log("--- Starting instance {0}", serviceName);
                mo.InvokeMethod("StartService", null);
                for (int x = 0; x < 20; x++) // Give it 30 seconds in 3-second increments to start.
                {
                    session.Log("--- Sleep for 5 seconds.");
                    Thread.Sleep(5000);
                    if (CheckServiceState(session, serviceName, "Running"))
                    {
                        session.Log("--- Successfully started " + serviceName);
                        session.Log("--- End StartInstance({0})", serviceName);
                        return true;
                    }
                }
            }
            session.Log("!!! Failure to start " + serviceName);
            session.Log("!!! End StartInstance({0})", serviceName);
            return false;
        }

        private static bool StopInstance(Session session, string serviceName)
        {
            session.Log("--- Begin StopInstance({0})", serviceName);
            if (serviceName == null || serviceName == string.Empty)
            {
                session.Log("!!! ArgumentNullException for serviceName.");
                throw new ArgumentNullException("serviceName may not be null or empty.");
            }
            if (CheckServiceState(session, serviceName, "Stopped"))
            {
                return true;
            }

            ManagementObjectSearcher mos = new ManagementObjectSearcher(
                @"ROOT\CIMV2",
                string.Format("SELECT * FROM Win32_Service WHERE Name = '{0}'", serviceName));
            ManagementObjectCollection moc = mos.Get();
            session.Log("--- ManagementObjectCollection.Count == {0}", moc.Count);
            foreach (ManagementObject mo in moc)
            {
                session.Log("--- Stopping instance " + serviceName);
                mo.InvokeMethod("StopService", null);
                for (int x = 0; x < 20; x++) // Give it 30 seconds in 3-second increments to stop.
                {
                    session.Log("--- Sleep for 5 seconds.");
                    Thread.Sleep(5000);
                    if (CheckServiceState(session, serviceName, "Stopped"))
                    {
                        session.Log("--- Successfully stopped " + serviceName);
                        session.Log("--- End StopInstance({0})", serviceName);
                        return true;
                    }
                }
            }
            session.Log("!!! Failure to stop " + serviceName);
            session.Log("!!! End StopInstance({0})", serviceName);
            return false;
        }
    }
}
