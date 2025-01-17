using System.ComponentModel;
using System.Windows;
using Echo.ViewModels;

namespace Echo.Views
{
    public partial class NoteWindowView : Window
    {
        private readonly NoteWindowViewModel _viewModel;

        public NoteWindowView()
        {
            InitializeComponent();

            _viewModel = new NoteWindowViewModel();
            DataContext = _viewModel;

            // 注册窗口关闭事件
            Closing += OnWindowClosing;
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            //if (_viewModel.HasUnsavedChanges)
            //{
            //    var result = MessageBox.Show(
            //        "You have unsaved changes. Do you want to save them before closing?",
            //        "Unsaved Changes",
            //        MessageBoxButton.YesNoCancel,
            //        MessageBoxImage.Question);

            //    switch (result)
            //    {
            //        case MessageBoxResult.Yes:
            //            _viewModel.SaveChangesCommand.Execute(null);
            //            break;
            //        case MessageBoxResult.Cancel:
            //            e.Cancel = true;
            //            break;
            //    }
            //}
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            // 恢复窗口位置和大小（如果有保存的话）
            //_viewModel.LoadWindowSettings(this);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            // 保存窗口位置和大小
            if (!e.Cancel)
            {
                //_viewModel.SaveWindowSettings(this);
            }
        }
    }
}