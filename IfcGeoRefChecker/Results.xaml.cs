using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xbim.Ifc;

namespace IfcGeoRefChecker
{
    /// <summary>
    /// Interaction logic for Results.xaml
    /// </summary>
    public partial class Results : Window
    {
        private IfcStore model;
        private Vector dirTN;
        private Vector dirMap;
        private Vector3D dirX;
        private Vector3D dirZ;
        private Vector3D dirX30;
        private Vector3D dirZ30;

        private double lat;
        private double lon;
        private double angleMap;
        private double angleTN;
        private double angleX;
        private double angleZ;
        private double angleX30;
        private double angleZ30;
        private string unit;
        private List<double> xyz30 = new List<double>();
        private List<double> xyz40 = new List<double>();

        private Dictionary<string, double> unitElev = new Dictionary<string, double>();
        private Dictionary<string, double> unitX30 = new Dictionary<string, double>();
        private Dictionary<string, double> unitY30 = new Dictionary<string, double>();
        private Dictionary<string, double> unitZ30 = new Dictionary<string, double>();
        private Dictionary<string, double> unitX40 = new Dictionary<string, double>();
        private Dictionary<string, double> unitY40 = new Dictionary<string, double>();
        private Dictionary<string, double> unitZ40 = new Dictionary<string, double>();

        private Appl.Level10 geoRef10;
        private Appl.Level20 geoRef20;
        private Appl.Level30 geoRef30;
        private Appl.Level40 geoRef40;
        private Appl.Level50 geoRef50;

        public Results(IfcStore model)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            this.model = model;

            InitializeComponent();

            //handle project units and unit views
            //----------------------------------------------------------------------

            //read the length unit of the selected ifc-file
            this.unit = new Appl.UnitReader().GetProjectLengthUnit(model);

            // possible unit views for Level20
            cb_UnitGeographicCoord20.Items.Add("[dd]");
            cb_UnitGeographicCoord20.Items.Add("[dms]");

            //add possible length units to the combobox

            var lengthUnits = new List<string>
            {
                {"mm" },
                {"cm" },
                {"dm" },
                {"m" },
                {"ft" },
                {"in" },
            };

            foreach(var unit in lengthUnits)
            {
                cb_UnitElevation20.Items.Add(unit);
                cb_Origin30.Items.Add(unit);
                cb_Origin40.Items.Add(unit);
            }

            // possible unit views for rotation in Level 30 / 40 /50

            var rotUnits = new List<string>
            {
                {"vect" },
                {"deg" },
            };

            foreach(var unit in rotUnits)
            {
                cb_Rotation30.Items.Add(unit);
                cb_Rotation40.Items.Add(unit);
                cb_TrueNorth40.Items.Add(unit);
                cb_Rotation50.Items.Add(unit);
            }

            //set default to "vect" because this view will be passed by XBim
            cb_Rotation30.SelectedItem = "vect";
            cb_Rotation40.SelectedItem = "vect";
            cb_TrueNorth40.SelectedItem = "vect";
            cb_Rotation50.SelectedItem = "vect";

            //----------------------------------------------------------------------

            //set textboxes readonly und set background color when window is initialized
            //--------------------------------------------------------------------------

            foreach(TabItem ti in prog.Items)
            {
                Grid gr1 = (Grid)ti.Content;

                foreach(GroupBox group in gr1.Children)
                {
                    Grid gr2 = (Grid)group.Content;

                    if(gr2 is Grid)
                    {
                        foreach(Control tb in gr2.Children)
                        {
                            if(tb is TextBox)
                            {
                                (tb as TextBox).IsReadOnly = true;
                                (tb as TextBox).Background = Brushes.WhiteSmoke;
                            }
                        }
                    }
                }
            }
            //--------------------------------------------------------------------------

            //read needed objects from ifc-file for level information
            //---------------------------------------------------------------------------------------------------------------------

            var siteReading = new Appl.SiteReader(model);       //for Level 10 and 20
            var bldgReading = new Appl.BldgReader(model);       //for Level 10
            var prodReading = new Appl.UpperPlcmReader(model);  //for Level 30
            var ctxReading = new Appl.ContextReader(model);     //for Level 40 and 50

            //fill lists for level10 and level20 with IfcSite-objects (ifc-hash + type)

            for(int i = 0; i < siteReading.SiteList.Count; i++)
            {
                string listbox = "#" + siteReading.SiteList[i].GetHashCode() + "=" + siteReading.SiteList[i].GetType().Name;
                SpatialElements10.Items.Add(listbox);
                SiteElements20.Items.Add(listbox);
            }

            //fill list for level10 with IfcBuilding-objects (ifc-hash + type)

            for(int i = 0; i < bldgReading.BldgList.Count; i++)
            {
                string listbox = "#" + bldgReading.BldgList[i].GetHashCode() + "=" + bldgReading.BldgList[i].GetType().Name;
                SpatialElements10.Items.Add(listbox);
            }

            //fill list for level30 with objects where IfcLocalPlacement is not relative to an other one (ifc-hash + type)

            for(int i = 0; i < prodReading.ProdList.Count; i++)
            {
                string listbox = "#" + prodReading.ProdList[i].GetHashCode() + "=" + prodReading.ProdList[i].GetType().Name;
                PlacementElements30.Items.Add(listbox);
            }

            //fill list for level40 and 50 with IfcGeometricRepresentationContext-objects (ifc-hash + type)

            for(int i = 0; i < ctxReading.CtxList.Count; i++)
            {
                string listbox = "#" + ctxReading.CtxList[i].GetHashCode() + "=" + ctxReading.CtxList[i].GetType().Name;
                PlacementElements40.Items.Add(listbox);
                MapElements50.Items.Add(listbox);
            }

            //---------------------------------------------------------------------------------------------------------------------
        }

        // GET all attribute values for each level adn write them into the textboxes and labels
        // --------------------------------------------------------------------------------------------------------------------------------------------------------------

        // TabItem Level 10: fill textboxes with attribute values of the actual selected site or building
        //------------------------------------------------------------------------------------------------

        public void SpatialElements10_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // read ifc-hash and type for passing to Level10-class

            string ref10 = SpatialElements10.SelectedItem.ToString();
            int index10 = ref10.IndexOf("=");
            string sub10 = ref10.Substring(1, index10 - 1);
            string type10 = ref10.Substring(index10 + 1);

            // get values for specific element

            this.geoRef10 = new Appl.Level10(model, sub10, type10);
            geoRef10.GetLevel10();

            // fill label and textboxes with certain values

            lb_Instance10.Content = geoRef10.Instance_Object[0] + "=" + geoRef10.Instance_Object[1];
            tb_adr0.Text = geoRef10.AddressLines[0];
            tb_adr1.Text = geoRef10.AddressLines[1];
            tb_adr2.Text = geoRef10.AddressLines[2];
            tb_plz.Text = geoRef10.Postalcode;
            tb_town.Text = geoRef10.Town;
            tb_region.Text = geoRef10.Region;
            tb_country.Text = geoRef10.Country;
        }

        //------------------------------------------------------------------------------------------------

        // TabItem Level 20: fill textboxes with attribute values of the actual selected site
        //------------------------------------------------------------------------------------------------

        private void SiteElements20_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // read ifc-hash for passing to Level20-class

            string ref20 = SiteElements20.SelectedItem.ToString();
            int index20 = ref20.IndexOf("=");
            string sub20 = ref20.Substring(1, index20 - 1);

            // get values for specific element
            this.geoRef20 = new Appl.Level20(model, sub20);
            geoRef20.GetLevel20();

            // fill variables with certain values

            this.lat = geoRef20.Latitude;
            this.lon = geoRef20.Longitude;

            //set default to "dd" because this view will be passed by XBim
            cb_UnitGeographicCoord20.SelectedItem = "[dd]";

            // calculate unit views for results window
            this.unitElev = new Appl.Calc().ConvertLengthUnits(this.unit, geoRef20.Elevation);

            //set combobox unit to the readed unit
            cb_UnitElevation20.SelectedItem = this.unit;

            tb_elev.Text = changeLengthUnit(unitElev, this.unit);
        }

        //------------------------------------------------------------------------------------------------

        // TabItem Level 30: fill textboxes with attribute values of the actual selected element
        //------------------------------------------------------------------------------------------------

        public void PlacementElements30_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // read ifc-hash and type for passing to Level30-class

            string ref30 = PlacementElements30.SelectedItem.ToString();
            int index30 = ref30.IndexOf("=");
            string sub30 = ref30.Substring(1, index30 - 1);
            string type30 = ref30.Substring(index30 + 1);

            // get values for specific element
            this.geoRef30 = new Appl.Level30(model, sub30, type30);
            geoRef30.GetLevel30();

            cb_Rotation30.SelectedItem = "vect";

            // fill label and textboxes with certain values

            lb_Instance30.Content = geoRef30.Instance_Object[0] + "=" + geoRef30.Instance_Object[1];

            // check if necessary !!!

            //this.xyz30.Add(geoRef30.ObjectLocationXYZ[0]);
            //this.xyz30.Add(geoRef30.ObjectLocationXYZ[1]);
            //this.xyz30.Add(geoRef30.ObjectLocationXYZ[2]);

            this.dirX30.X = geoRef30.ObjectRotationX[0];
            this.dirX30.Y = geoRef30.ObjectRotationX[1];
            this.dirX30.Z = geoRef30.ObjectRotationX[2];

            this.dirZ30.X = geoRef30.ObjectRotationZ[0];
            this.dirZ30.Y = geoRef30.ObjectRotationZ[1];
            this.dirZ30.Z = geoRef30.ObjectRotationZ[2];

            //--------------------

            // calculate unit views for results window
            this.unitX30 = new Appl.Calc().ConvertLengthUnits(this.unit, geoRef30.ObjectLocationXYZ[0]);
            this.unitY30 = new Appl.Calc().ConvertLengthUnits(this.unit, geoRef30.ObjectLocationXYZ[1]);
            this.unitZ30 = new Appl.Calc().ConvertLengthUnits(this.unit, geoRef30.ObjectLocationXYZ[2]);

            // when initialized set unit selection to project unit
            cb_Origin30.SelectedItem = this.unit;

            tb_originX_30.Text = changeLengthUnit(unitX30, this.unit);
            tb_originY_30.Text = changeLengthUnit(unitY30, this.unit);
            tb_originZ_30.Text = changeLengthUnit(unitZ30, this.unit);

            //tb_originX_30.Text = this.xyz30[0].ToString();
            //tb_originY_30.Text = this.xyz30[1].ToString();
            //tb_originZ_30.Text = this.xyz30[2].ToString();

            tb_rotationX_30.Text = this.dirX30.X + ", " + this.dirX30.Y + ", " + this.dirX30.Z;
            tb_rotationZ_30.Text = this.dirZ30.X + ", " + this.dirZ30.Y + ", " + this.dirZ30.Z;
        }

        //------------------------------------------------------------------------------------------------

        // TabItem Level 40: fill textboxes with attribute values of the actual selected geometric representation context
        //------------------------------------------------------------------------------------------------
        private void PlacementElements40_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // read ifc-hash for passing to Level40-class

            string ref40 = PlacementElements40.SelectedItem.ToString();
            int index40 = ref40.IndexOf("=");
            string sub40 = ref40.Substring(1, index40 - 1);

            // get values for specific element
            this.geoRef40 = new Appl.Level40(model, sub40);
            geoRef40.GetLevel40();

            // fill labels and textboxes with certain values

            lb_Instance_WCS.Content = geoRef40.Instance_Object_WCS[0] + "=" + geoRef40.Instance_Object_WCS[1];
            lb_Instance_TN.Content = geoRef40.Instance_Object_North[0] + "=" + geoRef40.Instance_Object_North[1];

            cb_Rotation40.SelectedItem = "vect";
            cb_TrueNorth40.SelectedItem = "vect";

            // check if necessary !!!

            //this.xyz40.Add(geoRef40.ProjectLocation[0]);
            //this.xyz40.Add(geoRef40.ProjectLocation[1]);
            //this.xyz40.Add(geoRef40.ProjectLocation[2]);

            this.dirZ.X = geoRef40.ProjectRotationZ[0];
            this.dirZ.Y = geoRef40.ProjectRotationZ[1];
            this.dirZ.Z = geoRef40.ProjectRotationZ[2];

            this.dirX.X = geoRef40.ProjectRotationX[0];
            this.dirX.Y = geoRef40.ProjectRotationX[1];
            this.dirX.Z = geoRef40.ProjectRotationX[2];

            this.dirTN.X = geoRef40.TrueNorthXY[0];
            this.dirTN.Y = geoRef40.TrueNorthXY[1];

            //--------------------

            // calculate unit views for results window
            this.unitX40 = new Appl.Calc().ConvertLengthUnits(this.unit, geoRef40.ProjectLocation[0]);
            this.unitY40 = new Appl.Calc().ConvertLengthUnits(this.unit, geoRef40.ProjectLocation[1]);
            this.unitZ40 = new Appl.Calc().ConvertLengthUnits(this.unit, geoRef40.ProjectLocation[2]);

            // when initialized set unit selection to project unit
            cb_Origin40.SelectedItem = this.unit;

            tb_originX_40.Text = changeLengthUnit(unitX40, this.unit);
            tb_originY_40.Text = changeLengthUnit(unitY40, this.unit);
            tb_originZ_40.Text = changeLengthUnit(unitZ40, this.unit);

            //tb_originX_40.Text = this.xyz40[0].ToString();
            //tb_originY_40.Text = this.xyz40[1].ToString();
            //tb_originZ_40.Text = this.xyz40[2].ToString();

            tb_rotationX_40.Text = this.dirX.X + ", " + this.dirX.Y + ", " + this.dirX.Z;
            tb_rotationZ_40.Text = this.dirZ.X + ", " + this.dirZ.Y + ", " + this.dirZ.Z;

            tb_rotationTN_40.Text = this.dirTN.X + ", " + this.dirTN.Y;
        }

        //------------------------------------------------------------------------------------------------

        // TabItem Level 50: fill textboxes with attribute values of the actual selected geometric representation context
        //
        private void MapElements50_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // if schema version is not IFC4 than there is no Level50 to view / edit

            if(model.IfcSchemaVersion.ToString().Equals("Ifc2X3"))
            {
                tab_50.IsEnabled = false;
            }
            else
            {
                // read ifc-hash for passing to Level50-class
                string ref50 = MapElements50.SelectedItem.ToString();
                int index50 = ref50.IndexOf("=");
                string sub50 = ref50.Substring(1, index50 - 1);

                // get values for specific element
                this.geoRef50 = new Appl.Level50(model, sub50);
                geoRef50.GetLevel50();

                cb_Rotation50.SelectedItem = "vect";

                // fill labels and textboxes with certain values

                // check if necessary !!!
                this.dirMap.X = geoRef50.RotationXY[0];
                this.dirMap.Y = geoRef50.RotationXY[1];
                // --------------------------

                lb_Reference50.Content = geoRef50.Reference_Object[0] + "=" + geoRef50.Reference_Object[1];
                lb_InstanceCRS50.Content = geoRef50.Instance_Object_CRS[0] + "=" + geoRef50.Instance_Object_CRS[1];

                tb_rotation50.Text = this.dirMap.X + ", " + this.dirMap.Y;

                tb_CRSname50.Text = geoRef50.CRS_Name;
                tb_CRSdesc50.Text = geoRef50.CRS_Description;
                tb_CRSgeod50.Text = geoRef50.CRS_Geodetic_Datum;
                tb_CRSvert50.Text = geoRef50.CRS_Vertical_Datum;
                tb_ProjName50.Text = geoRef50.CRS_Projection_Name;
                tb_ProjZone50.Text = geoRef50.CRS_Projection_Zone;

                // internal commitment: if double values are -999999 there is no value given by the ifc-file (see Level50-class)

                tb_eastings50.Text = (geoRef50.Translation_Eastings.Equals(-999999) == true) ? "n/a" : geoRef50.Translation_Eastings.ToString();
                tb_northings50.Text = (geoRef50.Translation_Northings.Equals(-999999) == true) ? "n/a" : geoRef50.Translation_Northings.ToString();
                tb_height50.Text = (geoRef50.Translation_Orth_Height.Equals(-999999) == true) ? "n/a" : geoRef50.Translation_Orth_Height.ToString();
                tb_scale50.Text = (geoRef50.Scale.Equals(-999999) == true) ? "n/a" : geoRef50.Scale.ToString();
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------
        // --------------------------------------------------------------------------------------------------------------------------------------------------------------

        // Change UNIT of the written attribute values if possible (length units or rotation units)
        // --------------------------------------------------------------------------------------------------------------------------------------------------------------

        //common method for change of length unit
        public string changeLengthUnit(Dictionary<string, double> dict, string cbUnit)
        {
            double length;
            string tbText;

            if(dict.TryGetValue(cbUnit, out length))
            {
                if(length == -999999)
                {
                    // internal commitment
                    tbText = "n/a";
                    dict.Clear();
                }
                else
                {
                    // get the user selected elevation view
                    tbText = length.ToString();
                }
            }
            else
            {
                tbText = "n/a";
            }

            return tbText;
        }

        // Level 20: geographic coordinates view
        // ---------------------------------------------------------------------------------------------------------
        private void cb_UnitGeographicCoord20_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // event will be triggered when Results-window is initialized and when the selection will be changed

            if(cb_UnitGeographicCoord20.SelectedItem.Equals("[dms]"))
            {
                if(this.lat.Equals(-999999) == true)
                {
                    tb_lat.Text = "n/a";
                    tb_lon.Text = "n/a";
                }
                else
                {
                    var dms_lat = new Appl.Calc().DDtoDMS(this.lat);
                    var dms_lon = new Appl.Calc().DDtoDMS(this.lon);

                    tb_lat.Text = dms_lat[0] + "° " + dms_lat[1] + "' " + dms_lat[2] + "''";
                    tb_lon.Text = dms_lon[0] + "° " + dms_lon[1] + "' " + dms_lon[2] + "''";
                }
            }
            else
            {
                tb_lat.Text = (this.lat.Equals(-999999) == true) ? "n/a" : this.lat.ToString();
                tb_lon.Text = (this.lon.Equals(-999999) == true) ? "n/a" : this.lon.ToString();
            }
        }

        // ---------------------------------------------------------------------------------------------------------

        // Level 20: elevation view
        // ---------------------------------------------------------------------------------------------------------
        private void cb_UnitElevation20_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // event will be triggered when Results-window is initialized and when the selection will be changed

            tb_elev.Text = changeLengthUnit(unitElev, cb_UnitElevation20.SelectedItem.ToString());
        }

        // ---------------------------------------------------------------------------------------------------------

        // Level 30: location view
        // --------------------------------------------------------------------------------------------------------
        private void cb_Origin30_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // event will be triggered when the selection will be changed

            tb_originX_30.Text = changeLengthUnit(unitX30, cb_Origin30.SelectedItem.ToString());
            tb_originY_30.Text = changeLengthUnit(unitY30, cb_Origin30.SelectedItem.ToString());
            tb_originZ_30.Text = changeLengthUnit(unitZ30, cb_Origin30.SelectedItem.ToString());
        }

        // ---------------------------------------------------------------------------------------------------------

        // Level 30: rotation view
        // ---------------------------------------------------------------------------------------------------------
        private void cb_Rotation30_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(cb_Rotation30.SelectedItem.Equals("deg"))
            {
                this.angleX30 = new Appl.Calc().GetAngleBetweenForXAxis(this.dirX30);
                tb_rotationX_30.Text = this.angleX30.ToString();

                this.angleZ30 = new Appl.Calc().GetAngleBetweenForZAxis(this.dirZ30);
                tb_rotationZ_30.Text = this.angleZ30.ToString();
            }
            else
            {
                tb_rotationX_30.Text = this.dirX30.X + ", " + this.dirX30.Y + ", " + this.dirX30.Z;  //ifc-default
                tb_rotationZ_30.Text = this.dirZ30.X + ", " + this.dirZ30.Y + ", " + this.dirZ30.Z;  //ifc-default

                //test calc backwards

                //var vectX = new Appl.Calc().GetVector3DForXAxis(this.angleX30);
                //var vectZ = new Appl.Calc().GetVector3DForZAxis(this.angleZ30);
            }
        }

        // ---------------------------------------------------------------------------------------------------------

        // Level 40: location view
        // --------------------------------------------------------------------------------------------------------
        private void cb_Origin40_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // event will be triggered when Results-window is initialized and when the selection will be changed
            tb_originX_40.Text = changeLengthUnit(unitX40, cb_Origin40.SelectedItem.ToString());
            tb_originY_40.Text = changeLengthUnit(unitY40, cb_Origin40.SelectedItem.ToString());
            tb_originZ_40.Text = changeLengthUnit(unitZ40, cb_Origin40.SelectedItem.ToString());
        }

        // ---------------------------------------------------------------------------------------------------------

        // Level 40: rotation view (Origin)
        // ---------------------------------------------------------------------------------------------------------
        private void cb_Rotation40_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(cb_Rotation40.SelectedItem.Equals("deg"))
            {
                this.angleX = new Appl.Calc().GetAngleBetweenForXAxis(this.dirX);
                tb_rotationX_40.Text = this.angleX.ToString();

                this.angleZ = new Appl.Calc().GetAngleBetweenForZAxis(this.dirZ);
                tb_rotationZ_40.Text = this.angleZ.ToString();
            }
            else
            {
                tb_rotationX_40.Text = this.dirX.X + ", " + this.dirX.Y + ", " + this.dirX.Z;  //ifc-default
                tb_rotationZ_40.Text = this.dirZ.X + ", " + this.dirZ.Y + ", " + this.dirZ.Z;  //ifc-default

                //test calc backwards

                //var vectX = new Appl.Calc().GetVector3DForXAxis(this.angleX);
                //var vectZ = new Appl.Calc().GetVector3DForZAxis(this.angleZ);
            }
        }

        // ---------------------------------------------------------------------------------------------------------

        // Level 40: rotation view (True North)
        // --------------------------------------------------------------------------------------------------------
        private void cb_TrueNorth40_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(cb_TrueNorth40.SelectedItem.Equals("deg"))
            {
                this.angleTN = new Appl.Calc().GetAngleBetweenForXYplane(this.dirTN);

                tb_rotationTN_40.Text = this.angleTN.ToString();
            }
            else
            {
                tb_rotationTN_40.Text = this.dirTN.X + ", " + this.dirTN.Y;
            }
        }

        // ---------------------------------------------------------------------------------------------------------

        // Level 50: rotation view
        // --------------------------------------------------------------------------------------------------------
        private void cb_Rotation50_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(cb_Rotation50.SelectedItem.Equals("deg"))
            {
                this.angleMap = new Appl.Calc().GetAngleBetweenForXYplane(this.dirMap);

                tb_rotation50.Text = this.angleMap.ToString();
            }
            else
            {
                tb_rotation50.Text = this.dirMap.X + ", " + this.dirMap.Y;
            }
        }

        // --------------------------------------------------------------------------------------------------------------------------------------------------------------

        // UPDATE user specific attribute values for each level and write them into the certain ifc-file
        // --------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void bt_UpdateGeoRef_Click(object sender, RoutedEventArgs e)
        {
            //make textboxes editable

            bt_SaveChanges.IsEnabled = true;

            foreach(TabItem ti in prog.Items)
            {
                Grid gr1 = (Grid)ti.Content;

                foreach(GroupBox group in gr1.Children)
                {
                    Grid gr2 = (Grid)group.Content;

                    if(gr2 is Grid)
                    {
                        foreach(Control tb in gr2.Children)
                        {
                            if(tb is TextBox)
                            {
                                (tb as TextBox).IsReadOnly = false;
                                (tb as TextBox).Background = Brushes.White;
                            }
                        }
                    }
                }
            }
        }

        private void bt_SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            if(tab_10.IsSelected)
            {
                geoRef10.AddressLines[0] = tb_adr0.Text;
                geoRef10.AddressLines[1] = tb_adr1.Text;
                geoRef10.AddressLines[2] = tb_adr2.Text;

                geoRef10.Postalcode = tb_plz.Text;
                geoRef10.Town = tb_town.Text;
                geoRef10.Region = tb_region.Text;
                geoRef10.Country = tb_country.Text;

                geoRef10.UpdateLevel10();

                geoRef10.GetLevel10();
                lb_Instance10.Content = geoRef10.Instance_Object[0] + "=" + geoRef10.Instance_Object[1];
            }

            if(tab_20.IsSelected)

            {
                if(cb_UnitGeographicCoord20.SelectedItem.ToString() == "[dd]")
                {
                    geoRef20.Latitude = double.Parse(tb_lat.Text);
                    geoRef20.Longitude = double.Parse(tb_lon.Text);
                }
                else
                {
                    geoRef20.Latitude = new Appl.Calc().DMStoDD(tb_lat.Text);
                    geoRef20.Longitude = new Appl.Calc().DMStoDD(tb_lon.Text);
                }

                this.lat = geoRef20.Latitude;
                this.lon = geoRef20.Longitude;

                var elevNew = double.Parse(tb_elev.Text);
                this.unitElev = new Appl.Calc().ConvertLengthUnits(cb_UnitElevation20.SelectedItem.ToString(), elevNew);

                double elevConv;
                this.unitElev.TryGetValue(this.unit, out elevConv);

                geoRef20.Elevation = elevConv;

                geoRef20.UpdateLevel20();
            }

            if(tab_30.IsSelected)

            {
                geoRef30.ObjectLocationXYZ.Clear();

                var x30New = double.Parse(tb_originX_30.Text);
                var y30New = double.Parse(tb_originY_30.Text);
                var z30New = double.Parse(tb_originZ_30.Text);

                this.unitX30 = new Appl.Calc().ConvertLengthUnits(cb_Origin30.SelectedItem.ToString(), x30New);
                this.unitY30 = new Appl.Calc().ConvertLengthUnits(cb_Origin30.SelectedItem.ToString(), y30New);
                this.unitZ30 = new Appl.Calc().ConvertLengthUnits(cb_Origin30.SelectedItem.ToString(), z30New);

                double x30Conv, y30Conv, z30Conv;
                this.unitX30.TryGetValue(this.unit, out x30Conv);
                this.unitY30.TryGetValue(this.unit, out y30Conv);
                this.unitZ30.TryGetValue(this.unit, out z30Conv);

                geoRef30.ObjectLocationXYZ.Add(x30Conv);
                geoRef30.ObjectLocationXYZ.Add(y30Conv);
                geoRef30.ObjectLocationXYZ.Add(z30Conv);

                geoRef30.ObjectRotationX.Clear();
                geoRef30.ObjectRotationZ.Clear();

                if(cb_Rotation30.SelectedItem.ToString() == "vect")
                {
                    char delimiter = ',';
                    string[] vectorX = tb_rotationX_30.Text.Split(delimiter);

                    foreach(var vect in vectorX)
                    {
                        vect.Trim();

                        geoRef30.ObjectRotationX.Add(double.Parse(vect));
                    }

                    string[] vectorZ = tb_rotationZ_30.Text.Split(delimiter);

                    foreach(var vect in vectorZ)
                    {
                        vect.Trim();

                        geoRef30.ObjectRotationZ.Add(double.Parse(vect));
                    }
                }
                else
                {
                    var vectorX = new Appl.Calc().GetVector3DForXAxis(double.Parse(tb_rotationX_30.Text));

                    geoRef30.ObjectRotationX.Add(vectorX.X);
                    geoRef30.ObjectRotationX.Add(vectorX.Y);
                    geoRef30.ObjectRotationX.Add(vectorX.Z);

                    var vectorZ = new Appl.Calc().GetVector3DForZAxis(double.Parse(tb_rotationZ_30.Text));

                    geoRef30.ObjectRotationZ.Add(vectorZ.X);
                    geoRef30.ObjectRotationZ.Add(vectorZ.Y);
                    geoRef30.ObjectRotationZ.Add(vectorZ.Z);
                }

                geoRef30.UpdateLevel30();

                geoRef30.GetLevel30();
                lb_Instance30.Content = geoRef30.Instance_Object[0] + "=" + geoRef30.Instance_Object[1];
            }

            if(tab_40.IsSelected)

            {
                geoRef40.ProjectLocation.Clear();

                var x40New = double.Parse(tb_originX_40.Text);
                var y40New = double.Parse(tb_originY_40.Text);
                var z40New = double.Parse(tb_originZ_40.Text);

                this.unitX30 = new Appl.Calc().ConvertLengthUnits(cb_Origin40.SelectedItem.ToString(), x40New);
                this.unitY30 = new Appl.Calc().ConvertLengthUnits(cb_Origin40.SelectedItem.ToString(), y40New);
                this.unitZ30 = new Appl.Calc().ConvertLengthUnits(cb_Origin40.SelectedItem.ToString(), z40New);

                double x40Conv, y40Conv, z40Conv;
                this.unitX30.TryGetValue(this.unit, out x40Conv);
                this.unitY30.TryGetValue(this.unit, out y40Conv);
                this.unitZ30.TryGetValue(this.unit, out z40Conv);

                geoRef40.ProjectLocation.Add(x40Conv);
                geoRef40.ProjectLocation.Add(y40Conv);
                geoRef40.ProjectLocation.Add(z40Conv);

                geoRef40.ProjectRotationX.Clear();
                geoRef40.ProjectRotationZ.Clear();

                if(cb_Rotation40.SelectedItem.ToString() == "vect")
                {
                    char delimiter = ',';
                    string[] vectorX = tb_rotationX_40.Text.Split(delimiter);

                    foreach(var vect in vectorX)
                    {
                        vect.Trim();

                        geoRef40.ProjectRotationX.Add(double.Parse(vect));
                    }

                    string[] vectorZ = tb_rotationZ_40.Text.Split(delimiter);

                    foreach(var vect in vectorZ)
                    {
                        vect.Trim();

                        geoRef40.ProjectRotationZ.Add(double.Parse(vect));
                    }
                }
                else
                {
                    var vectorX = new Appl.Calc().GetVector3DForXAxis(double.Parse(tb_rotationX_40.Text));

                    geoRef40.ProjectRotationX.Add(vectorX.X);
                    geoRef40.ProjectRotationX.Add(vectorX.Y);
                    geoRef40.ProjectRotationX.Add(vectorX.Z);

                    var vectorZ = new Appl.Calc().GetVector3DForZAxis(double.Parse(tb_rotationZ_40.Text));

                    geoRef40.ProjectRotationZ.Add(vectorZ.X);
                    geoRef40.ProjectRotationZ.Add(vectorZ.Y);
                    geoRef40.ProjectRotationZ.Add(vectorZ.Z);
                }

                geoRef40.TrueNorthXY.Clear();

                if(cb_TrueNorth40.SelectedItem.ToString() == "vect")
                {
                    char delimiter = ',';
                    string[] vectorTN = tb_rotationTN_40.Text.Split(delimiter);

                    foreach(var vect in vectorTN)
                    {
                        vect.Trim();

                        geoRef40.TrueNorthXY.Add(double.Parse(vect));
                    }
                }
                else
                {
                    var vectorTN = new Appl.Calc().GetVectorInXYplane(double.Parse(tb_rotationTN_40.Text));

                    geoRef40.TrueNorthXY.Add(vectorTN.X);
                    geoRef40.TrueNorthXY.Add(vectorTN.Y);
                }

                geoRef40.UpdateLevel40();

                geoRef40.GetLevel40();
                lb_Instance_WCS.Content = geoRef40.Instance_Object_WCS[0] + "=" + geoRef40.Instance_Object_WCS[1];
                lb_Instance_TN.Content = geoRef40.Instance_Object_North[0] + "=" + geoRef40.Instance_Object_North[1];
            }

            if(tab_50.IsSelected)
            {
                geoRef50.Translation_Eastings = double.Parse(tb_eastings50.Text);
                geoRef50.Translation_Northings = double.Parse(tb_northings50.Text);
                geoRef50.Translation_Orth_Height = double.Parse(tb_height50.Text);

                geoRef50.RotationXY.Clear();

                if(cb_Rotation50.SelectedItem.ToString() == "vect")
                {
                    char delimiter = ',';
                    string[] vectorMap = tb_rotation50.Text.Split(delimiter);

                    foreach(var vect in vectorMap)
                    {
                        vect.Trim();

                        geoRef50.RotationXY.Add(double.Parse(vect));
                    }
                }
                else
                {
                    var vectorMap = new Appl.Calc().GetVectorInXYplane(double.Parse(tb_rotation50.Text));

                    geoRef50.RotationXY.Add(vectorMap.Y);
                    geoRef50.RotationXY.Add(vectorMap.X);
                }

                geoRef50.Scale = double.Parse(tb_scale50.Text);

                geoRef50.CRS_Name = tb_CRSname50.Text;
                geoRef50.CRS_Description = tb_CRSdesc50.Text;
                geoRef50.CRS_Geodetic_Datum = tb_CRSgeod50.Text;
                geoRef50.CRS_Vertical_Datum = tb_CRSvert50.Text;
                geoRef50.CRS_Projection_Name = tb_ProjName50.Text;
                geoRef50.CRS_Projection_Zone = tb_ProjZone50.Text;

                geoRef50.UpdateLevel50();
            }
        }
    }
}