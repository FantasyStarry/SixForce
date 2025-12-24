using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SixForce.Models;
using SixForce.Services;
using SixForce.Views;
using System.Collections.ObjectModel;
using System.Windows;

namespace SixForce.ViewModels
{
    /// <summary>
    /// 通道曲线数据
    /// </summary>
    public partial class ChannelCurveData : ObservableObject
    {
        private const int MaxDataPoints = 100;
        
        public string ChannelName { get; set; } = string.Empty;
        public string Color { get; set; } = "#5B7FFF";
        public ObservableCollection<double> MvValues { get; } = new();
        public ObservableCollection<double> ForceValues { get; } = new();

        public void AddMvValue(double value)
        {
            if (MvValues.Count >= MaxDataPoints)
                MvValues.RemoveAt(0);
            MvValues.Add(value);
        }

        public void AddForceValue(double value)
        {
            if (ForceValues.Count >= MaxDataPoints)
                ForceValues.RemoveAt(0);
            ForceValues.Add(value);
        }

        public void Clear()
        {
            MvValues.Clear();
            ForceValues.Clear();
        }
    }

    /// <summary>
    /// 中心面板视图模型
    /// </summary>
    public partial class CenterPanelViewModel : ObservableObject
    {
        private IReadingController? _readingController;
        private readonly IDataRecordService _dataRecordService;
        private readonly IMessageService _messageService;
        private DataRecord? _currentRecord;
        private string _pendingComment = string.Empty;

        [ObservableProperty]
        private bool _isVoltageMode = true;

        [ObservableProperty]
        private bool _isForceMode;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanStartRecording))]
        [NotifyPropertyChangedFor(nameof(CanStopRecording))]
        [NotifyPropertyChangedFor(nameof(RecordingStatusText))]
        private bool _isRecording;

        [ObservableProperty]
        private bool _isHistoryPanelOpen;

        public string ChartTitle => IsVoltageMode ? "电压曲线 (mV)" : "力值曲线 (N)";
        public string YAxisUnit => IsVoltageMode ? "mV" : "N";
        public bool CanStartRecording => !IsRecording;
        public bool CanStopRecording => IsRecording;
        public string RecordingStatusText => IsRecording ? "记录中..." : "未记录";

        /// <summary>
        /// 记录历史视图模型
        /// </summary>
        public RecordHistoryViewModel RecordHistoryViewModel { get; }

        public ObservableCollection<ChannelCurveData> ChannelCurves { get; } = new()
        {
            new() { ChannelName = "Fx", Color = "#FC8181" },
            new() { ChannelName = "Fy", Color = "#68D391" },
            new() { ChannelName = "Fz", Color = "#63B3ED" },
            new() { ChannelName = "Mx", Color = "#F6AD55" },
            new() { ChannelName = "My", Color = "#B794F4" },
            new() { ChannelName = "Mz", Color = "#4FD1C5" }
        };

        public event Action? DataUpdated;

        public CenterPanelViewModel(IDataRecordService dataRecordService, IMessageService messageService)
        {
            _dataRecordService = dataRecordService;
            _messageService = messageService;

            RecordHistoryViewModel = new RecordHistoryViewModel(dataRecordService, messageService);
            RecordHistoryViewModel.ViewRecordRequested += OnViewRecordRequested;
        }

        public void SubscribeToCurveData(IReadingController readingController)
        {
            if (_readingController != null)
                _readingController.SensorDataUpdated -= OnSensorDataUpdated;

            _readingController = readingController;
            _readingController.SensorDataUpdated += OnSensorDataUpdated;
        }

        private void OnSensorDataUpdated(Dictionary<string, (string mvValue, string forceValue)> data)
        {
            foreach (var (channel, values) in data)
                UpdateChannelData(channel, values.mvValue, values.forceValue);

            // 如果正在记录，保存数据点
            if (IsRecording && _currentRecord != null)
            {
                var dataPoint = new DataPoint
                {
                    Timestamp = DateTime.Now,
                    MvValues = new Dictionary<string, double>(),
                    ForceValues = new Dictionary<string, double>()
                };

                foreach (var (channel, values) in data)
                {
                    if (double.TryParse(values.mvValue, out double mv))
                        dataPoint.MvValues[channel] = mv;
                    if (double.TryParse(values.forceValue, out double force))
                        dataPoint.ForceValues[channel] = force;
                }

                _currentRecord.DataPoints.Add(dataPoint);
            }

            DataUpdated?.Invoke();
        }

        [RelayCommand]
        private void SwitchToVoltage()
        {
            IsVoltageMode = true;
            IsForceMode = false;
            OnPropertyChanged(nameof(ChartTitle));
            OnPropertyChanged(nameof(YAxisUnit));
            DataUpdated?.Invoke();
        }

        [RelayCommand]
        private void SwitchToForce()
        {
            IsVoltageMode = false;
            IsForceMode = true;
            OnPropertyChanged(nameof(ChartTitle));
            OnPropertyChanged(nameof(YAxisUnit));
            DataUpdated?.Invoke();
        }

        public void UpdateChannelData(string channel, string mvValue, string forceValue)
        {
            var curve = ChannelCurves.FirstOrDefault(c => c.ChannelName == channel);
            if (curve == null) return;
            
            if (double.TryParse(mvValue, out double mv))
                curve.AddMvValue(mv);
            if (double.TryParse(forceValue, out double force))
                curve.AddForceValue(force);
        }

        public void NotifyDataUpdated() => DataUpdated?.Invoke();

        [RelayCommand]
        private void ClearData()
        {
            foreach (var curve in ChannelCurves)
                curve.Clear();
            DataUpdated?.Invoke();
        }

        /// <summary>
        /// 开始记录
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanStartRecording))]
        private void StartRecording()
        {
            // 显示注释输入对话框
            var dialog = new RecordCommentDialog
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                _pendingComment = dialog.Comment;
                
                _currentRecord = new DataRecord
                {
                    StartTime = DateTime.Now,
                    Name = $"记录_{DateTime.Now:yyyyMMdd_HHmmss}",
                    Comment = _pendingComment
                };

                IsRecording = true;
                StartRecordingCommand.NotifyCanExecuteChanged();
                StopRecordingCommand.NotifyCanExecuteChanged();
            }
        }

        /// <summary>
        /// 停止记录
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanStopRecording))]
        private async Task StopRecordingAsync()
        {
            if (_currentRecord == null)
                return;

            IsRecording = false;
            _currentRecord.EndTime = DateTime.Now;

            try
            {
                string filePath = await _dataRecordService.SaveRecordAsync(_currentRecord);
                _messageService.ShowMessage(
                    $"记录已保存\n数据点: {_currentRecord.PointCount}\n文件: {filePath}",
                    "保存成功");

                // 刷新历史记录列表
                await RecordHistoryViewModel.LoadRecordsAsync();
            }
            catch (Exception ex)
            {
                _messageService.ShowMessage($"保存记录失败: {ex.Message}", "错误");
            }
            finally
            {
                _currentRecord = null;
                _pendingComment = string.Empty;
                StartRecordingCommand.NotifyCanExecuteChanged();
                StopRecordingCommand.NotifyCanExecuteChanged();
            }
        }

        /// <summary>
        /// 切换历史面板显示
        /// </summary>
        [RelayCommand]
        private async Task ToggleHistoryPanelAsync()
        {
            IsHistoryPanelOpen = !IsHistoryPanelOpen;
            
            if (IsHistoryPanelOpen)
            {
                await RecordHistoryViewModel.LoadRecordsAsync();
            }
        }

        /// <summary>
        /// 查看记录
        /// </summary>
        private void OnViewRecordRequested(DataRecord record)
        {
            var window = new RecordViewerWindow(record)
            {
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();
        }
    }
}
