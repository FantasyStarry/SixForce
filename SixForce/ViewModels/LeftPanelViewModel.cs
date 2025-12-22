using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SixForce.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Windows;

namespace SixForce.ViewModels
{
    /// <summary>
    /// 读取控制器接口，用于控制数据读取的启动和停止
    /// </summary>
    public interface IReadingController
    {
        void StartReadingData();
        void StopReading();
        bool IsConnected { get; }
        string ProductId { get; }
        string? SelectedChannel { get; }
        event Action<Dictionary<string, (string mvValue, string forceValue)>>? SensorDataUpdated;
    }

    /// <summary>
    /// 左侧面板视图模型，负责串口连接和设备通信控制
    /// </summary>
    public partial class LeftPanelViewModel : ObservableObject, IDisposable, IReadingController
    {
        private readonly IModbusService _modbusService;
        private readonly IMessageService _messageService;
        private bool _disposed;
        
        /// <summary>
        /// 传感器数据更新事件
        /// </summary>
        public event Action<Dictionary<string, (string mvValue, string forceValue)>>? SensorDataUpdated;

        /// <summary>
        /// 构造函数
        /// </summary>
        public LeftPanelViewModel(
            IModbusService modbusService,
            IMessageService messageService)
        {
            _modbusService = modbusService ?? throw new ArgumentNullException(nameof(modbusService));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

            RefreshPorts();
            SelectedSerialPort = SerialPorts.FirstOrDefault();
            SelectedBaudRate = 115200;
            MachineCode = "1";
        }

        /// <summary>
        /// 可用串口列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> serialPorts = new();

        /// <summary>
        /// 当前选中的串口
        /// </summary>
        [ObservableProperty]
        private string? selectedSerialPort;

        /// <summary>
        /// 可用波特率列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<int> baudRates = new() { 9600, 14400, 19200, 38400, 57600, 115200, 460800 };

        /// <summary>
        /// 当前选中的波特率
        /// </summary>
        [ObservableProperty]
        private int selectedBaudRate;

        /// <summary>
        /// Modbus从机地址
        /// </summary>
        [ObservableProperty]
        private string machineCode = "1";

        /// <summary>
        /// 产品ID
        /// </summary>
        [ObservableProperty]
        private string productId = string.Empty;

        /// <summary>
        /// 可用通道列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> channels = new() { "Fx", "Fy", "Fz", "Mx", "My", "Mz" };

        /// <summary>
        /// 当前选中的通道
        /// </summary>
        [ObservableProperty]
        private string? selectedChannel;

        /// <summary>
        /// 是否已连接
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanConnect))]
        [NotifyPropertyChangedFor(nameof(CanDisconnect))]
        private bool isConnected;

        /// <summary>
        /// 是否可以连接
        /// </summary>
        public bool CanConnect => !IsConnected;

        /// <summary>
        /// 是否可以断开连接
        /// </summary>
        public bool CanDisconnect => IsConnected;

        /// <summary>
        /// 是否正在读取数据
        /// </summary>
        [ObservableProperty]
        private bool isReadingData;

        /// <summary>
        /// 当从机地址改变时的处理
        /// </summary>
        partial void OnMachineCodeChanged(string value)
        {
            if (!byte.TryParse(value, out byte slaveId) || slaveId < 1 || slaveId > 255)
            {
                _messageService.ShowMessage("从机地址必须是1到255之间的整数", "错误");
                MachineCode = "1";
                _modbusService.SlaveId = 1;
            }
            else
            {
                _modbusService.SlaveId = slaveId;
            }
        }

        /// <summary>
        /// 当连接状态改变时的处理
        /// </summary>
        partial void OnIsConnectedChanged(bool value)
        {
            ConnectCommand.NotifyCanExecuteChanged();
            DisconnectCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// 刷新可用串口列表
        /// </summary>
        [RelayCommand]
        private void RefreshPorts()
        {
            try
            {
                var ports = SerialPort.GetPortNames();
                SerialPorts = new ObservableCollection<string>(ports);
                
                if (SerialPorts.Count > 0 && 
                    (SelectedSerialPort == null || !SerialPorts.Contains(SelectedSerialPort)))
                {
                    SelectedSerialPort = SerialPorts[0];
                }
            }
            catch (Exception ex)
            {
                _messageService.ShowMessage($"刷新串口列表失败: {ex.Message}", "错误");
            }
        }

        /// <summary>
        /// 连接设备
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanConnect))]
        private void Connect()
        {
            if (string.IsNullOrEmpty(SelectedSerialPort))
            {
                _messageService.ShowMessage("请选择串口！", "错误");
                return;
            }

            try
            {
                if (!byte.TryParse(MachineCode, out byte slaveId) || slaveId < 1 || slaveId > 255)
                {
                    _messageService.ShowMessage("从机地址无效，请输入1到255之间的整数", "错误");
                    return;
                }

                _modbusService.SlaveId = slaveId;
                _modbusService.Connect(SelectedSerialPort, SelectedBaudRate);
                
                IsConnected = true;
                StartReadingData();
                
                _messageService.ShowMessage($"串口 {SelectedSerialPort} 连接成功！", "提示");
            }
            catch (Exception ex)
            {
                _messageService.ShowMessage($"连接失败: {ex.Message}", "错误");
                IsConnected = false;
                IsReadingData = false;
            }
        }

        /// <summary>
        /// 开始读取数据
        /// </summary>
        public void StartReadingData()
        {
            if (!IsConnected)
            {
                _messageService.ShowMessage("请先连接串口", "提示");
                return;
            }

            try
            {
                IsReadingData = true;
                _modbusService.StartReading(UpdateSensorData, HandleError);
            }
            catch (Exception ex)
            {
                _messageService.ShowMessage($"启动数据读取失败: {ex.Message}", "错误");
                IsReadingData = false;
            }
        }

        /// <summary>
        /// 停止读取数据
        /// </summary>
        public void StopReading()
        {
            if (IsReadingData)
            {
                try
                {
                    _modbusService.StopReading();
                    IsReadingData = false;
                }
                catch (Exception ex)
                {
                    _messageService.ShowMessage($"停止数据读取失败: {ex.Message}", "错误");
                }
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanDisconnect))]
        private void Disconnect()
        {
            try
            {
                StopReading();
                
                // 使用异步方式断开连接，避免阻塞UI
                Task.Run(() =>
                {
                    try
                    {
                        _modbusService.Disconnect();
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"Modbus断开异常: {ex.Message}");
                    }
                });
                
                IsConnected = false;
                _messageService.ShowMessage("串口已断开", "提示");
            }
            catch (Exception ex)
            {
                _messageService.ShowMessage($"断开失败: {ex.Message}", "错误");
                IsConnected = false; // 确保状态更新
            }
        }

        /// <summary>
        /// 更新传感器数据显示
        /// </summary>
        private void UpdateSensorData(Dictionary<string, (string mvValue, string forceValue)> data)
        {
            // 使用BeginInvoke避免阻塞后台线程
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    SensorDataUpdated?.Invoke(data);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"更新传感器数据失败: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 处理通信错误
        /// </summary>
        private void HandleError(Exception ex)
        {
            // 使用BeginInvoke避免阻塞，防止死锁
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    IsConnected = false;
                    IsReadingData = false;
                    ConnectCommand.NotifyCanExecuteChanged();
                    DisconnectCommand.NotifyCanExecuteChanged();

                    string message = ex.Message switch
                    {
                        var s when s.Contains("超时") => "传感器未响应，请检查连接或波特率设置",
                        var s when s.Contains("CRC校验失败") => "数据校验错误，请检查传感器状态",
                        var s when s.Contains("从机地址不匹配") => "从机地址错误，请确认设备地址",
                        var s when s.Contains("获取串口锁超时") => "串口通信冲突，请稍后重试",
                        _ => $"通信错误: {ex.Message}"
                    };

                    _messageService.ShowMessage(message, "错误");
                }
                catch (Exception uiEx)
                {
                    Trace.WriteLine($"UI错误处理异常: {uiEx.Message}");
                    // 避免在错误处理中再次抛出异常
                }
            });
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源（内部实现）
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    StopReading();
                    _modbusService?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}