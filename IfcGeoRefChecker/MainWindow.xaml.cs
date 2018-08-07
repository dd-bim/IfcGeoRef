using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Xbim.Ifc;

namespace IfcGeoRefChecker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dictionary<string, IfcStore> ModelList;

        public MainWindow()
        {
            InitializeComponent();

            bt_Show.IsEnabled = false;
        }

        private void BtInfo_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Noch hinzufügen");
        }

        private void Bt_Import(object sender, RoutedEventArgs e)
        {
            this.ModelList = new IO.IfcImport().ImportModels;

            foreach(string file in this.ModelList.Keys)
            {
                importFiles.Items.Add(file);
            }

            bt_Show.IsEnabled = true;
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void Bt_Show(object sender, RoutedEventArgs e)
        {
            if(importFiles.SelectedValue == null)
            {
                MessageBox.Show("Please select an imported IfcFile at first."); 
            }
            else
            {
                var showResults = new Results(this.ModelList[importFiles.SelectedValue.ToString()]);
                showResults.Show();
            }
        }
    }
}