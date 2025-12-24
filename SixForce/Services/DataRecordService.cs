using SixForce.Models;
using System.IO;
using System.Text.Json;

namespace SixForce.Services
{
    /// <summary>
    /// 数据记录服务实现
    /// </summary>
    public class DataRecordService : IDataRecordService
    {
        private string _saveDirectory;
        private readonly JsonSerializerOptions _jsonOptions;

        public string SaveDirectory => _saveDirectory;

        public DataRecordService()
        {
            // 默认保存到桌面的 SixForce_Records 文件夹
            _saveDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "SixForce_Records");

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            EnsureDirectoryExists();
        }

        public void SetSaveDirectory(string directory)
        {
            _saveDirectory = directory;
            EnsureDirectoryExists();
        }

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_saveDirectory))
            {
                Directory.CreateDirectory(_saveDirectory);
            }
        }

        public async Task<string> SaveRecordAsync(DataRecord record)
        {
            EnsureDirectoryExists();

            string fileName = $"record_{record.StartTime:yyyyMMdd_HHmmss}.json";
            string filePath = Path.Combine(_saveDirectory, fileName);

            string json = JsonSerializer.Serialize(record, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);

            record.FilePath = filePath;
            return filePath;
        }

        public async Task<List<DataRecord>> LoadAllRecordsAsync()
        {
            var records = new List<DataRecord>();

            if (!Directory.Exists(_saveDirectory))
                return records;

            var files = Directory.GetFiles(_saveDirectory, "record_*.json")
                .OrderByDescending(f => f);

            foreach (var file in files)
            {
                try
                {
                    var record = await LoadRecordAsync(file);
                    if (record != null)
                    {
                        record.FilePath = file;
                        records.Add(record);
                    }
                }
                catch
                {
                    // 跳过无法解析的文件
                }
            }

            return records;
        }

        public async Task<DataRecord?> LoadRecordAsync(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            string json = await File.ReadAllTextAsync(filePath);
            var record = JsonSerializer.Deserialize<DataRecord>(json, _jsonOptions);
            
            if (record != null)
                record.FilePath = filePath;

            return record;
        }

        public Task<bool> DeleteRecordAsync(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }
    }
}
