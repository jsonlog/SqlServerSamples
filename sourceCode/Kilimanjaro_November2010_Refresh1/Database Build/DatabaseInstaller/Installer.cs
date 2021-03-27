using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data;
using System.Data.SqlClient;

namespace Microsoft.SqlServer.DatabaseInstaller
{
    public class Installer
    {
        public string ScriptPath { get; set; }
        public string[] SubstitutionVars { get; set; }
        public string[] SubstitutionValues { get; set; }
        public string DatabaseName { get; set; }
        public string InstanceName { get; set; }

        private StringBuilder _CommandBuilder = new StringBuilder();
        private bool _IsInMultiLineComment = false;

        private SqlConnection _Connection = null;
        private SqlCommand _Command = null;
        private bool _AreVariablesConvertedToReferences = false;

        public event ConnectionInfoMessageEventHandler InfoMessage;

        public void Install(ICancel parentObject)
        {
            string line;

            if (!_AreVariablesConvertedToReferences)
            {
                for (int i = 0; i < SubstitutionVars.Length; i++)
                {
                    SubstitutionVars[i] = MakeVarReference(SubstitutionVars[i]);
                }
                _AreVariablesConvertedToReferences = true;
            }

            try
            {
                ProcessUse("USE master");

                using (StreamReader scriptReader = File.OpenText(ScriptPath))
                {
                    while (!scriptReader.EndOfStream)
                    {
                        line = scriptReader.ReadLine();
                        if (parentObject.IsCancelRequested)
                        {
                            ProcessCancel();
                            return;
                        }
                        ProcessLine(line);
                    }
                }
            }
            catch (System.Exception error)
            {
                if (InfoMessage != null)
                {
                    InfoMessage(this, new ConnectionInfoMessageEventArgs() { Message = "Error: " + error.Message + "\nInstallation Terminated" });
                }
            }
            finally
            {
                MaybeCloseConnection();
            }
        }

        private void ProcessCancel()
        {
            ProcessUse("USE master", 60); //Wait only a minute
            _Command.CommandText = string.Format("ALTER DATABASE {0} SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE {0};", DatabaseName);
            try
            {
                //Try to drop the partialy built database
                _Command.ExecuteNonQuery();
            }
            catch (SqlException)
            {
                //If we can't drop the database, that's OK, continue to cancel.
            }
        }

        private void ProcessLine(string line)
        {
            if (line == null || line.Length == 0) return;
            line = line.TrimStart();
            if (line.StartsWith("--")) return;
            if (line.Contains("/*") && !_IsInMultiLineComment)
            {
                _IsInMultiLineComment = (CountOccurrences(line, "/*") > CountOccurrences(line, "*/"));
                return;
            }

            if (_IsInMultiLineComment)
            {
                if (line.Contains("*/"))
                    _IsInMultiLineComment = (CountOccurrences(line, "/*") >= CountOccurrences(line, "*/"));
                return;
            }

            if (line.StartsWith(":setvar", StringComparison.InvariantCultureIgnoreCase))
            {
                int variableStart = FindFirstNonWhiteSpace(line, 7);
                //Invalid :setvar, ignore.
                if (variableStart == -1) return;
                int variableEnd = FindFirstWhiteSpace(line, variableStart);
                //Invalid :setvar, ignore.
                if (variableEnd == -1) return;
                int valueStart = FindFirstNonWhiteSpace(line, variableEnd);
                //Invalid :setvar, ignore.
                if (valueStart == -1) return;
                MergeSubstitutionValue(line.Substring(variableStart, variableEnd - variableStart),
                    line.Substring(valueStart));
            }

            //Ignore other SQLCMD commands.
            if (line.StartsWith(":")) return;

            if (line.StartsWith("USE", StringComparison.InvariantCultureIgnoreCase))
            {
                ProcessUse(line);
                return;
            }

            if (line.StartsWith("GO", StringComparison.InvariantCultureIgnoreCase)
                && line.Trim().Equals("GO", StringComparison.InvariantCultureIgnoreCase))
            {
                ExecuteBatch(_CommandBuilder.ToString());
                _CommandBuilder.Length = 0;
                return;
            }

            if (ContainsSubstitution(line))
            {
                line = MakeSubstitution(line);
            }

            _CommandBuilder.AppendLine(line);
        }

        private int FindFirstWhiteSpace(string line, int start)
        {
            for (int i = start; i < line.Length; i++)
            {
                if (Char.IsWhiteSpace(line[i])) return i;
            }
            return -1;
        }

        private int FindFirstNonWhiteSpace(string line, int start)
        {
            for (int i = start; i < line.Length; i++)
            {
                if (!Char.IsWhiteSpace(line[i])) return i;
            }
            return -1;
        }

        private int CountOccurrences(string line, string searchString)
        {
            int result = 0;
            int searchAt = 0;

            do
            {
                searchAt = line.IndexOf(searchString, searchAt);
                if (searchAt > -1)
                {
                    result += 1;
                    searchAt += searchString.Length;
                }
                else
                    break;
            }
            while (searchAt < line.Length);

            return result;
        }

        private string MakeVarReference(string variableName)
        {
            return string.Format("$({0})", variableName);
        }

        private void MergeSubstitutionValue(string variableName, string substititionValue)
        {
            int valueIndex = Array.IndexOf<string>(SubstitutionVars, variableName);
            if (valueIndex == -1)
            {
                SubstitutionVars = GrowArray(SubstitutionVars);
                SubstitutionValues = GrowArray(SubstitutionValues);
                valueIndex = SubstitutionVars.Length - 1;
                SubstitutionVars[valueIndex] = MakeVarReference(variableName);
            }
            SubstitutionValues[valueIndex] = substititionValue;
        }

        private string[] GrowArray(string[] stringArray)
        {
            string[] result = new string[stringArray.Length + 1];
            Array.Copy(stringArray, result, stringArray.Length);
            return result;
        }

        private bool ContainsSubstitution(string line)
        {
            for (int i = 0; i < SubstitutionVars.Length; i++)
            {
                if (line.Contains(SubstitutionVars[i])) return true;
            }
            return false;
        }

        private string MakeSubstitution(string line)
        {
            for (int i = 0; i < SubstitutionVars.Length; i++)
            {
                line = line.Replace(SubstitutionVars[i], SubstitutionValues[i]);
            }

            return line;
        }

        private void ProcessUse(string line)
        {
            ProcessUse(line, 5 * 60);  // Wait up to 5 minutes per command
        }

        private void ProcessUse(string line, int commandTimeout)
        {
            MaybeCloseConnection();
            SqlConnectionStringBuilder connectionBuilder = new SqlConnectionStringBuilder()
            {
                DataSource = InstanceName,
                InitialCatalog = ExtractDatabaseName(line.Substring(4).Trim()),
                IntegratedSecurity = true
            };
            _Connection = new SqlConnection(connectionBuilder.ToString());
            _Connection.InfoMessage += new SqlInfoMessageEventHandler(OnConnectionInfoMessage);
            _Connection.Open();
            _Command = _Connection.CreateCommand();
            _Command.CommandTimeout = commandTimeout; // Wait up to 5 minutes per command
            

        }

        void OnConnectionInfoMessage(object sender, SqlInfoMessageEventArgs e)
        {
            if (InfoMessage != null)
                InfoMessage(sender, new ConnectionInfoMessageEventArgs() { Message = e.Message });
        }

        private string ExtractDatabaseName(string line)
        {
            return line.Replace("[", "").Replace("]", "").Replace(";", "");
        }

        private void MaybeCloseConnection()
        {
            if (_Connection != null)
            {
                _Connection.Close();
                _Connection = null;
            }
        }

        private void ExecuteBatch(string batchStatements)
        {
            if (batchStatements.Length > 0)
            {
                _Command.CommandText = batchStatements;
                _Command.ExecuteNonQuery();
            }
        }


    }

    public delegate void ConnectionInfoMessageEventHandler(
	Object sender,
	ConnectionInfoMessageEventArgs e
);


    public class ConnectionInfoMessageEventArgs : EventArgs
    {
        public string Message { get; set; }
    }
}
