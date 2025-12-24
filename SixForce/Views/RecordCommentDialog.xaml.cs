using System.Windows;

namespace SixForce.Views
{
    /// <summary>
    /// RecordCommentDialog.xaml 的交互逻辑
    /// </summary>
    public partial class RecordCommentDialog : Window
    {
        /// <summary>
        /// 用户输入的注释
        /// </summary>
        public string Comment => CommentTextBox.Text.Trim();

        public RecordCommentDialog()
        {
            InitializeComponent();
            CommentTextBox.Focus();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
