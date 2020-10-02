using Microsoft.Extensions.Logging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Status.Services
{
    /// <summary>
    /// Class to read the Config.ini data file
    /// </summary>
    public class IniFileHandler
    {
        private readonly string Path;
        private readonly string EXE = Assembly.GetExecutingAssembly().GetName().Name;

        /// <summary>
        /// Write Private Profile string
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Section"></param>
        /// <param name="Value"></param>
        /// <param name="FilePath"></param>
        /// <returns></returns>
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern long WritePrivateProfileString(string Key, string Section, string Value, string FilePath);

        /// <summary>
        /// Get Private Profile string
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Section"></param>
        /// <param name="Default"></param>
        /// <param name="RetVal"></param>
        /// <param name="Size"></param>
        /// <param name="FilePath"></param>
        /// <returns></returns>
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string Key, string Section, string Default, StringBuilder RetVal, int Size, string FilePath);

        /// <summary>
        /// set ini path name
        /// </summary>
        /// <param name="IniPath"></param>
        public IniFileHandler(string IniPath)
        {
            Path = new FileInfo(IniPath ?? EXE + ".ini").FullName;
            if (Path == null)
            {
                StaticClass.Logger.LogError("IniFileHandler Path failed to instantiate");
            }
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
            if (RetVal == null)
            {
                StaticClass.Logger.LogError("IniFileHandler RetVal failed to instantiate");
            }

            GetPrivateProfileString(Section ?? EXE, Key, " ", RetVal, 255, Path);

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
}
