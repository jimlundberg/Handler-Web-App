using System;
using System.IO;

namespace Status.Services
{
    /// <summary>
    /// Class for file copy, move and delete handling
    /// </summary>
    public class FileHandling
    {
        private static readonly Object fileLock = new Object();

        /// <summary>
        /// CopyFolderContents - Copy files and folders from source to destination and optionally remove source files/folders
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destinationPath"></param>
        /// <param name="logFile"></param>
        /// <param name="removeSource"></param>
        /// <param name="overwrite"></param>
        public static void CopyFolderContents(string sourcePath, string destinationPath, string logFile,
            bool removeSource = false, bool overwrite = false)
        {
            DirectoryInfo sourceDI = new DirectoryInfo(sourcePath);
            DirectoryInfo destinationDI = new DirectoryInfo(destinationPath);

            StaticClass.Log(logFile, String.Format("CopyFolderContents from {0} to {1}", sourcePath, destinationPath));

            // If the destination directory does not exist, create it
            if (!destinationDI.Exists)
            {
                destinationDI.Create();
            }

            // Copy files one by one
            FileInfo[] sourceFiles = sourceDI.GetFiles();
            foreach (FileInfo sourceFile in sourceFiles)
            {
                // This is the destination folder plus the new filename
                FileInfo destFile = new FileInfo(Path.Combine(destinationDI.FullName, sourceFile.Name));

                // Delete the destination file if overwrite is true
                if (destFile.Exists && overwrite)
                {
                    lock (fileLock)
                    {
                        destFile.Delete();
                    }
                }

                sourceFile.CopyTo(Path.Combine(destinationDI.FullName, sourceFile.Name));

                // Delete the source file if removeSource is true
                if (removeSource)
                {
                    lock (fileLock)
                    {
                        sourceFile.Delete();
                    }
                }
            }

            // Delete the source directory if removeSource is true
            if (removeSource)
            {
                lock (fileLock)
                {
                    sourceDI.Delete();
                }
            }
        }

        /// <summary>
        /// Copy file from source to target
        /// </summary>
        /// <param name="logFile"></param>
        /// <param name="sourceFile"></param>
        /// <param name="targetFile"></param>
        public static void CopyFile(string sourceFile, string targetFile, string logFile)
        {
            FileInfo Source = new FileInfo(sourceFile);
            FileInfo Target = new FileInfo(targetFile);

            if (Target.Exists)
            {
                // Delete the Target file first
                lock (fileLock)
                {
                    Target.Delete();
                }
            }

            // Copy to target file
            lock (fileLock)
            {
                Source.CopyTo(targetFile);
            }

            StaticClass.Log(logFile, String.Format("Copied {0} -> {1}", sourceFile, targetFile));
        }

        /// <summary>
        /// Deletes a directory after deleting files inside
        /// </summary>
        /// <param name="targetDirectory"></param>
        /// <param name="logFile"></param>
        public static void DeleteDirectory(string targetDirectory, string logFile)
        {
            // First delete all files in target directory
            string[] files = Directory.GetFiles(targetDirectory);
            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            // Then delete directory
            Directory.Delete(targetDirectory, false);

            StaticClass.Log(logFile, String.Format("Deleted Directory {0}", targetDirectory));
        }
    }
}
