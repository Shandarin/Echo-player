using System.Windows;

namespace Echo.Views
{
    public partial class OxfordConfigDialog : Window
    {
        public string ApiId { get; private set; }
        public string ApiKey { get; private set; }

        public OxfordConfigDialog(string currentApiId = "", string currentApiKey = "")
        {
            InitializeComponent();
            txtApiId.Text = currentApiId;
            txtApiKey.Text = currentApiKey;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            ApiId = txtApiId.Text.Trim();
            ApiKey = txtApiKey.Text.Trim();
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}