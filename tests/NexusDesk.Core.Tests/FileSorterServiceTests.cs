using System;
using System.IO;
using System.Threading.Tasks;
using NexusDesk.Core.Services;
using Xunit;

namespace NexusDesk.Core.Tests
{
    public class FileSorterServiceTests : IDisposable
    {
        private readonly string _testTempDir;

        public FileSorterServiceTests()
        {
            _testTempDir = Path.Combine(Path.GetTempPath(), "NexusDeskTests_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testTempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testTempDir))
            {
                try
                {
                    Directory.Delete(_testTempDir, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [Fact]
        public async Task ProcessDirectoryAsync_SortsFilesIntoCategoriesAndLogsHistory()
        {
            // Arrange
            var sorter = new FileSorterService();
            
            // Create test files
            var docFile = Path.Combine(_testTempDir, "resume.pdf");
            var imgFile = Path.Combine(_testTempDir, "photo.jpg");
            var videoFile = Path.Combine(_testTempDir, "movie.mp4");
            var archiveFile = Path.Combine(_testTempDir, "backup.zip");
            var otherFile = Path.Combine(_testTempDir, "unknown.xyz");

            await File.WriteAllTextAsync(docFile, "some doc content");
            await File.WriteAllTextAsync(imgFile, "some img content");
            await File.WriteAllTextAsync(videoFile, "some video content");
            await File.WriteAllTextAsync(archiveFile, "some archive content");
            await File.WriteAllTextAsync(otherFile, "some other content");

            // Act
            await sorter.ProcessDirectoryAsync(_testTempDir);

            // Assert
            var dateStr = DateTime.Now.ToString("yyyy-MM-dd");
            var dateFolder = Path.Combine(_testTempDir, dateStr);

            Assert.True(Directory.Exists(Path.Combine(dateFolder, "Документы")));
            Assert.True(Directory.Exists(Path.Combine(dateFolder, "Изображения")));
            Assert.True(Directory.Exists(Path.Combine(dateFolder, "Видео")));
            Assert.True(Directory.Exists(Path.Combine(dateFolder, "Архивы")));
            Assert.True(Directory.Exists(Path.Combine(dateFolder, "Другое")));

            Assert.True(File.Exists(Path.Combine(dateFolder, "Документы", "resume.pdf")));
            Assert.True(File.Exists(Path.Combine(dateFolder, "Изображения", "photo.jpg")));
            Assert.True(File.Exists(Path.Combine(dateFolder, "Видео", "movie.mp4")));
            Assert.True(File.Exists(Path.Combine(dateFolder, "Архивы", "backup.zip")));
            Assert.True(File.Exists(Path.Combine(dateFolder, "Другое", "unknown.xyz")));

            // Check history CSV
            var historyPath = Path.Combine(_testTempDir, "sort_history.csv");
            Assert.True(File.Exists(historyPath));

            var historyLines = await File.ReadAllLinesAsync(historyPath);
            Assert.True(historyLines.Length >= 6); // Header + 5 files
            Assert.Equal("Дата;Имя файла;Откуда;Куда;Категория", historyLines[0]);
        }

        [Fact]
        public async Task ProcessDirectoryAsync_HandlesNameCollisions()
        {
            // Arrange
            var sorter = new FileSorterService();
            var dateStr = DateTime.Now.ToString("yyyy-MM-dd");
            var docFolder = Path.Combine(_testTempDir, dateStr, "Документы");
            Directory.CreateDirectory(docFolder);

            // Pre-create file with same name
            var existingFile = Path.Combine(docFolder, "note.txt");
            await File.WriteAllTextAsync(existingFile, "existing");

            // Source file with same name
            var sourceFile = Path.Combine(_testTempDir, "note.txt");
            await File.WriteAllTextAsync(sourceFile, "new");

            // Act
            await sorter.ProcessDirectoryAsync(_testTempDir);

            // Assert
            Assert.True(File.Exists(existingFile));
            var collidedFile = Path.Combine(docFolder, "note_1.txt");
            Assert.True(File.Exists(collidedFile));
            Assert.Equal("new", await File.ReadAllTextAsync(collidedFile));
        }

        [Fact]
        public async Task ProcessDirectoryAsync_ThrowsDirectoryNotFoundException_WhenDirDoesNotExist()
        {
            // Arrange
            var sorter = new FileSorterService();
            var nonExistentDir = Path.Combine(_testTempDir, "non_existent_folder_12345");

            // Act & Assert
            await Assert.ThrowsAsync<DirectoryNotFoundException>(() => sorter.ProcessDirectoryAsync(nonExistentDir));
        }
    }
}
