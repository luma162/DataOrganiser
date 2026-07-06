using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DataOrganiser.Services
{
    public class FileOperationsService
    {
        public FileOperationsService()
        {

        }

        public void Copy(List<IndexedFile> _selectedFiles, List<IndexedFolder> _selectedFolders, string _copyDir)
        {
            List<IndexedFile> selectedFiles = _selectedFiles;
            List<IndexedFolder> selectedFolders = _selectedFolders;

            string targetPath = _copyDir;

            foreach (var file in selectedFiles)
            {
                try
                {
                    string destPath = Path.Combine(targetPath, file.Name);
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file.Name);
                    string ext = Path.GetExtension(file.Name);
                    int count = 1;

                    while (File.Exists(destPath))
                    {
                        destPath = Path.Combine(targetPath, $"{fileNameWithoutExt} ({count}){ext}");
                        count++;
                    }

                    File.Copy(file.FullPath, destPath);
                    file.IsSelected = false;
                }
                catch (Exception ex)
                { System.Windows.MessageBox.Show($"Failed to copy folder: {file.Name}\n{ex.Message}"); }
            }

            foreach (var folder in selectedFolders)
            {
                try
                {
                    string destPath = Path.Combine(targetPath, folder.Name);
                    int count = 1;
                    while (Directory.Exists(destPath))
                    {
                        destPath = Path.Combine(targetPath, $"{folder.Name} ({count})");
                        count++;
                    }
                    CopyDirectory(folder.FullPath, destPath);
                    folder.IsSelected = false;
                }
                catch (Exception ex)
                { System.Windows.MessageBox.Show($"Failed to copy folder: {folder.Name}\n{ex.Message}"); }
            }
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                try
                {
                    File.Copy(file, destFile, true);
                }
                catch { }
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }

        public (List<IndexedFile> RemovedFiles, List<IndexedFolder> RemovedFolders) Delete(List<IndexedFile> _selectedFiles, List<IndexedFolder> _selectedFolders)
        {
            List<IndexedFile> selectedFiles = _selectedFiles;
            List<IndexedFolder> selectedFolders = _selectedFolders;

            List<IndexedFile> removedFiles = new List<IndexedFile>();
            List<IndexedFolder> removedFolders = new List<IndexedFolder>();

            if (selectedFiles.Count == 0 && selectedFolders.Count == 0)
            {
                System.Windows.MessageBox.Show("No files or folders selected for deletion.");
                return (removedFiles, removedFolders);
            }

            string folderWarning = selectedFolders.Count > 0 ? "\n\nWarning: Deleting a folder will also delete all its contents (files and subfolders)." : "";
            if (System.Windows.MessageBox.Show(
                    $"Are you sure you want to delete {selectedFiles.Count} file(s) and {selectedFolders.Count} folder(s)?{folderWarning}",
                    "Confirm Delete",
                    MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            {
                return (removedFiles, removedFolders);
            }

            foreach (var file in selectedFiles)
            {
                try
                {
                    File.Delete(file.FullPath);
                    removedFiles.Add(file);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to delete file: {file.Name}\n{ex.Message}");
                }
            }
            foreach (var folder in selectedFolders)
            {
                try
                {
                    Directory.Delete(folder.FullPath);
                    removedFolders.Add(folder);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to delete file: {folder.Name}\n{ex.Message}");
                }
            }
            return (removedFiles, removedFolders);
        }

        public void Move(List<IndexedFile> selectedFiles, List<IndexedFolder> selectedFolders, string moveDir)
        {

            if (selectedFiles.Count == 0 && selectedFolders.Count == 0)
            {
                System.Windows.MessageBox.Show("No files or folders selected for moving.");
                return;
            }

            foreach (var file in selectedFiles)
            {
                try
                {
                    string destPath = Path.Combine(moveDir, file.Name);
                    File.Move(file.FullPath, destPath);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to move file: {file.Name}\n{ex.Message}");
                }
            }

            foreach (var folder in selectedFolders)
            {
                try
                {
                    string destPath = Path.Combine(moveDir, folder.Name);
                    Directory.Move(folder.FullPath, destPath);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to move folder: {folder.Name}\n{ex.Message}");
                }
            }
            return;
        }
    }
}
