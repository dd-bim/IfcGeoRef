using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Newtonsoft.Json;

namespace IfcGeoRefChecker
{
    /// <summary>
    /// Interaction logic for Results.xaml
    /// </summary>
    public partial class Results : Window
    {
        private string ifcVersion;

        private string fileName;
        private string ifcPath;

        //private IO.JsonOutput json = new IO.JsonOutput();
        private Appl.GeoRefChecker json;

        private Appl.Level10 lev10Bldg;
        private Appl.Level10 lev10Site;
        private Appl.Level20 lev20site;
        private Appl.Level30 lev30site;
        private Appl.Level40 lev40proj;
        private Appl.Level50 lev50proj;

        public Results(string ifcPath, string fileName, string jsonObj)
        {
            try
            {
                this.json = new Appl.GeoRefChecker(jsonObj);

                //json.PopulateJson(jsonObj);
                this.ifcVersion = json.IFCSchema;
                this.fileName = fileName;
                this.ifcPath = ifcPath;

                this.lev10Bldg = (from l10Bldg in json.LoGeoRef10
                                  where l10Bldg.Reference_Object[1].Equals("IfcBuilding")
                                  select l10Bldg).Single();

                this.lev10Site = (from l10 in json.LoGeoRef10
                                  where l10.Reference_Object[1].Equals("IfcSite")
                                  select l10).Single();

                InitializeComponent();

                lb_name.Text = fileName;
                lb_schema.Content = json.IFCSchema;
                lb_unit.Content = json.LengthUnit;

                //----------------------------------------------------------------------

                lb_entity_no_10.Content = lev10Bldg.Reference_Object[0];
                lb_entity_type_10.Content = lev10Bldg.Reference_Object[1];
                lb_entity_id_10.Content = lev10Bldg.Reference_Object[2];

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

                tb_country.Text = lev10Bldg.Country;
                tb_region.Text = lev10Bldg.Region;
                tb_plz.Text = lev10Bldg.Postalcode;
                tb_town.Text = lev10Bldg.Town;

                this.lev20site = (from l20site in json.LoGeoRef20
                                  where l20site.Reference_Object[1].Equals("IfcSite")
                                  select l20site).Single();

                lb_entity_no_20.Content = lev20site.Reference_Object[0];
                lb_entity_type_20.Content = lev20site.Reference_Object[1];
                lb_entity_id_20.Content = lev20site.Reference_Object[2];

                tb_lat.Text = lev20site.Latitude.ToString();
                tb_lon.Text = lev20site.Longitude.ToString();
                tb_elev.Text = lev20site.Elevation.ToString();

                this.lev30site = (from l30site in json.LoGeoRef30
                                  where l30site.Reference_Object[1].Equals("IfcSite")
                                  select l30site).Single();

                lb_entity_no_30.Content = lev30site.Reference_Object[0];
                lb_entity_type_30.Content = lev30site.Reference_Object[1];
                lb_entity_id_30.Content = lev30site.Reference_Object[2];

                tb_originX_30.Text = lev30site.ObjectLocationXYZ[0].ToString();
                tb_originY_30.Text = lev30site.ObjectLocationXYZ[1].ToString();
                tb_originZ_30.Text = lev30site.ObjectLocationXYZ[2].ToString();

                tb_rotationX_30.Text = lev30site.ObjectRotationX[0].ToString() + ", " + lev30site.ObjectRotationX[1].ToString() + ", " + lev30site.ObjectRotationX[2].ToString();
                tb_rotationZ_30.Text = lev30site.ObjectRotationZ[0].ToString() + ", " + lev30site.ObjectRotationZ[1].ToString() + ", " + lev30site.ObjectRotationZ[2].ToString();

                this.lev40proj = (from l40 in json.LoGeoRef40
                                  where l40.Reference_Object[1].Equals("IfcProject")
                                  select l40).Single();

                lb_entity_no_40.Content = lev40proj.Reference_Object[0];
                lb_entity_type_40.Content = lev40proj.Reference_Object[1];
                lb_entity_id_40.Content = lev40proj.Reference_Object[2];

                tb_originX_40.Text = lev40proj.ProjectLocation[0].ToString();
                tb_originY_40.Text = lev40proj.ProjectLocation[1].ToString();
                tb_originZ_40.Text = lev40proj.ProjectLocation[2].ToString();

                tb_rotationX_40.Text = lev40proj.ProjectRotationX[0].ToString() + ", " + lev40proj.ProjectRotationX[1].ToString() + ", " + lev40proj.ProjectRotationX[2].ToString();
                tb_rotationZ_40.Text = lev40proj.ProjectRotationZ[0].ToString() + ", " + lev40proj.ProjectRotationZ[1].ToString() + ", " + lev40proj.ProjectRotationZ[2].ToString();

                tb_rotationTN_40.Text = lev40proj.TrueNorthXY[0].ToString() + ", " + lev40proj.TrueNorthXY[1].ToString();

                this.lev50proj = (from l50 in json.LoGeoRef50
                                  where l50.Reference_Object[1].Equals("IfcProject")
                                  select l50).Single();

                lb_entity_no_50.Content = lev50proj.Reference_Object[0];
                lb_entity_type_50.Content = lev50proj.Reference_Object[1];
                lb_entity_id_50.Content = lev50proj.Reference_Object[2];

                tb_eastings50.Text = lev50proj.Translation_Eastings.ToString();
                tb_northings50.Text = lev50proj.Translation_Northings.ToString();
                tb_height50.Text = lev50proj.Translation_Orth_Height.ToString();
                tb_scale50.Text = lev50proj.Scale.ToString();

                tb_CRSname50.Text = lev50proj.CRS_Name;
                tb_CRSdesc50.Text = lev50proj.CRS_Description;
                tb_CRSgeod50.Text = lev50proj.CRS_Geodetic_Datum;
                tb_CRSvert50.Text = lev50proj.CRS_Vertical_Datum;
                tb_ProjName50.Text = lev50proj.CRS_Projection_Name;
                tb_ProjZone50.Text = lev50proj.CRS_Projection_Zone;

                tb_rotation50.Text = lev50proj.RotationXY[0].ToString() + ", " + lev50proj.RotationXY[1].ToString();
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error occured while initializing dialogue for IfcGeoRefUpdater. \r\n Error message: " + ex.Message);
            }
            //---------------------------------------------------------------------------------------------------------------------
        }

        // --------------------------------------------------------------------------------------------------------------------------------------------------------------

        // UPDATE user specific attribute values for each level and write them into the certain ifc-file
        // --------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void bt_updateIFCman_Click(object sender, RoutedEventArgs e)
        {
            var convHelper = new Appl.Calc();

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

            lev20site.Latitude = convHelper.ParseDouble(tb_lat.Text);
            lev20site.Longitude = convHelper.ParseDouble(tb_lon.Text);
            lev20site.Elevation = convHelper.ParseDouble(tb_elev.Text);

            //------------------------------------------

            lev30site.ObjectLocationXYZ[0] = convHelper.ParseDouble(tb_originX_30.Text);
            lev30site.ObjectLocationXYZ[1] = convHelper.ParseDouble(tb_originY_30.Text);
            lev30site.ObjectLocationXYZ[2] = convHelper.ParseDouble(tb_originZ_30.Text);

            lev30site.ObjectRotationX = convHelper.ParseDoubleVector(tb_rotationX_30.Text);
            lev30site.ObjectRotationZ = convHelper.ParseDoubleVector(tb_rotationZ_30.Text);

            //------------------------------------------

            lev40proj.ProjectLocation[0] = convHelper.ParseDouble(tb_originX_40.Text);
            lev40proj.ProjectLocation[1] = convHelper.ParseDouble(tb_originY_40.Text);
            lev40proj.ProjectLocation[2] = convHelper.ParseDouble(tb_originZ_40.Text);

            lev40proj.ProjectRotationX = convHelper.ParseDoubleVector(tb_rotationX_30.Text);
            lev40proj.ProjectRotationZ = convHelper.ParseDoubleVector(tb_rotationZ_30.Text);

            lev40proj.TrueNorthXY = convHelper.ParseDoubleVector(tb_rotationTN_40.Text);

            //------------------------------------------

            lev50proj.Translation_Eastings = convHelper.ParseDouble(tb_eastings50.Text);
            lev50proj.Translation_Northings = convHelper.ParseDouble(tb_northings50.Text);
            lev50proj.Translation_Orth_Height = convHelper.ParseDouble(tb_height50.Text);
            lev50proj.Scale = convHelper.ParseDouble(tb_scale50.Text);

            lev50proj.CRS_Name = tb_CRSname50.Text;
            lev50proj.CRS_Description = tb_CRSdesc50.Text;
            lev50proj.CRS_Geodetic_Datum = tb_CRSgeod50.Text;
            lev50proj.CRS_Vertical_Datum = tb_CRSvert50.Text;

            lev50proj.CRS_Projection_Name = tb_ProjName50.Text;
            lev50proj.CRS_Projection_Zone = tb_ProjZone50.Text;

            lev50proj.RotationXY = convHelper.ParseDoubleVector(tb_rotation50.Text);

            var jsonUpdMan = JsonConvert.SerializeObject(json, Formatting.Indented);

            var write = new IO.IfcWriter(ifcPath, fileName, jsonUpdMan);
        }

        private void check_10_Checked(object sender, RoutedEventArgs e)
        {
            if (lev10Bldg.AddressLines != null)
                lev10Site.AddressLines = new List<string>(lev10Bldg.AddressLines.Count);

            for(var i = 0; i < (lev10Bldg.AddressLines.Count-1); i++)
            {
                lev10Site.AddressLines.Add(lev10Bldg.AddressLines[i]);
            }

            lev10Site.Postalcode = lev10Bldg.Postalcode;
            lev10Site.Town = lev10Bldg.Town;
            lev10Site.Region = lev10Bldg.Region;
            lev10Site.Country = lev10Bldg.Country;
        }

        private void check_10_Unchecked(object sender, RoutedEventArgs e)
        {
            lev10Site.AddressLines = null;
            lev10Site.Postalcode = null;
            lev10Site.Town = null;
            lev10Site.Region = null;
            lev10Site.Country = null;
        }

        private void bt_quit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}