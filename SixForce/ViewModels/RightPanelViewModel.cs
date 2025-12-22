using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SixForce.Models;
using SixForce.Services;
using SixForce.Views;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;

namespace SixForce.ViewModels
{
    public partial class RightPanelViewModel : ObservableObject
    {
        private readonly IModbusService _modbusService;
        private readonly IMessageService _messageService;
        private IReadingController? _readingController;
        private readonly IServiceProvider _serviceProvider;
        private int _demarcateCount = 0;

            public RightPanelViewModel(IModbusService modbusService, IMessageService messageService, IServiceProvider serviceProvider)
            {
                // 初始化时可以添加一些默认数据或其他逻辑
                _modbusService = modbusService;
                _messageService = messageService;
                _serviceProvider = serviceProvider;
        
                ChannelOptions = new ObservableCollection<int>(Enumerable.Range(1, 7));
        
            }
        // 是否显示弹框
        [ObservableProperty]
        private bool isClearChannelDialogOpen;

        // 下拉框数据
        public ObservableCollection<int> ChannelOptions { get; }

        /// <summary>
        /// 订阅读取控制器并设置事件订阅
        /// </summary>
        public void SubscribeToReadingController(IReadingController readingController) 
        {
            // 取消之前的事件订阅（如果存在）
            if (_readingController != null)
            {
                _readingController.SensorDataUpdated -= OnSensorDataUpdated;
            }
            
            _readingController = readingController;
            
            // 订阅传感器数据更新事件
            readingController.SensorDataUpdated += OnSensorDataUpdated;
        }
        
        /// <summary>
        /// 传感器数据更新事件处理
        /// </summary>
        private void OnSensorDataUpdated(Dictionary<string, (string mvValue, string forceValue)> data)
        {
            foreach (var item in data)
            {
                UpdateData(item.Key, item.Value.mvValue, item.Value.forceValue);
            }
        }

        // 选中的通道（默认7 = 全部）
        [ObservableProperty]
        private int selectedChannel = 7;


        [ObservableProperty]
        private ObservableCollection<CalibrationData> calibrationData = new()
        {
            new CalibrationData { Channel = "Fx", MvValue = "0.00", ForceValue = "0.00" },
            new CalibrationData { Channel = "Fy", MvValue = "0.00", ForceValue = "0.00" },
            new CalibrationData { Channel = "Fz", MvValue = "0.00", ForceValue = "0.00" },
            new CalibrationData { Channel = "Mx", MvValue = "0.00", ForceValue = "0.00" },
            new CalibrationData { Channel = "My", MvValue = "0.00", ForceValue = "0.00" },
            new CalibrationData { Channel = "Mz", MvValue = "0.00", ForceValue = "0.00" }
        };

        // 更新数据的方法
        public void UpdateData(string channel, string mvValue, string forceValue)
        {
            var item = CalibrationData.FirstOrDefault(x => x.Channel == channel);
            if (item != null)
            {
                item.MvValue = mvValue;
                item.ForceValue = forceValue;
            }
        }

        [RelayCommand]
        private void ShowClearChannelDialog()
        {
            IsClearChannelDialogOpen = true;
        }

        [RelayCommand]
        private async Task ConfirmClearChannel()
        {
            try
            {
                await _modbusService.ClearChannelAsync(SelectedChannel);

                _messageService.ShowMessage(
                    SelectedChannel == 7 ? "已清零所有通道" : $"已清零通道 {SelectedChannel}",
                    "成功"
                );
            }
            catch (Exception ex)
            {
                _messageService.ShowMessage($"清零失败: {ex.Message}", "错误");
            }
            finally
            {
                IsClearChannelDialogOpen = false;
            }
        }

        [RelayCommand]
        private void CancelClearChannel()
        {
            IsClearChannelDialogOpen = false;
        }

        [RelayCommand]
        private void OpenDecouplingMatrix()
        {
            _readingController?.StopReading();

            var viewModel = _serviceProvider.GetService<DecouplingMatrixViewModel>();
            if (viewModel == null)
                throw new InvalidOperationException("无法解析 DecouplingMatrixViewModel");
            var window = new Window
            {
                Title = "解耦系数编辑",
                Content = new DecouplingMatrixView { DataContext = viewModel },
                Width = 600,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            // 窗口关闭时恢复数据读取
            window.Closed += (s, e) =>
            {
                if (_readingController?.IsConnected == true)
                {
                    _readingController?.StartReadingData();
                }
            };

            window.ShowDialog();
        }

        private string _lastChannel = string.Empty;

        [RelayCommand]
        private void SaveCalibrationData()
        {
            try
            {
                if (_readingController == null)
                {
                    _messageService.ShowMessage("未绑定读取控制器，无法保存标定数据！", "错误");
                    return;
                }

                // 读取 ProductId 和 Channel
                string productId = _readingController.ProductId;
                string channel = _readingController.SelectedChannel ?? "Channel_1";

                // 如果切换了通道，就重置计数
                if (_lastChannel != channel)
                {
                    _demarcateCount = 0;
                    _lastChannel = channel;
                }

                _demarcateCount++;

                // 路径：桌面 → calibration_data → 产品编号
                string desktopDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "calibration_data",
                    productId
                );
                Directory.CreateDirectory(desktopDir);

                string excelPath = Path.Combine(desktopDir, $"calibration_{channel}.xlsx");

                // 图片保存路径（按通道区分文件夹）
                string imgDir = Path.Combine(desktopDir, channel);
                Directory.CreateDirectory(imgDir);
                string imagePath = Path.Combine(imgDir, $"calibration_{channel}_{_demarcateCount}.png");

                var channels = CalibrationData.Select(c => c.Channel).ToList();
                var mvValues = CalibrationData.Select(c => c.MvValue).ToList();
                var forceValues = CalibrationData.Select(c => c.ForceValue).ToList();

                XLWorkbook workbook;
                IXLWorksheet wsMv, wsForce;

                if (File.Exists(excelPath))
                {
                    workbook = new XLWorkbook(excelPath);
                    wsMv = workbook.Worksheet("mV_V");
                    wsForce = workbook.Worksheet("Force");
                }
                else
                {
                    workbook = new XLWorkbook();
                    wsMv = workbook.AddWorksheet("mV_V");
                    wsForce = workbook.AddWorksheet("Force");

                    // 初始化第一列
                    for (int i = 0; i < channels.Count; i++)
                    {
                        wsMv.Cell(i + 1, 1).Value = channels[i];
                        wsForce.Cell(i + 1, 1).Value = channels[i];
                    }
                }

                // 横向叠加，获取最后一列 +1
                int newColMv = wsMv.LastColumnUsed()?.ColumnNumber() + 1 ?? 2;
                int newColForce = wsForce.LastColumnUsed()?.ColumnNumber() + 1 ?? 2;

                for (int i = 0; i < channels.Count; i++)
                {
                    // 将字符串转换为 double 写入 Excel
                    if (int.TryParse(mvValues[i], out int mvValue))
                        wsMv.Cell(i + 1, newColMv).Value = mvValue;
                    else
                        throw new FormatException($"无法将 mvValue '{mvValues[i]}' 转换为数字");

                    if (int.TryParse(forceValues[i], out int forceValue))
                        wsForce.Cell(i + 1, newColForce).Value = forceValue;
                    else
                        throw new FormatException($"无法将 forceValue '{forceValues[i]}' 转换为数字");
                }

                // 标定次数行
                wsMv.Cell(channels.Count + 1, newColMv).Value = _demarcateCount;
                wsForce.Cell(channels.Count + 1, newColForce).Value = _demarcateCount;

                wsMv.Columns().AdjustToContents();

                wsForce.Columns().AdjustToContents();

                workbook.SaveAs(excelPath);

                // 生成图片
                CreateTableImage(
                    channels.Select(c => c ?? string.Empty).ToList(),
                    mvValues.Select(c => c ?? string.Empty).ToList(), 
                    forceValues.Select(c => c ?? string.Empty).ToList(), 
                    imagePath);

                _messageService.ShowMessage($"数据已保存到:\n{excelPath}\n{imagePath}", "成功");
            }
            catch (Exception ex)
            {
                _messageService.ShowMessage($"保存数据失败: {ex.Message}", "错误");
            }
        }

        private void CreateTableImage(List<string> channels, List<string> mvValues, List<string> forceValues, string imagePath)
        {
            int rows = channels.Count + 1; // 表头
            int cols = 3; // 通道、mV/V、Force
            int cellWidth = 150;
            int cellHeight = 40;
            int imgWidth = cellWidth * cols;
            int imgHeight = cellHeight * rows;

            using var bmp = new Bitmap(imgWidth, imgHeight);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.White);

            using var pen = new Pen(Color.Black, 1);
            using var font = new Font("Arial", 12);
            var brush = new SolidBrush(Color.Black);
            var headerBrush = new SolidBrush(Color.LightGray);

            // 绘制水平线
            for (int i = 0; i <= rows; i++)
                g.DrawLine(pen, 0, i * cellHeight, imgWidth, i * cellHeight);

            // 绘制垂直线
            for (int j = 0; j <= cols; j++)
                g.DrawLine(pen, j * cellWidth, 0, j * cellWidth, imgHeight);

            // 表头
            string[] headers = { "通道", "mV/V", "Force" };
            for (int j = 0; j < cols; j++)
            {
                g.FillRectangle(headerBrush, j * cellWidth, 0, cellWidth, cellHeight);
                g.DrawString(headers[j], font, brush, j * cellWidth + 10, 10);
            }

            // 填充数据
            for (int i = 0; i < channels.Count; i++)
            {
                g.DrawString(channels[i], font, brush, 10, (i + 1) * cellHeight + 10);
                g.DrawString(mvValues[i].ToString(), font, brush, cellWidth + 10, (i + 1) * cellHeight + 10);
                g.DrawString(forceValues[i].ToString(), font, brush, 2 * cellWidth + 10, (i + 1) * cellHeight + 10);
            }

            // 外边框
            g.DrawRectangle(new Pen(Color.Black, 2), 0, 0, imgWidth - 1, imgHeight - 1);

            bmp.Save(imagePath, ImageFormat.Png);
        }
    }
}
