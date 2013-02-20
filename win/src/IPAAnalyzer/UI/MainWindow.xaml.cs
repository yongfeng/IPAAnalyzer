using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using IPAAnalyzer.Domain;
using IPAAnalyzer.Service;
using IPAAnalyzer.Util;

namespace IPAAnalyzer.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private const string INI_SECTION_DIR = "dir";
        private const string INI_DIR_KEY_SOURCE = "source_dir";
        private const string INI_DIR_KEY_DESTIANTION = "destination_dir";

        #region Binding Properties
        private int _analyzedCount = 0;
        public int AnalyzedCount
        {
            get
            {
                return _analyzedCount;
            }
            set
            {
                _analyzedCount = value;
                NotifyPropertyChanged("AnalyzedCount");
            }
        }

        private int _transferedCount = 0;
        public int TransferedCount
        {
            get
            {
                return _transferedCount;
            }
            set
            {
                _transferedCount = value;
                NotifyPropertyChanged("TransferedCount");
            }
        }

        private int _totalFilesCount = 0;
        public int TotalFilesCount
        {
            get
            {
                return _totalFilesCount;
            }
            set
            {
                _totalFilesCount = value;
                NotifyPropertyChanged("TotalFilesCount");
            }
        }

        private bool _analyzed = false;
        public bool Analyzed
        {
            get
            {
                return _analyzed && _totalFilesCount > 0;
            }
            set
            {
                _analyzed = value;
                NotifyPropertyChanged("Analyzed");
            }
        }

        private string _srcDir;
        public string SourceDir
        {
            get
            {
                return _srcDir;
            }
            set
            {
                _srcDir = value;
                _iniFile.Write(INI_DIR_KEY_DESTIANTION, INI_DIR_KEY_SOURCE, value);
                NotifyPropertyChanged("SourceDir");
            }
        }

        private string _dstDir;
        public string DestinationDir
        {
            get
            {
                return _dstDir;
            }
            set
            {
                _dstDir = value;
                _iniFile.Write(INI_DIR_KEY_DESTIANTION, INI_DIR_KEY_DESTIANTION, value);
                NotifyPropertyChanged("DestinationDir");
            }
        }
        #endregion

        private bool _stopAnalyzing = false;
        private bool _isAnalyzing = false;


        private BackgroundWorker _analyzeBgWorker = null;
        private BackgroundWorker _transferBgWorker = null;

        private bool _isMove = false;

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) {
                PropertyChanged(this,
                  new PropertyChangedEventArgs(propertyName));
            }
        }

        private IniFile _iniFile = new IniFile(@".\IPAAnalyzer.ini");
        public MainWindow()
        {
            InitializeComponent();
            ButtonAnalyze.Focus();

            StackPanelProgress.Visibility = Visibility.Hidden;
            ListViewOutput.DataContext = new PackageInfoViewModel(ListViewOutput);

            string srcDir = _iniFile.Read(INI_DIR_KEY_DESTIANTION, INI_DIR_KEY_SOURCE);
            string dstDir = _iniFile.Read(INI_DIR_KEY_DESTIANTION, INI_DIR_KEY_DESTIANTION);
            if (!string.IsNullOrEmpty(srcDir)) {
                SourceDir = srcDir;
            }

            if (!string.IsNullOrEmpty(dstDir)) {
                DestinationDir = dstDir;
            }
        }

        private void ResetUI()
        {
            TextBlockStatus.Text = "Start processing...";
            StackPanelProgress.Visibility = Visibility.Visible;
            ProgressBarRun.Value = 0;
        }

        #region BackgroundWorker - Analyze
        void AnalyzeBgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Analyze();
        }

        void AnalyzeBgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressBarRun.Value = e.ProgressPercentage;

            if (e.UserState != null) {
                Type objType = e.UserState.GetType();
                if (objType == typeof(string)) {
                    string file = e.UserState as string;
                    TextBlockStatus.Text = "Analyzing " + file.Substring(file.LastIndexOf('\\') + 1) + "...";
                }
                else if (objType == typeof(PackageInfo)) {
                    PackageInfo pkgInfo = e.UserState as PackageInfo;
                    TextBlockStatus.Text = "Analyzed " + pkgInfo.OriginalFile.Substring(pkgInfo.OriginalFile.LastIndexOf('\\') + 1);
                    ListViewOutput.Items.Insert(0, pkgInfo);
                    AnalyzedCount++;
                }
            }
        }

        void AnalyzeBgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            TextBlockStatus.Text = "Analyzation completed";
            StackPanelProgress.Visibility = Visibility.Hidden;

            //this.Cursor = System.Windows.Input.Cursors.Arrow;
            _isAnalyzing = false;
            _stopAnalyzing = false;
            ButtonAnalyze.Content = "Analyze";
            ListViewOutput.Focus();

            TextBlockGlobalStatus.Text = "Analyzed";
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
            TransferedCount++;
        }

        void TransferBgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            TextBlockStatus.Text = (_isMove ? "Move" : "Copy") + " completed";
            StackPanelProgress.Visibility = Visibility.Hidden;
        }
        #endregion

        private void Analyze()
        {
            AnalyzedCount = 0;
            Analyzed = false;
            string[] ipaFiles = Directory.GetFiles(SourceDir, "*.ipa", SearchOption.AllDirectories);

            TotalFilesCount = ipaFiles.Count();
            int counter = 0;

            foreach (string file in ipaFiles) {
                if (_stopAnalyzing) {
                    _stopAnalyzing = false;
                    break;
                }
                try {
                    _analyzeBgWorker.ReportProgress((counter++ * 100) / TotalFilesCount, file);
                    PackageInfo pkgInfo = PackageService.Instance.GetPackageInfo(file);
                    _analyzeBgWorker.ReportProgress((counter * 100) / TotalFilesCount, pkgInfo);
                }
                catch (Exception e) {
                    // TODO: add error into the list, and show
                }
            }

            Analyzed = true;
        }

        private void Transfer()
        {
            TransferedCount = 0;
            int counter = 1;
            foreach (PackageInfo pkgInfo in ListViewOutput.Items) {
                string folder = DestinationDir + "\\" + pkgInfo.AppType;
                string dstFilePath = folder + "\\" + pkgInfo.RecommendedFileName;

                _transferBgWorker.ReportProgress((counter++ * 100) / TotalFilesCount, pkgInfo);
                if (File.Exists(dstFilePath)) {
                    continue;
                }

                if (!Directory.Exists(folder)) {
                    System.IO.Directory.CreateDirectory(folder);
                }

                if (_isMove) {
                    File.Move(pkgInfo.OriginalFile, DestinationDir + "\\" + pkgInfo.AppType + "\\" + pkgInfo.RecommendedFileName);
                }
                else {
                    File.Copy(pkgInfo.OriginalFile, DestinationDir + "\\" + pkgInfo.AppType + "\\" + pkgInfo.RecommendedFileName, true);
                }
            }
        }

        #region Window Control Events
        private bool ValidateInputs()
        {
            if (!Directory.Exists(SourceDir)) {
                MessageBox.Show("Invalid Source Directory", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TextBoxSrcDir.SelectAll();
                TextBoxSrcDir.Focus();
                return false;
            }



            return true;
        }

        private void ButtonAnalyze_Click(object sender, RoutedEventArgs e)
        {

            if (ValidateInputs()) {
                ResetUI();
                if (!_isAnalyzing) {
                    TextBlockGlobalStatus.Text = "Analyzing";

                    _isAnalyzing = true;
                    ButtonAnalyze.Content = "Stop";

                    ListViewOutput.Items.Clear();
                    //this.Cursor = System.Windows.Input.Cursors.Wait;

                    _analyzeBgWorker = new BackgroundWorker();
                    _analyzeBgWorker.DoWork += new DoWorkEventHandler(AnalyzeBgWorker_DoWork);
                    _analyzeBgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(AnalyzeBgWorker_RunWorkerCompleted);
                    _analyzeBgWorker.ProgressChanged += new ProgressChangedEventHandler(AnalyzeBgWorker_ProgressChanged);
                    _analyzeBgWorker.WorkerReportsProgress = true;
                    _analyzeBgWorker.RunWorkerAsync();
                }
                else {
                    _stopAnalyzing = true;
                    TextBlockStatus.Text = "Stopping";
                    ButtonAnalyze.Content = "Stopping";

                    TextBlockGlobalStatus.Text = "Stopping";
                }
            }
        }

        private void StartTransferBgWorker()
        {
            if (_analyzed) {
                MessageBoxResult result;

                if (string.IsNullOrEmpty(DestinationDir)) {
                    MessageBox.Show("Please specify Destiantion Directory", "Info", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    TextBoxDstDir.Focus();
                    return;
                }

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

                    ProgressBarRun.Value = 0;
                    StackPanelProgress.Visibility = Visibility.Visible;

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
            dialog.SelectedPath = SourceDir;
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK) {
                SourceDir = dialog.SelectedPath;
            }
        }

        private void ChangeDestinationButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = DestinationDir;
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK) {
                DestinationDir = dialog.SelectedPath;
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
