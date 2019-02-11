using System.IO;
using System.Linq;
using System.Windows;
using Newtonsoft.Json;

namespace IfcGeoRefChecker
{
    /// <summary>
    /// Interaction logic for UpdateMan.xaml
    /// </summary>
    public partial class UpdateMan : Window
    {
        private IO.JsonOutput jsonMap;
        private string direc;

        public UpdateMan(string direc)
        {
            this.direc = direc;

            var path = direc + ".json";

            var jsonObj = File.ReadAllText(path);

            this.jsonMap = new IO.JsonOutput();
            jsonMap.PopulateJson(jsonObj);

            InitializeComponent();
        }

        private void bt_updateJsonMan_Click(object sender, RoutedEventArgs e)
        {
            var convHelper = new Appl.Calc();

            var lev10Bldg = (from l10Bldg in jsonMap.LoGeoRef10
                             where l10Bldg.Reference_Object[1].Equals("IfcBuilding")
                             select l10Bldg).Single();

            lev10Bldg.AddressLines[0] = tb_adr0.Text;
            lev10Bldg.AddressLines[1] = tb_adr1.Text;
            lev10Bldg.AddressLines[2] = tb_adr2.Text;
            lev10Bldg.Country = tb_country.Text;
            lev10Bldg.Region = tb_region.Text;
            lev10Bldg.Postalcode = tb_plz.Text;
            lev10Bldg.Town = tb_town.Text;

            //------------------------------------------

            var lev20site = (from l20site in jsonMap.LoGeoRef20
                             where l20site.Reference_Object[1].Equals("IfcSite")
                             select l20site).Single();

            lev20site.Latitude = convHelper.ParseDouble(tb_lat.Text);
            lev20site.Longitude = convHelper.ParseDouble(tb_lon.Text);
            lev20site.Elevation = convHelper.ParseDouble(tb_elev.Text);

            //------------------------------------------
            var lev50proj = (from l50 in jsonMap.LoGeoRef50
                             where l50.Reference_Object[1].Equals("IfcProject")
                             select l50).Single();

            lev50proj.Translation_Eastings = convHelper.ParseDouble(tb_eastings50.Text);
            lev50proj.Translation_Northings = convHelper.ParseDouble(tb_northings50.Text);
            lev50proj.Translation_Orth_Height = convHelper.ParseDouble(tb_height50.Text);
            lev50proj.Scale = convHelper.ParseDouble(tb_scale50.Text);

            lev50proj.CRS_Name = tb_CRSname50.Text;

            var rot50 = convHelper.ParseDouble(tb_rotation50.Text);
            var vector = convHelper.GetVector3DForXAxis(rot50);

            lev50proj.RotationXY[0] = vector.X;
            lev50proj.RotationXY[1] = vector.Y;

            var jsonUpdMan = JsonConvert.SerializeObject(this.jsonMap, Formatting.Indented);

            var write = new IO.JsonOutput();
            write.WriteJSONfile(jsonUpdMan, this.direc + "_map");

            this.Close();
        }

        private void bt_quit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}