using System.IO;
using System.Linq;
using System.Windows;
using Newtonsoft.Json;

namespace IfcGeoRefChecker
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Export2IFC : Window
    {
        private string filePath;
        private string fileName;
        private string direc;

        private IO.JsonOutput jsonMap;

        public Export2IFC(string direc, string filePath, string fileName)
        {
            this.direc = direc;
            this.filePath = filePath;
            this.fileName = fileName;

            var jsonObj = File.ReadAllText(direc + "\\IfcGeoRefChecker\\buildingLocator\\json\\" + fileName + "_map.json");

            this.jsonMap = new IO.JsonOutput();
            jsonMap.PopulateJson(jsonObj);

            InitializeComponent();

            lb_jsonmap.Content = fileName + "_map.json";
        }

        private void getJsonContent()
        {
            var lev50map = (from l50 in jsonMap.LoGeoRef50
                            select l50).First();

            var lev50oth = from l50 in jsonMap.LoGeoRef50
                           where l50 != lev50map
                           select l50;

            foreach(var l50 in lev50oth)
            {
                if(radio_50.IsChecked == true)
                {
                    l50.CRS_Name = lev50map.CRS_Name;
                }
            }

            if(radio_50.IsChecked == true)
            {
                foreach(var l50 in lev50oth)
                {
                    l50.Translation_Eastings = lev50map.Translation_Eastings;
                    l50.Translation_Northings = lev50map.Translation_Northings;

                    l50.RotationXY = lev50map.RotationXY;
                }
            }
            else if(radio_40.IsChecked == true)
            {
                foreach(var l40 in jsonMap.LoGeoRef40)
                {
                    l40.ProjectLocation[0] = ConvertUnit((double)lev50map.Translation_Eastings, jsonMap.LengthUnit);
                    l40.ProjectLocation[1] = ConvertUnit((double)lev50map.Translation_Northings, jsonMap.LengthUnit);

                    l40.TrueNorthXY[0] = lev50map.RotationXY[1];
                    l40.TrueNorthXY[1] = lev50map.RotationXY[0];
                }

                lev50map.Translation_Eastings = 0;
                lev50map.Translation_Northings = 0;
            }
            else if(radio_30.IsChecked == true)
            {
                foreach(var l30 in jsonMap.LoGeoRef30)
                {
                    l30.ObjectLocationXYZ[0] += ConvertUnit((double)lev50map.Translation_Eastings, jsonMap.LengthUnit);
                    l30.ObjectLocationXYZ[1] += ConvertUnit((double)lev50map.Translation_Northings, jsonMap.LengthUnit);

                    l30.ObjectRotationX[0] = lev50map.RotationXY[0];
                    l30.ObjectRotationX[1] = lev50map.RotationXY[1];
                }

                lev50map.Translation_Eastings = 0;
                lev50map.Translation_Northings = 0;

            }
            else if(radio_mix.IsChecked == true)
            {
                foreach(var l30 in jsonMap.LoGeoRef30)
                {
                    l30.ObjectLocationXYZ[0] += ConvertUnit((double)lev50map.Translation_Eastings, jsonMap.LengthUnit);
                    l30.ObjectLocationXYZ[1] += ConvertUnit((double)lev50map.Translation_Northings, jsonMap.LengthUnit);
                }

                foreach(var l40 in jsonMap.LoGeoRef40)
                {
                    l40.TrueNorthXY[0] = lev50map.RotationXY[1];
                    l40.TrueNorthXY[1] = lev50map.RotationXY[0];
                }

                lev50map.Translation_Eastings = 0;
                lev50map.Translation_Northings = 0;

            }

            if(check_height.IsChecked == true)
            {
                var lev20Site = (from l20Site in jsonMap.LoGeoRef20
                                 where l20Site.Reference_Object[1].Equals("IfcSite")
                                 select l20Site).Single();

                var elev = tb_height.Text.Replace(",", ".");
                elev = elev.Replace(" ", "");

                if(double.TryParse(elev, out var res))
                {
                    lev20Site.Elevation = ConvertUnit(res, jsonMap.LengthUnit);

                    if(radio_50.IsChecked == true)
                    {
                        foreach(var l50 in jsonMap.LoGeoRef50)
                        {
                            l50.Translation_Orth_Height = res;
                            l50.CRS_Vertical_Datum = tb_height_datum.Text;
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Error occured. Please provide a number for the absolute height!");
                }
            }
        }

        private void edit_manually_Click(object sender, RoutedEventArgs e)
        {
            getJsonContent();

            var jsonUpd = JsonConvert.SerializeObject(jsonMap, Formatting.Indented);

            var ifcResults = new Results(direc, filePath, this.fileName, jsonUpd);
            ifcResults.Show();
        }

        private void final_export_Click(object sender, RoutedEventArgs e)
        {
            getJsonContent();

            var jsonUpd = JsonConvert.SerializeObject(jsonMap, Formatting.Indented);

            var write = new IO.IfcWriter(direc + "\\ifc\\", filePath, fileName, jsonUpd);
        }

        private void check_height_Checked(object sender, RoutedEventArgs e)
        {
            tb_height.IsEnabled = true;
            tb_height_datum.IsEnabled = true;
        }

        private double ConvertUnit(double coordinate, string unit)
        {
            switch(unit)
            {
                case "m":
                    break;

                case "mm":
                    coordinate *= 1000;
                    break;

                case "ft":
                    coordinate /= 0.3048;
                    break;

                case "in":
                    coordinate *= 39.3701;
                    break;

                default:
                    break;
            }

            return coordinate;
        }

        private void check_10_Checked(object sender, RoutedEventArgs e)
        {
            var lev10Site = (from l10Site in jsonMap.LoGeoRef10
                             where l10Site.Reference_Object[1].Equals("IfcSite")
                             select l10Site).Single();
            var lev10Bldg = (from l10Bldg in jsonMap.LoGeoRef10
                             where l10Bldg.Reference_Object[1].Equals("IfcBuilding")
                             select l10Bldg).Single();

            lev10Site.AddressLines[0] = lev10Bldg.AddressLines[0];
            lev10Site.AddressLines[1] = lev10Bldg.AddressLines[1];
            lev10Site.AddressLines[2] = lev10Bldg.AddressLines[2];
            lev10Site.Postalcode = lev10Bldg.Postalcode;
            lev10Site.Town = lev10Bldg.Town;
            lev10Site.Region = lev10Bldg.Region;
            lev10Site.Country = lev10Bldg.Country;
        }

        private void check_10_Unchecked(object sender, RoutedEventArgs e)
        {
            var lev10Site = (from l10Site in jsonMap.LoGeoRef10
                             where l10Site.Reference_Object[1].Equals("IfcSite")
                             select l10Site).Single();

            lev10Site.AddressLines[0] = null;
            lev10Site.AddressLines[1] = null;
            lev10Site.AddressLines[2] = null;
            lev10Site.Postalcode = null;
            lev10Site.Town = null;
            lev10Site.Region = null;
            lev10Site.Country = null;
        }

        private void radio_map_Checked(object sender, RoutedEventArgs e)
        {
        }
    }
}