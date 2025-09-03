using SixForce.Models;

namespace SixForce.Services
{
    public interface IModbusService: IDisposable
    {
        bool IsConnected { get; }
        byte SlaveId { get; set; } // 支持动态从机地址
        void Connect(string portName, int baudRate);
        void Disconnect();
        void StartReading(
            Action<Dictionary<string, (string mvValue, string forceValue)>> dataReceivedCallback, 
            Action<Exception> errorCallback,
            int interval = 50);
        void StopReading();
        Task ClearChannelAsync(int channel);
        void SetRegisterMap(String key, ModbusRegisterMap map);
        Task<int[,]> ReadDecouplingMatrixAsync();
        Task WriteDecouplingMatrixAsync(int[,] matrix);
        Task SaveParametersAsync();
    }
}
