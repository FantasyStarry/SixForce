using SixForce.ViewModels;
using System.Windows.Controls;

namespace SixForce.Views
{
    /// <summary>
    /// RightPanel.xaml 的交互逻辑
    /// </summary>
    public partial class RightPanel : UserControl
    {
        public RightPanel(RightPanelViewModel rightPanelViewModel)
        {
            InitializeComponent();
            DataContext = rightPanelViewModel;
        }
    }
}
