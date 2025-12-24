using System.Text.Json.Serialization;

namespace SixForce.Models
{
    /// <summary>
    /// 单个数据点
    /// </summary>
    public class DataPoint
    {
        public DateTime Timestamp { get; set; }
        public Dictionary<string, double> MvValues { get; set; } = new();
        public Dictionary<string, double> ForceValues { get; set; } = new();
    }

    /// <summary>
    /// 数据记录
    /// </summary>
    public class DataRecord
    {
        /// <summary>
        /// 记录唯一标识
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 记录名称（基于时间生成）
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 用户注释
        /// </summary>
        public string Comment { get; set; } = string.Empty;

        /// <summary>
        /// 记录开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 记录结束时间
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 记录时长（秒）
        /// </summary>
        [JsonIgnore]
        public double DurationSeconds => (EndTime - StartTime).TotalSeconds;

        /// <summary>
        /// 格式化的时长显示
        /// </summary>
        [JsonIgnore]
        public string DurationDisplay
        {
            get
            {
                var duration = EndTime - StartTime;
                if (duration.TotalHours >= 1)
                    return $"{(int)duration.TotalHours}时{duration.Minutes}分{duration.Seconds}秒";
                if (duration.TotalMinutes >= 1)
                    return $"{duration.Minutes}分{duration.Seconds}秒";
                return $"{duration.Seconds}秒";
            }
        }

        /// <summary>
        /// 数据点列表
        /// </summary>
        public List<DataPoint> DataPoints { get; set; } = new();

        /// <summary>
        /// 数据点数量
        /// </summary>
        [JsonIgnore]
        public int PointCount => DataPoints.Count;

        /// <summary>
        /// 文件路径
        /// </summary>
        [JsonIgnore]
        public string? FilePath { get; set; }
    }
}
