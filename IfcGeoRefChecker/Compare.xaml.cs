using System.Collections.Generic;
using System.Windows;
using Xbim.Ifc;

namespace IfcGeoRefChecker
{
    /// <summary>
    /// Interaction logic for Compare.xaml
    /// </summary>
    public partial class Compare : Window
    {
        private Dictionary<string, IfcStore> modelList;

        public Compare(Dictionary<string, IfcStore> modelList)
        {
            this.modelList = modelList;

            InitializeComponent();

            foreach(string file in modelList.Keys)
            {
                cb_compRef.Items.Add(file);
            }
        }

        private void bt_close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void cb_compRef_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            list_compModels.Items.Clear();

            foreach(string file in modelList.Keys)
            {
                list_compModels.Items.Add(file);
            }
            list_compModels.Items.Remove(cb_compRef.SelectedItem.ToString());
        }

        private void bt_compare_Click(object sender, RoutedEventArgs e)
        {
            var refFile = cb_compRef.SelectedItem.ToString();
            IfcStore refModel;
            modelList.TryGetValue(refFile, out refModel);

            var compList = new List<IfcStore>();

            foreach(var item in list_compModels.SelectedItems)
            {
                IfcStore compModel;
                modelList.TryGetValue(item.ToString(), out compModel);

                compList.Add(compModel);
            }

            var comparison = new Appl.GeoRefComparer(refModel, compList);
            comparison.CompareIFC();
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