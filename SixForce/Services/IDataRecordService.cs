using SixForce.Models;

namespace SixForce.Services
{
    /// <summary>
    /// 数据记录服务接口
    /// </summary>
    public interface IDataRecordService
    {
        /// <summary>
        /// 获取保存目录路径
        /// </summary>
        string SaveDirectory { get; }

        /// <summary>
        /// 保存记录到文件
        /// </summary>
        Task<string> SaveRecordAsync(DataRecord record);

        /// <summary>
        /// 加载所有记录
        /// </summary>
        Task<List<DataRecord>> LoadAllRecordsAsync();

        /// <summary>
        /// 加载单个记录
        /// </summary>
        Task<DataRecord?> LoadRecordAsync(string filePath);

        /// <summary>
        /// 删除记录
        /// </summary>
        Task<bool> DeleteRecordAsync(string filePath);

        /// <summary>
        /// 设置保存目录
        /// </summary>
        void SetSaveDirectory(string directory);
    }
}
