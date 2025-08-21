using SixForce.Services;
using SixForce.ViewModels;
using System.Windows.Controls;

namespace SixForce.Views
{
    /// <summary>
    /// LeftPanel.xaml 的交互逻辑
    /// </summary>
    public partial class LeftPanel : UserControl
    {
        public LeftPanel(LeftPanelViewModel viewModel)
        {
            InitializeComponent();

            // 将 ViewModel 注入 IMessageService
            DataContext = viewModel;
        }
    }
}
