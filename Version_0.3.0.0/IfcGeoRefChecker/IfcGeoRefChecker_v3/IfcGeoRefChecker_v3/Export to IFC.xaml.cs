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
            var jsonMap = new IO.JsonOutput();
            jsonMap.PopulateJson(jsonObj);

            var lev50map = (from l50 in jsonMap.LoGeoRef50
                            select l50).First();

            if(radio_50.IsChecked == true)
            {
                var lev50oth = from l50 in jsonMap.LoGeoRef50
                               where l50 != lev50map
                               select l50;

                foreach(var l50 in lev50oth)
                {
                    l50.Translation_Eastings = lev50map.Translation_Eastings;
                    l50.Translation_Northings = lev50map.Translation_Northings;

                    l50.RotationXY = lev50map.RotationXY;
                    l50.CRS_Name = lev50map.CRS_Name;
                }
            }
            else if(radio_40.IsChecked == true)
            {
                foreach(var l40 in jsonMap.LoGeoRef40)
                {
                    l40.ProjectLocation[0] = ConvertUnit(lev50map.Translation_Eastings, jsonMap.LengthUnit);
                    l40.ProjectLocation[1] = ConvertUnit(lev50map.Translation_Northings, jsonMap.LengthUnit);

                    l40.TrueNorthXY[0] = lev50map.RotationXY[1];
                    l40.TrueNorthXY[1] = lev50map.RotationXY[0];
                }

                jsonMap.LoGeoRef50 = null;
            }
            else if(radio_30.IsChecked == true)
            {
                foreach(var l30 in jsonMap.LoGeoRef30)
                {
                    l30.ObjectLocationXYZ[0] += ConvertUnit(lev50map.Translation_Eastings, jsonMap.LengthUnit);
                    l30.ObjectLocationXYZ[1] += ConvertUnit(lev50map.Translation_Northings, jsonMap.LengthUnit);

                    l30.ObjectRotationX[0] = lev50map.RotationXY[0];
                    l30.ObjectRotationX[1] = lev50map.RotationXY[1];
                }

                jsonMap.LoGeoRef50 = null;
            }
            else if(radio_mix.IsChecked == true)
            {
                foreach(var l30 in jsonMap.LoGeoRef30)
                {
                    l30.ObjectLocationXYZ[0] += ConvertUnit(lev50map.Translation_Eastings, jsonMap.LengthUnit);
                    l30.ObjectLocationXYZ[1] += ConvertUnit(lev50map.Translation_Northings, jsonMap.LengthUnit);
                }

                foreach(var l40 in jsonMap.LoGeoRef40)
                {
                    l40.TrueNorthXY[0] = lev50map.RotationXY[1];
                    l40.TrueNorthXY[1] = lev50map.RotationXY[0];
                }

                jsonMap.LoGeoRef50 = null;
            }

            if(check_10.IsChecked == true)
            {
                var lev10Bldg = (from l10Bldg in jsonMap.LoGeoRef10
                                 where l10Bldg.Reference_Object[1].Equals("IfcBuilding")
                                 select l10Bldg).Single();

                var lev10Site = (from l10Site in jsonMap.LoGeoRef10
                                 where l10Site.Reference_Object[1].Equals("IfcSite")
                                 select l10Site).Single();

                lev10Site.AddressLines = lev10Bldg.AddressLines;
                lev10Site.Postalcode = lev10Bldg.Postalcode;
                lev10Site.Town = lev10Bldg.Town;
                lev10Site.Region = lev10Bldg.Region;
                lev10Site.Country = lev10Bldg.Country;
            }

            if(check_height.IsChecked == true)
            {
                var lev20Site = (from l20Site in jsonMap.LoGeoRef20
                                 where l20Site.Instance_Object[1].Equals("IfcSite")
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

            var jsonUpd = JsonConvert.SerializeObject(jsonMap, Formatting.Indented);
            var write = new IO.IfcWriter(this.file, jsonUpd);
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
    }
}