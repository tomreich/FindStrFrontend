/*
 * Copyright (c) 2019 Tom Reich
 * 
 * Licensed under the Microsoft Public License (MS-PL) (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *  https://msdn.microsoft.com/en-us/library/ff649456.aspx
 *  or
 *  https://opensource.org/licenses/MS-PL
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FindStrFrontend
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<ResultLine> resultLines { get; private set; }
        private const string FINDSTR_FILE_NAME = "findstr.exe";
        private const string QUOTE = "\"";
        private Process _findStr;

        public MainWindow()
        {
            resultLines = new ObservableCollection<ResultLine>();
            InitializeComponent();
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.DefaultDirectory) && Directory.Exists(Properties.Settings.Default.DefaultDirectory))
                SourceDirectory.Text = Properties.Settings.Default.DefaultDirectory;
        }

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            if(_findStr?.HasExited != false)
            {
                resultLines.Clear();
                totalMatches.Text = "0";
                _findStr = new Process() { StartInfo = new ProcessStartInfo() { FileName = FINDSTR_FILE_NAME, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true, Arguments = GenerateFindstrCommandArguments() } };
                SearchButton.Content = "Cancel";
                progressBar.IsIndeterminate = true;
                await Task.Run(() =>
                {
                    _findStr.Start();
                    string line;
                    while ((line = _findStr.StandardOutput.ReadLine()) != null)
                        this.Dispatcher.Invoke(() => { resultLines.Add(new ResultLine(line)); totalMatches.Text = resultLines.Count.ToString(); });

                    string stderr = _findStr.StandardError.ReadToEnd();
                    if (!string.IsNullOrWhiteSpace(stderr))
                        MessageBox.Show(stderr, "FindStr Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    _findStr.WaitForExit();
                });
                progressBar.IsIndeterminate = false;
                progressBar.Value = 1;
                SearchButton.Content = "Search";
            }
            else
            {
                _findStr.Kill();
            }
        }

        private void DirectoryChoose_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new System.Windows.Forms.FolderBrowserDialog() { SelectedPath = Properties.Settings.Default.DefaultDirectory })
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    SourceDirectory.Text = Properties.Settings.Default.DefaultDirectory = fbd.SelectedPath;

            Properties.Settings.Default.Save();
        }

        private void DirectoryChoose_RightClick(object sender, MouseButtonEventArgs e)
        {
            using (var fbd = new System.Windows.Forms.SaveFileDialog() { InitialDirectory = Properties.Settings.Default.DefaultDirectory, FileName = "Use this Directory" })
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    SourceDirectory.Text = Properties.Settings.Default.DefaultDirectory = new FileInfo(fbd.FileName).DirectoryName;

            Properties.Settings.Default.Save();
        }

        private void ShowCommand_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(FINDSTR_FILE_NAME + " " + GenerateFindstrCommandArguments(), "FindStr Command");
        }

        private string GenerateFindstrCommandArguments()
        {
            return $"/N {(Recursive.IsChecked == true ? "/S" : "")} {(SkipBinary.IsChecked == true ? "/P" : "")} {(CaseSensitive.IsChecked == true ? "" : "/I")} {(RegularExpressions.IsChecked == true ? "/R" : "")} {(Literal.IsChecked == true ? "/C:" : "")}\"{Needle.Text}\" \"{SourceDirectory.Text}\\{FileFilter.Text}\"";
        }

        private void OpenInNotepad_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace((outputBox.SelectedItem as ResultLine)?.File))
                Process.Start("notepad.exe", QUOTE + (outputBox.SelectedItem as ResultLine).File + QUOTE);
        }

        private void OpenWithDefaultViewer_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace((outputBox.SelectedItem as ResultLine)?.File))
                Process.Start(QUOTE + (outputBox.SelectedItem as ResultLine).File + QUOTE);
        }

        private void OpenInNotepadPlusPlus_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace((outputBox.SelectedItem as ResultLine)?.File))
                Process.Start("notepad++", $"-n{(outputBox.SelectedItem as ResultLine).Line} \"{(outputBox.SelectedItem as ResultLine).File}\"");
        }

        private void OpenInNotepadPlusPlusAdmin_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace((outputBox.SelectedItem as ResultLine)?.File))
                Process.Start(new ProcessStartInfo("notepad++", $"-n{(outputBox.SelectedItem as ResultLine).Line} \"{(outputBox.SelectedItem as ResultLine).File}\"") { Verb="runas" });
        }

        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            if (!resultLines.Any()) return;
            //using (new ControlDisabler)
            using (var sfd = new System.Windows.Forms.SaveFileDialog() { Filter = "Comma-Separated Values|*.csv", Title = "Export Search Results" })
                if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    await Task.Run(() => File.WriteAllLines(sfd.FileName, resultLines.Select(x => x.CSVLine).ToArray()));
        }

        private void ShowInExplorer_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace((outputBox.SelectedItem as ResultLine)?.File))
                Process.Start("explorer.exe", $"/select,\"{(outputBox.SelectedItem as ResultLine).File}\"");
        }
    }
}
