using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Serilog;

namespace IfcGeoRefChecker
{
    /// <summary>
    /// Interaction logic for UpdateMan.xaml
    /// </summary>
    public partial class UpdateMan : Window
    {
        private Appl.GeoRefChecker jsonMap;
        private string fileName;
        private string direc;

        public UpdateMan(Appl.GeoRefChecker jsonMap, string direc, string fileName)
        {
            this.direc = direc;
            this.fileName = fileName;
            this.jsonMap = jsonMap;

            //var path = direc + "check.json";

            //var jsonObj = File.ReadAllText(path);

            //this.jsonMap = new Appl.GeoRefChecker(jsonObj);

            InitializeComponent();

            GetCheckAttributes();
        }

        private void GetCheckAttributes()
        {
            try
            {
                var lev10Bldg = (from l10Bldg in jsonMap.LoGeoRef10
                                 where l10Bldg.Reference_Object[1].Equals("IfcBuilding")
                                 select l10Bldg).Single();

                if(lev10Bldg.AddressLines != null)
                {
                    var k = lev10Bldg.AddressLines.Count;

                    switch(k)
                    {
                        case 1:
                            tb_adr0.Text = lev10Bldg.AddressLines[0];
                            break;

                        case 2:
                            tb_adr1.Text = lev10Bldg.AddressLines[1];
                            goto case 1;

                        case 3:
                            tb_adr2.Text = lev10Bldg.AddressLines[2];
                            goto case 2;
                    }
                }

                tb_plz.Text = lev10Bldg.Postalcode;
                tb_town.Text = lev10Bldg.Town;
                tb_region.Text = lev10Bldg.Region;
                tb_country.Text = lev10Bldg.Country;


                //-------------

                var lev20site = (from l20site in jsonMap.LoGeoRef20
                                 where l20site.Reference_Object[1].Equals("IfcSite")
                                 select l20site).Single();

                tb_lat.Text = lev20site.Latitude.ToString();

                tb_lon.Text = lev20site.Longitude.ToString();

                tb_elev.Text = lev20site.Elevation.ToString();

                //------------------

                var lev50proj = (from l50 in jsonMap.LoGeoRef50
                                 where l50.Reference_Object[1].Equals("IfcProject")
                                 select l50).Single();

                tb_eastings50.Text = lev50proj.Translation_Eastings.ToString();
                tb_northings50.Text = lev50proj.Translation_Northings.ToString();
                tb_height50.Text = lev50proj.Translation_Orth_Height.ToString();

                tb_scale50.Text = lev50proj.Scale.ToString();

                tb_CRSname50.Text = lev50proj.CRS_Name;

                var convHelper = new Appl.Calc();

                var angle = convHelper.GetAngleBetweenForXAxis(new System.Windows.Media.Media3D.Vector3D(lev50proj.RotationXY[0], lev50proj.RotationXY[1], 0));

                tb_rotation50.Text = angle.ToString();

                Log.Information("GeoRefUpdater: Write attributes to window successful.");
            }
            catch(Exception ex)
            {
                Log.Error("GeoRefUpdater: Error occured while writing attributes to window. Error: " + ex.Message);
            }
        }

        private void bt_updateJsonMan_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var convHelper = new Appl.Calc();

                var lev10Bldg = (from l10Bldg in jsonMap.LoGeoRef10
                                 where l10Bldg.Reference_Object[1].Equals("IfcBuilding")
                                 select l10Bldg).Single();

                lev10Bldg.AddressLines.Clear();

                if(!tb_adr0.Text.Equals(""))
                    lev10Bldg.AddressLines.Add(tb_adr0.Text);

                if(!tb_adr1.Text.Equals(""))
                    lev10Bldg.AddressLines.Add(tb_adr1.Text);

                if(!tb_adr2.Text.Equals(""))
                    lev10Bldg.AddressLines.Add(tb_adr2.Text);

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

                if(!tb_rotation50.Text.Equals(""))
                {
                    var rot50 = convHelper.ParseDouble(tb_rotation50.Text);
                    var vector = convHelper.GetVector3DForXAxis(rot50);

                    lev50proj.RotationXY = new List<double>();

                    lev50proj.RotationXY = new List<double>();

                    lev50proj.RotationXY.Add(vector.X);
                    lev50proj.RotationXY.Add(vector.Y);
                }

                var write = new IO.JsonOutput();
                write.JsonOutputDialog(this.jsonMap, this.direc, this.fileName + "update");

                Log.Information("GeoRefUpdater: Write updates to update.json file was successful.");
            }
            catch(Exception ex)
            {
                Log.Error("GeoRefUpdater: Error occured while writing updates to update.json. Error: " + ex.Message);
            }

            this.Close();
        }

        private void bt_quit_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("GeoRefUpdater: Closed without Saving.");
            this.Close();
        }
    }
}