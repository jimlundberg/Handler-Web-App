using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using StatusModels;

namespace Status.Services
{
    class IniFile
    {
        string Path;
        string EXE = Assembly.GetExecutingAssembly().GetName().Name;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern long WritePrivateProfileString(string Key, string Section, string Value, string FilePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string Key, string Section, string Default, StringBuilder RetVal, int Size, string FilePath);

        public IniFile(string IniPath = null)
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

    public class MonitorDataRepository : IStatusRepository
    {
        private List<StatusMonitorData> _monitorList;
        public MonitorDataRepository()
        {
            _monitorList = new List<StatusMonitorData>()
            {
                new StatusMonitorData() {
                    Job = "1278061_202006181549",
                    JobPrefix = "1278061",
                    JobDirectory = "1278061",
                    IniFileName = "config.ini",
                    XmlFileName = "test.xml",
                    UploadDir = @"C:\SSMCharacterizationHandler\Output Buffer",
                    ProcessingDir = @"C:\SSMCharacterizationHandler\ProcessingBuffer",
                    RepositoryDir = @"C:\SSMCharacterizationHandler\Archive",
                    FinishedDir = @"C:\SSMCharacterizationHandler\Output Buffer",
                    ErrorDir = @"C:\SSMCharacterizationHandler\Error Buffer",
                    ModelerRootDir = @"C:\SSMCharacterizationHandler\Application\Modelers"
                }
            };
        }

        public IEnumerable<StatusData> GetAllStatus()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<StatusMonitorData> GetMonitorStatus()
        {
            //get all the ini parameters

            return _monitorList;
        }

        IEnumerable<StatusMonitorData> IStatusRepository.GetMonitorStatus()
        {
            throw new NotImplementedException();
        }
    }

    public class MockStatusRepository : IStatusRepository
    {
        public List<StatusData> _statusList;
        public MockStatusRepository()
        {
            _statusList = new List<StatusData>()
            {
                new StatusData() { Job = "1278061_202006181549", JobStatus = JobStatus.STARTED, TimeReceived = new DateTime(2020, 6, 18, 8, 15, 0),  TimeStarted = new DateTime(2020, 6, 18, 8, 15, 0),  TimeCompleted = new DateTime(2020, 6, 18, 9, 13, 0) },
                new StatusData() { Job = "1202740_202006171645", JobStatus = JobStatus.RUNNING, TimeReceived = new DateTime(2020, 6, 17, 9, 15, 0),  TimeStarted = new DateTime(2020, 6, 18, 9, 15, 0),  TimeCompleted = new DateTime(2020, 6, 18, 10, 4, 0) },
                new StatusData() { Job = "1278061_202006181549", JobStatus = JobStatus.MONITORING, TimeReceived = new DateTime(2020, 6, 18, 10, 14, 0), TimeStarted = new DateTime(2020, 6, 18, 10, 15, 0), TimeCompleted = new DateTime(2020, 6, 18, 10, 55, 0) },
                new StatusData() { Job = "1278061_202006181549", JobStatus = JobStatus.COMPLETED, TimeReceived = new DateTime(2020, 6, 17, 1, 40, 0),  TimeStarted = new DateTime(2020, 6, 18, 1, 41, 0),  TimeCompleted = new DateTime(2020, 6, 18, 4, 4, 0) },
                new StatusData() { Job = "1278061_202006177423", JobStatus = JobStatus.COPYING, TimeReceived = new DateTime(2020, 6, 16, 1, 40, 0),  TimeStarted = new DateTime(2020, 6, 16, 1, 22, 0),  TimeCompleted = new DateTime(2020, 6, 16, 4, 5, 0) },
                new StatusData() { Job = "1278061_202006181549", JobStatus = JobStatus.STARTED, TimeReceived = new DateTime(2020, 6, 18, 8, 15, 0),  TimeStarted = new DateTime(2020, 6, 18, 8, 15, 0),  TimeCompleted = new DateTime(2020, 6, 18, 9, 13, 0) },
                new StatusData() { Job = "1202740_202006171645", JobStatus = JobStatus.RUNNING, TimeReceived = new DateTime(2020, 6, 17, 9, 15, 0),  TimeStarted = new DateTime(2020, 6, 18, 9, 15, 0),  TimeCompleted = new DateTime(2020, 6, 18, 10, 4, 0) },
                new StatusData() { Job = "1278061_202006181549", JobStatus = JobStatus.MONITORING, TimeReceived = new DateTime(2020, 6, 18, 10, 14, 0), TimeStarted = new DateTime(2020, 6, 18, 10, 15, 0), TimeCompleted = new DateTime(2020, 6, 18, 10, 55, 0) },
                new StatusData() { Job = "1278061_202006181549", JobStatus = JobStatus.COMPLETED, TimeReceived = new DateTime(2020, 6, 17, 1, 40, 0),  TimeStarted = new DateTime(2020, 6, 18, 1, 41, 0),  TimeCompleted = new DateTime(2020, 6, 18, 4, 4, 0) },
                new StatusData() { Job = "1278061_202006177423", JobStatus = JobStatus.COPYING, TimeReceived = new DateTime(2020, 6, 16, 1, 40, 0),  TimeStarted = new DateTime(2020, 6, 16, 1, 22, 0),  TimeCompleted = new DateTime(2020, 6, 16, 4, 5, 0) }
            };
        }

        public IEnumerable<StatusData> GetAllStatus()
        {
            return _statusList;
        }

        public IEnumerable<StatusMonitorData> GetMonitorStatus()
        {
            throw new NotImplementedException();
        }
    }
}
