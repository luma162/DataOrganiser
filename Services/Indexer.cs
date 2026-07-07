using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataOrganiser.Services
{
    public class Indexer
    {
        private readonly ExcludedFoldersService ef;
        public Indexer(ExcludedFoldersService excludedFolders)
        {
            ef = excludedFolders;
        }

        public void IndexDirectory(
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
                var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
                Parallel.ForEach(subDirs, options, dir => {      
                    try
                    {
                        if (IsReparsePoint(dir))
                            return;

                        IndexDirectory(dir, filesBag, foldersBag);
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

        public bool IsExcludedFolder(string path)
        {
            foreach (var folder in ef.ExcludedFolders)
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
    }
}
