using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Status.Services
{
    /// <summary>
    /// Class to read the Config.ini data file
    /// </summary>
    public class IniFileHandler
    {
        string Path;
        readonly string EXE = Assembly.GetExecutingAssembly().GetName().Name;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern long WritePrivateProfileString(string Key, string Section, string Value, string FilePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string Key, string Section, string Default, StringBuilder RetVal, int Size, string FilePath);

        /// <summary>
        /// set ini path name
        /// </summary>
        /// <param name="IniPath"></param>
        public IniFileHandler(string IniPath = null)
        {
            Path = new FileInfo(IniPath ?? EXE + ".ini").FullName;
        }

        /// <summary>
        /// Read section
        /// </summary>
        /// <param name="Section"></param>
        /// <param name="Key"></param>
        /// <returns></returns>
        public string Read(string Section, string Key = null)
        {
            StringBuilder RetVal = new StringBuilder(255);
            int length = GetPrivateProfileString(Section ?? EXE, Key, "", RetVal, 255, Path);
            return RetVal.ToString();
        }

        /// <summary>
        /// Write section
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        /// <param name="Section"></param>
        public void Write(string Key, string Value, string Section = null)
        {
            WritePrivateProfileString(Section ?? EXE, Key, Value, Path);
        }

        /// <summary>
        /// Delete Key
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Section"></param>
        public void DeleteKey(string Key, string Section = null)
        {
            Write(Key, null, Section ?? EXE);
        }

        /// <summary>
        /// Delete Section
        /// </summary>
        /// <param name="Section"></param>
        public void DeleteSection(string Section = null)
        {
            Write(null, null, Section ?? EXE);
        }

        /// <summary>
        /// Check if Key Exists
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Section"></param>
        /// <returns></returns>
        public bool KeyExists(string Key, string Section = null)
        {
            return Read(Key, Section).Length > 0;
        }
    }

    /// <summary>
    /// Class to Scan a directory for new directories
    /// </summary>
    public class ScanDirectory
    {
        /// <summary>
        /// ScanDirectory default constructor
        /// </summary>
        public ScanDirectory() {}

        /// <summary>
        /// Get the Job XML data
        /// </summary>
        /// <param name="job"></param>
        /// <param name="jobDirectory"></param>
        /// <returns>JobXmlData</returns>
        public Models.JobXmlData GetJobXmlData(string job, string jobDirectory)
        {
            Models.JobXmlData jobScanXmlData = new Models.JobXmlData();
            jobScanXmlData.JobDirectory = jobDirectory;
            jobScanXmlData.JobSerialNumber = job.Substring(0, job.IndexOf("_"));
            int start = job.IndexOf("_") + 1;
            jobScanXmlData.TimeStamp = job.Substring(start, job.Length - start);

            // Wait until the Xml file shows up
            bool xmlFileFound = false;
            do
            {
                string[] files = Directory.GetFiles(jobDirectory, "*.xml");
                if (files.Length > 0)
                {
                    jobScanXmlData.XmlFileName = Path.GetFileName(files[0]);
                    xmlFileFound = true;
                    return jobScanXmlData;
                }

                Thread.Yield();
            }
            while (xmlFileFound == false);

            return jobScanXmlData;
        }
    }
}
