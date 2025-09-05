using SixForce.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SixForce.Views
{
    /// <summary>
    /// CenterPanel.xaml 的交互逻辑
    /// </summary>
    public partial class CenterPanel : UserControl
    {
        public CenterPanel(CenterPanelViewModel centerPanelViewModel)
        {
            InitializeComponent();
            DataContext = centerPanelViewModel;
 
            DrawCurve();
        }

        private void DrawCurve()
        {
            // 创建 Polyline 用于绘制曲线
            Polyline polyline = new Polyline
            {
                Stroke = Brushes.Blue,
                StrokeThickness = 2
            };

            // 模拟数据点（例如 y = sin(x)）
            PointCollection points = new PointCollection();
            for (double x = 0; x <= 2 * Math.PI; x += 0.1)
            {
                double y = Math.Sin(x);
                // 缩放和平移，使曲线适应 Canvas
                points.Add(new Point(x * 80, 150 - y * 100));
            }

            polyline.Points = points;
            CurveCanvas.Children.Add(polyline);

            // 添加坐标轴
            Line xAxis = new Line
            {
                X1 = 0,
                Y1 = 150,
                X2 = 500,
                Y2 = 150,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            Line yAxis = new Line
            {
                X1 = 50,
                Y1 = 0,
                X2 = 50,
                Y2 = 300,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            CurveCanvas.Children.Add(xAxis);
            CurveCanvas.Children.Add(yAxis);
        }
    }
}
