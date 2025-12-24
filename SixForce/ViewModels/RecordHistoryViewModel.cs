using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SixForce.Models;
using SixForce.Services;
using System.Collections.ObjectModel;

namespace SixForce.ViewModels
{
    /// <summary>
    /// 记录项视图模型（用于列表显示）
    /// </summary>
    public partial class RecordItemViewModel : ObservableObject
    {
        public DataRecord Record { get; }

        public string Name => Record.Name;
        public string Comment => Record.Comment;
        public bool HasComment => !string.IsNullOrWhiteSpace(Record.Comment);
        public DateTime StartTime => Record.StartTime;
        public string StartTimeDisplay => Record.StartTime.ToString("yyyy-MM-dd HH:mm:ss");
        public string DurationDisplay => Record.DurationDisplay;
        public int PointCount => Record.PointCount;
        public string? FilePath => Record.FilePath;

        [ObservableProperty]
        private bool _isSelected;

        public RecordItemViewModel(DataRecord record)
        {
            Record = record;
        }
    }

    /// <summary>
    /// 记录历史视图模型
    /// </summary>
    public partial class RecordHistoryViewModel : ObservableObject
    {
        private readonly IDataRecordService _dataRecordService;
        private readonly IMessageService _messageService;

        /// <summary>
        /// 记录列表
        /// </summary>
        public ObservableCollection<RecordItemViewModel> Records { get; } = new();

        /// <summary>
        /// 当前选中的记录
        /// </summary>
        [ObservableProperty]
        private RecordItemViewModel? _selectedRecord;

        /// <summary>
        /// 是否正在加载
        /// </summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>
        /// 是否显示空状态
        /// </summary>
        public bool IsEmpty => !IsLoading && Records.Count == 0;

        /// <summary>
        /// 查看记录事件
        /// </summary>
        public event Action<DataRecord>? ViewRecordRequested;

        /// <summary>
        /// 记录删除事件
        /// </summary>
        public event Action? RecordDeleted;

        public RecordHistoryViewModel(IDataRecordService dataRecordService, IMessageService messageService)
        {
            _dataRecordService = dataRecordService;
            _messageService = messageService;
        }

        /// <summary>
        /// 加载所有记录
        /// </summary>
        [RelayCommand]
        public async Task LoadRecordsAsync()
        {
            try
            {
                IsLoading = true;
                Records.Clear();

                var records = await _dataRecordService.LoadAllRecordsAsync();
                foreach (var record in records.OrderByDescending(r => r.StartTime))
                {
                    Records.Add(new RecordItemViewModel(record));
                }

                OnPropertyChanged(nameof(IsEmpty));
            }
            catch (Exception ex)
            {
                _messageService.ShowMessage($"加载记录失败: {ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(IsEmpty));
            }
        }

        /// <summary>
        /// 查看选中的记录
        /// </summary>
        [RelayCommand]
        private void ViewRecord(RecordItemViewModel? item)
        {
            if (item?.Record != null)
            {
                ViewRecordRequested?.Invoke(item.Record);
            }
        }

        /// <summary>
        /// 删除选中的记录
        /// </summary>
        [RelayCommand]
        private async Task DeleteRecordAsync(RecordItemViewModel? item)
        {
            if (item?.FilePath == null)
                return;

            var result = System.Windows.MessageBox.Show(
                $"确定要删除记录 \"{item.Name}\" 吗？\n此操作不可撤销。",
                "确认删除",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes)
                return;

            try
            {
                bool deleted = await _dataRecordService.DeleteRecordAsync(item.FilePath);
                if (deleted)
                {
                    Records.Remove(item);
                    OnPropertyChanged(nameof(IsEmpty));
                    RecordDeleted?.Invoke();
                    _messageService.ShowMessage("记录已删除", "提示");
                }
                else
                {
                    _messageService.ShowMessage("删除记录失败", "错误");
                }
            }
            catch (Exception ex)
            {
                _messageService.ShowMessage($"删除记录失败: {ex.Message}", "错误");
            }
        }

        /// <summary>
        /// 打开保存目录
        /// </summary>
        [RelayCommand]
        private void OpenSaveDirectory()
        {
            try
            {
                var directory = _dataRecordService.SaveDirectory;
                if (System.IO.Directory.Exists(directory))
                {
                    System.Diagnostics.Process.Start("explorer.exe", directory);
                }
                else
                {
                    _messageService.ShowMessage("保存目录不存在", "提示");
                }
            }
            catch (Exception ex)
            {
                _messageService.ShowMessage($"打开目录失败: {ex.Message}", "错误");
            }
        }
    }
}
