using System.Windows;

namespace FindStrFrontend
{
    /// <summary>
    /// Interaction logic for CommandDialog.xaml
    /// </summary>
    public partial class CommandDialog : Window
    {
        public CommandDialog(string command)
        {
            InitializeComponent();
            CommandText.Text = command;
            CommandText.Focus();
            CommandText.SelectAll();
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(CommandText.Text);
            CopyButton.Content = "Copied!";
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
