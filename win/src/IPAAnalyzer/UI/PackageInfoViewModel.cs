using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IPAAnalyzer.Domain;
using IPAAnalyzer.Service;
using System.Collections;
using System.Collections.Generic;

namespace IPAAnalyzer.UI
{
    public class PackageInfoViewModel : INotifyPropertyChanged
    {
        private ListView _listView;
        public PackageInfoViewModel(ListView listView)
        {
            _listView = listView;
        }

        private PackageInfo _selectedPackageInfo;
        public PackageInfo SelectedPackageInfo
        {
            get { return _selectedPackageInfo; }
            set
            {
                if (value != _selectedPackageInfo) {
                    _selectedPackageInfo = value;
                    this.OnPropertyChanged("SelectedPackageInfo");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null) {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }

        public ICommand DeleteIPAFile
        {
            get { return new DelegateCommand(this.OnDeleteIPAFile); }
        }

        public ICommand ShowItunesDetail
        {
            get { return new DelegateCommand(this.OnShowItunesDetail); }
        }

        public ICommand ShowPackageInfo
        {
            get { return new DelegateCommand(this.OnShowPackageInfo); }
        }

        public ICommand MoveUp
        {
            get { return new DelegateCommand(this.OnMoveUp); }
        }

        public ICommand MoveDown
        {
            get { return new DelegateCommand(this.OnMoveDown); }
        }

        private void OnDeleteIPAFile()
        {
            if (_listView.SelectedItems != null && _listView.SelectedItems.Count > 0) {
                MessageBoxResult result;
                if (_listView.SelectedItems.Count == 1) {
                    result = MessageBox.Show(
                        "Are sure you want to delete the file: " + _selectedPackageInfo.OriginalFile + "?",
                        "Delete File Confirmation",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);
                }
                else {
                    result = MessageBox.Show(
                        "Are sure you want to delete all selected files?",
                        "Delete File Confirmation",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);
                }

                if (result == MessageBoxResult.Yes) {
                    int currentIndex = _listView.SelectedIndex;

                    IList<PackageInfo> list = new List<PackageInfo>();
                    foreach (PackageInfo pkgInfo in _listView.SelectedItems) {
                        list.Add(pkgInfo);
                    }
                    foreach (PackageInfo item in list) {
                        System.IO.File.Delete(item.OriginalFile);
                        _listView.Items.Remove(item);
                    }

                    if (_listView.Items.Count > 0) {
                        if (currentIndex < _listView.Items.Count) {
                            _listView.SelectedIndex = currentIndex;
                            EndMove();
                        }
                        else if (currentIndex == _listView.Items.Count) {
                            _listView.SelectedIndex = currentIndex - 1;
                            EndMove();
                        }
                    }
                }
            }
        }

        private void OnMoveDown()
        {
            int selectedIndex = _listView.SelectedIndex;
            if (selectedIndex + 1 < _listView.Items.Count) {
                _listView.SelectedIndex = selectedIndex + 1;
                EndMove();
            }
        }


        private void OnMoveUp()
        {
            int selectedIndex = _listView.SelectedIndex;

            if (selectedIndex > 0) {
                _listView.SelectedIndex = selectedIndex - 1;
                EndMove();
            }
        }

        private void EndMove()
        {
            ListViewItem item;
            item = _listView.ItemContainerGenerator.ContainerFromIndex(_listView.SelectedIndex) as ListViewItem;
            item.Focus();
            _listView.ScrollIntoView(_selectedPackageInfo);
        }


        private void OnShowPackageInfo()
        {
            if (_selectedPackageInfo != null) {
                //MessageBox.Show(_selectedPackageInfo.ToString());
                PackageInfoDetailWindow detailWindow = new PackageInfoDetailWindow(_selectedPackageInfo);
                detailWindow.Owner = App.Current.MainWindow;
                detailWindow.ShowDialog();
            }
        }

        internal void OnShowItunesDetail()
        {
            if (_selectedPackageInfo != null) {
                if (_selectedPackageInfo.ItunesId > 0) {
                    App.Current.MainWindow.Cursor = System.Windows.Input.Cursors.Wait;
                    ItunesAppInfo itunesAppInfo = PackageService.Instance.FetchOnlineItunesDetails(_selectedPackageInfo.ItunesId);
                    App.Current.MainWindow.Cursor = System.Windows.Input.Cursors.Arrow;
                    if (itunesAppInfo != null) {
                        ItunesAppDetailWindow detailWindow = new ItunesAppDetailWindow(itunesAppInfo);
                        detailWindow.Owner = App.Current.MainWindow;
                        detailWindow.ShowDialog();
                    }
                    else {
                        MessageBox.Show("Cannot fetch the details from iTunes server (iTunes ID: " + _selectedPackageInfo.ItunesId + ")",
                             _selectedPackageInfo.GetFormattedName(), MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else {
                    MessageBox.Show("No iTunes ID available for this package for fetching details from iTunes server",
                             _selectedPackageInfo.GetFormattedName(), MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
    }
}
