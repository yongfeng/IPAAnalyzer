using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using IPAAnalyzer.Domain;
using IPAAnalyzer.Service;

namespace IPAAnalyzer.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private PackageService _pkgService = new PackageService();
     
        private int _totalFilesCount = 0;

        private string _srcDir;
        private string _dstDir;

        private BackgroundWorker _analyzeBgWorker = null;
        private BackgroundWorker _transferBgWorker = null;

        private bool _analyzed = false;
        private bool _isMove = false;

        public MainWindow()
        {
            InitializeComponent();
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

            StackPanelRunningStatus.Visibility = Visibility.Collapsed;
            ListViewOutput.DataContext = new PackageInfoViewModel(ListViewOutput);

            TextBoxSrcDir.Text = @"d:\workspace\tmp";
            TextBoxDstDir.Text = @"d:\workspace\organized_ipa";
        }

        private void ResetUI()
        {
            StackPanelRunningStatus.Visibility = Visibility.Visible;
            TextBlockStatus.Text = "Start processing...";
        }

        #region BackgroundWorker - Analyze
        void AnalyzeBgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Analyze();
        }

        void AnalyzeBgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressBarRun.Value = e.ProgressPercentage;

            PackageInfo pkgInfo = e.UserState as PackageInfo;
            TextBlockStatus.Text = "Analyzed " + pkgInfo.OriginalFile.Substring(pkgInfo.OriginalFile.LastIndexOf('\\') + 1);
            ListViewOutput.Items.Insert(0, pkgInfo);
        }

        void AnalyzeBgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            TextBlockStatus.Text = "Analyzation completed";
            TextBlockTotalCount.Text = "Total: " + _totalFilesCount;
            StackPanelRunningStatus.Visibility = Visibility.Collapsed;
        }
        #endregion

        #region BackgroundWorker - File IO
        void TransferBgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Transfer();
        }

        void TransferBgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressBarRun.Value = e.ProgressPercentage;

            PackageInfo pkgInfo = e.UserState as PackageInfo;
            TextBlockStatus.Text = (_isMove ? "Moving " : "Copying ") + pkgInfo.OriginalFile.Substring(pkgInfo.OriginalFile.LastIndexOf('\\') + 1);
        }

        void TransferBgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            TextBlockStatus.Text = (_isMove ? "Move" : "Copy") + " completed";
            //StackPanelRunningStatus.Visibility = Visibility.Collapsed;
        }
        #endregion

        private void Analyze()
        {
            _analyzed = false;
            string[] ipaFiles = Directory.GetFiles(_srcDir, "*.ipa", SearchOption.AllDirectories);

            _totalFilesCount = ipaFiles.Count();
            int counter = 1;
            
            foreach (string file in ipaFiles) {
                try {
                    PackageInfo pkgInfo = _pkgService.GetPackageInfo(file);
                    _analyzeBgWorker.ReportProgress((counter++ * 100 ) / _totalFilesCount, pkgInfo);
                }
                catch (Exception e) {
                    //
                }
            }

            _analyzed = true;
        }

        private void Transfer()
        {
            int counter = 1;
            foreach (PackageInfo pkgInfo in ListViewOutput.Items) {
                string folder = _dstDir + "\\" + pkgInfo.AppType;
                string dstFilePath = folder + "\\" + pkgInfo.RecommendedFileName;

                _transferBgWorker.ReportProgress((counter++ * 100) / _totalFilesCount, pkgInfo);
                if (File.Exists(dstFilePath)) {
                    continue;
                }

                if (!Directory.Exists(folder)) {
                    System.IO.Directory.CreateDirectory(folder);
                }

                if (_isMove) {
                    File.Move(pkgInfo.OriginalFile, _dstDir + "\\" + pkgInfo.AppType + "\\" + pkgInfo.RecommendedFileName);
                }
                else {
                    File.Copy(pkgInfo.OriginalFile, _dstDir + "\\" + pkgInfo.AppType + "\\" + pkgInfo.RecommendedFileName, true);
                }
            }
        }

        #region Window Control Events
        private bool ValidateInputs()
        {
            _srcDir = TextBoxSrcDir.Text;
            _dstDir = TextBoxDstDir.Text;

            if (!Directory.Exists(_srcDir)) {
                MessageBox.Show("Invalid Source Directory", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TextBoxSrcDir.SelectAll();
                TextBoxSrcDir.Focus();
                return false;
            }

            if (string.IsNullOrEmpty(_dstDir)) {
                MessageBox.Show("Please specify Destiantion Directory", "Info", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                TextBoxDstDir.Focus();
                return false;
            }


            return true;
        }

        private void ButtonAnalyze_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInputs()) {
                ResetUI();

                ListViewOutput.Items.Clear();
                TextBlockTotalCount.Text = "";

                _analyzeBgWorker = new BackgroundWorker();
                _analyzeBgWorker.DoWork += new DoWorkEventHandler(AnalyzeBgWorker_DoWork);
                _analyzeBgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(AnalyzeBgWorker_RunWorkerCompleted);
                _analyzeBgWorker.ProgressChanged += new ProgressChangedEventHandler(AnalyzeBgWorker_ProgressChanged);
                _analyzeBgWorker.WorkerReportsProgress = true;
                _analyzeBgWorker.RunWorkerAsync();
            }
        }

        private void StartTransferBgWorker()
        {
            if (_analyzed) {
                MessageBoxResult result;
                if (!Directory.Exists(_dstDir)) {
                     result = MessageBox.Show("Destination directory does not exist, create it?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.No) {
                        TextBoxDstDir.SelectAll();
                        TextBoxDstDir.Focus();
                        return;
                    }
                }

                result = MessageBox.Show("Are you sure you want to " + 
                    (_isMove ? "move" : "copy") + 
                    " the file to the Destination Directory with the recommended file name?",
                    "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes) {
                    if (!Directory.Exists(_dstDir)) {
                        System.IO.Directory.CreateDirectory(_dstDir);
                    }

                    ResetUI();

                    _transferBgWorker = new BackgroundWorker();
                    _transferBgWorker.DoWork += new DoWorkEventHandler(TransferBgWorker_DoWork);
                    _transferBgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(TransferBgWorker_RunWorkerCompleted);
                    _transferBgWorker.ProgressChanged += new ProgressChangedEventHandler(TransferBgWorker_ProgressChanged);
                    _transferBgWorker.WorkerReportsProgress = true;
                    _transferBgWorker.RunWorkerAsync();
                }
            }
        }

        private void ButtonMove_Click(object sender, RoutedEventArgs e)
        {
            _isMove = true;
            StartTransferBgWorker();
        }

        private void ButtonCopy_Click(object sender, RoutedEventArgs e)
        {
            _isMove = false;
            StartTransferBgWorker();
        }

        private void PrepareTransfer(bool isMove)
        {
            if (_analyzed) {
                MessageBoxResult result = MessageBox.Show("Are you sure you want to move the file to the Destination Directory with the recommended file name?",
                    "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes) {
                    if (!Directory.Exists(_dstDir)) {
                        System.IO.Directory.CreateDirectory(_dstDir);
                    }

                    foreach (PackageInfo pkgInfo in ListViewOutput.Items) {
                        File.Move(pkgInfo.OriginalFile, _dstDir + "\\" + pkgInfo.AppType + "\\" + pkgInfo.RecommendedFileName);
                    }
                }
            }
        }
        
        private void ChangeSourceButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = TextBoxSrcDir.Text;
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK) {
                TextBoxSrcDir.Text = dialog.SelectedPath;
            }
        }

        private void ChangeDestinationButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = TextBoxDstDir.Text;
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK) {
                TextBoxDstDir.Text = dialog.SelectedPath;
            }
        }

        //private void ListViewOutput_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        //{
        //    ListViewItem item = sender as ListViewItem;
        //    PackageInfo pkgInfo = (PackageInfo)item.Content;
        //    MessageBox.Show(pkgInfo.ToString());
        //}
        #endregion
    }
}
