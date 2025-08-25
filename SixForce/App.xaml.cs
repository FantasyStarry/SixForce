using Microsoft.Extensions.DependencyInjection;
using SixForce.Services;
using SixForce.ViewModels;
using SixForce.Views;
using System.Windows;

namespace SixForce
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; private set; }

        public App()
        {
            var services = new ServiceCollection();
            // 注册服务和ViewModel
            services.AddSingleton<IModbusService, ModbusRTUService>();
            services.AddSingleton<IMessageService, MessageService>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<LeftPanelViewModel>();
            services.AddSingleton<RightPanelViewModel>();
            services.AddSingleton<DecouplingMatrixViewModel>();
            ServiceProvider = services.BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // 调用你的主程序入口
            base.OnStartup(e);

            // 创建 MainWindow 并注入 LeftPanelViewModel
            var mainWindow = new MainWindow();

            mainWindow.Show();

        }
    }

}
