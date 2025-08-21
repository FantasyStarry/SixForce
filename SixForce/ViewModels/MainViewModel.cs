using CommunityToolkit.Mvvm.ComponentModel;
using SixForce.Models;
using SixForce.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace SixForce.ViewModels
{
    public partial class MainViewModel:ObservableObject
    {
        private readonly IModbusService _modbusService;
        private Dictionary<string, ModbusRegisterMap>? _configs;

        [ObservableProperty]
        private ObservableCollection<string> products = new() ;

        [ObservableProperty]
        private string? selectedProduct;

        public ModbusRegisterMap? CurrentMap =>
        SelectedProduct != null && _configs != null && _configs.ContainsKey(SelectedProduct)
            ? _configs[SelectedProduct]
            : null;

        public MainViewModel(IModbusService modbusService)
        {
            _modbusService = modbusService;
            LoadConfigs();
        }

        partial void OnSelectedProductChanged(string? value)
        {
            if (value != null && _configs != null && _configs.ContainsKey(value))
            {
                _modbusService.SetRegisterMap(_configs[value]);
            }
        }

        private void LoadConfigs()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs", "register_config.json");
            string json = File.ReadAllText(path);

            _configs = JsonSerializer.Deserialize<Dictionary<string, ModbusRegisterMap>>(json)!;

            foreach (var key in _configs.Keys)
            {
                Products.Add(key);
            }

            if (Products.Count > 0)
            {
                SelectedProduct = Products[0]; // 默认选第一个
                _modbusService.SetRegisterMap(_configs[SelectedProduct]); // 设置默认寄存器映射
            }
        }
    }
}
