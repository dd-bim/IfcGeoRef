using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IfcGeoRefChecker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dictionary<string, IfcStore> ModelList;
        private Dictionary<string, Dictionary<string, bool>> Dict;

        private Dictionary<string, string> JsonObjects = new Dictionary<string, string>();
        private Dictionary<string, string> ModelUnit = new Dictionary<string, string>();
        private Dictionary<string, IEnumerable<IIfcBuildingElement>> GroundWallObjects = new Dictionary<string, IEnumerable<IIfcBuildingElement>>();

        public MainWindow()
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                InitializeComponent();

                bt_log.IsEnabled = false;
                bt_json.IsEnabled = false;
                bt_update.IsEnabled = false;
            }

            catch(Exception ex)
            {
                MessageBox.Show("Error occured while initializing program. \r\n" + "Error message: " + ex.Message);
            }
        }

        private void BtInfo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var info = new Info();
                info.Show();
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error occured while showing Information/License window. \r\n" + "Error message: " + ex.Message);
            }
        }

        private void Bt_Import(object sender, RoutedEventArgs e)
        {
            try
            {
                if(this.ModelList == null)
                {
                    this.ModelList = new IO.IfcImport().ImportModels;
                }
                else
                {
                    var addList = new IO.IfcImport().ImportModels;

                    try
                    {
                        foreach(var kp in addList)
                        {
                            this.ModelList.Add(kp.Key, kp.Value);
                        }
                    }
                    catch
                    {
                        MessageBox.Show("It is not supported to import IfcModels with the same file name. Please rename Ifc-file and try again.");
                    }
                }

                foreach(string file in this.ModelList.Keys)
                {
                    if(importFiles.Items.Contains(file))
                    {
                        continue;
                    }
                    else
                    {
                        importFiles.Items.Add(file);
                    }
                }

                lb_importMsg.Content = this.ModelList.Count + " file(s) loaded";
            }

            catch(Exception ex)
            {
                MessageBox.Show("Error occured while Import. \r\n " + "Error message: " + ex.Message);
            }
            finally { }

            try
            {
                CheckGeoRef();
            }
            catch
            {
                MessageBox.Show("Error occured while Checking!");
            }

            foreach(var m in ModelList)
            {
                try
                {
                    var reader = new IO.IfcReader(m.Value);
                    var bldgs = reader.BldgReader();
                    var groundWalls = reader.GroundFloorWallReader(bldgs[0]);   //nur Wände des ersten Gebäudes derzeit in scope

                    this.GroundWallObjects.Add(m.Key, groundWalls);

                    m.Value.Close();
                }
                catch
                {
                    MessageBox.Show("Error occured while detection of groundwalls and/or Closing!");
                    m.Value.Close();
                }
            }
        }

        private void CheckGeoRef_Click(object sender, RoutedEventArgs e)
        {
        }

        private void CheckGeoRef()
        {
            try
            {
                this.Dict = new Dictionary<string, Dictionary<string, bool>>();
                var dictBool = new Dictionary<string, bool>();

                var logOutput = new List<string>();

                var output = new IO.LogOutput();

                foreach(var kvpModel in this.ModelList)
                {
                    var jsonout = new IO.JsonOutput();

                    var file = kvpModel.Key;
                    var model = kvpModel.Value;

                    if(ifcModels.Items.Contains(file) == false)
                    {
                        ifcModels.Items.Add(file);
                    }

                    var unit = new IO.IfcReader(model).LengthUnitReader();
                    this.ModelUnit.Add(file, unit);

                    logOutput.Add("Project Length Unit:" + unit + "\r\n");

                    var reader = new IO.IfcReader(model);

                    var boolList10 = new List<bool>();
                    var boolList20 = new List<bool>();
                    var boolList30 = new List<bool>();
                    var boolList40 = new List<bool>();
                    var boolList50 = new List<bool>();

                    for(var i = 0; i < reader.BldgReader().Count; i++)
                    {
                        var bNo = reader.BldgReader()[i].GetHashCode();
                        var bNa = reader.BldgReader()[i].GetType().Name;

                        //var georef10 = new Appl.Level10(model, bNo, bNa);
                        var georef10 = new Appl.Level10(reader.BldgReader()[i]/*model, bNo, bNa*/);
                        georef10.GetLevel10(reader.BldgReader()[i]);

                        jsonout.GetGeoRefElements10(georef10);
                        boolList10.Add(georef10.GeoRef10);
                        logOutput.Add(georef10.LogOutput());
                    }

                    for(var i = 0; i < reader.SiteReader().Count; i++)
                    {
                        var sNo = reader.SiteReader()[i].GetHashCode();
                        var sNa = reader.SiteReader()[i].GetType().Name;

                        var georef10 = new Appl.Level10(reader.SiteReader()[i]/*model, sNo, sNa*/);

                        georef10.GetLevel10(reader.SiteReader()[i]);
                        jsonout.GetGeoRefElements10(georef10);
                        boolList10.Add(georef10.GeoRef10);
                        logOutput.Add(georef10.LogOutput());

                        var georef20 = new Appl.Level20(model, sNo);
                        georef20.GetLevel20();
                        jsonout.GetGeoRefElements20(georef20);
                        boolList20.Add(georef20.GeoRef20);
                        logOutput.Add(georef20.LogOutput());
                    }

                    for(var i = 0; i < reader.UpperPlcmProdReader().Count; i++)
                    {
                        var pNo = reader.UpperPlcmProdReader()[i].GetHashCode();
                        var pNa = reader.UpperPlcmProdReader()[i].GetType().Name;

                        var georef30 = new Appl.Level30(model, pNo, pNa);
                        georef30.GetLevel30();
                        jsonout.GetGeoRefElements30(georef30);
                        boolList30.Add(georef30.GeoRef30);
                        logOutput.Add(georef30.LogOutput());
                    }

                    for(var i = 0; i < reader.ContextReader().Count; i++)
                    {
                        var ctxNo = reader.ContextReader()[i].GetHashCode();

                        var georef40 = new Appl.Level40(model, ctxNo);
                        georef40.GetLevel40();
                        jsonout.GetGeoRefElements40(georef40);
                        boolList40.Add(georef40.GeoRef40);
                        logOutput.Add(georef40.LogOutput());

                        var georef50 = new Appl.Level50(model, ctxNo);
                        georef50.GetLevel50();
                        jsonout.GetGeoRefElements50(georef50);
                        boolList50.Add(georef50.GeoRef50);
                        logOutput.Add(georef50.LogOutput());
                    }

                    if(boolList10.Contains(true))
                    {
                        dictBool.Add(file + "georef10", true);
                    }
                    else
                    {
                        dictBool.Add(file + "georef10", false);
                    }

                    if(boolList20.Contains(true))
                    {
                        dictBool.Add(file + "georef20", true);
                    }
                    else
                    {
                        dictBool.Add(file + "georef20", false);
                    }

                    if(boolList30.Contains(true))
                    {
                        dictBool.Add(file + "georef30", true);
                    }
                    else
                    {
                        dictBool.Add(file + "georef30", false);
                    }

                    if(boolList40.Contains(true))
                    {
                        dictBool.Add(file + "georef40", true);
                    }
                    else
                    {
                        dictBool.Add(file + "georef40", false);
                    }

                    if(boolList50.Contains(true))
                    {
                        dictBool.Add(file + "georef50", true);
                    }
                    else
                    {
                        dictBool.Add(file + "georef50", false);
                    }

                    this.Dict.Add(file, dictBool);

                    var jsonObj = jsonout.CreateJSON(model);
                    JsonObjects.Add(file, jsonObj);

                    //var pos = model.Header.FileName.Name.LastIndexOf("\\");
                    //var directory = model.Header.FileName.Name.Substring(0, pos);

                    if(check_log.IsChecked == true)
                    {
                        output.WriteLogfile(logOutput, file/*, directory*/);
                        bt_log.IsEnabled = true;
                    }

                    if(check_json.IsChecked == true)
                    {
                        jsonout.WriteJSONfile(jsonObj, file/*, directory*/);
                        bt_json.IsEnabled = true;
                    }
                    logOutput.Clear();

                    bt_update.IsEnabled = true;
                }
                ReadBool();

                lb_checkMsg.Content = this.ModelList.Count + " file(s) checked.";
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error occured while checking GeoRef. \r\n Error message: " + ex.Message);
            }
        }

        private void ifcModels_SelectionChanged(object sender, SelectionChangedEventArgs e)

        {
            ReadBool();
        }

        public void ReadBool()
        {
            try
            {
                foreach(var keyModel in this.Dict)
                {
                    var file = keyModel.Key;

                    if(this.ifcModels.SelectedItem.ToString() == file)
                    {
                        foreach(var dec in keyModel.Value)
                        {
                            if(dec.Key.Contains(file + "georef10"))
                            {
                                bool10.Content = dec.Value;
                            }

                            if(dec.Key.Contains(file + "georef20"))
                            {
                                bool20.Content = dec.Value;
                            }

                            if(dec.Key.Contains(file + "georef30"))
                            {
                                bool30.Content = dec.Value;
                            }

                            if(dec.Key.Contains(file + "georef40"))
                            {
                                bool40.Content = dec.Value;
                            }

                            if(dec.Key.Contains(file + "georef50"))
                            {
                                bool50.Content = dec.Value;
                            }
                        }
                    }
                }
            }
            catch
            {
                MessageBox.Show("Error occured, unable to read Georef decision.");
            }
        }

        private void bt_log_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //var pos = this.ModelList[ifcModels.Text].Header.FileName.Name.LastIndexOf("\\");
                //var directory = this.ModelList[ifcModels.Text].Header.FileName.Name.Substring(0, pos + 1);

                var path = /*directory + */ifcModels.Text + ".txt";

                System.Diagnostics.Process.Start(path);
            }
            catch
            {
                MessageBox.Show("Error occured. Please check directory of your IFC-file for the corresponding GeoRef log file.");
            }
        }

        private void bt_json_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //var pos = this.ModelList[ifcModels.Text].Header.FileName.Name.LastIndexOf("\\");
                //var directory = this.ModelList[ifcModels.Text].Header.FileName.Name.Substring(0, pos + 1);

                var path = /*directory + */ifcModels.Text + ".json";

                System.Diagnostics.Process.Start(path);
            }
            catch
            {
                MessageBox.Show("Error occured. Please check directory of your IFC-file for the corresponding GeoRef JSON-file");
            }
        }

        private void bt_update_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                JsonObjects.TryGetValue(ifcModels.Text, out var jsObj);

                var showResultsJs = new Results(ifcModels.Text, jsObj);
                showResultsJs.Show();

                //var showResults = new Results(this.ModelList[ifcModels.Text], ifcModels.Text);
                //showResults.Show();
            }

            catch(Exception ex)
            {
                MessageBox.Show("Error occured. Unable to open IFCGeoRefUpdater. \r\n Error message: " + ex.Message);
            }
        }

        private void bt_comparer_Click(object sender, RoutedEventArgs e)
        {
            if(this.ModelList != null && this.ModelList.Count > 1)
            {
                var comp = new Compare(this.ModelList);
                comp.Show();
            }
            else
            {
                MessageBox.Show("Please import at least 2 Ifc-files for comparison.");
            }
        }

        private void bt_quit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void bt_help_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(@"Documentation.html");
            }
            catch
            {
                MessageBox.Show("No help file available. Please check application directory for file Documentation.html");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
        }

        private void check_log_Checked(object sender, RoutedEventArgs e)
        {
        }

        private void bt_update_man_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                JsonObjects.TryGetValue(ifcModels.Text, out var jsObj);

                var showResultsJs = new Results(ifcModels.Text, jsObj);
                showResultsJs.Show();

                //var showResults = new Results(this.ModelList[ifcModels.Text], ifcModels.Text);
                //showResults.Show();
            }

            catch(Exception ex)
            {
                MessageBox.Show("Error occured. Unable to open IFCGeoRefUpdater. \r\n Error message: " + ex.Message);
            }
        }

        private void bt_update_map_Click(object sender, RoutedEventArgs e)
        {
            GroundWallObjects.TryGetValue(ifcModels.Text, out var groundWalls);
            JsonObjects.TryGetValue(ifcModels.Text, out var jsObj);
            ModelUnit.TryGetValue(ifcModels.Text, out var unit);

            var wkt = new Appl.BldgFootprintExtraxtor().CalcBuildingFootprint(groundWalls, unit);

            var jsonWkt = new IO.JsonOutput();
            var jsonString = jsonWkt.AddWKTtoJSON(wkt, jsObj);

            jsonWkt.WriteJSONfile(jsonString, ifcModels.Text + "_wkt");



            //    tb_wkt.Text = wkt;

            //    //var jsonWKT = new IO.JsonOutput();
            //    this.jsonout.WKTRep = wkt;
            //    //this.jsonout.WriteJSONfile(model, file + "_wkt", this.directory);

            //try
            //{
            //    System.Diagnostics.Process.Start(@"\buildingLocator\index.html");
            //}
            //catch
            //{
            //    MessageBox.Show("No html-map file available. Please check application directory for file buildingLocator.index.html");
            //}
        }

        private void bt_update_ifc_Click(object sender, RoutedEventArgs e)
        {
            var showExport2IFC = new Export2IFC(ifcModels.Text);
            showExport2IFC.Show();

        }
    }
}