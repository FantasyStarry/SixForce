using Microsoft.Extensions.DependencyInjection;
using SixForce.Services;
using SixForce.ViewModels;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace SixForce.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var serviceProvider = (Application.Current as App)?.ServiceProvider
                ?? throw new InvalidOperationException("ServiceProvider 未初始化");

            var mainViewModel = serviceProvider.GetService<MainViewModel>()
                ?? throw new InvalidOperationException("无法解析 MainViewModel");

            var leftPanelViewModel = serviceProvider.GetService<LeftPanelViewModel>()
                ?? throw new InvalidOperationException("无法解析 LeftPanelViewModel");
            var centerPanelViewModel = serviceProvider.GetService<CenterPanelViewModel>()
                ?? throw new InvalidOperationException("无法解析 CenterPanelViewModel");
            var rightPanelViewModel = serviceProvider.GetService<RightPanelViewModel>()
                ?? throw new InvalidOperationException("无法解析 RightPanelViewModel");

            // 订阅读取控制器
            rightPanelViewModel.SubscribeToReadingController(leftPanelViewModel);
            
            // 订阅曲线数据更新
            centerPanelViewModel.SubscribeToCurveData(leftPanelViewModel);

            // 设置面板
            LeftArea.Child = new LeftPanel(leftPanelViewModel);
            CenterArea.Child = new CenterPanel(centerPanelViewModel);
            RightArea.Child = new RightPanel(rightPanelViewModel);

            DataContext = mainViewModel;

            // 初始化主题切换按钮状态
            ThemeToggle.IsChecked = ThemeService.Instance.IsDarkMode;
        }

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggle)
            {
                ThemeService.Instance.SetTheme(toggle.IsChecked == true);
            }
        }
    }
}