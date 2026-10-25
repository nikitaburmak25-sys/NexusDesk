using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NexusDesk.Core.Services
{
    public class FileSorterService
    {
        public async Task ProcessDirectoryAsync(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
            }

            var files = Directory.GetFiles(directoryPath, "*", SearchOption.TopDirectoryOnly);
            var historyFileName = "sort_history.csv";
            var targetFiles = files.Where(f => !Path.GetFileName(f).Equals(historyFileName, StringComparison.OrdinalIgnoreCase)).ToList();

            if (targetFiles.Count == 0)
            {
                return;
            }

            var dateStr = DateTime.Now.ToString("yyyy-MM-dd");
            var dateFolder = Path.Combine(directoryPath, dateStr);
            var historyPath = Path.Combine(directoryPath, historyFileName);

            foreach (var filePath in targetFiles)
            {
                var category = GetCategory(filePath);
                var categoryFolder = Path.Combine(dateFolder, category);
                Directory.CreateDirectory(categoryFolder);

                var fileName = Path.GetFileName(filePath);
                var destPath = Path.Combine(categoryFolder, fileName);

                if (File.Exists(destPath))
                {
                    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    var ext = Path.GetExtension(fileName);
                    int counter = 1;
                    do
                    {
                        var newFileName = $"{fileNameWithoutExt}_{counter}{ext}";
                        destPath = Path.Combine(categoryFolder, newFileName);
                        counter++;
                    }
                    while (File.Exists(destPath));
                }

                await Task.Run(() => File.Move(filePath, destPath));

                var logDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var logLine = $"{logDate};{fileName};{filePath};{destPath};{category}";

                bool writeHeader = !File.Exists(historyPath);
                if (writeHeader)
                {
                    await File.WriteAllTextAsync(historyPath, "Дата;Имя файла;Откуда;Куда;Категория" + Environment.NewLine);
                }
                await File.AppendAllTextAsync(historyPath, logLine + Environment.NewLine);
            }
        }

        private string GetCategory(string filePath)
        {
            var ext = Path.GetExtension(filePath);
            if (string.IsNullOrEmpty(ext))
            {
                return "Другое";
            }

            if (ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals(".docx", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals(".txt", StringComparison.OrdinalIgnoreCase))
            {
                return "Документы";
            }
            if (ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals(".gif", StringComparison.OrdinalIgnoreCase))
            {
                return "Изображения";
            }
            if (ext.Equals(".mp4", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals(".mkv", StringComparison.OrdinalIgnoreCase))
            {
                return "Видео";
            }
            if (ext.Equals(".zip", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals(".rar", StringComparison.OrdinalIgnoreCase))
            {
                return "Архивы";
            }

            return "Другое";
        }
    }
}
