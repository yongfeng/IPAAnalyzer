using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IPAAnalyzer.Domain;

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

        public ICommand ShowPackageInfo
        {
            get { return new DelegateCommand(this.OnShowPackageInfo); }
        }

        private void OnShowPackageInfo()
        {
            if (_selectedPackageInfo != null) {
                MessageBox.Show(_selectedPackageInfo.ToString());
            }
        }

        public ICommand MoveDown
        {
            get { return new DelegateCommand(this.OnMoveDown); }
        }

        private void OnMoveDown()
        {
            int selectedIndex = _listView.SelectedIndex;
            if (selectedIndex + 1 < _listView.Items.Count) {
                _listView.SelectedIndex = selectedIndex + 1;
                EndMove();
            }
        }

        public ICommand MoveUp
        {
            get { return new DelegateCommand(this.OnMoveUp); }
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

        public ICommand ShowItunesDetail
        {
            get { return new DelegateCommand(this.OnShowItunesDetail); }
        }

        private void OnShowItunesDetail()
        {
            if (_selectedPackageInfo != null && _selectedPackageInfo.ItunesId > 0) {
                ItunesAppInfo itunesAppInfo = _selectedPackageInfo.FetchOnlineItunesDetails();
                ItunesAppDetailWindow detailWindow = new ItunesAppDetailWindow(itunesAppInfo);
                detailWindow.ShowDialog();
            }
        }
    }
}
