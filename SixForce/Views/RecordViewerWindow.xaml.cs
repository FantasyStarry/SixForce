using SixForce.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SixForce.Views
{
    /// <summary>
    /// RecordViewerWindow.xaml 的交互逻辑
    /// </summary>
    public partial class RecordViewerWindow : Window
    {
        private readonly DataRecord _record;
        private bool _isVoltageMode = true;

        private readonly Dictionary<string, string> _channelColors = new()
        {
            { "Fx", "#FC8181" },
            { "Fy", "#68D391" },
            { "Fz", "#63B3ED" },
            { "Mx", "#F6AD55" },
            { "My", "#B794F4" },
            { "Mz", "#4FD1C5" }
        };

        public RecordViewerWindow(DataRecord record)
        {
            InitializeComponent();
            _record = record;
            InitializeRecordInfo();
        }

        private void InitializeRecordInfo()
        {
            RecordNameText.Text = _record.Name;
            RecordTimeText.Text = _record.StartTime.ToString("yyyy-MM-dd HH:mm:ss");
            RecordDurationText.Text = _record.DurationDisplay;
            RecordPointsText.Text = $"{_record.PointCount} 个数据点";

            if (!string.IsNullOrWhiteSpace(_record.Comment))
            {
                CommentBorder.Visibility = Visibility.Visible;
                CommentText.Text = _record.Comment;
            }
        }

        private void VoltageTab_Checked(object sender, RoutedEventArgs e)
        {
            _isVoltageMode = true;
            DrawCurves();
        }

        private void ForceTab_Checked(object sender, RoutedEventArgs e)
        {
            _isVoltageMode = false;
            DrawCurves();
        }

        private void CurveCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawCurves();
        }

        private void DrawCurves()
        {
            if (CurveCanvas == null || _record.DataPoints.Count == 0)
                return;

            CurveCanvas.Children.Clear();

            double width = CurveCanvas.ActualWidth;
            double height = CurveCanvas.ActualHeight;

            if (width <= 0 || height <= 0)
                return;

            const double padding = 40;
            double chartWidth = width - padding * 2;
            double chartHeight = height - padding * 2;

            // 获取所有通道的数据
            var channelData = new Dictionary<string, List<double>>();
            foreach (var channel in _channelColors.Keys)
            {
                channelData[channel] = new List<double>();
            }

            foreach (var point in _record.DataPoints)
            {
                var values = _isVoltageMode ? point.MvValues : point.ForceValues;
                foreach (var channel in _channelColors.Keys)
                {
                    if (values.TryGetValue(channel, out double value))
                        channelData[channel].Add(value);
                    else
                        channelData[channel].Add(0);
                }
            }

            // 计算Y轴范围
            double minValue = double.MaxValue;
            double maxValue = double.MinValue;

            foreach (var data in channelData.Values)
            {
                if (data.Count > 0)
                {
                    minValue = Math.Min(minValue, data.Min());
                    maxValue = Math.Max(maxValue, data.Max());
                }
            }

            if (minValue == double.MaxValue || maxValue == double.MinValue)
                return;

            // 添加一些边距
            double range = maxValue - minValue;
            if (range < 0.001) range = 1;
            minValue -= range * 0.1;
            maxValue += range * 0.1;

            int pointCount = _record.DataPoints.Count;

            // 绘制每个通道的曲线
            foreach (var (channel, color) in _channelColors)
            {
                var data = channelData[channel];
                if (data.Count < 2) continue;

                var polyline = new Polyline
                {
                    Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                    StrokeThickness = 1.5,
                    StrokeLineJoin = PenLineJoin.Round
                };

                for (int i = 0; i < data.Count; i++)
                {
                    double x = padding + (i * chartWidth / (pointCount - 1));
                    double y = padding + chartHeight - ((data[i] - minValue) / (maxValue - minValue) * chartHeight);
                    polyline.Points.Add(new Point(x, y));
                }

                CurveCanvas.Children.Add(polyline);
            }

            // 绘制Y轴标签
            DrawYAxisLabels(padding, chartHeight, minValue, maxValue);
        }

        private void DrawYAxisLabels(double padding, double chartHeight, double minValue, double maxValue)
        {
            const int labelCount = 5;
            double range = maxValue - minValue;

            for (int i = 0; i <= labelCount; i++)
            {
                double value = minValue + (range * i / labelCount);
                double y = padding + chartHeight - (chartHeight * i / labelCount);

                var label = new TextBlock
                {
                    Text = value.ToString("F1"),
                    FontSize = 10,
                    Foreground = (Brush)FindResource("ThemeTextMutedBrush")
                };

                Canvas.SetLeft(label, 2);
                Canvas.SetTop(label, y - 7);
                CurveCanvas.Children.Add(label);
            }
        }
    }
}
