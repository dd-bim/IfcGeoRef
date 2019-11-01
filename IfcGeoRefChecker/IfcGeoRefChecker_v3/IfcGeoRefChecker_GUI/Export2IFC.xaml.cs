using System;
using System.Collections.Generic;
using System.IO;
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
using IfcGeoRefChecker.IO;
using Newtonsoft.Json;

namespace IfcGeoRefChecker_GUI
{
    /// <summary>
    /// Interaktionslogik für Export2IFC.xaml
    /// </summary>
    public partial class Export2IFC : Window
    {
        private string filePath;
        private string fileName;

        private GeoRefChecker jsonMap;

        public Export2IFC(string filePath, string fileName)
        {
            //this.direc = direc;
            this.filePath = filePath;
            this.fileName = fileName;

            var fd = new Microsoft.Win32.OpenFileDialog();

            fd.Filter = "json files (*.json)|*.json";
            fd.Multiselect = true;

            fd.ShowDialog();

            var jsonPath = fd.FileName;
            var jsonName = fd.SafeFileName;

            var jsonObj = File.ReadAllText(jsonPath);

            //var jsonObj = File.ReadAllText(direc + "\\IfcGeoRefChecker\\buildingLocator\\json\\update.json");

            this.jsonMap = new GeoRefChecker(jsonObj);

            InitializeComponent();

            lb_jsonmap.Text = fileName;
            lb_jsonmap_json.Text = jsonName;
        }

        private void final_export_Click(object sender, RoutedEventArgs e)
        {
            getJsonContent();

            var jsonUpd = JsonConvert.SerializeObject(jsonMap, Formatting.Indented);

            var write = new IfcWriter(filePath, fileName, jsonUpd);
        }

        private void getJsonContent()       //Auslesen der update-JSON in Anbhängigkeit der gewählten Export-Funktion
        {
            var lev50map = (from l50 in jsonMap.LoGeoRef50          //Lesen des ersten GeoRef50-Eintrages (dieser enthält die neue Georef aus dem BuildingLocator)
                            select l50).First();

            //var lev50oth = from l50 in jsonMap.LoGeoRef50           //opt.: falls weitere Georef50-Objekte vorhanden sind, werden diese hier gelesen      --> DEPRECATED
            //               where l50 != lev50map
            //               select l50;

            //foreach(var l50 in lev50oth)
            //{
            //    if(radio_50.IsChecked == true)
            //    {
            //        l50.CRS_Name = lev50map.CRS_Name;
            //    }
            //}                                                         //DEPRECATED, da projekt nur noch eine Georef50-Instanz enthalten darf

            //if(radio_50.IsChecked == true)
            //{
            //    foreach(var l50 in lev50oth)
            //    {
            //        l50.Translation_Eastings = lev50map.Translation_Eastings;
            //        l50.Translation_Northings = lev50map.Translation_Northings;

            //        l50.RotationXY = lev50map.RotationXY;
            //    }
            //}
            //else                                                      //DEPRECATED, da projekt nur noch eine Georef50-Instanz enthalten darf --> radio_50 ist standard-export

            if (radio_40.IsChecked == true)                              //Option GeoRef projektbezogen speichern
            {
                foreach (var l40 in jsonMap.LoGeoRef40)
                {
                    l40.ProjectLocation[0] = ConvertUnit((double)lev50map.Translation_Eastings, jsonMap.LengthUnit);    //Umwandlung zu Projektlängeneinheit, wenn nötig
                    l40.ProjectLocation[1] = ConvertUnit((double)lev50map.Translation_Northings, jsonMap.LengthUnit);

                    //l40.TrueNorthXY[0] = lev50map.RotationXY[1];      //Rotation on projected CRS is not equal to True North
                    //l40.TrueNorthXY[1] = lev50map.RotationXY[0];
                }

                lev50map.GeoRef50 = false;
                lev50map.Translation_Eastings = 0;
                lev50map.Translation_Northings = 0;
                lev50map.RotationXY[0] = 0;
                lev50map.RotationXY[1] = 0;
            }

            //else if (radio_30.IsChecked == true)                         //Option GeoRef baustellenbezogen speichern    // Exportoption für LoGeoRef30 entfernt
            //{
            //    foreach (var l30 in jsonMap.LoGeoRef30)
            //    {
            //        l30.ObjectLocationXYZ[0] += ConvertUnit((double)lev50map.Translation_Eastings, jsonMap.LengthUnit); //Addition zur eventuellen vorhandenen Projektkoordinate,
            //        l30.ObjectLocationXYZ[1] += ConvertUnit((double)lev50map.Translation_Northings, jsonMap.LengthUnit); //damit keine projektinterne Verschiebung stattfindet

            //        l30.ObjectRotationX[0] = lev50map.RotationXY[0];
            //        l30.ObjectRotationX[1] = lev50map.RotationXY[1];
            //    }

            //    lev50map.GeoRef50 = false;
            //    lev50map.Translation_Eastings = 0;
            //    lev50map.Translation_Northings = 0;
            //    lev50map.RotationXY[0] = 0;
            //    lev50map.RotationXY[1] = 0;
            //}
            //else if (radio_mix.IsChecked == true)                    //Option GeoRef-Location baustellenbezogen und Rotation projektbezogen speichern (Revit-konform)
            //{
            //    foreach (var l30 in jsonMap.LoGeoRef30)
            //    {
            //        l30.ObjectLocationXYZ[0] += ConvertUnit((double)lev50map.Translation_Eastings, jsonMap.LengthUnit);
            //        l30.ObjectLocationXYZ[1] += ConvertUnit((double)lev50map.Translation_Northings, jsonMap.LengthUnit);
            //    }

            //    foreach (var l40 in jsonMap.LoGeoRef40)
            //    {
            //        l40.TrueNorthXY[0] = lev50map.RotationXY[1];
            //        l40.TrueNorthXY[1] = lev50map.RotationXY[0];
            //    }

            //    lev50map.GeoRef50 = false;
            //    lev50map.Translation_Eastings = 0;
            //    lev50map.Translation_Northings = 0;
            //    lev50map.RotationXY[0] = 0;
            //    lev50map.RotationXY[1] = 0;
            //}

            if (check_height.IsChecked == true)
            {
                var lev20Site = (from l20Site in jsonMap.LoGeoRef20
                                 where l20Site.Reference_Object[1].Equals("IfcSite")
                                 select l20Site).Single();

                var elev = tb_height.Text.Replace(",", ".");
                elev = elev.Replace(" ", "");

                if (double.TryParse(elev, out var res))
                {
                    lev20Site.Elevation = ConvertUnit(res, jsonMap.LengthUnit);

                    if (radio_50.IsChecked == true)
                    {
                        foreach (var l50 in jsonMap.LoGeoRef50)
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

        private double ConvertUnit(double coordinate, string unit)
        {
            switch (unit)
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

            lev10Site.AddressLines.Clear();

            foreach (var addLine in lev10Bldg.AddressLines)
            {
                lev10Site.AddressLines.Add(addLine);
            }

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

            lev10Site.AddressLines.Clear();
            //lev10Site.AddressLines[0] = null;
            //lev10Site.AddressLines[1] = null;
            //lev10Site.AddressLines[2] = null;
            lev10Site.Postalcode = null;
            lev10Site.Town = null;
            lev10Site.Region = null;
            lev10Site.Country = null;
        }

        private void edit_manually_Click(object sender, RoutedEventArgs e)
        {
            getJsonContent();

            var jsonUpd = JsonConvert.SerializeObject(jsonMap, Formatting.Indented);

            var ifcResults = new Results(this.filePath, this.fileName, jsonUpd);
            ifcResults.Show();
        }

        private void check_height_Checked(object sender, RoutedEventArgs e)
        {
            tb_height.IsEnabled = true;
            tb_height_datum.IsEnabled = true;
        }
    }
}
