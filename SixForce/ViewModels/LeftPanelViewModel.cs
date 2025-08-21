using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SixForce.Services;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Windows;

namespace SixForce.ViewModels
{
    public partial class LeftPanelViewModel : ObservableObject, IDisposable
    {
        private readonly IModbusService _modbusService;
        private readonly IMessageService _messageService;
        private readonly RightPanelViewModel _rightPanelViewModel;
        private bool _disposed;

        public LeftPanelViewModel(IModbusService modbusService,
            IMessageService messageService,
            RightPanelViewModel rightPanelViewModel
        )
        {
            _modbusService = modbusService;
            _messageService = messageService;
            _rightPanelViewModel = rightPanelViewModel;

            // 初始化可用串口列表
            RefreshPorts();

            SelectedSerialPort = serialPorts.FirstOrDefault();
            // 默认选择常用波特率115200
            SelectedBaudRate = 115200;

            MachineCode = "1"; // 默认从机地址
        }

        [ObservableProperty] private ObservableCollection<string> serialPorts = new();

        [ObservableProperty] private string? selectedSerialPort;

        [ObservableProperty]
        private ObservableCollection<int> baudRates = new() { 9600, 14400, 19200, 38400, 57600, 115200, 460800 };

        [ObservableProperty] private int selectedBaudRate;

        [ObservableProperty] private string machineCode = "1";

        [ObservableProperty] private string productId = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> channels = new() { "Fx", "Fy", "Fz", "Mx", "My", "Mz" };

        [ObservableProperty] private string? selectedChannel;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanConnect))]
        [NotifyPropertyChangedFor(nameof(CanDisconnect))]
        private bool isConnected;

        public bool CanConnect => !IsConnected;
        public bool CanDisconnect => IsConnected;

        [ObservableProperty] private bool isReadingData;

        partial void OnMachineCodeChanged(string value)
        {
            if (!byte.TryParse(value, out byte slaveId) || slaveId < 1)
            {
                _messageService.ShowMessage("从机地址必须是1到255之间的整数", "错误");
                MachineCode = "1";
                _modbusService.SlaveId = 1;
            }
            else
            {
                _modbusService.SlaveId = slaveId; // 更新从机地址
            }
        }

        // 当 IsConnected 变化时，刷新按钮状态
        partial void OnIsConnectedChanged(bool value)
        {
            ConnectCommand.NotifyCanExecuteChanged();
            DisconnectCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void RefreshPorts()
        {
            SerialPorts = new ObservableCollection<string>(SerialPort.GetPortNames());
            if (SerialPorts.Count > 0 && SelectedSerialPort != null && !SerialPorts.Contains(SelectedSerialPort))
            {
                SelectedSerialPort = SerialPorts[0];
            }
        }

        [RelayCommand(CanExecute = nameof(CanConnect))]
        private void Connect()
        {
            if (string.IsNullOrEmpty(SelectedSerialPort))
            {
                // 如果没有选择串口，
                _messageService.ShowMessage("请选择串口！", "错误");
                IsConnected = false;
                return;
            }

            try
            {
                // 在连接时设置从机地址
                if (byte.TryParse(MachineCode, out byte slaveId) && slaveId >= 1)
                {
                    _modbusService.SlaveId = slaveId;
                }
                else
                {
                    _messageService.ShowMessage("从机地址无效，请输入1到255之间的整数", "错误");
                    return;
                }

                _modbusService.Connect(SelectedSerialPort, SelectedBaudRate);
                IsConnected = true;
                // 连接成功后可以发送初始化命令等
                // _serialPort.WriteLine("INIT");

                StartReadingData();

                _messageService.ShowMessage($"串口 {SelectedSerialPort} 连接成功！", "提示");
            }
            catch (Exception ex)
            {
                // 处理连接失败
                _messageService.ShowMessage($"连接失败: {ex.Message}", "错误");
                IsConnected = false;
                IsReadingData = false;
            }
        }

        public void StartReadingData()
        {
            if (!IsConnected)
            {
                _messageService.ShowMessage("请先连接串口", "提示");
                return;
            }
            IsReadingData = true;
            _modbusService.StartReading(UpdateSensorData, HandleError);
        }

        public void StopReading()
        {
            if (IsReadingData)
            {
                _modbusService.StopReading();
                IsReadingData = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanDisconnect))]
        private void Disconnect()
        {
            try
            {
                _modbusService.StopReading();
                _modbusService.Disconnect();
                IsConnected = false;
                IsReadingData = false;
                _messageService.ShowMessage("串口已断开", "提示");
            }
            catch (Exception ex)
            {
                _messageService.ShowMessage($"断开失败: {ex.Message}", "错误");
            }
        }

        private void UpdateSensorData(Dictionary<string, (string mvValue, string forceValue)> data)
        {
            // 在主线程更新UI
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 这里可以将数据发送到主ViewModel或直接更新显示
                // 示例：假设我们只更新当前选中的通道
                foreach (var item in data)
                {
                    _rightPanelViewModel.UpdateData(item.Key, item.Value.mvValue, item.Value.forceValue);
                }
            });
        }

        private void HandleError(Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 更新连接状态
                IsConnected = false;
                IsReadingData = false;
                // 通知命令状态更新
                ConnectCommand.NotifyCanExecuteChanged();
                DisconnectCommand.NotifyCanExecuteChanged();

                string message = ex.Message switch
                {
                    { } s when s.Contains("超时") => "传感器未响应，请检查连接或波特率设置",
                    { } s when s.Contains("CRC校验失败") => "数据校验错误，请检查传感器状态",
                    { } s when s.Contains("从机地址不匹配") => "从机地址错误，请确认设备地址",
                    _ => $"通信错误: {ex.Message} "
                };

                _messageService.ShowMessage(message, "错误");
            });
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _modbusService.Dispose();
                _disposed = true;
            }
        }
    }
}