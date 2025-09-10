using IndexerLib.IndexSearch;
using IndexerLib.Sample;
using Microsoft.WindowsAPICodePack.Dialogs;
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

            // Build HTML with StringBuilder
            var sb = new StringBuilder();
            sb.Append("<html><body style='font-family:Segoe UI;'>");

            foreach (var snippet in results)
            {
                sb.Append("<div style='margin-bottom:12px;'>");
                sb.AppendFormat("<b>Document {0}</b><br/>", snippet.DocId);

                // show the doc path
                sb.AppendFormat("<small style='color:gray;'>Path: {0}</small><br/>",
                    WebUtility.HtmlEncode(snippet.DocPath));

                sb.AppendFormat("<span>{0}</span><br/>", WebUtility.HtmlEncode(snippet.Text));

                if (snippet.MatchPositions?.Any() == true)
                {
                    sb.AppendFormat("<small>Match positions: {0}</small>",
                        string.Join(", ", snippet.MatchPositions));
                }

                sb.Append("</div>");
            }

            sb.Append("</body></html>");
            WebView.NavigateToString(sb.ToString());
        }

    }
}
