using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace DataOrganiser.Services;

public class FileSystemScanner
{
    public Task<(List<IndexedFile> Files, List<IndexedFolder> Folders)> ScanAsync(
        string rootPath, IReadOnlyList<string> excludedFolders)
    {
        return Task.Run(() =>
        {
            var indexer = new FileSystemIndexer(excludedFolders.ToList());
            var filesBag = new ConcurrentBag<IndexedFile>();
            var foldersBag = new ConcurrentBag<IndexedFolder>();

            indexer.ScanDirectoryParallel(rootPath, filesBag, foldersBag);

            var files = filesBag.OrderByDescending(f => f.Modified)
                                 .ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                                 .ToList();
            var folders = foldersBag.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                                     .ToList();

            return (files, folders);
        });
    }
}
