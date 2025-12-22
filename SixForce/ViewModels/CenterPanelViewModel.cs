using CommunityToolkit.Mvvm.ComponentModel;

namespace SixForce.ViewModels
{
    /// <summary>
    /// 中心面板视图模型，负责显示传感器曲线图表
    /// </summary>
    public partial class CenterPanelViewModel : ObservableObject
    {
        /// <summary>
        /// 图表标题
        /// </summary>
        [ObservableProperty]
        private string _chartTitle = "传感器数据曲线";

        /// <summary>
        /// 是否显示图表
        /// </summary>
        [ObservableProperty]
        private bool _isChartVisible = true;

        /// <summary>
        /// 图表宽度
        /// </summary>
        [ObservableProperty]
        private double _chartWidth = 500;

        /// <summary>
        /// 图表高度
        /// </summary>
        [ObservableProperty]
        private double _chartHeight = 300;
    }
}
