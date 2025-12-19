using CommunityToolkit.Mvvm.ComponentModel;
using SixForce.Models;
using SixForce.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace SixForce.ViewModels
{
    /// <summary>
    /// 主视图模型，负责管理设备型号选择和配置加载
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        private readonly IModbusService _modbusService;
        private Dictionary<string, ModbusRegisterMap>? _configs;

        /// <summary>
        /// 可用的产品型号列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> products = new();

        /// <summary>
        /// 当前选中的产品型号
        /// </summary>
        [ObservableProperty]
        private string? selectedProduct;

        /// <summary>
        /// 当前选中产品的寄存器映射配置
        /// </summary>
        public ModbusRegisterMap? CurrentMap =>
            SelectedProduct != null && _configs != null && _configs.ContainsKey(SelectedProduct)
                ? _configs[SelectedProduct]
                : null;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="modbusService">Modbus服务实例</param>
        public MainViewModel(IModbusService modbusService)
        {
            _modbusService = modbusService ?? throw new ArgumentNullException(nameof(modbusService));
            LoadConfigs();
        }

        /// <summary>
        /// 当选择的产品型号改变时的处理
        /// </summary>
        partial void OnSelectedProductChanged(string? value)
        {
            if (value != null && _configs != null && _configs.ContainsKey(value))
            {
                _modbusService.SetRegisterMap(value, _configs[value]);
            }
        }

        /// <summary>
        /// 从配置文件加载寄存器映射配置
        /// </summary>
        /// <exception cref="FileNotFoundException">当配置文件不存在时抛出</exception>
        /// <exception cref="JsonException">当配置文件格式无效时抛出</exception>
        private void LoadConfigs()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs", "register_config.json");
                
                if (!File.Exists(configPath))
                {
                    throw new FileNotFoundException($"配置文件不存在: {configPath}");
                }

                string json = File.ReadAllText(configPath);
                _configs = JsonSerializer.Deserialize<Dictionary<string, ModbusRegisterMap>>(json)
                    ?? throw new JsonException("配置文件反序列化失败");

                foreach (var key in _configs.Keys)
                {
                    Products.Add(key);
                }

                if (Products.Count > 0)
                {
                    SelectedProduct = Products[0];
                    _modbusService.SetRegisterMap(SelectedProduct, _configs[SelectedProduct]);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"加载配置文件失败: {ex.Message}", ex);
            }
        }
    }
}
