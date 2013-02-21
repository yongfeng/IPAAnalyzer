using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using IPAAnalyzer.Domain;
using IPAAnalyzer.Service;
using IPAAnalyzer.Util;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace IPAAnalyzer.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private const string FILE_TRANSFER_LOG = @".\transfer.log";
        private const string FILE_ANALYZE_LOG = @".\analyze.log";
        private const string FILE_INI = @".\IPAAnalyzer.ini";

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

        private bool _isSrcDirRecursive = true;
        public bool IsSourceDirRecursive
        {
            get
            {
                return _isSrcDirRecursive;
            }
            set
            {
                _isSrcDirRecursive = value;
                NotifyPropertyChanged("IsSourceDirRecursive");
            }
        }

        private string _appVersion;
        public string AppVersion
        {
            get
            {
                return _appVersion;
            }
            set
            {
                _appVersion = value;
                NotifyPropertyChanged("AppVersion");
            }
        }
        #endregion

        private bool _stopAnalyzing = false;
        private bool _isAnalyzing = false;


        private BackgroundWorker _analyzeBgWorker = null;
        private BackgroundWorker _transferBgWorker = null;

        private bool _isMove = false;

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) {
                PropertyChanged(this,
                  new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        private IniFile _iniFile = new IniFile(FILE_INI);
        private PackageInfoViewModel _pkgInfoViewModel;
        public MainWindow()
        {
            InitializeComponent();

            // version
            var dllVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            AppVersion = string.Format("v{0}.{1} (r{2})", dllVersion.Major, dllVersion.Minor, dllVersion.Revision);

            ButtonAnalyze.Focus();

            StackPanelProgress.Visibility = Visibility.Hidden;
            _pkgInfoViewModel = new PackageInfoViewModel(ListViewOutput);
            ListViewOutput.DataContext = _pkgInfoViewModel;

            string srcDir = _iniFile.Read(INI_DIR_KEY_DESTIANTION, INI_DIR_KEY_SOURCE);
            string dstDir = _iniFile.Read(INI_DIR_KEY_DESTIANTION, INI_DIR_KEY_DESTIANTION);
            if (!string.IsNullOrEmpty(srcDir)) {
                SourceDir = srcDir;
            }

            if (!string.IsNullOrEmpty(dstDir)) {
                DestinationDir = dstDir;
            }


            // create cache dir if not available
            if (!Directory.Exists(PackageService.CACHE_DIR)) {
                Directory.CreateDirectory(PackageService.CACHE_DIR);
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
        }

        void TransferBgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            TextBlockStatus.Text = (_isMove ? "Move" : "Copy") + " completed";
            StackPanelProgress.Visibility = Visibility.Hidden;
        }
        #endregion

        private void Analyze()
        {
            SimpleFileLogger logger = GetAnalyzeLogger();

            AnalyzedCount = 0;
            Analyzed = false;
            string[] ipaFiles = Directory.GetFiles(SourceDir, "*.ipa", IsSourceDirRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            TotalFilesCount = ipaFiles.Count();
            int counter = 0;

            foreach (string file in ipaFiles) {
                if (_stopAnalyzing) {
                    _stopAnalyzing = false;
                    break;
                }

                PackageInfo pkgInfo = null;
                _analyzeBgWorker.ReportProgress((counter++ * 100) / TotalFilesCount, file);
                try {
                    pkgInfo = PackageService.Instance.GetPackageInfo(file);
                    logger.LogInfo("analyzed " + file + ", recommeded name: " + pkgInfo.RecommendedFileName);
                    _analyzeBgWorker.ReportProgress((counter * 100) / TotalFilesCount, pkgInfo);
                }
                catch (Exception e) {
                    logger.LogError("failed to analyze " + file + ". " + e);
                    pkgInfo = new PackageInfo
                    {
                        OriginalFile = file,
                        IsProcessed = false,
                        ProcessingRemarks = e.Message
                    };
                    _analyzeBgWorker.ReportProgress((counter * 100) / TotalFilesCount, pkgInfo);
                }
            }

            Analyzed = true;
        }

        private void Transfer()
        {
            SimpleFileLogger logger = GetTransferLogger();
            TransferedCount = 0;
            int counter = 1;
            foreach (PackageInfo pkgInfo in ListViewOutput.Items) {
                string folder = DestinationDir;
                if (!string.IsNullOrEmpty(pkgInfo.AppType)) {
                    folder += "\\" + pkgInfo.AppType;
                }
                string dstFilePath = folder + "\\" + pkgInfo.RecommendedFileName;

                _transferBgWorker.ReportProgress((counter++ * 100) / TotalFilesCount, pkgInfo);
                if (!File.Exists(pkgInfo.OriginalFile)) {
                    logger.LogInfo("SKIP original file does not exist: " + pkgInfo.OriginalFile);
                    continue;
                }

                if (File.Exists(dstFilePath)) {
                    logger.LogInfo("file already exists: " + dstFilePath);
                    continue;
                }

                if (!Directory.Exists(folder)) {
                    System.IO.Directory.CreateDirectory(folder);
                }

                if (_isMove) {
                    try {
                        File.Move(pkgInfo.OriginalFile, dstFilePath);
                        TransferedCount++;
                        logger.LogInfo("moved " + pkgInfo.OriginalFile + " to " + dstFilePath);
                    }
                    catch (Exception e) {
                        logger.LogError("Failed to move " + pkgInfo.OriginalFile + " to " + dstFilePath + ". " + e.Message + " " + e.StackTrace);
                    }
                }
                else {
                    try {
                        File.Copy(pkgInfo.OriginalFile, dstFilePath, true);
                        TransferedCount++;
                        logger.LogInfo("copied " + pkgInfo.OriginalFile + " to " + dstFilePath);
                    }
                    catch (Exception e) {
                        logger.LogError("Failed to copy " + pkgInfo.OriginalFile + " to " + dstFilePath + ". " + e.Message + " " + e.StackTrace);

                    }
                }
            }
        }

        #region Logger
        private SimpleFileLogger GetTransferLogger()
        {
            return GetLogger(FILE_TRANSFER_LOG);
        }

        private SimpleFileLogger GetAnalyzeLogger()
        {
            return GetLogger(FILE_ANALYZE_LOG);
        }

        private SimpleFileLogger GetLogger(string logFilename)
        {
            string backupFilename = logFilename + ".old";
            if (File.Exists(logFilename)) {
                if (File.Exists(backupFilename)) {
                    File.Delete(backupFilename);
                }
                File.Move(logFilename, backupFilename);
            }
            return new SimpleFileLogger(logFilename);
        }
        #endregion

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

                // TODO: verify the Logical Drives
                if (!Directory.Exists(_dstDir)) {
                    result = MessageBox.Show("Destination directory does not exist, create it?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.No) {
                        TextBoxDstDir.SelectAll();
                        TextBoxDstDir.Focus();
                        return;
                    }
                    else {
                        try {
                            System.IO.Directory.CreateDirectory(_dstDir);
                        }
                        catch (Exception e) {
                            MessageBox.Show("Cannot create Destination Directory: " + _dstDir, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                }

                result = MessageBox.Show("Are you sure you want to " +
                    (_isMove ? "move" : "copy") +
                    " the file to the Destination Directory with the recommended file name?",
                    "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes) {
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

        private void ListViewOutput_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _pkgInfoViewModel.OnShowItunesDetail();
        }

        private GridViewColumn _lastColumnSorted;
        private void ListViewOutput_OnColumnHeaderClick(object sender, RoutedEventArgs e)
        {
            GridViewColumn column = ((GridViewColumnHeader)e.OriginalSource).Column;
            if (_lastColumnSorted != null) {
                _lastColumnSorted.HeaderTemplate = null;
            }
            SortDescriptionCollection sorts = ListViewOutput.Items.SortDescriptions;
            RenderSort(sorts, column, GetSortDirection(sorts, column));
        }

        private ListSortDirection GetSortDirection(SortDescriptionCollection sorts, GridViewColumn column)
        {
            if (column == _lastColumnSorted && sorts[0].Direction == ListSortDirection.Ascending) {
                return ListSortDirection.Descending;
            }
            return ListSortDirection.Ascending;
        }

        private void RenderSort(SortDescriptionCollection sorts, GridViewColumn column, ListSortDirection direction)
        {
            column.HeaderTemplate = (DataTemplate)ListViewOutput.FindResource("HeaderTemplate" + direction);

            Binding columnBinding = column.DisplayMemberBinding as Binding;
            if (columnBinding != null) {
                sorts.Clear();
                sorts.Add(new SortDescription(columnBinding.Path.Path, direction));
                _lastColumnSorted = column;
            }
        }
        #endregion

        

    }

    public class ProcessStatusImageConverter : IValueConverter
    {
        private BitmapImage _yes = new BitmapImage(new Uri(@"pack://application:,,,/Images/tick.png", UriKind.RelativeOrAbsolute));
        private BitmapImage _no = new BitmapImage(new Uri(@"pack://application:,,,/Images/cross.png", UriKind.RelativeOrAbsolute));

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool) {
                bool isProcessed = (bool)value;
                return isProcessed ? _yes : _no;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    };


    public class IconImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null && value is string) {
                string iconFile = string.Format(@"{0}/{1}.png", PackageService.CACHE_DIR, value);
                if (File.Exists(iconFile)) {
                    BitmapImage image = new BitmapImage();

                    try {
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                        image.UriSource = new Uri(iconFile, UriKind.RelativeOrAbsolute);
                        image.EndInit();
                    }
                    catch {
                        return DependencyProperty.UnsetValue;
                    }

                    return image;
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    };
}
