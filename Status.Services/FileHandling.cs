using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace Status.Services
{
    /// <summary>
    /// Class for file copy, move and delete handling
    /// </summary>
    public class FileHandling
    {
        private static readonly Object FileLock = new Object();

        /// <summary>
        /// CopyFolderContents - Copy files and folders from source to destination and optionally remove source files/folders
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destinationPath"></param>
        /// <param name="removeSource"></param>
        /// <param name="overwrite"></param>
        public static void CopyFolderContents(string sourcePath, string destinationPath,
            bool removeSource = false, bool overwrite = false)
        {
            DirectoryInfo sourceDI = new DirectoryInfo(sourcePath);
            if (sourceDI == null)
            {
                StaticClass.Logger.LogError("FileHandling sourceDI failed to instantiate");
            }

            DirectoryInfo destinationDI = new DirectoryInfo(destinationPath);
            if (destinationDI == null)
            {
                StaticClass.Logger.LogError("FileHandling destinationDI failed to instantiate");
            }

            StaticClass.Log(String.Format("CopyFolderContents from {0} -> {1}", sourcePath, destinationPath));

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
                if (destFile == null)
                {
                    StaticClass.Log("FileHandling destFile failed to instantiate");
                }

                // Delete the destination file if overwrite is true
                if (destFile.Exists && overwrite)
                {
                    //lock (FileLock)
                    {
                        destFile.Delete();
                    }
                }

                sourceFile.CopyTo(Path.Combine(destinationDI.FullName, sourceFile.Name));

                // Delete the source file if removeSource is true
                if (removeSource)
                {
                    //lock (FileLock)
                    {
                        sourceFile.Delete();
                    }
                }
            }

            // Delete the source directory if removeSource is true
            if (removeSource)
            {
                //lock (FileLock)
                {
                    sourceDI.Delete();
                }
            }
        }

        /// <summary>
        /// Copy file from source to target
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="targetFile"></param>
        public static void CopyFile(string sourceFile, string targetFile)
        {
            FileInfo Source = new FileInfo(sourceFile);
            if (Source == null)
            {
                StaticClass.Logger.LogError("FileHandling Source failed to instantiate");
            }

            FileInfo Target = new FileInfo(targetFile);
            if (Target == null)
            {
                StaticClass.Logger.LogError("FileHandling Target failed to instantiate");
            }

            if (Target.Exists)
            {
                // Delete the Target file first
                //lock (FileLock)
                {
                    Target.Delete();
                }
            }

            // Copy to target file
            //lock (FileLock)
            {
                Source.CopyTo(targetFile);
            }

            StaticClass.Log(String.Format("Copied {0} -> {1}", sourceFile, targetFile));
        }

        /// <summary>
        /// Deletes a directory after deleting files inside
        /// </summary>
        /// <param name="targetDirectory"></param>
        public static void DeleteDirectory(string targetDirectory)
        {
            // First delete all files in target directory
            string[] files = Directory.GetFiles(targetDirectory);
            foreach (string file in files)
            {
              //lock (FileLock)
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
            }

            //lock (FileLock)
            {
                // Then delete the directory
                Directory.Delete(targetDirectory, false);
            }

            StaticClass.Log(String.Format("Deleted Directory {0}", targetDirectory));
        }
    }
}
