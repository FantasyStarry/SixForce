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
        private readonly IServiceProvider _serviceProvider;
        private IReadingController? _readingController;
        private int _demarcateCount;
        private string _lastChannel = string.Empty;

        public RightPanelViewModel(IModbusService modbusService, IMessageService messageService, IServiceProvider serviceProvider)
        {
            _modbusService = modbusService;
            _messageService = messageService;
            _serviceProvider = serviceProvider;
            ChannelOptions = new ObservableCollection<int>(Enumerable.Range(1, 7));
        }

        [ObservableProperty]
        private bool isClearChannelDialogOpen;

        [ObservableProperty]
        private int selectedChannel = 7;

        public ObservableCollection<int> ChannelOptions { get; }

        [ObservableProperty]
        private ObservableCollection<CalibrationData> calibrationData = new()
        {
            new() { Channel = "Fx", MvValue = "0.00", ForceValue = "0.00" },
            new() { Channel = "Fy", MvValue = "0.00", ForceValue = "0.00" },
            new() { Channel = "Fz", MvValue = "0.00", ForceValue = "0.00" },
            new() { Channel = "Mx", MvValue = "0.00", ForceValue = "0.00" },
            new() { Channel = "My", MvValue = "0.00", ForceValue = "0.00" },
            new() { Channel = "Mz", MvValue = "0.00", ForceValue = "0.00" }
        };

        public void SubscribeToReadingController(IReadingController readingController)
        {
            if (_readingController != null)
                _readingController.SensorDataUpdated -= OnSensorDataUpdated;

            _readingController = readingController;
            readingController.SensorDataUpdated += OnSensorDataUpdated;
        }

        private void OnSensorDataUpdated(Dictionary<string, (string mvValue, string forceValue)> data)
        {
            foreach (var (channel, values) in data)
                UpdateData(channel, values.mvValue, values.forceValue);
        }

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
        private void ShowClearChannelDialog() => IsClearChannelDialogOpen = true;

        [RelayCommand]
        private async Task ConfirmClearChannel()
        {
            try
            {
                await _modbusService.ClearChannelAsync(SelectedChannel);
                _messageService.ShowMessage(
                    SelectedChannel == 7 ? "已清零所有通道" : $"已清零通道 {SelectedChannel}",
                    "成功");
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
        private void CancelClearChannel() => IsClearChannelDialogOpen = false;

        [RelayCommand]
        private void OpenDecouplingMatrix()
        {
            try
            {
                _readingController?.StopReading();

                var viewModel = _serviceProvider.GetService<DecouplingMatrixViewModel>()
                    ?? throw new InvalidOperationException("无法解析 DecouplingMatrixViewModel");

                var window = new Window
                {
                    Title = "解耦系数编辑",
                    Content = new DecouplingMatrixView { DataContext = viewModel },
                    Width = 720,
                    Height = 480,
                    MinWidth = 600,
                    MinHeight = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.CanResize
                };

                window.Closed += (_, _) =>
                {
                    if (_readingController?.IsConnected == true)
                        _readingController.StartReadingData();
                };

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                _messageService.ShowMessage($"打开解耦系数编辑失败: {ex.Message}", "错误");
                if (_readingController?.IsConnected == true)
                    _readingController.StartReadingData();
            }
        }

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

                string productId = _readingController.ProductId;
                string channel = _readingController.SelectedChannel ?? "Channel_1";

                if (_lastChannel != channel)
                {
                    _demarcateCount = 0;
                    _lastChannel = channel;
                }
                _demarcateCount++;

                string desktopDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "calibration_data", productId);
                Directory.CreateDirectory(desktopDir);

                string excelPath = Path.Combine(desktopDir, $"calibration_{channel}.xlsx");
                string imgDir = Path.Combine(desktopDir, channel);
                Directory.CreateDirectory(imgDir);
                string imagePath = Path.Combine(imgDir, $"calibration_{channel}_{_demarcateCount}.png");

                var channels = CalibrationData.Select(c => c.Channel ?? string.Empty).ToList();
                var mvValues = CalibrationData.Select(c => c.MvValue ?? string.Empty).ToList();
                var forceValues = CalibrationData.Select(c => c.ForceValue ?? string.Empty).ToList();

                SaveToExcel(excelPath, channels, mvValues, forceValues);
                CreateTableImage(channels, mvValues, forceValues, imagePath);

                _messageService.ShowMessage($"数据已保存到:\n{excelPath}\n{imagePath}", "成功");
            }
            catch (Exception ex)
            {
                _messageService.ShowMessage($"保存数据失败: {ex.Message}", "错误");
            }
        }

        private void SaveToExcel(string excelPath, List<string> channels, List<string> mvValues, List<string> forceValues)
        {
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

                for (int i = 0; i < channels.Count; i++)
                {
                    wsMv.Cell(i + 1, 1).Value = channels[i];
                    wsForce.Cell(i + 1, 1).Value = channels[i];
                }
            }

            int newColMv = wsMv.LastColumnUsed()?.ColumnNumber() + 1 ?? 2;
            int newColForce = wsForce.LastColumnUsed()?.ColumnNumber() + 1 ?? 2;

            for (int i = 0; i < channels.Count; i++)
            {
                if (int.TryParse(mvValues[i], out int mvValue))
                    wsMv.Cell(i + 1, newColMv).Value = mvValue;
                else
                    throw new FormatException($"无法将 mvValue '{mvValues[i]}' 转换为数字");

                if (int.TryParse(forceValues[i], out int forceValue))
                    wsForce.Cell(i + 1, newColForce).Value = forceValue;
                else
                    throw new FormatException($"无法将 forceValue '{forceValues[i]}' 转换为数字");
            }

            wsMv.Cell(channels.Count + 1, newColMv).Value = _demarcateCount;
            wsForce.Cell(channels.Count + 1, newColForce).Value = _demarcateCount;

            wsMv.Columns().AdjustToContents();
            wsForce.Columns().AdjustToContents();
            workbook.SaveAs(excelPath);
        }

        private static void CreateTableImage(List<string> channels, List<string> mvValues, List<string> forceValues, string imagePath)
        {
            const int cellWidth = 150, cellHeight = 40, cols = 3;
            int rows = channels.Count + 1;
            int imgWidth = cellWidth * cols, imgHeight = cellHeight * rows;

            using var bmp = new Bitmap(imgWidth, imgHeight);
            using var g = Graphics.FromImage(bmp);
            using var pen = new Pen(Color.Black, 1);
            using var font = new Font("Arial", 12);
            using var brush = new SolidBrush(Color.Black);
            using var headerBrush = new SolidBrush(Color.LightGray);

            g.Clear(Color.White);

            // 绘制网格线
            for (int i = 0; i <= rows; i++)
                g.DrawLine(pen, 0, i * cellHeight, imgWidth, i * cellHeight);
            for (int j = 0; j <= cols; j++)
                g.DrawLine(pen, j * cellWidth, 0, j * cellWidth, imgHeight);

            // 表头
            string[] headers = ["通道", "mV/V", "Force"];
            for (int j = 0; j < cols; j++)
            {
                g.FillRectangle(headerBrush, j * cellWidth, 0, cellWidth, cellHeight);
                g.DrawString(headers[j], font, brush, j * cellWidth + 10, 10);
            }

            // 数据
            for (int i = 0; i < channels.Count; i++)
            {
                int y = (i + 1) * cellHeight + 10;
                g.DrawString(channels[i], font, brush, 10, y);
                g.DrawString(mvValues[i], font, brush, cellWidth + 10, y);
                g.DrawString(forceValues[i], font, brush, 2 * cellWidth + 10, y);
            }

            g.DrawRectangle(new Pen(Color.Black, 2), 0, 0, imgWidth - 1, imgHeight - 1);
            bmp.Save(imagePath, ImageFormat.Png);
        }
    }
}
