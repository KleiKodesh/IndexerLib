using IndexerLib.Helpers;
using IndexerLib.Index;
using IndexerLib.IndexManger;
using IndexerLib.Sample;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace IndexerTest
{
    public partial class MainWindow : Window
    {
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
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string directory = dialog.FileName;
                IndexManager.CreateIndex(directory);
            }
        }


        void Search()
        {
            // Run the query on your index
            var results = IndexManager.Search(SearchBox.Text);

            var sb = new StringBuilder();
            string htmlStyle = "font-family:Segoe UI; direction:rtl;";

            sb.AppendLine($"<html><body style='{htmlStyle}'>");

            foreach (var result in results)
            {
                string safePath = WebUtility.HtmlEncode(result.DocId.ToString());

                sb.AppendLine("<div style='margin-bottom:12px;'>");
                sb.AppendLine($"<b>Document {result.DocId}</b><br/>");
                sb.AppendLine($"<small style='color:gray;'>Path: {safePath}</small><br/>");

                // Snippet is assumed to already contain <mark> tags (safe HTML)
                sb.AppendLine(result.Snippet + "<br/>");

                sb.AppendLine("</div>");

            }

            sb.AppendLine("</body></html>");
            WebView.NavigateToString(sb.ToString());
        }

      
        private void DebugButton_Click(object sender, RoutedEventArgs e)
        {
            WordsStore.SortWordsByIndex();
        }

        void TokenizerTest()
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = false, // pick a single file
                Filters = { new CommonFileDialogFilter("Text files", "*.txt") }
            };

            if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;

            string filePath = dialog.FileName;
            string text = System.IO.File.ReadAllText(filePath);

            // Tokenize
            var tokensDict = IndexerLib.Tokens.RegexTokenizer.Tokenize(text, filePath);
            var tokens = new List<string>(tokensDict.Keys);

            // Build HTML list
            var sb = new StringBuilder();
            sb.AppendLine("<html><body style='font-family:Segoe UI; direction:rtl;'>");
            sb.AppendLine("<h3>Tokens:</h3>");
            sb.AppendLine("<ul>");
            foreach (var token in tokens)
                sb.AppendLine($"<li>{WebUtility.HtmlEncode(token)}</li>");
            sb.AppendLine("</ul>");
            sb.AppendLine("</body></html>");

            WebView.NavigateToString(sb.ToString());
        }
    }
}
