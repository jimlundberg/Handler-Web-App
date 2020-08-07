using System;
using System.IO;
using System.Threading;

namespace Status.Services
{
    /// <summary>
    /// Class for file copy, move and delete handling
    /// </summary>
    public class FileHandling
    {
        private static Object FileLock = new Object();

        /// <summary>
        /// CopyFolderContents - Copy files and folders from source to destination and optionally remove source files/folders
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destinationPath"></param>
        /// <param name="removeSource"></param>
        /// <param name="overwrite"></param>
        public static void CopyFolderContents(string sourcePath, string destinationPath, bool removeSource = false, bool overwrite = false)
        {
            DirectoryInfo sourceDI = new DirectoryInfo(sourcePath);
            DirectoryInfo destinationDI = new DirectoryInfo(destinationPath);

            // If the destination directory does not exist, create it
            if (!destinationDI.Exists)
            {
                lock (FileLock)
                {
                    destinationDI.Create();
                }
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
                    lock (FileLock)
                    {
                        destFile.Delete();
                    }
                }

                sourceFile.CopyTo(Path.Combine(destinationDI.FullName, sourceFile.Name));

                // Delete the source file if removeSource is true
                if (removeSource)
                {
                    lock (FileLock)
                    {
                        sourceFile.Delete();
                    }
                }
            }

            // Handle subdirectories
            DirectoryInfo[] dirs = sourceDI.GetDirectories();
            foreach (DirectoryInfo dir in dirs)
            {
                // Get destination folder
                string destination = Path.Combine(destinationDI.FullName, dir.Name);

                // Call CopyFolderContents() recursively
                // Overwrite doesn't matter in the case of a folder.  We just won't need to create it
                CopyFolderContents(dir.FullName, destination, removeSource, overwrite);

                // Delete the source file if removeSource is true
                if (removeSource)
                {
                    lock (FileLock)
                    {
                        dir.Delete();
                    }
                }
            }

            // Delete the source directory if removeSource is true
            if (removeSource)
            {
                lock (FileLock)
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
        public static void CopyFile(String sourceFile, String targetFile)
        {
            FileInfo Source = new FileInfo(sourceFile);
            FileInfo Target = new FileInfo(targetFile);
            if (Target.Exists)
            {
                lock (FileLock)
                {
                    Target.Delete();
                }
            }

            Source.CopyTo(targetFile);
            Console.WriteLine(@"Copied {0} -> {1}", sourceFile, targetFile);
        }
    }
}
