using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using IfcGeoRefChecker.Appl;
using Newtonsoft.Json;

namespace IfcGeoRefChecker_GUI
{
    /// <summary>
    /// Interaktionslogik für Compare.xaml
    /// </summary>
    public partial class Compare : Window
    {
        private GeoRefComparer comparison;

        private string direc;

        private Dictionary<string, string> jsonDict = new Dictionary<string, string>();

        public Compare(string direc, Dictionary<string, GeoRefChecker> checkDict)
        {
            this.direc = direc;

            InitializeComponent();

            foreach (var obj in checkDict)
            {
                cb_compRef.Items.Add(obj.Key);

                var jsonObj = JsonConvert.SerializeObject(obj.Value, Formatting.Indented);

                this.jsonDict.Add(obj.Key, jsonObj);
            }
        }

        private void cb_compRef_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            list_compModels.Items.Clear();

            foreach (string file in this.jsonDict.Keys)
            {
                list_compModels.Items.Add(file);
            }
            list_compModels.Items.Remove(cb_compRef.SelectedItem.ToString());
        }

        private void bt_compare_Click(object sender, RoutedEventArgs e)
        {
            if(cb_compRef.SelectedItem == null)
            {
                //Log.Information("Comparing IFC Files not possible. No Reference selected.");
                System.Windows.MessageBox.Show("Please select an IFC file for reference.");
            }
            else if(list_compModels.SelectedItems.Count == 0)
            {
                System.Windows.MessageBox.Show("Please select at least 1 IFC file to compare to your reference.");
            }
            else
            {
                var refName = cb_compRef.SelectedItem.ToString();

                this.jsonDict.TryGetValue(refName, out var refModel);

                var refJson = new KeyValuePair<string, string>(refName, refModel);

                var compList = new Dictionary<string, string>();

                foreach (var item in list_compModels.SelectedItems)
                {
                    this.jsonDict.TryGetValue(item.ToString(), out var compModel);

                    compList.Add(item.ToString(), compModel);
                }

                this.comparison = new GeoRefComparer(this.direc, refJson, compList);
                comparison.CompareIFC();

                bt_compLog.IsEnabled = true;
            }
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
    }
}
