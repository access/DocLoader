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
/// ==========================================
///  Title:     Recognizer for patterns from PDF, Image, Excel, etc. file types;
///  Author:    Jevgeni Kostenko
///  Copyright: Baltic Bolt OÜ
///  Date:      21.09.2020
/// ==========================================

using RequestRecognitionToolLib.Main.classes;
using RequestRecognitionToolLib.Main.Interfaces;
using System.Collections.ObjectModel;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;

namespace DocLoader
{
    using Recognizer = RequestRecognitionToolLib.Main.classes;

    public partial class MainWindow : Window
    {
        Parser _parser;
        DataFile dfile;
        string lastFilePath = "";
        string _cmdInputFile = string.Empty;
        string _cmdOutputFile = string.Empty;
        string _cmdOutputDictionaryFile = string.Empty;
        string _cmdoutputJSON = string.Empty;
        string _cmdRemoveItem = string.Empty;
        string _cmdAddItem = string.Empty;

        bool _cmdIsCaseSensitive = false;
        bool _cmdIsRegEx = false;
        bool _cmdIsSkipWholeLine = false;

        ParserConfig parserConfig = new ParserConfig()
        {
            Language = "est+rus+eng",
            TesseractExecuteDir = @"D:\Program Files\Tesseract-OCR",
            TessDataDir = @"D:\Program Files\Tesseract-OCR\tessdata",
            NativeLibraryDirectory = @"D:\source\repos\ParserTool\RequestRecognitionToolLib\RequestRecognitionToolLib\bin\x64\Debug\netcoreapp3.1\runtimes\win-x64\native",
            GhostscriptDirectory = @"D:\Program Files\gs\gs9.53.2\bin",
            BlackListDictionaryPath = @"D:\source\repos\ParserTool\RequestRecognitionToolLib\RequestRecognitionToolLib\bin\Debug\netcoreapp3.1\blackListDictionary.json",
            CleanPercentageOfColumnsCount = 50
        };

        public MainWindow()
        {
            string[] args = Environment.GetCommandLineArgs();
            //--------------- command line check mode ----------------------------
            if (args.Length > 1)
            {
                InitializeComponent();
                mainWin.Visibility = Visibility.Hidden;
                progressLoad.Visibility = Visibility.Hidden;

                //------------------ checking cmd arguments -------------------------
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].ToUpper() == "-I" || args[i].ToUpper() == "/I" || args[i].ToUpper() == "-INPUTFILE" || args[i].ToUpper() == "--INPUTFILE" || args[i].ToUpper() == "/INPUTFILE")
                        try { _cmdInputFile = args[i + 1]; } catch (Exception) { }
                    if (args[i].ToUpper() == "-O" || args[i].ToUpper() == "/O" || args[i].ToUpper() == "-OUTPUTFILE" || args[i].ToUpper() == "--OUTPUTFILE" || args[i].ToUpper() == "/OUTPUTFILE")
                        try { _cmdOutputFile = args[i + 1]; } catch (Exception) { }
                    if (args[i].ToUpper() == "-D" || args[i].ToUpper() == "/D" || args[i].ToUpper() == "-DICTIONARY" || args[i].ToUpper() == "--DICTIONARY" || args[i].ToUpper() == "/DICTIONARY")
                        try { _cmdOutputDictionaryFile = args[i + 1]; } catch (Exception) { }
                    if (args[i].ToUpper() == "-R" || args[i].ToUpper() == "/R" || args[i].ToUpper() == "-REMOVE" || args[i].ToUpper() == "--REMOVE" || args[i].ToUpper() == "/REMOVE")
                        try { _cmdRemoveItem = args[i + 1]; } catch (Exception) { }
                    if (args[i].ToUpper() == "-ADD" || args[i].ToUpper() == "/ADD" || args[i].ToUpper() == "--ADD")
                        try { _cmdAddItem = args[i + 1]; } catch (Exception) { }
                    if (args[i].ToUpper() == "-ISCASESENS" || args[i].ToUpper() == "/ISCASESENS" || args[i].ToUpper() == "--ISCASESENS")
                        try { _cmdIsCaseSensitive = args[i + 1] == "true" ? true : false; } catch (Exception) { }
                    if (args[i].ToUpper() == "-ISREGEX" || args[i].ToUpper() == "/ISREGEX" || args[i].ToUpper() == "--ISREGEX")
                        try { _cmdIsRegEx = args[i + 1] == "true" ? true : false; } catch (Exception) { }
                    if (args[i].ToUpper() == "-ISSKIPLINE" || args[i].ToUpper() == "/ISSKIPLINE" || args[i].ToUpper() == "--ISSKIPLINE")
                        try { _cmdIsSkipWholeLine = args[i + 1] == "true" ? true : false; } catch (Exception) { }
                }
                if (!String.IsNullOrEmpty(_cmdOutputDictionaryFile)) GenerateDictionary();
                if (!String.IsNullOrEmpty(_cmdRemoveItem)) deleteFilterWord(_cmdRemoveItem);
                if (!String.IsNullOrEmpty(_cmdAddItem)) addFilterWord(_cmdAddItem, _cmdIsCaseSensitive, _cmdIsRegEx, _cmdIsSkipWholeLine);
                if (String.IsNullOrEmpty(_cmdInputFile) || String.IsNullOrEmpty(_cmdOutputFile)) Process.GetCurrentProcess().Kill();
                //------------------ checking OK ---------------------------------
                GenerateDocumentJSON();
            }
            //--------------------------------------------------------------------
            else
            {
                InitializeComponent();
                progressLoad.Visibility = Visibility.Hidden;
            }
        }

        private void addFilterWord(string itemVal, bool IsCaseSensitive, bool IsRegularExpression, bool SkipWholeLine)
        {
            byte[] data = System.Convert.FromBase64String(itemVal);
            string base64Decoded = System.Text.UTF8Encoding.UTF8.GetString(data);

            _parser = new Parser(parserConfig);
            _parser.AddFilterWord(base64Decoded, IsCaseSensitive, IsRegularExpression, SkipWholeLine);
            Process.GetCurrentProcess().Kill();
        }

        private void deleteFilterWord(string itemVal)
        {
            _parser = new Parser(parserConfig);
            _parser.DeleteFilterWord(itemVal);
            Process.GetCurrentProcess().Kill();
        }

        private void GenerateDictionary()
        {
            File.WriteAllText(_cmdOutputDictionaryFile, File.ReadAllText(parserConfig.BlackListDictionaryPath));
            Process.GetCurrentProcess().Kill();
        }

        private async void GenerateDocumentJSON()
        {
            _parser = new Parser(dfile = new DataFile(_cmdInputFile), parserConfig);
            await _parser.GenerateDocumentAsync();
            _cmdoutputJSON = _parser.CleanDocumentJSON;
            try { File.WriteAllText(_cmdOutputFile, _cmdoutputJSON); } catch (Exception) { }
            Process.GetCurrentProcess().Kill();
        }

        private void drawAnalyseTable(IDocument doc)
        {
            data_table.Items.Clear();
            data_table.Columns.Clear();

            DataGridTextColumn textColumn = new DataGridTextColumn();
            foreach (Recognizer.Page page in doc)
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
                btn_reloadFile.IsEnabled = true;
                string fname = openFileDlg.FileName;
                lastFilePath = fname;
                _parser = new Parser(dfile = new DataFile(fname), parserConfig);
                await _parser.GenerateDocumentAsync();
                _parser.GetFilterBlackListWords().ForEach(el => data_list.Items.Add(el));

                progressLoad.IsIndeterminate = false;
                progressLoad.Value = 100;
                if (!dfile.HasLoadError)
                {
                    log("file: " + dfile.fileInfo.Name + " size: " + dfile.fileInfo.Length + " bytes");
                }
                else
                    log("error: " + dfile.LastErrorMsg);
                drawAnalyseTable(_parser.GetCleanDocument());
                Debug.WriteLine(_parser.CleanDocumentJSON);
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
                btn_reloadFile.IsEnabled = true;
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                {
                    _parser = new Parser(dfile = new DataFile(file), parserConfig);
                    lastFilePath = file;
                    await _parser.GenerateDocumentAsync();
                    _parser.GetFilterBlackListWords().ForEach(el => data_list.Items.Add(el));

                    progressLoad.IsIndeterminate = false;
                    progressLoad.Value = 100;

                    if (!dfile.HasLoadError)
                    {
                        log("file: " + dfile.fileInfo.Name + " size: " + dfile.fileInfo.Length + " bytes");
                    }
                    else
                        log("error: " + dfile.LastErrorMsg);
                    drawAnalyseTable(_parser.GetCleanDocument());
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
            itm.Content = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + txt;
            log_list.Items.Add(itm);
        }

        private void txt_ignoreWord_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!String.IsNullOrEmpty(txt_ignoreWord.Text.Trim()))
            {
                btn_addBlackList.IsEnabled = true;
                chk_caseSensitive.IsEnabled = true;
                chk_isRegular.IsEnabled = true;
                chk_skipWholeLine.IsEnabled = true;
            }
            else
            {
                btn_addBlackList.IsEnabled = false;
                chk_caseSensitive.IsEnabled = false;
                chk_isRegular.IsEnabled = false;
                chk_skipWholeLine.IsEnabled = false;
            }
        }

        private async void btn_addBlackList_Click(object sender, RoutedEventArgs e)
        {
            bool isCaseSensitive = false;
            if (chk_caseSensitive.IsChecked != null)
            {
                isCaseSensitive = (bool)chk_caseSensitive.IsChecked;
            }
            bool isRegularExpr = false;
            if (chk_isRegular.IsChecked != null)
            {
                isRegularExpr = (bool)chk_isRegular.IsChecked;
            }
            bool skipWholeLine = false;
            if (chk_skipWholeLine.IsChecked != null)
            {
                skipWholeLine = (bool)chk_skipWholeLine.IsChecked;
            }

            await _parser.AddFilterWordAsync(txt_ignoreWord.Text.Trim(), isCaseSensitive, isRegularExpr, skipWholeLine);
            data_list.Items.Clear();
            _parser.GetFilterBlackListWords().ForEach(el => data_list.Items.Add(el));

            chk_caseSensitive.IsChecked = false;
            chk_isRegular.IsChecked = false;
            chk_skipWholeLine.IsChecked = false;
            txt_ignoreWord.Text = "";
        }

        private void data_table_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                data_list.SelectedIndex = -1;
                DataGrid dataGrid = sender as DataGrid;
                DataGridRow row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(dataGrid.SelectedIndex);
                var sunit = dataGrid.CurrentCell;
                if (sunit.Column != null)
                {
                    int? idx = sunit.Column.DisplayIndex;
                    DataGridCell RowColumn = dataGrid.Columns[(int)idx].GetCellContent(row).Parent as DataGridCell;
                    string CellValue = ((TextBlock)RowColumn.Content).Text;

                    txt_ignoreWord.Text = CellValue;
                }
            }
            catch (Exception) { }
        }

        private void data_list_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (data_list.SelectedIndex != -1)
            {
                btn_deleteFilterWord.IsEnabled = true;
            }
            else
            {
                btn_deleteFilterWord.IsEnabled = false;

            }
        }

        private async void btn_deleteFilterWord_Click(object sender, RoutedEventArgs e)
        {
            string val = data_list.SelectedItem.ToString();

            await _parser.DeleteFilterWordAsync(val);
            data_list.Items.Clear();
            _parser.GetFilterBlackListWords().ForEach(el => data_list.Items.Add(el));
        }

        private async void btn_reloadFile_Click(object sender, RoutedEventArgs e)
        {
            data_list.Items.Clear();

            progressLoad.Visibility = Visibility.Visible;
            progressLoad.IsIndeterminate = true;

            _parser = new Parser(dfile = new DataFile(lastFilePath), parserConfig);
            await _parser.GenerateDocumentAsync();
            _parser.GetFilterBlackListWords().ForEach(el => data_list.Items.Add(el));

            progressLoad.IsIndeterminate = false;
            progressLoad.Value = 100;
            if (!dfile.HasLoadError)
            {
                log("file: " + dfile.fileInfo.Name + " size: " + dfile.fileInfo.Length + " bytes");
            }
            else
                log("error: " + dfile.LastErrorMsg);
            drawAnalyseTable(_parser.GetCleanDocument());

        }
    }
}
