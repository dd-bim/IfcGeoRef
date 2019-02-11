using System.Collections.Generic;
using System.Windows;

namespace IfcGeoRefChecker
{
    /// <summary>
    /// Interaction logic for Compare.xaml
    /// </summary>
    public partial class Compare : Window
    {
        //private Dictionary<string, IfcStore> modelList;
        private Appl.GeoRefComparer comparison;

        private string direc;

        private Dictionary<string, string> jsonDict = new Dictionary<string, string>();

        public Compare(string direc, Dictionary<string, string> jsonDict)
        {
            this.direc = direc;
            this.jsonDict = jsonDict;

            InitializeComponent();

            foreach(var js in jsonDict.Keys)
            {
                //var splits = js.Split('\\');
                //var name = splits[splits.Length - 1];

                cb_compRef.Items.Add(js);
            }
        }

        private void cb_compRef_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            list_compModels.Items.Clear();

            foreach(string file in this.jsonDict.Keys)
            {
                list_compModels.Items.Add(file);
            }
            list_compModels.Items.Remove(cb_compRef.SelectedItem.ToString());
        }

        private void bt_compare_Click(object sender, RoutedEventArgs e)
        {
            var refName = cb_compRef.SelectedItem.ToString();

            this.jsonDict.TryGetValue(refName, out var refModel);

            var refJson = new KeyValuePair<string, string>(refName, refModel);

            var compList = new Dictionary<string, string>();

            foreach(var item in list_compModels.SelectedItems)
            {
                this.jsonDict.TryGetValue(item.ToString(), out var compModel);

                compList.Add(item.ToString(), compModel);
            }

            this.comparison = new Appl.GeoRefComparer(this.direc, refJson, compList);
            comparison.CompareIFC();

            bt_compLog.IsEnabled = true;
        }

        private void checkAll_Checked(object sender, RoutedEventArgs e)
        {
            list_compModels.SelectAll();
            uncheckAll.IsChecked = false;
        }

        private void uncheckAll_Checked(object sender, RoutedEventArgs e)
        {
            list_compModels.UnselectAll();
            checkAll.IsChecked = false;
        }

        private void bt_compLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.comparison.ShowCompareLog();
            }
            catch
            {
                MessageBox.Show("Error occured. Please check directory of your IFC-file for the corresponding Comparison log file.");
            }
        }

        private void bt_close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}