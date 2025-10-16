using SimplifiedIndexerLib.Index;
using Microsoft.WindowsAPICodePack.Dialogs;
using SimplifiedIndexerLib.IndexSearch;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using SimplifiedIndexerLib.Tokens;

namespace IndexerTest
{
    public partial class MainWindow : Window
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        public MainWindow()
        {
            InitializeComponent();
            WebView.EnsureCoreWebView2Async();
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { Search(); e.Handled = true; }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }


        private void CreateIndexButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new CommonOpenFileDialog
                {
                    IsFolderPicker = true
                };

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    string directory = dialog.FileName;
                    IndexCreator.Execute(directory, new string[] { ".txt", ".pdf" }, MemoryUsageBox.Value);
                    //IndexManager.CreateIndex(directory, new string[] { ".txt", ".pdf" }, MemoryUsageBox.Value);
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }


        public void Search()
        {
            new SearchHelper().Search(cts, SearchBox.Text, (short)AdjacencySettingsBox.Value, WebView);
        }



        private void DebugButton_Click(object sender, RoutedEventArgs e)
        {
            var filePicker = new Microsoft.Win32.OpenFileDialog();
            if (filePicker.ShowDialog() == true)
            {
                var tokens = new Tokenizer(filePicker.FileName, 0).Tokens;
                foreach (var token in tokens)
                {
                    return;
                }
            }
        }
    }
}
