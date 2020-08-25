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
        private static readonly Object fileLock = new Object();

        /// <summary>
        /// CopyFolderContents - Copy files and folders from source to destination and optionally remove source files/folders
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destinationPath"></param>
        /// <param name="removeSource"></param>
        /// <param name="overwrite"></param>
        /// <param name="logger"></param>
        public static void CopyFolderContents(string sourcePath, string destinationPath, ILogger<StatusRepository> logger, bool removeSource = false, bool overwrite = false)
        {
            DirectoryInfo sourceDI = new DirectoryInfo(sourcePath);
            DirectoryInfo destinationDI = new DirectoryInfo(destinationPath);

            Console.WriteLine(String.Format("CopyFolderContents from {0} to {1}", sourcePath, destinationPath));

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
                    // Wait for the data.xml file to be ready
                    var destFileReadytask = StaticClass.IsFileReady(destFile.FullName);
                    destFileReadytask.Wait();
                    destFile.Delete();
                }

                sourceFile.CopyTo(Path.Combine(destinationDI.FullName, sourceFile.Name));

                // Delete the source file if removeSource is true
                if (removeSource)
                {
                    // Wait for the data.xml file to be ready
                    var removeSoureFileTask = StaticClass.IsFileReady(sourceFile.Name);
                    removeSoureFileTask.Wait();
                    sourceFile.Delete();
                }
            }

            // Delete the source directory if removeSource is true
            if (removeSource)
            {
                // Wait for the data.xml file to be ready
                var removeSoureDirTask = StaticClass.IsFileReady(sourceDI.Name);
                removeSoureDirTask.Wait();
                sourceDI.Delete();
            }
        }

        /// <summary>
        /// Copy file from source to target
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="targetFile"></param>
        /// <param name="logger"></param>
        public static void CopyFile(string sourceFile, string targetFile, ILogger<StatusRepository> logger)
        {
            FileInfo Source = new FileInfo(sourceFile);
            FileInfo Target = new FileInfo(targetFile);

            if (Target.Exists)
            {
                // Delete the Target file first
                var deleteFileTask = StaticClass.IsFileReady(Target.Name);
                deleteFileTask.Wait();
                Target.Delete();
            }

            // Copy to target file
            var copyFiletask = StaticClass.IsFileReady(targetFile.ToString());
            copyFiletask.Wait();
            Source.CopyTo(targetFile);

            Console.WriteLine("Copied {0} -> {1}", sourceFile, targetFile);
        }
    }
}
