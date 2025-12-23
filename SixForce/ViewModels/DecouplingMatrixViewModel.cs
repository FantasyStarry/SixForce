using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SixForce.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace SixForce.ViewModels
{
    public class MatrixRow : ObservableObject
    {
        public int RowIndex { get; set; }
        private ObservableCollection<int> _values = new(Enumerable.Repeat(0, 6));
        public ObservableCollection<int> Values
        {
            get => _values;
            set => SetProperty(ref _values, value);
        }
    }

    public partial class DecouplingMatrixViewModel : ObservableObject
    {
        private readonly IModbusService _modbusService;
        private readonly IMessageService _messageService;
        public ObservableCollection<MatrixRow> MatrixRows { get; } = [];

        public DecouplingMatrixViewModel(IModbusService modbusService, IMessageService messageService)
        {
            _modbusService = modbusService;
            _messageService = messageService;
            for (int i = 0; i < 6; i++)
            {
                MatrixRows.Add(new MatrixRow { RowIndex = i + 1 });
            }
        }

        [RelayCommand]
        private async Task ReadFromDevice()
        {
            try
            {
                var matrix = await _modbusService.ReadDecouplingMatrixAsync();
                for (int row = 0; row < 6; row++)
                {
                    for (int col = 0; col < 6; col++)
                    {
                        MatrixRows[row].Values[col] = matrix[row, col];
                    }
                }
                _messageService.ShowMessage("读取成功");
            }
            catch (Exception ex)
            {
                _messageService.ShowMessage($"读取失败: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task WriteToDevice()
        {
            try
            {
                int[,] matrix = new int[6, 6];
                for (int row = 0; row < 6; row++)
                {
                    for (int col = 0; col < 6; col++)
                    {
                        matrix[row, col] = MatrixRows[row].Values[col];
                    }
                }
                await _modbusService.WriteDecouplingMatrixAsync(matrix);
                _messageService.ShowMessage("写入成功");
            }
            catch (Exception ex)
            {
                _messageService.ShowMessage($"写入失败: {ex.Message}");
            }
        }

        [RelayCommand]
        private void CreateFile()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(desktopPath, "解耦系数.csv");
            using (var writer = new StreamWriter(filePath))
            {
                for (int row = 0; row < 6; row++)
                {
                    writer.WriteLine(string.Join(",", MatrixRows[row].Values));
                }
            }
            _messageService.ShowMessage("文件已创建至桌面：解耦系数.csv");
        }

        [RelayCommand]
        private void ReadFile()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(desktopPath, "解耦系数.csv");
            if (!File.Exists(filePath))
            {
                _messageService.ShowMessage("桌面未找到解耦系数.csv 文件");
                return;
            }

            var lines = File.ReadAllLines(filePath);
            if (lines.Length < 6)
            {
                _messageService.ShowMessage("文件格式无效");
                return;
            }

            for (int row = 0; row < 6; row++)
            {
                var values = lines[row].Split(',').Select(int.Parse).ToArray();
                if (values.Length != 6)
                {
                    _messageService.ShowMessage("文件格式无效");
                    return;
                }
                for (int col = 0; col < 6; col++)
                {
                    MatrixRows[row].Values[col] = values[col];
                }
            }
            _messageService.ShowMessage("文件已从桌面读取：解耦系数.csv");
        }
    }
}
