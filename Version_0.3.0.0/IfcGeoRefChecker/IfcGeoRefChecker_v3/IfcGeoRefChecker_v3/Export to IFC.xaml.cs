using System.IO;
using System.Windows;

namespace IfcGeoRefChecker
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Export2IFC : Window
    {
        private string file;
        private string jsonObj;

        public Export2IFC(string file)
        {
            InitializeComponent();

            this.file = file;
            this.jsonObj = File.ReadAllText(file + "_map.json");
        }

        private void edit_manually_Click(object sender, RoutedEventArgs e)
        {
            var ifcResults = new Results(this.file, this.jsonObj);
            ifcResults.Show();
        }

        private void final_export_Click(object sender, RoutedEventArgs e)
        {
            var write = new IO.IfcWriter(this.file, this.jsonObj);

            if(radio_50.IsChecked == true)
            {
                // do something here
            }
            else if(radio_40.IsChecked == true)
            {
                // do something here
            }
            else if(radio_30.IsChecked == true)
            {
                // do something here
            }
            else if(radio_mix.IsChecked == true)
            {
                // do something here
            }
        }
    }
}