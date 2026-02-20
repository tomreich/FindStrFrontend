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

using System;
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
                if (!Directory.Exists(SourceDirectory.Text))
                {
                    MessageBox.Show("The specified base directory does not exist:\n\n" + SourceDirectory.Text, "Invalid Directory", MessageBoxButton.OK, MessageBoxImage.Warning);
                    SourceDirectory.Focus();
                    SourceDirectory.SelectAll();
                    return;
                }

                Properties.Settings.Default.DefaultDirectory = SourceDirectory.Text;
                Properties.Settings.Default.Save();

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
            new CommandDialog(FINDSTR_FILE_NAME + " " + GenerateFindstrCommandArguments()) { Owner = this }.ShowDialog();
        }

        private string GenerateFindstrCommandArguments()
        {
            return $"/N {(Recursive.IsChecked == true ? "/S" : "")} {(SkipBinary.IsChecked == true ? "/P" : "")} {(CaseSensitive.IsChecked == true ? "" : "/I")} {(RegularExpressions.IsChecked == true ? "/R" : "")} {(Literal.IsChecked == true ? "/C:" : "")}\"{Needle.Text}\" \"{SourceDirectory.Text}\\{FileFilter.Text}\"";
        }

        private string GetSelectedFilePath()
        {
            var file = (outputBox.SelectedItem as ResultLine)?.File;
            if (string.IsNullOrWhiteSpace(file))
                return null;
            if (!File.Exists(file))
            {
                MessageBox.Show("The file no longer exists:\n\n" + file, "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }
            return file;
        }

        private void OpenInNotepad_Click(object sender, RoutedEventArgs e)
        {
            var file = GetSelectedFilePath();
            if (file == null) return;
            try
            {
                Process.Start("notepad.exe", QUOTE + file + QUOTE);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open file in Notepad:\n\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenWithDefaultViewer_Click(object sender, RoutedEventArgs e)
        {
            var file = GetSelectedFilePath();
            if (file == null) return;
            try
            {
                Process.Start(QUOTE + file + QUOTE);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open file:\n\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenInNotepadPlusPlus_Click(object sender, RoutedEventArgs e)
        {
            var file = GetSelectedFilePath();
            if (file == null) return;
            var line = (outputBox.SelectedItem as ResultLine).Line;
            var nppPath = GetNotepadPlusPlusPath();
            if (nppPath == null) return;
            try
            {
                Process.Start(nppPath, $"-n{line} \"{file}\"");
            }
            catch (Exception ex)
            {
                if (!PromptForNotepadPlusPlusPath(ex.Message)) return;
                try
                {
                    Process.Start(Properties.Settings.Default.NotepadPlusPlusPath, $"-n{line} \"{file}\"");
                }
                catch (Exception ex2)
                {
                    MessageBox.Show("Could not open file in Notepad++:\n\n" + ex2.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenInNotepadPlusPlusAdmin_Click(object sender, RoutedEventArgs e)
        {
            var file = GetSelectedFilePath();
            if (file == null) return;
            var line = (outputBox.SelectedItem as ResultLine).Line;
            var nppPath = GetNotepadPlusPlusPath();
            if (nppPath == null) return;
            try
            {
                Process.Start(new ProcessStartInfo(nppPath, $"-n{line} \"{file}\"") { Verb = "runas" });
            }
            catch (Exception ex)
            {
                if (!PromptForNotepadPlusPlusPath(ex.Message)) return;
                try
                {
                    Process.Start(new ProcessStartInfo(Properties.Settings.Default.NotepadPlusPlusPath, $"-n{line} \"{file}\"") { Verb = "runas" });
                }
                catch (Exception ex2)
                {
                    MessageBox.Show("Could not open file in Notepad++ (Admin):\n\n" + ex2.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string GetNotepadPlusPlusPath()
        {
            var saved = Properties.Settings.Default.NotepadPlusPlusPath;
            if (!string.IsNullOrWhiteSpace(saved))
            {
                if (File.Exists(saved))
                    return saved;
                MessageBox.Show("The saved Notepad++ path no longer exists:\n\n" + saved, "Notepad++ Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                Properties.Settings.Default.NotepadPlusPlusPath = "";
                Properties.Settings.Default.Save();
            }
            return "notepad++";
        }

        private bool PromptForNotepadPlusPlusPath(string errorMessage)
        {
            var result = MessageBox.Show("Could not launch Notepad++:\n\n" + errorMessage + "\n\nWould you like to browse for the Notepad++ executable?",
                "Notepad++ Not Found", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return false;
            using (var ofd = new System.Windows.Forms.OpenFileDialog()
            {
                Title = "Locate Notepad++",
                Filter = "Executable Files|*.exe",
                FileName = "notepad++.exe"
            })
            {
                if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return false;
                Properties.Settings.Default.NotepadPlusPlusPath = ofd.FileName;
                Properties.Settings.Default.Save();
                return true;
            }
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
            var file = GetSelectedFilePath();
            if (file == null) return;
            try
            {
                Process.Start("explorer.exe", $"/select,\"{file}\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open Explorer:\n\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            new AboutDialog().ShowDialog();
        }
    }
}
