using System;
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
        private string directory;
        private string file;
        private Vector3D dirTN;
        private Vector3D dirMap;
        private Vector3D dirX;
        private Vector3D dirZ;
        private Vector3D dirX30;
        private Vector3D dirZ30;

        private double lat;
        private double lon;
        private double elev;
        private double east;
        private double north;
        private double orthHt;
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

        private Dictionary<string, Appl.Level10> Level10List = new Dictionary<string, Appl.Level10>();
        private Dictionary<string, Appl.Level20> Level20List = new Dictionary<string, Appl.Level20>();
        private Dictionary<string, Appl.Level30> Level30List = new Dictionary<string, Appl.Level30>();
        private Dictionary<string, Appl.Level40> Level40List = new Dictionary<string, Appl.Level40>();
        private Dictionary<string, Appl.Level50> Level50List = new Dictionary<string, Appl.Level50>();

        private List<string> logOutput = new List<string>();
        private IO.JsonOutput jsonout = new IO.JsonOutput();

        public Results(IfcStore model, string file)
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                this.model = model;
                this.file = file;

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

                var siteReading = new Appl.SiteReader(model).SiteList;       //for Level 10 and 20
                var bldgReading = new Appl.BldgReader(model).BldgList;       //for Level 10
                var prodReading = new Appl.UpperPlcmReader(model).ProdList;  //for Level 30
                var ctxReading = new Appl.ContextReader(model).CtxList;     //for Level 40 and 50

                for(int i = 0; i < siteReading.Count; i++)
                {
                    var ifcHash = siteReading[i].GetHashCode();
                    var ifcType = siteReading[i].GetType().Name;

                    string listbox = "#" + ifcHash + "=" + ifcType;

                    var geoRef10 = new Appl.Level10(model, ifcHash, ifcType);
                    geoRef10.GetLevel10();
                    this.Level10List.Add(listbox, geoRef10);
                    var geoRef20 = new Appl.Level20(model, ifcHash);
                    geoRef20.GetLevel20();
                    this.Level20List.Add(listbox, geoRef20);

                    SpatialElements10.Items.Add(listbox);
                    SiteElements20.Items.Add(listbox);
                }

                for(int i = 0; i < bldgReading.Count; i++)
                {
                    var ifcHash = bldgReading[i].GetHashCode();
                    var ifcType = bldgReading[i].GetType().Name;
                    string listbox = "#" + ifcHash + "=" + ifcType;

                    var geoRef10 = new Appl.Level10(model, ifcHash, ifcType);
                    geoRef10.GetLevel10();
                    this.Level10List.Add(listbox, geoRef10);

                    SpatialElements10.Items.Add(listbox);
                }

                for(int i = 0; i < prodReading.Count; i++)
                {
                    var ifcHash = prodReading[i].GetHashCode();
                    var ifcType = prodReading[i].GetType().Name;

                    string listbox = "#" + ifcHash + "=" + ifcType;

                    // get values for specific element
                    var geoRef30 = new Appl.Level30(model, ifcHash, ifcType);
                    geoRef30.GetLevel30();
                    this.Level30List.Add(listbox, geoRef30);

                    PlacementElements30.Items.Add(listbox);
                }

                for(int i = 0; i < ctxReading.Count; i++)
                {
                    var ifcHash = ctxReading[i].GetHashCode();
                    var ifcType = ctxReading[i].GetType().Name;

                    string listbox = "#" + ifcHash + "=" + ifcType;

                    // get values for specific element
                    var geoRef40 = new Appl.Level40(model, ifcHash);
                    geoRef40.GetLevel40();
                    this.Level40List.Add(listbox, geoRef40);

                    // get values for specific element
                    var geoRef50 = new Appl.Level50(model, ifcHash);
                    geoRef50.GetLevel50();
                    this.Level50List.Add(listbox, geoRef50);

                    PlacementElements40.Items.Add(listbox);
                    MapElements50.Items.Add(listbox);
                }

                var pos = model.FileName.LastIndexOf("\\");
                this.directory = model.FileName.Substring(0, pos);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error occured while initializing dialogue for IfcGeoRefUpdater. \r\n Error message: " + ex.Message);
            }

            //---------------------------------------------------------------------------------------------------------------------
        }

        // GET all attribute values for each level adn write them into the textboxes and labels
        // --------------------------------------------------------------------------------------------------------------------------------------------------------------

        // TabItem Level 10: fill textboxes with attribute values of the actual selected site or building
        //------------------------------------------------------------------------------------------------

        public void SpatialElements10_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Appl.Level10 geoRef10;

                Level10List.TryGetValue(SpatialElements10.SelectedItem.ToString(), out geoRef10);

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
            catch(Exception ex)
            {
                MessageBox.Show("Error occured. Unable to fill textboxes with GeoRef values at Level 10. \r\nError message: " + ex.Message);
            }
        }

        //------------------------------------------------------------------------------------------------

        // TabItem Level 20: fill textboxes with attribute values of the actual selected site
        //------------------------------------------------------------------------------------------------

        private void SiteElements20_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Appl.Level20 geoRef20;

                Level20List.TryGetValue(SiteElements20.SelectedItem.ToString(), out geoRef20);

                // fill variables with certain values

                this.lat = geoRef20.Latitude;
                this.lon = geoRef20.Longitude;
                this.elev = geoRef20.Elevation;

                //set default to "dd" because this view will be passed by XBim
                cb_UnitGeographicCoord20.SelectedItem = "[dd]";

                // calculate unit views for results window
                this.unitElev = new Appl.Calc().ConvertLengthUnits(this.unit, geoRef20.Elevation);

                //set combobox unit to the readed unit
                cb_UnitElevation20.SelectedItem = this.unit;

                tb_elev.Text = changeLengthUnit(unitElev, this.unit);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error occured. Unable to fill textboxes with GeoRef values at Level 20. \r\nError message: " + ex.Message);
            }
        }

        //------------------------------------------------------------------------------------------------

        // TabItem Level 30: fill textboxes with attribute values of the actual selected element
        //------------------------------------------------------------------------------------------------

        public void PlacementElements30_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Appl.Level30 geoRef30;

                Level30List.TryGetValue(PlacementElements30.SelectedItem.ToString(), out geoRef30);

                cb_Rotation30.SelectedItem = "vect";

                // fill label and textboxes with certain values

                lb_Instance30.Content = geoRef30.Instance_Object[0] + "=" + geoRef30.Instance_Object[1];

                // check if necessary !!!

                this.xyz30.Insert(0, geoRef30.ObjectLocationXYZ[0]);
                this.xyz30.Insert(1, geoRef30.ObjectLocationXYZ[1]);
                this.xyz30.Insert(2, geoRef30.ObjectLocationXYZ[2]);

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
            catch(Exception ex)
            {
                MessageBox.Show("Error occured. Unable to fill textboxes with GeoRef values at Level 30. \r\nError message: " + ex.Message);
            }
        }

        //------------------------------------------------------------------------------------------------

        // TabItem Level 40: fill textboxes with attribute values of the actual selected geometric representation context
        //------------------------------------------------------------------------------------------------
        private void PlacementElements40_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Appl.Level40 geoRef40;

                Level40List.TryGetValue(PlacementElements40.SelectedItem.ToString(), out geoRef40);

                // fill labels and textboxes with certain values

                lb_Instance_WCS.Content = geoRef40.Instance_Object_WCS[0] + "=" + geoRef40.Instance_Object_WCS[1];
                lb_Instance_TN.Content = geoRef40.Instance_Object_North[0] + "=" + geoRef40.Instance_Object_North[1];

                cb_Rotation40.SelectedItem = "vect";
                cb_TrueNorth40.SelectedItem = "vect";

                this.xyz40.Insert(0, geoRef40.ProjectLocation[0]);
                this.xyz40.Insert(1, geoRef40.ProjectLocation[1]);

                this.dirX.X = geoRef40.ProjectRotationX[0];
                this.dirX.Y = geoRef40.ProjectRotationX[1];

                this.dirTN.X = geoRef40.TrueNorthXY[0];
                this.dirTN.Y = geoRef40.TrueNorthXY[1];

                if(geoRef40.ProjectLocation.Count > 2)
                {
                    this.xyz40.Insert(2, geoRef40.ProjectLocation[2]);

                    this.dirZ.X = geoRef40.ProjectRotationZ[0];
                    this.dirZ.Y = geoRef40.ProjectRotationZ[1];
                    this.dirZ.Z = geoRef40.ProjectRotationZ[2];

                    this.dirX.Z = geoRef40.ProjectRotationX[2];

                    this.unitZ40 = new Appl.Calc().ConvertLengthUnits(this.unit, geoRef40.ProjectLocation[2]);
                    tb_originZ_40.Text = changeLengthUnit(unitZ40, this.unit);

                    tb_rotationZ_40.Text = this.dirZ.X + ", " + this.dirZ.Y + ", " + this.dirZ.Z;
                    tb_rotationX_40.Text = this.dirX.X + ", " + this.dirX.Y + ", " + this.dirX.Z;

                    if(!cb_Rotation40.Items.Contains("deg"))
                    {
                        cb_Rotation40.Items.Add("deg");
                    }

                    tb_originZ_40.IsEnabled = true;
                    tb_rotationZ_40.IsEnabled = true;
                }
                else
                {
                    tb_rotationZ_40.Text = "";
                    tb_rotationX_40.Text = this.dirX.X + ", " + this.dirX.Y;
                    tb_originZ_40.Text = "";

                    cb_Rotation40.Items.Remove("deg");

                    tb_originZ_40.IsEnabled = false;
                    tb_rotationZ_40.IsEnabled = false;
                }

                //--------------------

                // calculate unit views for results window
                this.unitX40 = new Appl.Calc().ConvertLengthUnits(this.unit, geoRef40.ProjectLocation[0]);
                this.unitY40 = new Appl.Calc().ConvertLengthUnits(this.unit, geoRef40.ProjectLocation[1]);

                // when initialized set unit selection to project unit
                cb_Origin40.SelectedItem = this.unit;

                tb_originX_40.Text = changeLengthUnit(unitX40, this.unit);
                tb_originY_40.Text = changeLengthUnit(unitY40, this.unit);

                tb_rotationTN_40.Text = this.dirTN.X + ", " + this.dirTN.Y;
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error occured. Unable to fill textboxes with GeoRef values at Level 40. \r\nError message: " + ex.Message);
            }
        }

        //------------------------------------------------------------------------------------------------

        // TabItem Level 50: fill textboxes with attribute values of the actual selected geometric representation context
        //
        private void MapElements50_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // if schema version is not IFC4 than there is no Level50 to view / edit

                if(model.IfcSchemaVersion.ToString().Equals("Ifc2X3"))
                {
                    tab_50.IsEnabled = false;
                }
                else
                {
                    Appl.Level50 geoRef50;

                    Level50List.TryGetValue(MapElements50.SelectedItem.ToString(), out geoRef50);

                    cb_Rotation50.SelectedItem = "vect";

                    // fill labels and textboxes with certain values

                    this.east = geoRef50.Translation_Eastings;
                    this.north = geoRef50.Translation_Northings;
                    this.orthHt = geoRef50.Translation_Orth_Height;

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
            catch(Exception ex)
            {
                MessageBox.Show("Error occured. Unable to fill textboxes with GeoRef values at Level 50. \r\nError message: " + ex.Message);
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
            try
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
                        var dms_lat = new Appl.Calc().DDtoCompound(this.lat);
                        var dms_lon = new Appl.Calc().DDtoCompound(this.lon);

                        tb_lat.Text = dms_lat[0] + "° " + Math.Abs(dms_lat[1]) + "' " + Math.Abs(dms_lat[2]) + "." + Math.Abs(dms_lat[3]) + "''";
                        tb_lon.Text = dms_lon[0] + "° " + Math.Abs(dms_lon[1]) + "' " + Math.Abs(dms_lon[2]) + "." + Math.Abs(dms_lon[3]) + "''";
                    }
                }
                else
                {
                    tb_lat.Text = (this.lat.Equals(-999999) == true) ? "n/a" : this.lat.ToString();
                    tb_lon.Text = (this.lon.Equals(-999999) == true) ? "n/a" : this.lon.ToString();

                    //var lat_view = new Appl.Calc().DMStoDD(tb_lat.Text);
                    //var lon_view = new Appl.Calc().DMStoDD(tb_lon.Text);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error occured while setting unit for geograhic coordinates (Level 20). \r\n Error message: " + ex.Message);
            }
        }

        // ---------------------------------------------------------------------------------------------------------

        // Level 20: elevation view
        // ---------------------------------------------------------------------------------------------------------
        private void cb_UnitElevation20_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // event will be triggered when Results-window is initialized and when the selection will be changed
                tb_elev.Text = changeLengthUnit(unitElev, cb_UnitElevation20.SelectedItem.ToString());
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error occured while setting unit for elevation (Level 20). \r\n Error message: " + ex.Message);
            }
        }

        // ---------------------------------------------------------------------------------------------------------

        // Level 30: location view
        // --------------------------------------------------------------------------------------------------------
        private void cb_Origin30_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // event will be triggered when the selection will be changed
                tb_originX_30.Text = changeLengthUnit(unitX30, cb_Origin30.SelectedItem.ToString());
                tb_originY_30.Text = changeLengthUnit(unitY30, cb_Origin30.SelectedItem.ToString());
                tb_originZ_30.Text = changeLengthUnit(unitZ30, cb_Origin30.SelectedItem.ToString());
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error occured while setting unit for location (Level 30). \r\n Error message: " + ex.Message);
            }
        }

        // ---------------------------------------------------------------------------------------------------------

        // Level 30: rotation view
        // ---------------------------------------------------------------------------------------------------------
        private void cb_Rotation30_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
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
            catch(Exception ex)
            {
                MessageBox.Show("Error occured while setting unit for rotation (Level 30). \r\n Error message: " + ex.Message);
            }
        }

        // ---------------------------------------------------------------------------------------------------------

        // Level 40: location view
        // --------------------------------------------------------------------------------------------------------
        private void cb_Origin40_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // event will be triggered when Results-window is initialized and when the selection will be changed
                tb_originX_40.Text = changeLengthUnit(unitX40, cb_Origin40.SelectedItem.ToString());
                tb_originY_40.Text = changeLengthUnit(unitY40, cb_Origin40.SelectedItem.ToString());
                tb_originZ_40.Text = changeLengthUnit(unitZ40, cb_Origin40.SelectedItem.ToString());
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error occured while setting unit for location (Level 40). \r\n Error message: " + ex.Message);
            }
        }

        // ---------------------------------------------------------------------------------------------------------

        // Level 40: rotation view (Origin)
        // ---------------------------------------------------------------------------------------------------------
        private void cb_Rotation40_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
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
            catch(Exception ex)
            {
                MessageBox.Show("Error occured while setting unit for rotation (Level 40). \r\n Error message: " + ex.Message);
            }
        }

        // ---------------------------------------------------------------------------------------------------------

        // Level 40: rotation view (True North)
        // --------------------------------------------------------------------------------------------------------
        private void cb_TrueNorth40_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if(cb_TrueNorth40.SelectedItem.Equals("deg"))
                {
                    this.angleTN = new Appl.Calc().GetAngleBetweenForXAxis(this.dirTN);

                    tb_rotationTN_40.Text = this.angleTN.ToString();
                }
                else
                {
                    tb_rotationTN_40.Text = this.dirTN.X + ", " + this.dirTN.Y;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error occured while setting unit for True North rotation (Level 40). \r\n Error message: " + ex.Message);
            }
        }

        // ---------------------------------------------------------------------------------------------------------

        // Level 50: rotation view
        // --------------------------------------------------------------------------------------------------------
        private void cb_Rotation50_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if(cb_Rotation50.SelectedItem.Equals("deg"))
                {
                    this.angleMap = new Appl.Calc().GetAngleBetweenForXAxis(this.dirMap);

                    tb_rotation50.Text = this.angleMap.ToString();
                }
                else
                {
                    tb_rotation50.Text = this.dirMap.X + ", " + this.dirMap.Y;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error occured while setting unit for rotation (Level 50). \r\n Error message: " + ex.Message);
            }
        }

        // --------------------------------------------------------------------------------------------------------------------------------------------------------------

        // UPDATE user specific attribute values for each level and write them into the certain ifc-file
        // --------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void bt_UpdateGeoRef_Click(object sender, RoutedEventArgs e)
        {
            //make textboxes editable

            bt_save10.IsEnabled = true;
            bt_save20.IsEnabled = true;
            bt_save30.IsEnabled = true;
            bt_save40.IsEnabled = true;
            bt_save50.IsEnabled = true;
            bt_writeIFC.IsEnabled = true;
            check_log.IsEnabled = true;
            check_json.IsEnabled = true;

            lb_statusMsg.Content = "ready";

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

        public string ReplaceTrim(string entry)
        {
            if(entry.Contains(" ") == true)
                entry = entry.Replace(" ", "");

            if(entry.Contains(",") == true)
                entry = entry.Replace(",", ".");

            return entry;
        }

        public string ReplaceTrimVector(string entry)
        {
            if(entry.Contains(" ") == true)
                entry = entry.Replace(" ", "");

            if(entry.Contains(";") == true)
                entry = entry.Replace(";", ",");

            if(entry.Contains("/") == true)
                entry = entry.Replace("/", ",");

            if(entry.Contains("|") == true)
                entry = entry.Replace("|", ",");

            return entry;
        }

        private void bt_save10_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Appl.Level10 geoRef10;
                Level10List.TryGetValue(SpatialElements10.SelectedItem.ToString(), out geoRef10);

                geoRef10.AddressLines[0] = tb_adr0.Text;
                geoRef10.AddressLines[1] = tb_adr1.Text;
                geoRef10.AddressLines[2] = tb_adr2.Text;

                geoRef10.Postalcode = tb_plz.Text;
                geoRef10.Town = tb_town.Text;
                geoRef10.Region = tb_region.Text;
                geoRef10.Country = tb_country.Text;

                if(SpatialElements10.Items.Count > 1)
                    bt_equal10.IsEnabled = true;
            }
            catch(Exception ex)
            {
                MessageBox.Show("Unknown error occured while set new values for Level of GeoRef 10." + "error message: " + ex.Message);
            }
        }

        private void bt_save20_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Appl.Level20 geoRef20;
                Level20List.TryGetValue(SiteElements20.SelectedItem.ToString(), out geoRef20);

                try
                {
                    if(cb_UnitGeographicCoord20.SelectedItem.ToString() == "[dd]")
                    {
                        geoRef20.Latitude = double.Parse(ReplaceTrim(tb_lat.Text));
                        geoRef20.Longitude = double.Parse(ReplaceTrim(tb_lon.Text));
                    }
                    else
                    {
                        geoRef20.Latitude = new Appl.Calc().DMStoDD(tb_lat.Text);
                        geoRef20.Longitude = new Appl.Calc().DMStoDD(tb_lon.Text);
                    }

                    this.lat = geoRef20.Latitude;
                    this.lon = geoRef20.Longitude;
                }
                catch(Exception ex)
                {
                    if(ex is FormatException)
                    {
                        if(cb_UnitGeographicCoord20.SelectedItem.ToString() == "[dd]")
                            MessageBox.Show("Latitude und Longitude can only contain numbers with point as decimal separator, e.g. 51.3435");
                        else
                            MessageBox.Show("Latitude und Longitude can only contain numbers separeted like following exmaple -51°12'34.43422''");
                    }
                }

                try
                {
                    var elevNew = double.Parse(tb_elev.Text);
                    this.unitElev = new Appl.Calc().ConvertLengthUnits(cb_UnitElevation20.SelectedItem.ToString(), elevNew);

                    double elevConv;
                    this.unitElev.TryGetValue(this.unit, out elevConv);

                    geoRef20.Elevation = elevConv;
                }
                catch(Exception ex)
                {
                    if(ex is FormatException)
                    {
                        MessageBox.Show("Elevation can only contain numbers with point as decimal separator");
                    }
                }

                if(SiteElements20.Items.Count > 1)
                    bt_equal20.IsEnabled = true;
            }
            catch(Exception ex)
            {
                MessageBox.Show("Unknown error occured while set new values for Level of GeoRef 20." + "error message: " + ex.Message);
            }
        }

        private void bt_save30_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Appl.Level30 geoRef30;

                Level30List.TryGetValue(PlacementElements30.SelectedItem.ToString(), out geoRef30);

                try
                {
                    geoRef30.ObjectLocationXYZ.Clear();

                    var x30New = double.Parse(ReplaceTrim(tb_originX_30.Text));
                    var y30New = double.Parse(ReplaceTrim(tb_originY_30.Text));
                    var z30New = double.Parse(ReplaceTrim(tb_originZ_30.Text));

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
                }
                catch(Exception ex)
                {
                    if(ex is FormatException)
                    {
                        MessageBox.Show("Location (X,Y,Z) can only contain numbers with point as decimal separator.");
                    }
                }
                try
                {
                    geoRef30.ObjectRotationX.Clear();
                    geoRef30.ObjectRotationZ.Clear();

                    if(cb_Rotation30.SelectedItem.ToString() == "vect")
                    {
                        char delimiter = ',';
                        var entryX = ReplaceTrimVector(tb_rotationX_30.Text);
                        string[] vectorX = entryX.Split(delimiter);

                        foreach(var vect in vectorX)
                        {
                            geoRef30.ObjectRotationX.Add(double.Parse(vect));
                        }

                        var entryZ = ReplaceTrimVector(tb_rotationZ_30.Text);
                        string[] vectorZ = entryZ.Split(delimiter);

                        foreach(var vect in vectorZ)
                        {
                            geoRef30.ObjectRotationZ.Add(double.Parse(vect));
                        }
                    }
                    else
                    {
                        var vectorX = new Appl.Calc().GetVector3DForXAxis(double.Parse(ReplaceTrim(tb_rotationX_30.Text)));

                        geoRef30.ObjectRotationX.Add(vectorX.X);
                        geoRef30.ObjectRotationX.Add(vectorX.Y);
                        geoRef30.ObjectRotationX.Add(vectorX.Z);

                        var vectorZ = new Appl.Calc().GetVector3DForZAxis(double.Parse(ReplaceTrim(tb_rotationZ_30.Text)));

                        geoRef30.ObjectRotationZ.Add(vectorZ.X);
                        geoRef30.ObjectRotationZ.Add(vectorZ.Y);
                        geoRef30.ObjectRotationZ.Add(vectorZ.Z);
                    }
                }

                catch(Exception ex)
                {
                    if(ex is FormatException)
                    {
                        if(cb_Rotation30.SelectedItem.ToString() == "vect")
                            MessageBox.Show("Rotation can only contain 3 comma-separated values for any 3DVector, e.g. 1,0,0");
                        else
                            MessageBox.Show("Rotation can only contain numbers for angle value, e.g. 180.5");
                    }
                }

                if(PlacementElements30.Items.Count > 1)
                    bt_equal30.IsEnabled = true;
            }
            catch(Exception ex)
            {
                MessageBox.Show("Unknown error occured while set new values for Level of GeoRef 30. /r/n" + "error message: " + ex.Message);
            }
        }

        private void bt_save40_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Appl.Level40 geoRef40;

                Level40List.TryGetValue(PlacementElements40.SelectedItem.ToString(), out geoRef40);

                try
                {
                    if(geoRef40.ProjectLocation.Count > 2)
                    {
                        geoRef40.ProjectLocation.Clear();

                        var x40New = double.Parse(ReplaceTrim(tb_originX_40.Text));
                        var y40New = double.Parse(ReplaceTrim(tb_originY_40.Text));
                        var z40New = double.Parse(ReplaceTrim(tb_originZ_40.Text));

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
                    }
                    else
                    {
                        geoRef40.ProjectLocation.Clear();

                        var x40New = double.Parse(ReplaceTrim(tb_originX_40.Text));
                        var y40New = double.Parse(ReplaceTrim(tb_originY_40.Text));

                        this.unitX30 = new Appl.Calc().ConvertLengthUnits(cb_Origin40.SelectedItem.ToString(), x40New);
                        this.unitY30 = new Appl.Calc().ConvertLengthUnits(cb_Origin40.SelectedItem.ToString(), y40New);

                        double x40Conv, y40Conv, z40Conv;
                        this.unitX30.TryGetValue(this.unit, out x40Conv);
                        this.unitY30.TryGetValue(this.unit, out y40Conv);

                        geoRef40.ProjectLocation.Add(x40Conv);
                        geoRef40.ProjectLocation.Add(y40Conv);
                    }
                }

                catch(Exception ex)
                {
                    if(ex is FormatException)
                    {
                        MessageBox.Show("Location (X,Y,Z) can only contain numbers with point as decimal separator.");
                    }
                }

                try
                {
                    if(geoRef40.ProjectRotationX.Count > 2)
                    {
                        geoRef40.ProjectRotationX.Clear();
                        geoRef40.ProjectRotationZ.Clear();

                        if(cb_Rotation40.SelectedItem.ToString() == "vect")
                        {
                            char delimiter = ',';
                            var entryX = ReplaceTrimVector(tb_rotationX_40.Text);
                            string[] vectorX = entryX.Split(delimiter);

                            foreach(var vect in vectorX)
                            {
                                geoRef40.ProjectRotationX.Add(double.Parse(vect));
                            }

                            var entryZ = ReplaceTrimVector(tb_rotationZ_40.Text);
                            string[] vectorZ = entryZ.Split(delimiter);

                            foreach(var vect in vectorZ)
                            {
                                geoRef40.ProjectRotationZ.Add(double.Parse(vect));
                            }
                        }
                        else
                        {
                            var vectorX = new Appl.Calc().GetVector3DForXAxis(double.Parse(ReplaceTrim(tb_rotationX_40.Text)));

                            geoRef40.ProjectRotationX.Add(vectorX.X);
                            geoRef40.ProjectRotationX.Add(vectorX.Y);
                            geoRef40.ProjectRotationX.Add(vectorX.Z);

                            var vectorZ = new Appl.Calc().GetVector3DForZAxis(double.Parse(ReplaceTrim(tb_rotationZ_40.Text)));

                            geoRef40.ProjectRotationZ.Add(vectorZ.X);
                            geoRef40.ProjectRotationZ.Add(vectorZ.Y);
                            geoRef40.ProjectRotationZ.Add(vectorZ.Z);
                        }
                    }
                    else
                    {
                        geoRef40.ProjectRotationX.Clear();
                        //geoRef40.ProjectRotationZ.Clear();

                        char delimiter = ',';
                        var entryX = ReplaceTrimVector(tb_rotationX_40.Text);
                        string[] vectorX = entryX.Split(delimiter);

                        foreach(var vect in vectorX)
                        {
                            geoRef40.ProjectRotationX.Add(double.Parse(vect));
                        }

                    }
                }
                catch(Exception ex)
                {
                    if(ex is FormatException)
                    {
                        if(cb_Rotation40.SelectedItem.ToString() == "vect")
                            MessageBox.Show("Rotation can only contain 2 or 3 comma-separated values for any 2D or 3D Vector, e.g. 1,0 or 1,0,0");
                        else
                            MessageBox.Show("Rotation can only contain numbers for angle value, e.g. 180.5");
                    }
                }
                try
                {
                    geoRef40.TrueNorthXY.Clear();

                    if(cb_TrueNorth40.SelectedItem.ToString() == "vect")
                    {
                        char delimiter = ',';
                        var entryTN = ReplaceTrimVector(tb_rotationTN_40.Text);
                        string[] vectorTN = entryTN.Split(delimiter);

                        foreach(var vect in vectorTN)
                        {
                            geoRef40.TrueNorthXY.Add(double.Parse(vect));
                        }
                    }
                    else
                    {
                        var vectorTN = new Appl.Calc().GetVector3DForXAxis(double.Parse(ReplaceTrim(tb_rotationTN_40.Text)));

                        geoRef40.TrueNorthXY.Add(vectorTN.X);
                        geoRef40.TrueNorthXY.Add(vectorTN.Y);
                    }
                }

                catch(Exception ex)
                {
                    if(ex is FormatException)
                    {
                        if(cb_TrueNorth40.SelectedItem.ToString() == "vect")
                            MessageBox.Show("True North can only contain 2 comma-separated values for any 2DVector, e.g. 1,0");
                        else
                            MessageBox.Show("True North can only contain numbers for angle value, e.g. 180.5");
                    }
                }

                if(PlacementElements40.Items.Count > 1)
                    bt_equal40.IsEnabled = true;
            }
            catch(Exception ex)
            {
                MessageBox.Show("Unknown error occured while set new values for Level of GeoRef 40. /r/n" + "error message: " + ex.Message);
            }
        }

        private void bt_save50_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Appl.Level50 geoRef50;

                Level50List.TryGetValue(MapElements50.SelectedItem.ToString(), out geoRef50);

                try
                {
                    geoRef50.Translation_Eastings = double.Parse(ReplaceTrim(tb_eastings50.Text));
                    geoRef50.Translation_Northings = double.Parse(ReplaceTrim(tb_northings50.Text));
                    geoRef50.Translation_Orth_Height = double.Parse(ReplaceTrim(tb_height50.Text));
                    geoRef50.Scale = double.Parse(ReplaceTrim(tb_scale50.Text));
                }

                catch(Exception ex)
                {
                    if(ex is FormatException)
                    {
                        MessageBox.Show("Translation and Scale can only contain numbers with point as decimal separator.");
                    }
                }

                try
                {
                    geoRef50.RotationXY.Clear();

                    if(cb_Rotation50.SelectedItem.ToString() == "vect")
                    {
                        char delimiter = ',';
                        var entryMap = ReplaceTrimVector(tb_rotation50.Text);
                        string[] vectorMap = entryMap.Split(delimiter);

                        foreach(var vect in vectorMap)
                        {
                            geoRef50.RotationXY.Add(double.Parse(vect));
                        }
                    }
                    else
                    {
                        var vectorMap = new Appl.Calc().GetVector3DForXAxis(double.Parse(ReplaceTrim(tb_rotation50.Text)));

                        geoRef50.RotationXY.Add(vectorMap.Y);
                        geoRef50.RotationXY.Add(vectorMap.X);
                    }
                }
                catch(Exception ex)
                {
                    if(ex is FormatException)
                    {
                        if(cb_Rotation50.ToString() == "vect")
                            MessageBox.Show("Rotation can only contain 2 comma-separated values for any 2DVector, e.g. 1,0");
                        else
                            MessageBox.Show("Rotation can only contain numbers for angle value, e.g. 180.5");
                    }
                }

                geoRef50.CRS_Name = tb_CRSname50.Text;
                geoRef50.CRS_Description = tb_CRSdesc50.Text;
                geoRef50.CRS_Geodetic_Datum = tb_CRSgeod50.Text;
                geoRef50.CRS_Vertical_Datum = tb_CRSvert50.Text;
                geoRef50.CRS_Projection_Name = tb_ProjName50.Text;
                geoRef50.CRS_Projection_Zone = tb_ProjZone50.Text;

                if(MapElements50.Items.Count > 1)
                    bt_equal50.IsEnabled = true;
            }
            catch(Exception ex)
            {
                MessageBox.Show("Unknown error occured while set new values for Level of GeoRef 50. /r/n" + "error message: " + ex.Message);
            }
        }

        private void bt_equal10_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Appl.Level10 geoRef10;

                Level10List.TryGetValue(SpatialElements10.SelectedItem.ToString(), out geoRef10);

                foreach(var geoRef in Level10List)
                {
                    if(geoRef.Value == geoRef10)
                        continue;

                    geoRef.Value.AddressLines = geoRef10.AddressLines;
                    geoRef.Value.Postalcode = geoRef10.Postalcode;
                    geoRef.Value.Town = geoRef10.Town;
                    geoRef.Value.Region = geoRef10.Region;
                    geoRef.Value.Country = geoRef10.Country;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error occured. Unable to set equal all occurences of Level 10. \r\n + Error message: " + ex.Message);
            }
        }

        private void bt_equal20_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Appl.Level20 geoRef20;

                Level20List.TryGetValue(SiteElements20.SelectedItem.ToString(), out geoRef20);

                foreach(var geoRef in Level20List)
                {
                    if(geoRef.Value == geoRef20)
                        continue;

                    geoRef.Value.Latitude += (geoRef20.Latitude - this.lat);
                    geoRef.Value.Longitude += (geoRef20.Longitude - this.lon);
                    geoRef.Value.Elevation += (geoRef20.Elevation - this.elev);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error occured. Unable to set equal all occurences of Level 20. \r\n + Error message: " + ex.Message);
            }
        }

        private void bt_equal30_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Appl.Level30 geoRef30;

                Level30List.TryGetValue(PlacementElements30.SelectedItem.ToString(), out geoRef30);

                foreach(var geoRef in Level30List)
                {
                    if(geoRef.Value == geoRef30)
                        continue;

                    geoRef.Value.ObjectLocationXYZ[0] += (geoRef30.ObjectLocationXYZ[0] - this.xyz30[0]);
                    geoRef.Value.ObjectLocationXYZ[1] += (geoRef30.ObjectLocationXYZ[1] - this.xyz30[1]);
                    geoRef.Value.ObjectLocationXYZ[2] += (geoRef30.ObjectLocationXYZ[2] - this.xyz30[2]);
                    geoRef.Value.ObjectRotationX = geoRef30.ObjectRotationX;
                    geoRef.Value.ObjectRotationZ = geoRef30.ObjectRotationZ;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error occured. Unable to set equal all occurences of Level 30. \r\n + Error message: " + ex.Message);
            }
        }

        private void bt_equal40_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Appl.Level40 geoRef40;

                Level40List.TryGetValue(PlacementElements40.SelectedItem.ToString(), out geoRef40);

                foreach(var geoRef in Level40List)
                {
                    if(geoRef.Value == geoRef40)
                        continue;

                    geoRef.Value.ProjectLocation[0] += (geoRef40.ProjectLocation[0] - this.xyz40[0]);
                    geoRef.Value.ProjectLocation[1] += (geoRef40.ProjectLocation[1] - this.xyz40[1]);
                    geoRef.Value.ProjectLocation[2] += (geoRef40.ProjectLocation[2] - this.xyz40[2]);
                    geoRef.Value.ProjectRotationX = geoRef40.ProjectRotationX;
                    geoRef.Value.ProjectRotationZ = geoRef40.ProjectRotationZ;
                    geoRef.Value.TrueNorthXY = geoRef40.TrueNorthXY;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error occured. Unable to set equal all occurences of Level 40. \r\n + Error message: " + ex.Message);
            }
        }

        private void bt_equal50_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Appl.Level50 geoRef50;

                Level50List.TryGetValue(MapElements50.SelectedItem.ToString(), out geoRef50);

                foreach(var geoRef in Level50List)
                {
                    if(geoRef.Value == geoRef50)
                        continue;

                    geoRef.Value.Translation_Eastings += (geoRef50.Translation_Eastings - this.east);
                    geoRef.Value.Translation_Northings += (geoRef50.Translation_Northings - this.north);
                    geoRef.Value.Translation_Orth_Height += (geoRef50.Translation_Orth_Height - this.orthHt);
                    geoRef.Value.RotationXY = geoRef50.RotationXY;
                    geoRef.Value.Scale = geoRef50.Scale;
                    geoRef.Value.CRS_Name = geoRef50.CRS_Name;
                    geoRef.Value.CRS_Description = geoRef50.CRS_Description;
                    geoRef.Value.CRS_Geodetic_Datum = geoRef50.CRS_Geodetic_Datum;
                    geoRef.Value.CRS_Vertical_Datum = geoRef50.CRS_Vertical_Datum;
                    geoRef.Value.CRS_Projection_Name = geoRef50.CRS_Projection_Name;
                    geoRef.Value.CRS_Projection_Zone = geoRef50.CRS_Projection_Zone;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error occured. Unable to set equal all occurences of Level 50. \r\n + Error message: " + ex.Message);
            }
        }

        public void UpdateIFCfile()
        {
            try
            {
                foreach(var geoRef in Level10List.Values)
                {
                    geoRef.UpdateLevel10();
                    this.logOutput.Add(geoRef.LogOutput());
                    this.jsonout.GetGeoRefElements10(geoRef);
                }
                foreach(var geoRef in Level20List.Values)
                {
                    geoRef.UpdateLevel20();
                    this.logOutput.Add(geoRef.LogOutput());
                    this.jsonout.GetGeoRefElements20(geoRef);
                }
                foreach(var geoRef in Level30List.Values)
                {
                    geoRef.UpdateLevel30();
                    this.logOutput.Add(geoRef.LogOutput());
                    this.jsonout.GetGeoRefElements30(geoRef);
                }
                foreach(var geoRef in Level40List.Values)
                {
                    geoRef.UpdateLevel40();
                    this.logOutput.Add(geoRef.LogOutput());
                    this.jsonout.GetGeoRefElements40(geoRef);
                }

                if(model.IfcSchemaVersion.ToString() != "Ifc2X3")
                {
                    foreach(var geoRef in Level50List.Values)
                    {
                        geoRef.UpdateLevel50();
                        this.logOutput.Add(geoRef.LogOutput());
                        this.jsonout.GetGeoRefElements50(geoRef);
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error occured. Unable to update IFCfile. \r\n + Error message: " + ex.Message);
            }
        }

        private void bt_WriteIFC_Click(object sender, RoutedEventArgs e)
        {
            UpdateIFCfile();

            if(check_log.IsChecked == true)
            {
                try
                {
                    var output = new IO.LogOutput();
                    output.WriteLogfile(this.logOutput, file + "_edit", this.directory);
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Error occured. Unable to export new Logfile. \r\n + Error message: " + ex.Message);
                }
            }

            if(check_json.IsChecked == true)
            {
                try
                {
                    this.jsonout.WriteJSONfile(model, file + "_edit", this.directory);
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Error occured. Unable to export new Logfile. \r\n + Error message: " + ex.Message);
                }
            }

            lb_statusMsg.Content = "complete";
        }

        private void bt_quit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var bldg = new Appl.BldgReader();

            bldg.ReadSlab(model);
        }
    }
}