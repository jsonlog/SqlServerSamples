using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.SqlServer.Samples.MSBuildHelperTasks
{
    /// <summary>
    /// MSBuild task to supplement WiX that reads in a WXI template XML file 
    /// and inserts or appends a list of files from the specified directories 
    /// in the Include element.
    /// </summary>
    /// <example>
    /// TODO
    /// </example>
    public class WixHelperTask : Task
    {
        #region Input Parameters
        private ITaskItem[] _files;

        /// <summary>
        /// Gets or sets the array of files to write into nodes in the target WXI file.
        /// </summary>
        /// <value>The array of files to write into nodes in the target WXI file.</value>
        [Required]
        public ITaskItem[] Files
        {
            get { return _files; }
            set { _files = value; }
        }

        private string _workingDirectory = string.Empty;
        /// <summary>
        /// Gets or sets the part of path of the source files to ignore for the purposes of installation.
        /// </summary>
        /// <value>The part of path of the source files to ignore for the purposes of installation.</value>
        public string WorkingDirectory
        {
            get { return _workingDirectory; }
            set { _workingDirectory = value; }
        }

        private string _templateWxiFile = string.Empty;
        /// <summary>
        /// The path to template wxi file.
        /// </summary>
        public string TemplateWxiFile
        {
            get { return _templateWxiFile; }
            set { _templateWxiFile = value; }
        }

        private string _targetWxiFile = string.Empty;
        /// <summary>
        /// The path to write the wxi file to.
        /// </summary>
        [Required()]
        public string TargetWxiFile
        {
            get { return _targetWxiFile; }
            set { _targetWxiFile = value; }
        }

        private bool _appendOnly = true;
        /// <summary>
        /// Append only? If true, the children of template file will not be cleared.
        /// </summary>
        /// <remarks>Default: true</remarks>
        public bool AppendOnly
        {
            get { return _appendOnly; }
            set { _appendOnly = value; }
        }

        private string _featureId = string.Empty;
        /// <summary>
        /// The string name of the feature to add the component(s) to.
        /// </summary>
        [Required()]
        public string FeatureId
        {
            get { return _featureId; }
            set { _featureId = value; }
        }

        private string _win64 = string.Empty;
        /// <summary>
        /// The string value of the Win64 attribute for components.
        /// </summary>
        /// <remarks>
        /// yes|no|or preprocessor script, foex: $(var.PlatformIs64bit)
        /// </remarks>
        [Required()]
        public string Win64
        {
            get { return _win64; }
            set { _win64 = value; }
        }
        #endregion /Input Parameters

        /// <summary>
        /// Reads in the template wxi file, inserts the file elements dictated by the source path
        /// and writes the resulting wxi file to the target path.
        /// </summary>
        /// <returns></returns>
        public override bool Execute()
        {
            if (_files == null || _files.Length == 0)
            {
                Log.LogWarning("No files were specified. Aborting WiXHelperTask.Execute().");
                return false;
            }
            Log.LogMessage(@"
Creating WiX include file: {0}
   Using template file: {1}
   Working Directory:   {2}
   Number of files to be included: {3}
", _targetWxiFile, _templateWxiFile, _workingDirectory, _files.Length);

            XmlDocument xdoc = new XmlDocument();
            FileStream streamXml = null;
            XmlReader xrdr = null;
            try
            {
                if (string.IsNullOrEmpty(_templateWxiFile))
                {
                    // Insert empty XML node.
                    XmlElement incl = xdoc.CreateElement("Include");
                    xdoc.AppendChild(incl);
                }
                else
                {
                    if (!File.Exists(_templateWxiFile))
                    {
                        throw new IOException(string.Format("The file specified for TemplateWxiFile does not exist: {0}", _templateWxiFile));
                    }
                    // Load XML template and clear files.
                    streamXml = new FileStream(_templateWxiFile, FileMode.Open, FileAccess.Read);
                    xrdr = XmlReader.Create(streamXml);
                    xdoc.Load(xrdr);
                    xrdr.Close();
                    xrdr = null;
                    streamXml.Close(); // If we don't close it, we can't overwrite it later as needed...
                    streamXml = null;
                }

                // UNDONE Figure out why this fails if xmls attributes are applied to Include.
                XmlNodeList includes = xdoc.SelectNodes(@"//Include");
                if (includes.Count != 1)
                {
                    throw new XmlException(string.Format("Incorrect number of Include nodes found in the WXI template: {0}; it must be 1.", includes.Count));
                }
                XmlElement include = (XmlElement)includes[0];
                if (include.Name != "Include")
                {
                    throw new XmlException(string.Format("The first child node of the WXI template is <{0}>; it should be <Include>.", include.Name));
                }
                if (!_appendOnly)
                {
                    include.RemoveAll();
                }

                string currentdirectoryname = null;
                XmlElement currentxmlelement = null;
                foreach (ITaskItem fileitem in _files)
                {
                    string filepath = fileitem.ItemSpec;
                    FileInfo fileinfo = new FileInfo(filepath);

                    string thisfilename = fileinfo.Name;
                    string thisdirectoryname = fileinfo.DirectoryName + "\\"; // HACK The FileInfo class returns directories without trailing slashes, which breaks comparisons with _workingDirectory when it has a trailing slash.

                    if (!fileinfo.Exists)
                    {
                        Log.LogWarning("File Specified Is Missing: {0}", fileinfo.FullName);
                        continue;
                    }
                    if (!string.IsNullOrEmpty(_workingDirectory)
                        && filepath.StartsWith(_workingDirectory, true, CultureInfo.InvariantCulture))
                    {
                        filepath = filepath.Remove(0, _workingDirectory.Length);
                    }
                    if (!string.IsNullOrEmpty(_workingDirectory)
                        && thisdirectoryname.StartsWith(_workingDirectory, true, CultureInfo.InvariantCulture))
                    {
                        thisdirectoryname = thisdirectoryname.Remove(0, _workingDirectory.Length);
                    }
                    // No need to repeat this exercise if we just did it.
                    if (currentdirectoryname != thisdirectoryname)
                    {
                        // Reset our root node pointer.
                        currentxmlelement = include;

                        // Append all the directory elements, if they're missing.
                        string[] pathelements = thisdirectoryname.Split(new string[] { @"\" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string dir in pathelements)
                        {
                            // If this node doesn't already exist, append a new one.
                            XmlElement thiselement = (XmlElement)currentxmlelement.SelectSingleNode(string.Format("Directory[@Name='{0}']", dir));
                            if (thiselement == null)
                            {
                                Log.LogMessage(@"   Directory Added: {0}", dir);
                                currentxmlelement = (XmlElement)currentxmlelement.AppendChild(CreateDirectoryElement(xdoc, dir));
                            }
                            else
                            {
                                currentxmlelement = thiselement;
                            }
                        }

                        // Set marker for the path just processed. currentelement already contains our target directory or Include.
                        currentdirectoryname = thisdirectoryname;
                    }
                    // Verify that we have a Component child.
                    XmlElement xmlcomponent = (XmlElement)currentxmlelement.SelectSingleNode("Component");
                    if (xmlcomponent == null)
                    {
                        Log.LogMessage(@"      Component Added for Directory: {0}", currentdirectoryname);
                        xmlcomponent = (XmlElement)currentxmlelement.AppendChild(CreateComponentElement(xdoc, _featureId, CreateGuidString(), _win64));
                    }
                    // Append file element.
                    Log.LogMessage(@"          File Added: {0}", thisfilename);
                    xmlcomponent.AppendChild(CreateFileElement(xdoc, thisfilename, CreateGuidString(), fileinfo.FullName, "1", "yes"));
                }

                // Write output file.
                XmlTextWriter writer = new XmlTextWriter(_targetWxiFile, Encoding.UTF8);
                writer.Formatting = Formatting.Indented;
                xdoc.Save(writer);
                writer.Close();
                writer = null;

                Log.LogMessage(@"Completed WXI generation.");
                return true;
            }
            catch(Exception ex)
            {
                Log.LogErrorFromException(ex, true);
                return false;
            }
            finally
            {
                if (streamXml != null)
                {
                    streamXml.Close();
                }
                streamXml = null;
                if (xrdr != null)
                {
                    xrdr.Close();
                }
                xrdr = null;
                xdoc = null;
            }
        }

        private void AppendDirectoriesAndFiles(XmlDocument xmlDoc, XmlElement thisNode, string currentDirectoryPath, string currentBasePath, string sourceFilePattern, bool sourceRecursively)
        {
            string fullcurrentdir = Path.GetFullPath(currentDirectoryPath);
            string fullcurrentbasepath = Path.GetFullPath(currentBasePath);
            string thisdir = fullcurrentdir.Replace(fullcurrentbasepath, string.Empty);
            XmlElement nextnode = thisNode;

            // HACK There's probably a much simpler pattern for doing this.
            if (thisdir.Length > 0)
            {
                bool recurse = true;
                string dirstring = thisdir;
                while (recurse)
                {
                    if (dirstring.Length > 0)
                    {
                        // Fill in missing directories.
                        if (dirstring[0] == '\\') // Strip off a leading \.
                        {
                            dirstring = dirstring.Substring(1);
                        }
                        int length = dirstring.IndexOf(@"\") > 0 ? dirstring.IndexOf(@"\") : dirstring.Length;
                        string nextdir = dirstring.Substring(0, length);

                        // Insert directory node.
                        XmlElement newnode = CreateDirectoryElement(xmlDoc, nextdir);

                        // Append to the current node.
                        nextnode.AppendChild(newnode);
                        // Set the current node pointer to this node.
                        nextnode = newnode;

                        // Setup to recurse and strip off leading \.
                        length++;
                        if (length > dirstring.Length)
                        {
                            length = dirstring.Length;
                        }
                        dirstring = dirstring.Substring(length);
                    }

                    if (!dirstring.Contains(@"\"))
                    {
                        recurse = false;
                    }
                }
            }

            // Load and append new children for this directory.
            string[] files = Directory.GetFiles(fullcurrentdir, sourceFilePattern, SearchOption.TopDirectoryOnly);
            if (files != null && files.Length > 0)
            {
                // Create the component to contain the files.
                string guidstring = CreateGuidString();
                XmlElement component = CreateComponentElement(xmlDoc, _featureId, guidstring, _win64);

                foreach (string file in files)
                {
                    string fullpath = Path.GetFullPath(file);
                    string fullbase = Path.GetFullPath(_workingDirectory);
                    string filename = Path.GetFileName(file);
                    component.AppendChild(CreateFileElement(xmlDoc, filename, guidstring, fullpath, "1", "yes"));
                }
                nextnode.AppendChild(component);
            }

            if (sourceRecursively)
            {
                // <Directory Id="D_SubdirName" Name="SubdirName">
                // Load directory list for this level.
                string[] directories = Directory.GetDirectories(fullcurrentdir);
                if (directories.Length > 0)
                {
                    foreach (string directory in directories)
                    {
                        AppendDirectoriesAndFiles(xmlDoc, nextnode, directory, fullcurrentdir, sourceFilePattern, sourceRecursively);
                    }
                }
            }
        }

        /// <summary>
        /// Create a formatted Guid and return the string.
        /// </summary>
        /// <returns></returns>
        private string CreateGuidString()
        {
            return string.Format("{{{0}}}", Guid.NewGuid());
        }

        /// <summary>
        /// Create and return a WiX Directory element from the inputs provided.
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="nextDir"></param>
        /// <returns></returns>
        private XmlElement CreateDirectoryElement(XmlDocument xml, string nextDir)
        {
            // <Directory Id="D_SubdirName" Name="SubdirName">
            XmlElement newdirnode = xml.CreateElement("Directory");
            XmlAttribute dirid = xml.CreateAttribute("Id");
            dirid.Value = LegalizeIdValue(string.Format("D_{0}_{1}", nextDir, CreateGuidString()));
            newdirnode.Attributes.Append(dirid);

            XmlAttribute dirname = xml.CreateAttribute("Name");
            dirname.Value = nextDir;
            newdirnode.Attributes.Append(dirname);

            return newdirnode;
        }

        /// <summary>
        /// Create and return a WiX Component element from the inputs provided.
        /// </summary>
        /// <param name="_featureId"></param>
        /// <param name="guidstring"></param>
        /// <param name="_win64"></param>
        /// <returns></returns>
        private XmlElement CreateComponentElement(XmlDocument xml, string featureId, string guidString, string win64)
        {
            // <Component Id="C_Files_??" Guid="guid" Feature="F_SampleFiles" Win64="$(var.Win64)"></Component>
            XmlElement component = xml.CreateElement("Component");

            XmlAttribute cid = xml.CreateAttribute("Id");
            cid.Value = LegalizeIdValue(string.Format("C_{0}_Files_4_{1}"
                , featureId
                , guidString
                ));
            component.Attributes.Append(cid);

            XmlAttribute cguid = xml.CreateAttribute("Guid");
            cguid.Value = guidString;
            component.Attributes.Append(cguid);

            XmlAttribute cfeature = xml.CreateAttribute("Feature");
            cfeature.Value = featureId;
            component.Attributes.Append(cfeature);

            XmlAttribute cwin64 = xml.CreateAttribute("Win64");
            cwin64.Value = win64;
            component.Attributes.Append(cwin64);

            return component;
        }

        /// <summary>
        /// Create and return a WiX File element from the inputs provided.
        /// </summary>
        /// <returns></returns>
        private XmlElement CreateFileElement(XmlDocument xml, string fileName, string guidString, string fullFilePath, string diskIdNumber, string vitalYesNo)
        {
            // <File Id="F_Placeholder_txt" Name="Placeholder.txt" Source="Binary\Placeholder.txt" DiskId="1" Vital="yes" />
            XmlElement node = xml.CreateElement("File");

            XmlAttribute id = xml.CreateAttribute("Id");
            id.Value = LegalizeIdValue(string.Format("F_{0}_{1}", fileName, guidString));
            node.Attributes.Append(id);

            XmlAttribute name = xml.CreateAttribute("Name");
            name.Value = fileName;
            node.Attributes.Append(name);

            XmlAttribute source = xml.CreateAttribute("Source");
            source.Value = fullFilePath.Replace("$", "$$"); // HACK Double dollar signs apparently need to be escaped in Source file paths? Grrr.
            node.Attributes.Append(source);

            XmlAttribute diskid = xml.CreateAttribute("DiskId");
            diskid.Value = diskIdNumber;
            node.Attributes.Append(diskid);

            XmlAttribute vital = xml.CreateAttribute("Vital");
            vital.Value = vitalYesNo;
            node.Attributes.Append(vital);

            return node;
        }

        /// <summary>
        /// Replace all illegal characters in a candidate WiX @Id value.
        /// </summary>
        /// <param name="candiateIdValue"></param>
        /// <returns></returns>
        public string LegalizeIdValue(string candiateIdValue)
        {
            string clean = Regex.Replace(candiateIdValue, @"[^a-z0-9\._]", "_", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            clean = clean.Length <= 72 ? clean : clean.Substring(0, 72); // HACK: There's probably a way to enforce length within Regex.Replace.
            return clean;
        }
    }
}
