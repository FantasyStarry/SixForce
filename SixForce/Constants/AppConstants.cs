namespace SixForce.Constants
{
    /// <summary>
    /// 应用程序常量定义
    /// </summary>
    public static class AppConstants
    {
        /// <summary>
        /// Modbus通信相关常量
        /// </summary>
        public static class Modbus
        {
            /// <summary>
            /// 读取请求之间的默认延迟（毫秒）
            /// </summary>
            public const int DefaultReadDelayMs = 20;
            
            /// <summary>
            /// 通道清零操作前的等待时间（毫秒）
            /// </summary>
            public const int ClearChannelWaitTimeMs = 150;
            
            /// <summary>
            /// 默认通信超时时间（毫秒）
            /// </summary>
            public const int DefaultTimeoutMs = 10000;
            
            /// <summary>
            /// 默认从机地址
            /// </summary>
            public const byte DefaultSlaveId = 1;
            
            /// <summary>
            /// 解耦矩阵重试次数
            /// </summary>
            public const int DecouplingRetryCount = 3;
            
            /// <summary>
            /// 解耦矩阵重试延迟（毫秒）
            /// </summary>
            public const int DecouplingRetryDelayMs = 20;
            
            /// <summary>
            /// 解耦矩阵写入间隔（毫秒）
            /// </summary>
            public const int DecouplingWriteIntervalMs = 10;
        }
        
        /// <summary>
        /// 数据采集相关常量
        /// </summary>
        public static class DataAcquisition
        {
            /// <summary>
            /// 默认数据采集间隔（毫秒）
            /// </summary>
            public const int DefaultIntervalMs = 50;
            
            /// <summary>
            /// 错误重试等待时间（毫秒）
            /// </summary>
            public const int ErrorRetryWaitTimeMs = 1000;
            
            /// <summary>
            /// 传感器通道数量
            /// </summary>
            public const int SensorChannelCount = 6;
        }
        
        /// <summary>
        /// 文件路径相关常量
        /// </summary>
        public static class FilePaths
        {
            /// <summary>
            /// 标定数据默认保存路径（相对桌面）
            /// </summary>
            public const string CalibrationDataFolder = "calibration_data";
            
            /// <summary>
            /// 解耦矩阵CSV文件名
            /// </summary>
            public const string DecouplingMatrixCsvFile = "解耦系数.csv";
        }
        
        /// <summary>
        /// 设备型号常量
        /// </summary>
        public static class DeviceModels
        {
            /// <summary>
            /// 503B设备型号标识（不支持解耦功能）
            /// </summary>
            public const string Model503B = "503B";
        }
    }
}