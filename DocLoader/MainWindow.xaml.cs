using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using RequestRecognitionToolLib.Main;
using RequestRecognitionToolLib.Main.classes;
using System.Collections.ObjectModel;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace DocLoader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    using Recognizer = RequestRecognitionToolLib.Main.classes;
    public partial class MainWindow : Window
    {
        Parser parser;
        DataFile dfile;
        ParserConfig parserConfig = new ParserConfig()
        {
            Language = "eng+rus+est",
            TesseractExecuteDir = @"D:\Program Files\Tesseract-OCR",
            TessDataDir = @"D:\Program Files\Tesseract-OCR\tessdata",
            NativeLibraryDirectory = @"D:\source\repos\ParserTool\RequestRecognitionToolLib\RequestRecognitionToolLib\bin\x64\Debug\netcoreapp3.1\runtimes\win-x64\native",
            GhostscriptDirectory = @"D:\Program Files\gs\gs9.53.2\bin"
        };
        public MainWindow()
        {
            InitializeComponent();
            progressLoad.Visibility = Visibility.Hidden;
            parser = new Parser();
            log("assembly loaded: " + parser.getAssemblyName());
        }

        private void drawAnalyseTable(List<Recognizer.Page> pages)
        {
            data_table.Items.Clear();
            data_table.Columns.Clear();

            DataGridTextColumn textColumn = new DataGridTextColumn();
            foreach (var page in pages)
            {
                List<string[]> prows = page.PageRows;
                for (int i = 0; i < page.MaxColumnsCount; i++)
                {
                    textColumn = new DataGridTextColumn();
                    textColumn.Header = "col_" + i;
                    textColumn.Binding = new Binding(string.Format("[{0}]", i));
                    data_table.Columns.Add(textColumn);
                }

                foreach (string[] row in page)
                {
                    data_table.Items.Add(data_table.DataContext = row);
                }
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            progressLoad.Visibility = Visibility.Visible;
            progressLoad.IsIndeterminate = true;
            data_list.Items.Clear();
            Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog();
            Nullable<bool> result = openFileDlg.ShowDialog();
            if (result == true)
            {
                string fname = openFileDlg.FileName;
                var task = Task.Factory.StartNew(() => parser = new Parser(dfile = new DataFile(fname), parserConfig));

                await task;
                progressLoad.IsIndeterminate = false;
                progressLoad.Value = 100;
                if (!dfile.HasLoadError)
                {
                    log("file: " + dfile.fileInfo.Name + " size: " + dfile.fileInfo.Length + " bytes");
                    parser.getAllElements().ForEach(e => data_list.Items.Add(new ListBoxItem().Content = e));
                }
                else
                    log("error: " + dfile.LastErrorMsg);
                parser = null;
            }

        }
        // Drag and drop files on the grid
        private async void Grid_Drop(object sender, DragEventArgs e)
        {
            progressLoad.Visibility = Visibility.Visible;
            progressLoad.IsIndeterminate = true;

            data_list.Items.Clear();
            if (null != e.Data && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var i in files)
                {
                    var task = Task.Factory.StartNew(() => parser = new Parser(dfile = new DataFile(i), parserConfig));

                    await task;
                    progressLoad.IsIndeterminate = false;
                progressLoad.Value = 100;

                    if (!dfile.HasLoadError)
                    {
                        log("file: " + dfile.fileInfo.Name + " size: " + dfile.fileInfo.Length + " bytes");
                        parser.getAllElements().ForEach(e => data_list.Items.Add(new ListBoxItem().Content = e));
                    }
                    else
                        log("error: " + dfile.LastErrorMsg);
                    drawAnalyseTable(parser.DocumentPages);
                    parser = null;
                }
            }
        }

        private void Grid_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void log(string txt)
        {
            ListBoxItem itm = new ListBoxItem();
            itm.Content = txt;
            log_list.Items.Add(itm);
        }
    }
}
