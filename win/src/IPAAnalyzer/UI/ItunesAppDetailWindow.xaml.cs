using System.Windows;
using IPAAnalyzer.Domain;
using System.Windows.Input;
using System;
using System.Diagnostics;

namespace IPAAnalyzer.UI
{
    /// <summary>
    /// Interaction logic for ItunesAppDetailWindow.xaml
    /// </summary>
    public partial class ItunesAppDetailWindow : Window
    {
        private ItunesAppInfo _itunesAppInfo;
        public ItunesAppDetailWindow(ItunesAppInfo itunesAppInfo)
        {
            InitializeComponent();

            _itunesAppInfo = itunesAppInfo;

            if (itunesAppInfo != null && !string.IsNullOrEmpty(itunesAppInfo.trackName)) {
                Title += " - " + itunesAppInfo.trackName;
            }
            GridInfo.DataContext = itunesAppInfo;
        }

        private void OnCloseCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(_itunesAppInfo.trackViewUrl);
        }
    }
}
