﻿using Microsoft.Extensions.Logging;
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
        string Path;
        readonly String EXE = Assembly.GetExecutingAssembly().GetName().Name;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern long WritePrivateProfileString(string Key, string Section, string Value, string FilePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string Key, string Section, string Default, StringBuilder RetVal, int Size, string FilePath);

        public IniFileHandler(string IniPath = null)
        {
            Path = new FileInfo(IniPath ?? EXE + ".ini").FullName;
        }

        public string Read(string Section, string Key = null)
        {
            StringBuilder RetVal = new StringBuilder(255);
            int length = GetPrivateProfileString(Section ?? EXE, Key, "", RetVal, 255, Path);
            return RetVal.ToString();
        }

        public void Write(string Key, string Value, string Section = null)
        {
            WritePrivateProfileString(Section ?? EXE, Key, Value, Path);
        }

        public void DeleteKey(string Key, string Section = null)
        {
            Write(Key, null, Section ?? EXE);
        }

        public void DeleteSection(string Section = null)
        {
            Write(null, null, Section ?? EXE);
        }

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
        public string JobDirectory;
        public string JobSerialNumber;
        public string Job;
        public string TimeStamp;
        public string XmlFileName;
        private static readonly Object xmlLock = new Object();

        /// <summary>
        /// ScanDirectory default constructor
        /// </summary>
        public ScanDirectory()
        {
        }

        /// <summary>
        /// Get the Job XML data
        /// </summary>
        /// <param name="jobDirectory"></param>
        /// <param name="logger"></param>
        /// <returns>JobXmlData</returns>
        public StatusModels.JobXmlData GetJobXmlData(string job, string jobDirectory, ILogger<StatusRepository> logger)
        {
            StatusModels.JobXmlData jobScanData = new StatusModels.JobXmlData();
            jobScanData.JobDirectory = jobDirectory;
            jobScanData.JobSerialNumber = job.Substring(0, job.IndexOf("_"));
            int start = job.IndexOf("_") + 1;
            jobScanData.TimeStamp = job.Substring(start, job.Length - start);

            // Wait until the Xml file shows up
            bool XmlFileFound = false;
            do
            {
                lock (xmlLock)
                {
                    String[] files = Directory.GetFiles(jobDirectory, "*.xml");
                    if (files.Length > 0)
                    {
                        jobScanData.XmlFileName = Path.GetFileName(files[0]);
                        XmlFileFound = true;
                        return jobScanData;
                    }
                }

                Thread.Sleep(500);
            }
            while (XmlFileFound == false);

            return jobScanData;
        }
    }
}
