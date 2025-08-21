using Microsoft.Extensions.DependencyInjection;
using SixForce.Services;
using SixForce.ViewModels;
using System.Windows;

namespace SixForce.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
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
            var rightPanelViewModel = serviceProvider.GetService<RightPanelViewModel>()
                ?? throw new InvalidOperationException("无法解析 RightPanelViewModel");

            // 订阅 LeftPanel 属性变化
            rightPanelViewModel.SubscribeToLeftPanel(leftPanelViewModel);

            // 设置 LeftPanel 和 RightPanel
            var leftPanelContainer = LeftArea;
            var leftPanel = new LeftPanel(leftPanelViewModel);
            
            leftPanelContainer.Child = leftPanel;

            var rightPanelContainer = RightArea;
            var rightPanel = new RightPanel(rightPanelViewModel);
            
            rightPanelContainer.Child = rightPanel;

            DataContext = mainViewModel;
        }


    }
}