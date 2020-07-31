using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Status.Services
{
    /// <summary>
    /// Class to Read and Ini file data
    /// </summary>
    public class IniFileHandler
    {
        String Path;
        readonly String EXE = Assembly.GetExecutingAssembly().GetName().Name;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern long WritePrivateProfileString(String Key, String Section, String Value, String FilePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(String Key, String Section, String Default, StringBuilder RetVal, int Size, String FilePath);

        public IniFileHandler(String IniPath = null)
        {
            Path = new FileInfo(IniPath ?? EXE + ".ini").FullName;
        }

        public String Read(String Section, String Key = null)
        {
            StringBuilder RetVal = new StringBuilder(255);
            int length = GetPrivateProfileString(Section ?? EXE, Key, "", RetVal, 255, Path);
            return RetVal.ToString();
        }

        public void Write(String Key, String Value, String Section = null)
        {
            WritePrivateProfileString(Section ?? EXE, Key, Value, Path);
        }

        public void DeleteKey(String Key, String Section = null)
        {
            Write(Key, null, Section ?? EXE);
        }

        public void DeleteSection(String Section = null)
        {
            Write(null, null, Section ?? EXE);
        }

        public bool KeyExists(String Key, String Section = null)
        {
            return Read(Key, Section).Length > 0;
        }
    }

    /// <summary>
    /// Class to Scan a directory for new directories
    /// </summary>
    public class ScanDirectory
    {
        public String DirectoryName;
        public String JobDirectory;
        public String JobSerialNumber;
        public String Job;
        public String TimeStamp;
        public String XmlFileName;

        /// <summary>
        /// ScanDirectory constructor
        /// </summary>
        /// <param name="directoryName"></param>
        public ScanDirectory(String directoryName)
        {
            // Save directory name for class use
            DirectoryName = directoryName;
        }

        /// <summary>
        /// Get the Job XML data
        /// </summary>
        /// <param name="jobDirectory"></param>
        /// <returns></returns>
        public StatusModels.JobXmlData GetJobXmlData(String jobDirectory)
        {
            StatusModels.JobXmlData jobScanData = new StatusModels.JobXmlData();
            jobScanData.JobDirectory = jobDirectory;
            jobScanData.Job = jobScanData.JobDirectory.Remove(0, DirectoryName.Length + 1);
            jobScanData.JobSerialNumber = jobScanData.Job.Substring(0, jobScanData.Job.IndexOf("_"));
            int start = jobScanData.Job.IndexOf("_") + 1;
            jobScanData.TimeStamp = jobScanData.Job.Substring(start, jobScanData.Job.Length - start);

            // Wait until the Xml file shows up
            bool XmlFileFound = false;
            do
            {
                String[] files = System.IO.Directory.GetFiles(jobScanData.JobDirectory, "*.xml");
                if (files.Length > 0)
                {
                    jobScanData.XmlFileName = Path.GetFileName(files[0]);
                    XmlFileFound = true;
                    return jobScanData;
                }

                Thread.Sleep(500);
            }
            while (XmlFileFound == false);

            return jobScanData;
        }
    }
}
