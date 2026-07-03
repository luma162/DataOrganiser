using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace DataOrganiser
{
    
    public class FileSystemIndexer
    {
        //private readonly string[] _excludedFolders;

        //ExcludedFoldersManager _excludedFoldersManager = new ExcludedFoldersManager();
        //private IEnumerable<object> _excludedFolders;

        private List<String> _excludedFolders;

        //private List<string> _excludedFolders;

        //public FileSystemIndexer(string[] excludedFolders)
        //{
        //    //_excludedFolders = excludedFolders ?? Array.Empty<string>();
        //List<string> _excludedFolders = _excludedFoldersManager.ExcludedFolders;

        //}

        public FileSystemIndexer(List<string> excludedFolders)
        {
            _excludedFolders = excludedFolders ?? new List<string>();
        }

        public bool IsExcludedFolder(string path)
        {
            foreach (var folder in _excludedFolders)
            {
                if (path.IndexOf(folder, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        public bool IsReparsePoint(string path)
        {
            var attr = File.GetAttributes(path);
            return (attr & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
        }

        public void ScanDirectoryParallel(
            string rootPath,
            ConcurrentBag<IndexedFile> filesBag,
            ConcurrentBag<IndexedFolder> foldersBag)
        {
            if (IsExcludedFolder(rootPath) || IsReparsePoint(rootPath))
                return;

            try
            {
                var dirInfo = new DirectoryInfo(rootPath);
                foldersBag.Add(new IndexedFolder
                {
                    Name = dirInfo.Name,
                    FullPath = dirInfo.FullName,
                    Created = dirInfo.CreationTime,
                    Modified = dirInfo.LastWriteTime
                });

                var subDirs = Directory.EnumerateDirectories(rootPath);

                System.Threading.Tasks.Parallel.ForEach(subDirs, dir =>
                {
                    try
                    {
                        if (IsReparsePoint(dir))
                            return;

                        ScanDirectoryParallel(dir, filesBag, foldersBag);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // skip
                    }
                });

                foreach (var file in Directory.EnumerateFiles(rootPath, "*.*", SearchOption.TopDirectoryOnly))
                {
                    var info = new FileInfo(file);

                    if (string.IsNullOrEmpty(info.Extension))
                        continue;

                    filesBag.Add(new IndexedFile
                    {
                        Name = info.Name,
                        FullPath = info.FullName,
                        Extension = info.Extension,
                        SizeBytes = info.Length,
                        Created = info.CreationTime,
                        Modified = info.LastWriteTime
                    });
                }
            }
            catch (UnauthorizedAccessException)
            {
                // skip rootPath
            }
        }
    }
}