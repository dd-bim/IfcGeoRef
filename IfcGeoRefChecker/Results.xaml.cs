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
        private double[] dms_lat;
        private double[] dms_lon;
        private double lat;
        private double lon;
        private double angleMap;
        private double angleTN;
        private double angleX;
        private double angleZ;
        private double angleX30;
        private double angleZ30;
        private string unit;
        private double elev;
        private List<double> xyz30 = new List<double>();
        private List<double> xyz40 = new List<double>();

        private Appl.Level10 geoRef10;
        private Appl.Level20 geoRef20;
        private Appl.Level30 geoRef30;
        private Appl.Level40 geoRef40;
        private Appl.Level50 geoRef50;

        private Dictionary<string, string> _MyDict;

        public Dictionary<string, string> MyDict
        {
            get { return _MyDict; }
            set { _MyDict = value; }
        }

        public Results(IfcStore model)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            this.model = model;

            InitializeComponent();

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

            var siteReading = new Appl.SiteReader(model);       //for Level 10 and 20
            var bldgReading = new Appl.BldgReader(model);       //for Level 10
            var prodReading = new Appl.UpperPlcmReader(model);  //for Level 30
            var ctxReading = new Appl.ContextReader(model);     //for Level 40
            //var mapReading = new Appl.MapConvReader(model);     //for Level 50

            this.unit = new Appl.UnitReader().GetProjectLengthUnit(model);

            //list for level10 and level20

            for(int i = 0; i < siteReading.SiteList.Count; i++)
            {
                string listbox = "#" + siteReading.SiteList[i].GetHashCode() + "=" + siteReading.SiteList[i].GetType().Name;
                SpatialElements10.Items.Add(listbox);
                SiteElements20.Items.Add(listbox);
            }

            //list for level20

            for(int i = 0; i < bldgReading.BldgList.Count; i++)
            {
                string listbox = "#" + bldgReading.BldgList[i].GetHashCode() + "=" + bldgReading.BldgList[i].GetType().Name;
                SpatialElements10.Items.Add(listbox);
            }

            //list for level30

            for(int i = 0; i < prodReading.ProdList.Count; i++)
            {
                string listbox = "#" + prodReading.ProdList[i].GetHashCode() + "=" + prodReading.ProdList[i].GetType().Name;
                PlacementElements30.Items.Add(listbox);
            }

            //list for level40 and 50

            for(int i = 0; i < ctxReading.CtxList.Count; i++)
            {
                string listbox = "#" + ctxReading.CtxList[i].GetHashCode() + "=" + ctxReading.CtxList[i].GetType().Name;
                PlacementElements40.Items.Add(listbox);
                MapElements50.Items.Add(listbox);
            }

            cb_Origin30.Items.Add("m");
            cb_Origin30.Items.Add("mm");
            cb_Origin30.Items.Add("ft");
            cb_Origin30.Items.Add("in");

            cb_Origin30.SelectedItem = this.unit;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
        }

        public void SpatialElements10_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string ref10 = SpatialElements10.SelectedItem.ToString();
            int index10 = ref10.IndexOf("=");
            string sub10 = ref10.Substring(1, index10 - 1);
            string type10 = ref10.Substring(index10 + 1);

            //TabItem LoGeoRef10

            this.geoRef10 = new Appl.Level10(model, sub10, type10);

            geoRef10.GetLevel10();

            lb_Instance10.Content = geoRef10.Instance_Object[0] + "=" + geoRef10.Instance_Object[1];

            tb_adr0.Text = geoRef10.AddressLines[0];
            tb_adr1.Text = geoRef10.AddressLines[1];
            tb_adr2.Text = geoRef10.AddressLines[2];
            tb_plz.Text = geoRef10.Postalcode;
            tb_town.Text = geoRef10.Town;
            tb_region.Text = geoRef10.Region;
            tb_country.Text = geoRef10.Country;
        }

        private void SiteElements20_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //TabItem LoGeoRef20

            string ref20 = SiteElements20.SelectedItem.ToString();
            int index20 = ref20.IndexOf("=");
            string sub20 = ref20.Substring(1, index20 - 1);

            this.geoRef20 = new Appl.Level20(model, sub20);

            //GeoRef20 = new Appl.Level20(model, sub20);

            cb_UnitGeographicCoord20.Items.Add("[dd]");
            cb_UnitGeographicCoord20.Items.Add("[dms]");
            cb_UnitGeographicCoord20.SelectedItem = "[dd]";

            geoRef20.GetLevel20();

            this.lat = geoRef20.Latitude;
            this.lon = geoRef20.Longitude;
            this.elev = geoRef20.Elevation;

            tb_lat.Text = (this.lat.Equals(-999999) == true) ? "n/a" : this.lat.ToString();
            tb_lon.Text = (this.lon.Equals(-999999) == true) ? "n/a" : this.lon.ToString();
            tb_elev.Text = (this.elev.Equals(-999999) == true) ? "n/a" : this.elev.ToString();
        }

        public void PlacementElements30_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string ref30 = PlacementElements30.SelectedItem.ToString();
            int index30 = ref30.IndexOf("=");
            string sub30 = ref30.Substring(1, index30 - 1);
            string type30 = ref30.Substring(index30 + 1);

            //TabItem LoGeoRef30

            this.geoRef30 = new Appl.Level30(model, sub30, type30);

            lb_Instance30.Content = geoRef30.Instance_Object[0] + "=" + geoRef30.Instance_Object[1];

            geoRef30.GetLevel30();

            this.xyz30.Add(geoRef30.ObjectLocationXYZ[0]);
            this.xyz30.Add(geoRef30.ObjectLocationXYZ[1]);
            this.xyz30.Add(geoRef30.ObjectLocationXYZ[2]);

            tb_originX_30.Text = this.xyz30[0].ToString();
            tb_originY_30.Text = this.xyz30[1].ToString();
            tb_originZ_30.Text = this.xyz30[2].ToString();

            cb_Rotation30.Items.Add("vect");
            cb_Rotation30.Items.Add("deg");
            cb_Rotation30.SelectedItem = "vect";

            this.dirX30.X = geoRef30.ObjectRotationX[0];
            this.dirX30.Y = geoRef30.ObjectRotationX[1];
            this.dirX30.Z = geoRef30.ObjectRotationX[2];

            tb_rotationX_30.Text = this.dirX30.X + ", " + this.dirX30.Y + ", " + this.dirX30.Z;

            this.dirZ30.X = geoRef30.ObjectRotationZ[0];
            this.dirZ30.Y = geoRef30.ObjectRotationZ[1];
            this.dirZ30.Z = geoRef30.ObjectRotationZ[2];

            tb_rotationZ_30.Text = this.dirZ30.X + ", " + this.dirZ30.Y + ", " + this.dirZ30.Z;
        }

        private void PlacementElements40_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //TabItem LoGeoRef40

            string ref40 = PlacementElements40.SelectedItem.ToString();
            int index40 = ref40.IndexOf("=");
            string sub40 = ref40.Substring(1, index40 - 1);

            this.geoRef40 = new Appl.Level40(model, sub40);
            geoRef40.GetLevel40();

            lb_Instance_WCS.Content = geoRef40.Instance_Object_WCS[0] + "=" + geoRef40.Instance_Object_WCS[1];
            lb_Instance_TN.Content = geoRef40.Instance_Object_North[0] + "=" + geoRef40.Instance_Object_North[1];

            List<double> xyz40 = new List<double>();

            this.xyz40.Add(geoRef40.ProjectLocation[0]);
            this.xyz40.Add(geoRef40.ProjectLocation[1]);
            this.xyz40.Add(geoRef40.ProjectLocation[2]);

            this.dirZ.X = geoRef40.ProjectRotationZ[0];
            this.dirZ.Y = geoRef40.ProjectRotationZ[1];
            this.dirZ.Z = geoRef40.ProjectRotationZ[2];

            tb_rotationX_40.Text = this.dirX.X + ", " + this.dirX.Y + ", " + this.dirX.Z;
            tb_rotationZ_40.Text = this.dirZ.X + ", " + this.dirZ.Y + ", " + this.dirZ.Z;

            tb_originX_40.Text = this.xyz40[0].ToString();
            tb_originY_40.Text = this.xyz40[1].ToString();
            tb_originZ_40.Text = this.xyz40[2].ToString();

            cb_Rotation40.Items.Add("vect");
            cb_Rotation40.Items.Add("deg");
            cb_Rotation40.SelectedItem = "vect";

            this.dirX.X = geoRef40.ProjectRotationX[0];
            this.dirX.Y = geoRef40.ProjectRotationX[1];
            this.dirX.Z = geoRef40.ProjectRotationX[2];

            cb_TrueNorth40.Items.Add("vect");
            cb_TrueNorth40.Items.Add("deg");
            cb_TrueNorth40.SelectedItem = "vect";

            this.dirTN.X = geoRef40.TrueNorthXY[0];
            this.dirTN.Y = geoRef40.TrueNorthXY[1];

            tb_rotationTN_40.Text = this.dirTN.X + ", " + this.dirTN.Y;

            cb_Origin40.Items.Add("m");
            cb_Origin40.Items.Add("mm");
            cb_Origin40.Items.Add("ft");
            cb_Origin40.Items.Add("in");

            cb_Origin40.SelectedItem = this.unit;
        }

        private void MapElements50_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // input for level50 (only one valid context per file)

            if(model.IfcSchemaVersion.ToString().Equals("Ifc2X3"))
            {
                tab_50.IsEnabled = false;
            }
            else
            {
                string ref50 = MapElements50.SelectedItem.ToString();
                int index50 = ref50.IndexOf("=");
                string sub50 = ref50.Substring(1, index50 - 1);

                this.geoRef50 = new Appl.Level50(model, sub50);
                geoRef50.GetLevel50();

                cb_Rotation50.Items.Add("vect");
                cb_Rotation50.Items.Add("deg");

                lb_Reference50.Content = geoRef50.Reference_Object[0] + "=" + geoRef50.Reference_Object[1];
                lb_InstanceCRS50.Content = geoRef50.Instance_Object_CRS[0] + "=" + geoRef50.Instance_Object_CRS[1];

                tb_eastings50.Text = (geoRef50.Translation_Eastings.Equals(-999999) == true) ? "n/a" : geoRef50.Translation_Eastings.ToString();
                tb_northings50.Text = (geoRef50.Translation_Northings.Equals(-999999) == true) ? "n/a" : geoRef50.Translation_Northings.ToString();
                tb_height50.Text = (geoRef50.Translation_Orth_Height.Equals(-999999) == true) ? "n/a" : geoRef50.Translation_Orth_Height.ToString();

                this.dirMap.X = geoRef50.RotationXY[0];
                this.dirMap.Y = geoRef50.RotationXY[1];

                tb_rotation50.Text = this.dirMap.X + ", " + this.dirMap.Y;

                tb_scale50.Text = (geoRef50.Scale.Equals(-999999) == true) ? "n/a" : geoRef50.Scale.ToString();

                tb_CRSname50.Text = geoRef50.CRS_Name;
                tb_CRSdesc50.Text = geoRef50.CRS_Description;
                tb_CRSgeod50.Text = geoRef50.CRS_Geodetic_Datum;
                tb_CRSvert50.Text = geoRef50.CRS_Vertical_Datum;
                tb_ProjName50.Text = geoRef50.CRS_Projection_Name;
                tb_ProjZone50.Text = geoRef50.CRS_Projection_Zone;
            }

            cb_UnitElevation20.Items.Add("m");
            cb_UnitElevation20.Items.Add("mm");
            cb_UnitElevation20.Items.Add("ft");
            cb_UnitElevation20.Items.Add("in");

            cb_UnitElevation20.SelectedItem = this.unit;
        }

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

        private void cb_UnitGeographicCoord20_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(cb_UnitGeographicCoord20.SelectedItem.Equals("[dms]"))
            {
                if(this.lat.Equals(-999999) == true)
                {
                    tb_lat.Text = "n/a";
                    tb_lon.Text = "n/a";
                }
                else
                {
                    this.dms_lat = new Appl.Calc().DDtoDMS(this.lat);
                    this.dms_lon = new Appl.Calc().DDtoDMS(this.lon);

                    tb_lat.Text = this.dms_lat[0] + "° " + this.dms_lat[1] + "' " + this.dms_lat[2] + "''";
                    tb_lon.Text = this.dms_lon[0] + "° " + this.dms_lon[1] + "' " + this.dms_lon[2] + "''";
                }
            }
            else
            {
                tb_lat.Text = (this.lat.Equals(-999999) == true) ? "n/a" : this.lat.ToString();
                tb_lon.Text = (this.lon.Equals(-999999) == true) ? "n/a" : this.lon.ToString();
            }
        }

        private void cb_UnitElevation20_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string old = this.unit;

            foreach(var item in e.RemovedItems)
            {
                old = item.ToString();
            }

            List<double> elev_list = new List<double>
            {
                {this.elev }
            };

            var convert = new Appl.Calc().ConvertLengthUnit(old, cb_UnitElevation20.SelectedItem.ToString(), elev_list);

            this.elev = convert[0];

            tb_elev.Text = (this.elev.Equals(-999999) == true) ? "n/a" : this.elev.ToString();
        }

        private void cb_Origin30_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string old = this.unit;

            foreach(var item in e.RemovedItems)
            {
                old = item.ToString();
            }

            var convert = new Appl.Calc().ConvertLengthUnit(old, cb_Origin30.SelectedItem.ToString(), this.xyz30);

            this.xyz30 = convert;

            tb_originX_30.Text = this.xyz30[0].ToString();
            tb_originY_30.Text = this.xyz30[1].ToString();
            tb_originZ_30.Text = this.xyz30[2].ToString();
        }

        private void cb_Origin40_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string old = this.unit;

            foreach(var item in e.RemovedItems)
            {
                old = item.ToString();
            }

            var convert = new Appl.Calc().ConvertLengthUnit(old, cb_Origin40.SelectedItem.ToString(), this.xyz40);

            this.xyz40 = convert;

            tb_originX_40.Text = this.xyz40[0].ToString();
            tb_originY_40.Text = this.xyz40[1].ToString();
            tb_originZ_40.Text = this.xyz40[2].ToString();
        }

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

                geoRef20.Elevation = double.Parse(tb_elev.Text);

                geoRef20.UpdateLevel20();
            }

            if(tab_30.IsSelected)

            {
                geoRef30.ObjectLocationXYZ.Clear();
                geoRef30.ObjectLocationXYZ.Add(double.Parse(tb_originX_30.Text));
                geoRef30.ObjectLocationXYZ.Add(double.Parse(tb_originY_30.Text));
                geoRef30.ObjectLocationXYZ.Add(double.Parse(tb_originZ_30.Text));

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
            }

            if(tab_40.IsSelected)

            {
                geoRef40.ProjectLocation.Clear();
                geoRef40.ProjectLocation.Add(double.Parse(tb_originX_40.Text));
                geoRef40.ProjectLocation.Add(double.Parse(tb_originY_40.Text));
                geoRef40.ProjectLocation.Add(double.Parse(tb_originZ_40.Text));

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
            }

            if(tab_50.IsEnabled)
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