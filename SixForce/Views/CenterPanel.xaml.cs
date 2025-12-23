using SixForce.ViewModels;
using SixForce.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SixForce.Views
{
    public partial class CenterPanel : UserControl
    {
        private readonly CenterPanelViewModel? _viewModel;
        
        // 绘图边距常量
        private const double LeftMargin = 45;
        private const double RightMargin = 15;
        private const double TopMargin = 15;
        private const double BottomMargin = 15;

        public CenterPanel(CenterPanelViewModel centerPanelViewModel)
        {
            InitializeComponent();
            _viewModel = centerPanelViewModel;
            DataContext = centerPanelViewModel;
            _viewModel.DataUpdated += OnDataUpdated;
            ThemeService.Instance.ThemeChanged += OnDataUpdated;
            Loaded += (_, _) => DrawCurves();
        }

        private void OnDataUpdated() => Dispatcher.Invoke(DrawCurves);

        private void CurveCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => DrawCurves();

        // 获取主题相关的画刷
        private static SolidColorBrush GetThemeBrush(string key)
        {
            var brush = Application.Current.TryFindResource(key) as SolidColorBrush;
            return brush ?? new SolidColorBrush(Colors.Gray);
        }

        private void DrawCurves()
        {
            if (_viewModel == null || CurveCanvas.ActualWidth <= 0 || CurveCanvas.ActualHeight <= 0)
                return;

            CurveCanvas.Children.Clear();

            double canvasWidth = CurveCanvas.ActualWidth;
            double canvasHeight = CurveCanvas.ActualHeight;
            double plotWidth = canvasWidth - LeftMargin - RightMargin;
            double plotHeight = canvasHeight - TopMargin - BottomMargin;

            var (minValue, maxValue) = GetDataRange();
            if (minValue == maxValue)
            {
                minValue = -100;
                maxValue = 100;
            }
            else
            {
                double range = maxValue - minValue;
                minValue -= range * 0.1;
                maxValue += range * 0.1;
            }

            DrawAxes(canvasWidth, canvasHeight);
            DrawYAxisLabels(minValue, maxValue, plotHeight);

            foreach (var curve in _viewModel.ChannelCurves)
            {
                var values = _viewModel.IsVoltageMode ? curve.MvValues : curve.ForceValues;
                if (values.Count >= 2)
                    DrawChannelCurve(curve, values, minValue, maxValue, plotWidth, plotHeight);
            }
        }

        private void DrawAxes(double canvasWidth, double canvasHeight)
        {
            var axisBrush = GetThemeBrush("ThemeBorderBrush");
            var gridBrush = GetThemeBrush("ThemeGridLineBrush");

            // X轴
            CurveCanvas.Children.Add(new Line
            {
                X1 = LeftMargin,
                Y1 = canvasHeight - BottomMargin,
                X2 = canvasWidth - RightMargin,
                Y2 = canvasHeight - BottomMargin,
                Stroke = axisBrush,
                StrokeThickness = 1
            });

            // Y轴
            CurveCanvas.Children.Add(new Line
            {
                X1 = LeftMargin,
                Y1 = TopMargin,
                X2 = LeftMargin,
                Y2 = canvasHeight - BottomMargin,
                Stroke = axisBrush,
                StrokeThickness = 1
            });

            // 零线
            _ = CurveCanvas.Children.Add(new Line
            {
                X1 = LeftMargin,
                Y1 = canvasHeight / 2,
                X2 = canvasWidth - RightMargin,
                Y2 = canvasHeight / 2,
                Stroke = gridBrush,
                StrokeThickness = 1,
                StrokeDashArray = [4, 4]
            });
        }

        private void DrawYAxisLabels(double minValue, double maxValue, double plotHeight)
        {
            const int tickCount = 5;
            double tickInterval = (maxValue - minValue) / (tickCount - 1);
            var labelBrush = GetThemeBrush("ThemeTextSecondaryBrush");
            var gridBrush = GetThemeBrush("ThemeGridLineBrush");

            for (int i = 0; i < tickCount; i++)
            {
                double value = maxValue - i * tickInterval;
                double y = TopMargin + i * plotHeight / (tickCount - 1);

                var label = new TextBlock
                {
                    Text = value.ToString("F0"),
                    FontSize = 9,
                    Foreground = labelBrush,
                    TextAlignment = TextAlignment.Right,
                    Width = LeftMargin - 6
                };
                Canvas.SetLeft(label, 0);
                Canvas.SetTop(label, y - 5);
                CurveCanvas.Children.Add(label);

                if (i > 0 && i < tickCount - 1)
                {
                    CurveCanvas.Children.Add(new Line
                    {
                        X1 = LeftMargin,
                        Y1 = y,
                        X2 = CurveCanvas.ActualWidth - RightMargin,
                        Y2 = y,
                        Stroke = gridBrush,
                        StrokeThickness = 0.5
                    });
                }
            }
        }

        private void DrawChannelCurve(ChannelCurveData curve, 
            System.Collections.ObjectModel.ObservableCollection<double> values,
            double minValue, double maxValue, double plotWidth, double plotHeight)
        {
            var points = new PointCollection();
            double xStep = plotWidth / Math.Max(1, values.Count - 1);
            double valueRange = maxValue - minValue;

            for (int i = 0; i < values.Count; i++)
            {
                double x = LeftMargin + i * xStep;
                double normalizedValue = (values[i] - minValue) / valueRange;
                double y = Math.Clamp(TopMargin + plotHeight - normalizedValue * plotHeight, 
                                      TopMargin, TopMargin + plotHeight);
                points.Add(new Point(x, y));
            }

            CurveCanvas.Children.Add(new Polyline
            {
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString(curve.Color)),
                StrokeThickness = 2,
                StrokeLineJoin = PenLineJoin.Round,
                Points = points
            });
        }

        private (double min, double max) GetDataRange()
        {
            if (_viewModel == null) return (0, 0);

            double min = double.MaxValue, max = double.MinValue;
            bool hasData = false;

            foreach (var curve in _viewModel.ChannelCurves)
            {
                var values = _viewModel.IsVoltageMode ? curve.MvValues : curve.ForceValues;
                foreach (var value in values)
                {
                    hasData = true;
                    min = Math.Min(min, value);
                    max = Math.Max(max, value);
                }
            }

            return hasData ? (min, max) : (0, 0);
        }
    }
}