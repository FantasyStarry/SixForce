using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

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

        [ObservableProperty]
        private bool _isVoltageMode = true;

        [ObservableProperty]
        private bool _isForceMode;

        public string ChartTitle => IsVoltageMode ? "电压曲线 (mV)" : "力值曲线 (N)";
        public string YAxisUnit => IsVoltageMode ? "mV" : "N";

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
    }
}