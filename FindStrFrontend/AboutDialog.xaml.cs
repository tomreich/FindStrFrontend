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

using System.Windows;
using System.Diagnostics;
using System.Reflection;

namespace FindStrFrontend
{
    /// <summary>
    /// Interaction logic for AboutDialog.xaml
    /// </summary>
    public partial class AboutDialog : Window
    {
        public AboutDialog()
        {
            InitializeComponent();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
            txtAbout.Content = $"Findstr Frontend v{assembly.GetName().Version.ToString()}\n{versionInfo.CompanyName}\n{versionInfo.LegalCopyright}"
                + "\n\nDistributed under terms of the Microsoft Public License.";
        }
    }
}
