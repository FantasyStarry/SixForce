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
        private ObservableCollection<int> _values = new ObservableCollection<int>(Enumerable.Repeat(0, 6));
        public ObservableCollection<int> Values
        {
            get => _values;
            set => SetProperty(ref _values, value);
        }
    }

    public partial class DecouplingMatrixViewModel : ObservableObject
    {
        private readonly IModbusService _modbusService;
        public ObservableCollection<MatrixRow> MatrixRows { get; } = new ObservableCollection<MatrixRow>();

        public DecouplingMatrixViewModel(IModbusService modbusService)
        {
            _modbusService = modbusService;
            for (int i = 0; i < 6; i++)
            {
                MatrixRows.Add(new MatrixRow { RowIndex = i + 1 });
            }
        }

        [RelayCommand]
        private async void ReadFromDevice()
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
                MessageBox.Show("读取成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"读取失败: {ex.Message}");
            }
        }

        [RelayCommand]
        private async void WriteToDevice()
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
                MessageBox.Show("写入成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"写入失败: {ex.Message}");
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
            MessageBox.Show("文件已创建至桌面：解耦系数.csv");
        }

        [RelayCommand]
        private void ReadFile()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(desktopPath, "解耦系数.csv");
            if (!File.Exists(filePath))
            {
                MessageBox.Show("桌面未找到解耦系数.csv 文件");
                return;
            }

            var lines = File.ReadAllLines(filePath);
            if (lines.Length < 6)
            {
                MessageBox.Show("文件格式无效");
                return;
            }

            for (int row = 0; row < 6; row++)
            {
                var values = lines[row].Split(',').Select(int.Parse).ToArray();
                if (values.Length != 6)
                {
                    MessageBox.Show("文件格式无效");
                    return;
                }
                for (int col = 0; col < 6; col++)
                {
                    MatrixRows[row].Values[col] = values[col];
                }
            }
            MessageBox.Show("文件已从桌面读取：解耦系数.csv");
        }
    }
}
