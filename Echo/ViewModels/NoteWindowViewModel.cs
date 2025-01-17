using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Controls;

namespace Echo.ViewModels
{
    public partial class NoteWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<WordItem> _words = new();

        [ObservableProperty]
        private ObservableCollection<WordCollection> _collections = new();

        [ObservableProperty]
        private WordCollection _selectedCollection;

        [ObservableProperty]
        private WordItem _selectedWord;

        [ObservableProperty]
        private string _selectedSortOption;

        [ObservableProperty]
        private string _selectedFilterOption;

        [ObservableProperty]
        private bool _isListView = true;

        public ObservableCollection<string> SortOptions { get; } = new()
        {
            "新旧排序",
            "按名称",
            "按难度"
        };

        public ObservableCollection<string> FilterOptions { get; } = new()
        {
            "筛选",
            "已掌握",
            "待复习",
            "未标记"
        };

        public NoteWindowViewModel()
        {
            LoadDataAsync().ConfigureAwait(false);
        }

        [RelayCommand]
        private void ShowDetail(WordItem word)
        {
            // 显示单词详情
            SelectedWord = word;
            // TODO: 打开详情对话框或面板
        }

        [RelayCommand]
        private void Play()
        {
            // 播放发音
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadDataAsync();
        }

        [RelayCommand]
        private void Search()
        {
            // 打开搜索对话框
        }

        [RelayCommand]
        private void ToggleListView()
        {
            IsListView = true;
        }

        [RelayCommand]
        private void ToggleCardView()
        {
            IsListView = false;
        }

        [RelayCommand]
        private void Review()
        {
            // 打开复习模式
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // 加载数据
                await Task.Delay(100); // 模拟加载

                // 测试数据
                Words.Clear();
                for (int i = 1; i <= 20; i++)
                {
                    Words.Add(new WordItem
                    {
                        Index = i,
                        Word = $"word{i}",
                        FirstDefinition = $"这是第{i}个单词的释义",
                        Type = "n.",
                        IsMastered = false
                    });
                }

                Collections.Clear();
                Collections.Add(new WordCollection { Name = "所有分组", Count = 20 });
                Collections.Add(new WordCollection { Name = "未分组", Count = 5 });
                Collections.Add(new WordCollection { Name = "已掌握", Count = 8 });
                Collections.Add(new WordCollection { Name = "待复习", Count = 7 });
            }
            catch (Exception ex)
            {
                // 错误处理
                Debug.WriteLine($"Error loading data: {ex.Message}");
            }
        }
    }

    public partial class WordItem : ObservableObject
    {
        [ObservableProperty]
        private int _index;

        [ObservableProperty]
        private string _word;

        [ObservableProperty]
        private string _firstDefinition;

        [ObservableProperty]
        private string _type;

        [ObservableProperty]
        private bool _isMastered;
    }

    public partial class WordCollection : ObservableObject
    {
        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private int _count;
    }
}