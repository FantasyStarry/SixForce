using SixForce.ViewModels;
using System.Windows.Controls;

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
        }
    }
}
