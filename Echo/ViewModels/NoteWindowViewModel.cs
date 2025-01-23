﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Echo.Models;
using Echo.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Echo.ViewModels
{
    public partial class NoteWindowViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;

        // 搜索文字
        [ObservableProperty]
        private string _searchText;

        // 收藏夹列表
        public ObservableCollection<CollectionModel> Collections { get; } = new();

        // 当前选中的收藏夹
        [ObservableProperty]
        private CollectionModel _selectedCollection;

        // 排序选项
        public ObservableCollection<string> SortOptions { get; }

        // 当前选中的排序选项
        [ObservableProperty]
        private string _selectedSortOption;

        // 过滤选项
        public ObservableCollection<string> FilterOptions { get; }

        // 当前选中的过滤选项
        [ObservableProperty]
        private string _selectedFilterOption;

        // 单词列表
        public ObservableCollection<WordBasicModel> Words { get; } = new();

        // 完整单词
        [ObservableProperty]
        private WordModel _currentWord;

        // 完整单词
        [ObservableProperty]
        private List<SenseGroup> _senseGroups;

        // 当前选中的单词
        [ObservableProperty]
        private WordBasicModel _selectedWord;


        // 构造函数
        public NoteWindowViewModel()
        {
            _databaseService = new DatabaseService();

            // 初始化集合
            //Collections = new ObservableCollection<CollectionModel>();
            //Words = new ObservableCollection<WordModel>();

            // 准备选项
            SortOptions = new ObservableCollection<string> { "按时间", "按字母" };
            FilterOptions = new ObservableCollection<string> { "全部", "已收藏", "未收藏" };

            // 加载数据
            LoadCollections();
            //LoadWords();

            // 默认选择
            SelectedSortOption = SortOptions[0];
            SelectedFilterOption = FilterOptions[0];
        }

        /// <summary>
        /// 从数据库加载所有收藏夹
        /// </summary>
        private async Task LoadCollections()
        {
            var collectionList = await _databaseService.GetAllCollections();
            Collections.Clear();
            foreach (var col in collectionList)
            {
                Collections.Add(col);
            }

            // 默认选中第一个（可选）
            if (Collections.Count > 0)
            {
                SelectedCollection = Collections[0];
            }
        }

        [RelayCommand]
        public async Task LoadBasicWordsAsync()
        {
            var wordList = await _databaseService.GetAllWordsBasicAsync();
            Words.Clear();
            foreach (var w in wordList)
            {
                Words.Add(w);
            }
        }

        // 选中单词时，加载详细数据
        partial void OnSelectedWordChanged(WordBasicModel value)
        {
            if (value != null)
            {
                LoadWordDetailsAsync(value);
            }
        }

        private async void LoadWordDetailsAsync(WordBasicModel basicWord)
        {
            //如果已经加载过详细信息，可以不重复加载
            //这里简单写一下，如果需要判断，可加标记
             //if (basicWord.HasDetails) return;

            //int wordId = (int)basicWord.Id; 
            var detailedWord = await _databaseService.GetWordDetailsAsync(basicWord.Id);

            if (detailedWord != null)
            {
                // 把详细信息更新到 basicWord 中
                detailedWord.SourceLanguageCode = detailedWord.SourceLanguageCode;
                detailedWord.TargetLanguageCode = detailedWord.TargetLanguageCode;
                detailedWord.Pronounciations = detailedWord.Pronounciations;
                detailedWord.Definitions = detailedWord.Definitions;
                // ...
                // 其他需要的字段
            }
            CurrentWord = detailedWord;
            //GroupedSenses();
        }

        //private void GroupedSenses()
        //{
        //    // 若 CurrentWord 或其 Senses 为空，置空列表后返回
        //    if (CurrentWord?.Senses == null || CurrentWord.Senses.Count == 0)
        //    {
        //        SenseGroups = new List<SenseGroup>();
        //        return;
        //    }

        //    // 使用 GroupBy 按 (ExplanationLanguageCode, Category) 分组
        //    var groups = CurrentWord.Senses
        //        .GroupBy(s => new { s.ExplanationLanguageCode, s.Category })
        //        .Select(g => new SenseGroup
        //        {
        //            ExplanationLanguageCode = g.Key.ExplanationLanguageCode,
        //            Category = g.Key.Category,
        //            Senses = g.ToList()
        //        })
        //        .ToList();

        //    // 将结果赋值给可观察属性 SenseGroups
        //    SenseGroups = groups;
        //}

        /// <summary>
        /// 搜索、筛选、排序等逻辑统一在这个命令里处理
        /// 你也可以分开多个命令或在属性的 PropertyChanged 回调中处理
        /// </summary>
        [RelayCommand]
        //private void ApplyFilterAndSort()
        //{
        //    // 1. 根据搜索关键字过滤
        //    // 2. 根据 SelectedCollection 过滤
        //    // 3. 根据 SelectedFilterOption 过滤
        //    // 4. 根据 SelectedSortOption 排序
        //    // 以下是简单示例，可自行扩展

        //    // 首先重新加载或刷新列表（如果不想每次都查询数据库，可以在内存中操作 Words）
        //    //LoadWords();

        //    // 在内存中进行过滤和排序
        //    var filteredList = new List<WordBasicModel>(Words);

        //    // 搜索过滤
        //    if (!string.IsNullOrWhiteSpace(SearchText))
        //    {
        //        filteredList = filteredList
        //            .Where(w => w.Word != null && w.Word.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase))
        //            .ToList();
        //    }

        //    // 收藏夹过滤（如果需要根据 SelectedCollection.ID 之类进行过滤）
        //    if (SelectedCollection != null)
        //    {
        //        // 假设 WordModel 里有一个 CollectionId 或 DatabaseService 有方法检查隶属
        //        // 此处仅为演示
        //        // filteredList = filteredList.Where(w => w.CollectionId == SelectedCollection.Id).ToList();
        //    }

        //    // 过滤选项
        //    // “已收藏” or “未收藏” 之类
        //    if (SelectedFilterOption == "已收藏")
        //    {
        //        filteredList = filteredList.Where(w => w.IsFavorite).ToList();
        //    }
        //    else if (SelectedFilterOption == "未收藏")
        //    {
        //        filteredList = filteredList.Where(w => !w.IsFavorite).ToList();
        //    }

        //    // 排序选项
        //    if (SelectedSortOption == "按时间")
        //    {
        //        // 假设 WordModel 里有个 CreateTime
        //        filteredList = filteredList.OrderBy(w => w.SourceStartTime).ToList();
        //    }
        //    else if (SelectedSortOption == "按字母")
        //    {
        //        filteredList = filteredList.OrderBy(w => w.Word).ToList();
        //    }

        //    // 最后更新 Words
        //    Words.Clear();
        //    int index = 1;
        //    foreach (var w in filteredList)
        //    {
        //        w.SourceFileName = index.ToString(); // 模拟一个序号
        //        index++;
        //        Words.Add(w);
        //    }
        //}

        // 在 XAML 中，如果需要在搜索框 TextChanged 时立即执行过滤，
        // 可以在 SearchText 的 set 里调用 ApplyFilterAndSort()，或用一个延迟搜索机制
        // 此处仅演示。可以在属性变化时调用命令：
        partial void OnSearchTextChanged(string value)
        {
            //ApplyFilterAndSort();
        }

        partial void OnSelectedCollectionChanged(CollectionModel value)
        {
            //ApplyFilterAndSort();
        }

        partial void OnSelectedFilterOptionChanged(string value)
        {
            //ApplyFilterAndSort();
        }

        partial void OnSelectedSortOptionChanged(string value)
        {
            //ApplyFilterAndSort();
        }
    }
}