using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.SqlServer.Samples.MSBuildHelperTasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Microsoft.SqlServer.Samples.MSBuildHelperTasks.Tests
{
    [TestClass()]
    public class WixHelperTaskTest
    {
        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        /// <summary>
        /// A simple test case for string input longer (> 72 characters) than allowed by WiX @Id values.
        /// </summary>
        [TestMethod()]
        public void LegalizeIdValueTest_Length_Pass()
        {
            string input            = "D_QWERTY_UIOP_ASDF_ZXCVVBNM_GHGHGH_QWERTY_UIOP_ASDF_ZXCVVBNM_GHGHGH_QWERTY_UIOP_ASDF_ZXCVVBNM_GHGHGH";
            string expectedoutput   = "D_QWERTY_UIOP_ASDF_ZXCVVBNM_GHGHGH_QWERTY_UIOP_ASDF_ZXCVVBNM_GHGHGH_QWER";
            WixHelperTask target = new WixHelperTask();
            string output = target.LegalizeIdValue(input);
            Assert.AreEqual(expectedoutput, output);
        }

        /// <summary>
        /// A simple test case for string input with illegal characters disallowed in Wix @Id values.
        /// </summary>
        [TestMethod()]
        public void LegalizeIdValueTest_Simple_Pass()
        {
            string input = "F_$a;slkdjf.(-^ASFADS";
            string expectedoutput = "F__a_slkdjf.___ASFADS";
            WixHelperTask target = new WixHelperTask();
            string output = target.LegalizeIdValue(input);
            Assert.AreEqual(expectedoutput, output);
        }

        /// <summary>
        /// A simple test case for string input with illegal characters disallowed in Wix @Id values.
        /// </summary>
        [TestMethod()]
        public void LegalizeIdValueTest_Numeric_Pass()
        {
            string input            = "C_203948-+=#_654321aA.EXt";
            string expectedoutput   = "C_203948_____654321aA.EXt";
            WixHelperTask target = new WixHelperTask();
            string output = target.LegalizeIdValue(input);
            Assert.AreEqual(expectedoutput, output);
        }

        private ITaskItem[] MakeItemArray(string[] stringItems)
        {
            TaskItem[] items = new TaskItem[stringItems.Length];
            for (int x = 0; x < stringItems.Length; x++)
            {
                items[x] = new TaskItem(stringItems[x]);
            }
            return items;
        }

        /// <summary>
        ///A simple test for Execute
        ///</summary>
        [TestMethod()]
        public void ExecuteTest_Baseline_4_Placeholder_txt()
        {
            string workingdir = Path.GetFullPath(@"..\..\..\Samples Setup\\");
            WixHelperTask target = new WixHelperTask();
            target.BuildEngine = new MockEngine(); // To prevent Log* errors during execution.
            target.AppendOnly = false;
            string binaryfolder = Path.Combine(workingdir, "Binary\\");
            string[] textfiles = Directory.GetFiles(binaryfolder, "*.txt");
            string[] bakfiles = Directory.GetFiles(workingdir, "*.bak");
            string[] files = new string[textfiles.Length + bakfiles.Length];
            int x = 0;
            foreach (string file in textfiles)
            {
                files[x] = file;
                x++;
            }
            foreach (string file in bakfiles)
            {
                files[x] = file;
                x++;
            }
            target.Files = MakeItemArray(files);
            target.WorkingDirectory = workingdir;
            target.TemplateWxiFile = Path.Combine(workingdir, @"Files.wxi");
            target.TargetWxiFile = Path.GetFullPath(@"Files_Placeholder_txt.wxi");
            target.FeatureId = "F_SampleFiles";
            target.Win64 = "$(var.Win64)";
            bool expected = true;
            bool actual;
            actual = target.Execute();
            Assert.AreEqual(expected, actual, "True is expected for successful execution.");

            // TODO Validate document contents.
            Assert.Inconclusive("Need to validate document contents.");
        }

        // TODO Multiple call test.
    }
}
