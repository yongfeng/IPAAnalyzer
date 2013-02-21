using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using IPAAnalyzer.Domain;
using System.Reflection;

namespace IPAAnalyzer.UI
{
    /// <summary>
    /// Interaction logic for PackageInfoDetailWindow.xaml
    /// </summary>
    public partial class PackageInfoDetailWindow : Window
    {
        public PackageInfoDetailWindow(PackageInfo packageInfo)
        {
            InitializeComponent();

            List<DataVO> dataList = new List<DataVO>();
            foreach (PropertyInfo propety in typeof(PackageInfo).GetProperties()) {
                object obj = propety.GetValue(packageInfo, null);
                string value = obj == null ? string.Empty : obj.ToString();
                dataList.Add(new DataVO
                {
                    Key = propety.Name,
                    Value = value
                });
            };

            TextBlockTitle.Text = packageInfo.RecommendedFileName;
            ListViewOutput.ItemsSource = dataList;

            ListViewOutput.Focus();
        }

        private void OnCloseCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class DataVO
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
