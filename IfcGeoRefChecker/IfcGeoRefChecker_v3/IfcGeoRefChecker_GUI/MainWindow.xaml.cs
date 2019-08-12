using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Windows.Forms;
using IfcGeoRefChecker.Appl;
using IfcGeoRefChecker.IO;
using Xbim.Ifc4.Interfaces;
using Serilog;

namespace IfcGeoRefChecker_GUI
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string direc = Environment.CurrentDirectory;
        private Dictionary<string, IfcGeoRefChecker.Appl.GeoRefChecker> CheckObjList = new Dictionary<string, IfcGeoRefChecker.Appl.GeoRefChecker>();
        private Dictionary<string, IList<IIfcBuildingElement>> GroundWallObjects;
        private Dictionary<string, string> NamePathDict;

        public MainWindow()
        {
            try
            {
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.File(this.direc, rollingInterval: RollingInterval.Day)
                    //.MinimumLevel.Debug()
                    .CreateLogger();

                //Log.Logger = new LoggerConfiguration()
                //    .WriteTo.File("C:\\Users\\goerne\\Desktop\\logtest\\log.txt", rollingInterval: RollingInterval.Day)
                //    //.MinimumLevel.Debug()
                //    .CreateLogger();

                Log.Information("Start of IfcGeoRefChecker");

                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                InitializeComponent();

                tb_direc.Text = this.direc;

                bt_log.IsEnabled = false;
                bt_json.IsEnabled = false;
            }

            catch (Exception ex)
            {
                Log.Error("Start of IfcGeoRefChecker failed. Error message: " + ex);
            }
        }

        private void bt_change_direc_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
                {
                    //Log.Information("Start of changing directory. Dialogue opened.");

                    fbd.RootFolder = Environment.SpecialFolder.Desktop;
                    fbd.Description = "Select folder";

                    fbd.ShowNewFolderButton = true;

                    var result = fbd.ShowDialog();

                    if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                    {
                        this.direc = fbd.SelectedPath;
                        //Log.Information("Directory changed to " + this.direc);
                    }
                    else
                    {
                        //Log.Information("Directory not changed.");
                    }
                }
            }
            catch (Exception ex)
            {
                //Log.Error("Not able to change directory. Error: " + ex);
                //Log.Information("Program will use the default path (install folder).");
            }

            tb_direc.Text = this.direc;
        }

        private void BtInfo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Log.Information("Open Terms of Use window.");
                var info = new Info();
                info.Show();
            }
            catch (Exception ex)
            {
                //Log.Error("Not able to open Terms of Use window. Error: " + ex);
            }
        }

        private void Bt_Import(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Information("Start of Checking Georef...");
                Log.Debug("Files loaded: " + this.CheckObjList.Count);

                if (this.CheckObjList.Count == 0)  //default at start of program
                {
                    var importObj = new IfcImport(this.direc);

                    this.NamePathDict = importObj.NamePathDict;
                    this.CheckObjList = importObj.CheckObjs;
                    this.GroundWallObjects = importObj.GroundWallObjects;
                }
                else
                {
                    var addedObj = new IfcImport(this.direc);

                    var addCheckObjs = addedObj.CheckObjs;
                    var addGroundWalls = addedObj.GroundWallObjects;
                    var addedPath = addedObj.NamePathDict;

                    try
                    {
                        foreach (var kp in addCheckObjs)
                        {
                            this.CheckObjList.Add(kp.Key, kp.Value);
                        }

                        foreach (var kp in addGroundWalls)
                        {
                            this.GroundWallObjects.Add(kp.Key, kp.Value);
                        }

                        foreach (var kp in addedPath)
                        {
                            this.NamePathDict.Add(kp.Key, kp.Value);
                        }
                    }
                    catch (ArgumentException aex)
                    {
                        var exStr = "Ifc-file already imported.";
                        Log.Error(exStr + aex.Message);
                        System.Windows.MessageBox.Show(exStr);
                    }
                }

                foreach (string fileName in this.CheckObjList.Keys)
                {
                    if (importFiles.Items.Contains(fileName))
                    {
                        continue;
                    }
                    else
                    {
                        importFiles.Items.Add(fileName);
                    }

                    //if(ifcModels.Items.Contains(file) == false)
                    //{
                    //    ifcModels.Items.Add(file);
                    //}
                }
                lb_checkMsg.Content = this.CheckObjList.Count + " file(s) checked";

                lb_importMsg.Content = this.CheckObjList.Count + " file(s) loaded";

                foreach (var checkObj in this.CheckObjList)
                {
                    string[] paths = { direc, checkObj.Key };
                    var path = System.IO.Path.Combine(paths);
                    if (check_log.IsChecked == true)
                    {
                        try
                        {
                            Log.Information("Export checking-log...");

                            var log = new LogOutput(checkObj.Value, path, checkObj.Key);
                            bt_log.IsEnabled = true;

                            Log.Information("Export successful to: " + path);
                        }
                        catch (IOException exIO)
                        {
                            Log.Error("Not able to export log. Error: " + exIO);
                        }
                    }

                    if (check_json.IsChecked == true)
                    {
                        try
                        {
                            Log.Information("Export JSON-file...");

                            var js = new JsonOutput();
                            js.JsonOutputFile(checkObj.Value, path);
                            bt_json.IsEnabled = true;

                            Log.Information("Export successful to: " + path);
                        }
                        catch (IOException exIO)
                        {
                            Log.Error("Not able to export json. Error: " + exIO);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Unknown error occured. Error: " + ex.Message);
                System.Windows.MessageBox.Show("Unknown error occured. Error: " + ex.Message);
            }
}

        private void bt_log_Click(object sender, RoutedEventArgs e)
        {
            if (this.CheckObjList.Count == 0)
            {
                Log.Information("Showing log not possible. No model imported.");
                System.Windows.MessageBox.Show("Please import at least 1 Ifc-file.");
            }
            else if (importFiles.SelectedItem == null)
            {
                Log.Information("Showing log not possible. No model selected.");
                System.Windows.MessageBox.Show("Please select the ifc file, which should be updated, in the box above.");
            }
            else
            {
                try
                {
                    System.Diagnostics.Process.Start(this.direc + importFiles.SelectedItem.ToString() + ".txt");
                    Log.Information("Checking log opened.");
                }
                catch (Exception ex)
                {
                    Log.Error("Not able to open log-file. Error: " + ex);
                    System.Windows.MessageBox.Show("Error occured. Please check directory of your IFC-file for the corresponding GeoRef log file." + ex);
                }
            }
        }

        private void bt_json_Click(object sender, RoutedEventArgs e)
        {
            if (this.CheckObjList.Count == 0)
            {
                Log.Information("Showing JSON not possible. No model imported.");
                System.Windows.MessageBox.Show("Please import at least 1 Ifc-file.");
            }
            else if (importFiles.SelectedItem == null)
            {
                Log.Information("Showing JSON not possible. No model selected.");
                System.Windows.MessageBox.Show("Please select the ifc file, which should be updated, in the box above.");
            }
            else
            {
                try
                {
                    System.Diagnostics.Process.Start(this.direc + importFiles.SelectedItem.ToString() + ".json");
                    Log.Information("Checking json opened.");
                }
                catch (Exception ex)
                {
                    Log.Error("Not able to open JSON-file. Error: " + ex);
                    System.Windows.MessageBox.Show("Error occured. Please check directory of your IFC-file for the corresponding GeoRef JSON-file." + ex.Message);
                }
            }
        }

        private void bt_guide_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(@"Quick_Guide.html");
                Log.Information("Quick Guide HTML opened.");
            }
            catch (Exception ex)
            {
                Log.Error("Not able to open Documentation. Error: " + ex.Message);
                System.Windows.MessageBox.Show("Error occured. Please check directory for Quick_Guide HTML-file." + ex.Message);
            }
        }

        private void bt_docu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(@"Documentation.html");

                Log.Information("Documentation HTML opened.");
            }
            catch (Exception ex)
            {
                Log.Error("Not able to open Documentation. Error: " + ex.Message);
                System.Windows.MessageBox.Show("Error occured. Please check directory for Documentation HTML-file." + ex.Message);
            }
        }

        private void bt_quit_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Application terminated.");
            System.Windows.Application.Current.Shutdown();
        }

        private void bt_update_man_Click(object sender, RoutedEventArgs e)
        {
            if (this.CheckObjList.Count == 0)
            {
                Log.Information("Starting Updating not possible. No model imported.");
                System.Windows.MessageBox.Show("Please import at least 1 Ifc-file.");
            }
            else if (importFiles.SelectedItem == null)
            {
                Log.Information("Starting Updating not possible. No model selected.");
                System.Windows.MessageBox.Show("Please select the ifc file, which should be updated, in the box above.");
            }
            else
            {
                try
                {
                    Log.Information("Manual updating started...");

                    CheckObjList.TryGetValue(importFiles.SelectedItem.ToString(), out var checkObj);

                    //Log.Information("Write JSON-check file to local 'buildingLocator\\json' directory...");

                    //var jsonPath = direc + "\\IfcGeoRefChecker\\buildingLocator\\json\\check";
                    //var jsonout = new IO.JsonOutput();
                    //jsonout.JsonOutputFile(checkObj, jsonPath);

                    //Log.Information("Done.");

                    //var jsonFolder = direc + "\\IfcGeoRefChecker\\buildingLocator\\json\\";

                    var manExp = new UpdateMan(checkObj, this.direc, importFiles.SelectedItem.ToString());
                    manExp.Show();
                }
                catch (Exception ex)
                {
                    var str = "Not able to start manual update process. Maybe necessary JSON-Export to local directory failed. Please check local directory! Error: " + ex.Message;

                    Log.Error(str);
                    System.Windows.MessageBox.Show(str);
                }
            }
        }

        private void bt_update_map_Click(object sender, RoutedEventArgs e)
        {
            if (this.CheckObjList.Count == 0)
            {
                Log.Information("Starting Updating not possible. No model imported.");
                System.Windows.MessageBox.Show("Please import at least 1 Ifc-file.");
            }
            else if (importFiles.SelectedItem == null)
            {
                Log.Information("Starting Updating not possible. No model selected.");
                System.Windows.MessageBox.Show("Please select the ifc file, which should be updated, in the box above.");
            }
            else
            {
                Log.Information("Updating via map started...");

                try
                {
                    GroundWallObjects.TryGetValue(importFiles.SelectedItem.ToString(), out var groundWalls);
                    CheckObjList.TryGetValue(importFiles.SelectedItem.ToString(), out var checkObj);

                    System.Windows.MessageBox.Show("Start of calculating the BuildingFootprint and writing it into json file.\r\n \r\n" +
                        "Please save the file and continue with the Building Locator in web browser.\r\n" +
                        "You will need Internet connection to display the required web map service.\r\n \r\n" +
                        "After changing the georef via map, please continue with step 2 \"Export Updates to IFC\" in this application. \r\n" +
                        "You will then need to import the updated JSON file which was exported by the Building Locator web tool.", "Important Information");

                    Log.Information("Calculate building perimeter...");

                    var unit = checkObj.LengthUnit;

                    Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                    try
                    {
                        var wkt = new BldgContourCalculator().GetBldgContour(groundWalls, unit);
                        checkObj.WKTRep = wkt;
                    }
                    finally
                    {
                        Mouse.OverrideCursor = null;
                    }

                    Log.Information("Calculation finished.");

                    Log.Information("Write JSON-check file with WKTZ-string for perimeter to local 'buildingLocator\\json' directory...");

                    var jsonWkt = new JsonOutput();
                    jsonWkt.JsonOutputDialog(checkObj, this.direc, importFiles.SelectedItem.ToString());

                    Log.Information("Done.");
                }
                catch (Exception ex)
                {
                    if (GroundWallObjects == null)
                    {
                        System.Windows.MessageBox.Show("Error: Not able to select GroundWalls. Please make sure, " +
                            "you checked the required file before and that your IFC file contains walls.");
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Error: Not able to calculate required building footprint. Message: " + ex.Message);
                    }
                }

                try
                {
                    Log.Information("Opening of HTML-Site for updating via map...");

                    //System.Diagnostics.Process.Start(Environment.CurrentDirectory + "\\buildingLocator\\index.html");

                    System.Diagnostics.Process.Start(Environment.CurrentDirectory + "\\win-unpacked\\test.exe");

                    Log.Information("Done.");
                }
                catch (Exception ex)
                {
                    var str = "No html-map file available. Please check local directory 'buildingLocator' for 'index.html'. Error: " + ex;

                    Log.Error(str);
                    System.Windows.MessageBox.Show(str);
                }
            }
        }

        private void bt_update_ifc_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Information("Open export window...");

                var ifcName = importFiles.SelectedItem.ToString();

                this.NamePathDict.TryGetValue(ifcName, out string ifcPath);

                var showExport2IFC = new Export2IFC(ifcPath, ifcName);
                showExport2IFC.Show();
            }
            catch (Exception ex)
            {
                var str = "Not able to open Export window. Error: " + ex;

                Log.Error(str);
                System.Windows.MessageBox.Show(str);
            }
        }

        private void bt_comparer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.CheckObjList != null && this.CheckObjList.Count > 1)
                {
                    var comp = new Compare(this.direc, this.CheckObjList);
                    comp.Show();

                    Log.Information("GeoRefComparer started.");
                }
                else
                {
                    Log.Information("Starting GeoRefComparer not possible. Not enough models imported.");
                    System.Windows.MessageBox.Show("Please import at least 2 Ifc-files for comparison.");
                }
            }
            catch (Exception ex)
            {
                Log.Error("Starting GeoRefComparer failed. Error: " + ex.Message);
            }
        }

        private void ImportFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                CheckObjList.TryGetValue(importFiles.SelectedItem.ToString(), out var currentObj);

                var ctL10 = (from l in currentObj.LoGeoRef10
                             where l.GeoRef10
                             select l).Count();

                var ctL20 = (from l in currentObj.LoGeoRef20
                             where l.GeoRef20
                             select l).Count();

                var ctL30 = (from l in currentObj.LoGeoRef30
                             where l.GeoRef30
                             select l).Count();

                var ctL40 = (from l in currentObj.LoGeoRef40
                             where l.GeoRef40
                             select l).Count();

                var ctL50 = (from l in currentObj.LoGeoRef50
                             where l.GeoRef50
                             select l).Count();

                bool10.Content = (ctL10 > 0) ? true : false;
                bool20.Content = (ctL20 > 0) ? true : false;
                bool30.Content = (ctL30 > 0) ? true : false;
                bool40.Content = (ctL40 > 0) ? true : false;
                bool50.Content = (ctL50 > 0) ? true : false;

                Log.Information("Selected model in GUI changed.");
            }
            catch
            {
                Log.Error("Not able to read short results for selected model.");
            }
        }
    }
}
