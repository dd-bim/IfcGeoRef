using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Serilog;
using Xbim.Ifc4.Interfaces;

namespace IfcGeoRefChecker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dictionary<string, Appl.GeoRefChecker> CheckObjList;
        private string direc = Environment.CurrentDirectory;
        private Dictionary<string, IList<IIfcBuildingElement>> GroundWallObjects;

        public MainWindow()
        {
            try
            {
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.File(@"D:\\1_CityBIM\\1_Programmierung\\City2BIM\\City2BIM_Revit\\log.txt", rollingInterval: RollingInterval.Day)
                    //.MinimumLevel.Debug()
                    .CreateLogger();

                Log.Information("Start of IfcGeoRefChecker");

                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                InitializeComponent();

                tb_direc.Text = this.direc;

                bt_log.IsEnabled = false;
                bt_json.IsEnabled = false;
            }

            catch(Exception ex)
            {
                Log.Error("Start of IfcGeoRefChecker failed. Error message: " + ex);
                MessageBox.Show("Error occured while initializing program. \r\n" + "Error message: " + ex.Message);
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------
        // Change directory functionality
        //--------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Button for change of directory (needed for write permissions later on)
        /// </summary>
        private void bt_change_direc_Click(object sender, RoutedEventArgs e)
        {
            using(var fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                fbd.RootFolder = Environment.SpecialFolder.Desktop;
                fbd.Description = "Select folder";

                fbd.ShowNewFolderButton = false;

                Log.Information("sadasdd");

                var result = fbd.ShowDialog();

                if(result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    tb_direc.Text = fbd.SelectedPath;
                    this.direc = fbd.SelectedPath;
                }
            }

            // Copy from the current directory, include subdirectories.
            DirectoryCopy(@".\IfcGeoRefChecker", this.direc + "\\IfcGeoRefChecker\\", true);
        }

        /// <summary>
        /// Creates a copy of defined folder in new directory with all subfolders and files
        /// </summary>
        private void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo dirDest = new DirectoryInfo(destDirName);

            if(!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if(!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] filesSource = dir.GetFiles();

            FileInfo[] filesDest = dirDest.GetFiles();

            foreach(FileInfo file in filesSource)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if(copySubDirs)
            {
                foreach(DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------
        // Info / Terms of Use reference
        //--------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Button for displaying of Info & Terms of Use
        /// </summary>
        private void BtInfo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var info = new Info();
                info.Show();
            }
            catch(Exception ex)
            {
                System.Windows.MessageBox.Show("Error occured while showing Information/License window. \r\n" + "Error message: " + ex.Message);
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------
        // Import of IFC-files
        //--------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Button for Import & Checking functionality
        /// </summary>
        private void Bt_Import(object sender, RoutedEventArgs e)
        {
            try
            {
                if(this.CheckObjList == null)  //default at start of program
                {
                    var importObj = new IO.IfcImport();

                    this.CheckObjList = importObj.CheckObjs; // new IO.IfcImport().ImportModels;
                    this.GroundWallObjects = importObj.GroundWallObjects;

                }
                else
                {
                    var addedObj = new IO.IfcImport();

                    var addCheckObjs = addedObj.CheckObjs;
                    var addGroundWalls = addedObj.GroundWallObjects;

                    try
                    {
                        foreach(var kp in addCheckObjs)
                        {
                            this.CheckObjList.Add(kp.Key, kp.Value);
                        }

                        foreach(var kp in addGroundWalls)
                        {
                            this.GroundWallObjects.Add(kp.Key, kp.Value);
                        }
                    }
                    catch(ArgumentException aex)
                    {
                        System.Windows.MessageBox.Show("It is not supported to import the same IfcModel again or with the same file name. Please rename Ifc-file and try again." + aex);
                    }

                    catch(Exception ex)
                    {
                        System.Windows.MessageBox.Show("Error while importing additional models." + ex);
                    }
                }

                foreach(string file in this.CheckObjList.Keys)
                {
                    if(importFiles.Items.Contains(file))
                    {
                        continue;
                    }
                    else
                    {
                        importFiles.Items.Add(file);
                    }

                    if(ifcModels.Items.Contains(file) == false)
                    {
                        ifcModels.Items.Add(file);
                    }
                }

                lb_importMsg.Content = this.CheckObjList.Count + " file(s) loaded";

                foreach(var checkObj in this.CheckObjList)
                {
                    if(check_log.IsChecked == true)
                    {
                        var log = new IO.LogOutput(checkObj.Value, direc + "\\IfcGeoRefChecker\\export\\" + NameFromPath(checkObj.Key), checkObj.Key);
                        bt_log.IsEnabled = true;
                    }

                    if(check_json.IsChecked == true)
                    {
                        var js = new IO.JsonOutput(checkObj.Value, direc + "\\IfcGeoRefChecker\\export\\" + NameFromPath(checkObj.Key));
                        bt_json.IsEnabled = true;
                    }
                }
            }

            catch(Exception ex)
            {
                System.Windows.MessageBox.Show("Error occured while Import. \r\n " + "Error message: " + ex.Message);
            }
            finally { }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------
        // Short results (true / false) regarding GeoRef concept
        //--------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Selection of imported Models
        /// </summary>
        private void ifcModels_SelectionChanged(object sender, SelectionChangedEventArgs e)

        {
            CheckObjList.TryGetValue(ifcModels.SelectedItem.ToString(), out var currentObj);

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
        }

        //--------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------
        // Display of log and json file
        //--------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Button for displaying of Log-file
        /// </summary>
        private void bt_log_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(this.direc + "\\IfcGeoRefChecker\\export\\" + NameFromPath(ifcModels.Text) + ".txt");
            }
            catch
            {
                System.Windows.MessageBox.Show("Error occured. Please check directory of your IFC-file for the corresponding GeoRef log file.");
            }
        }

        /// <summary>
        /// Button for displaying of Json-file
        /// </summary>
        private void bt_json_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //var pos = this.ModelList[ifcModels.Text].Header.FileName.Name.LastIndexOf("\\");
                //var directory = this.ModelList[ifcModels.Text].Header.FileName.Name.Substring(0, pos + 1);

                System.Diagnostics.Process.Start(this.direc + "\\IfcGeoRefChecker\\export\\" + NameFromPath(ifcModels.Text) + ".json");
            }
            catch
            {
                System.Windows.MessageBox.Show("Error occured. Please check directory of your IFC-file for the corresponding GeoRef JSON-file");
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------
        // Compare funtionality
        //--------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Button for starting of Compare funtionality
        /// </summary>
        private void bt_comparer_Click(object sender, RoutedEventArgs e)
        {
            if(this.CheckObjList != null && this.CheckObjList.Count > 1)
            {
                var comp = new Compare(this.direc, this.CheckObjList);
                comp.Show();
            }
            else
            {
                System.Windows.MessageBox.Show("Please import at least 2 Ifc-files for comparison.");
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------
        // Termination
        //--------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Button for termination of program
        /// </summary>
        private void bt_quit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();

            //this.Close();
        }

        //--------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------
        // Link to documentation
        //--------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Button for opening of documentation HTML-file
        /// </summary>
        private void bt_help_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(@"Documentation.html");
            }
            catch
            {
                System.Windows.MessageBox.Show("No help file available. Please check application directory for file Documentation.html");
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------
        // Update functionality
        //--------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Button for starting of manually updating Georef
        /// </summary>
        private void bt_update_man_Click(object sender, RoutedEventArgs e)
        {
            CheckObjList.TryGetValue(ifcModels.Text, out var checkObj);

            var jsonPath = direc + "\\IfcGeoRefChecker\\buildingLocator\\json\\" + NameFromPath(ifcModels.Text);
            var jsonout = new IO.JsonOutput(checkObj, jsonPath);

            var manExp = new UpdateMan(jsonPath);
            manExp.Show();
        }

        /// <summary>
        /// Button for starting of updating Georef via map browser window
        /// </summary>
        private void bt_update_map_Click(object sender, RoutedEventArgs e)
        {
            GroundWallObjects.TryGetValue(ifcModels.Text, out var groundWalls);
            CheckObjList.TryGetValue(ifcModels.Text, out var checkObj);

            var unit = checkObj.LengthUnit;

            var a = groundWalls.Count();

            var wkt = new Appl.BldgFootprintExtraxtor().CalcBuildingFootprint(groundWalls, unit);

            checkObj.WKTRep = wkt;

            var jsonWkt = new IO.JsonOutput(checkObj, this.direc + "\\IfcGeoRefChecker\\buildingLocator\\json\\map");

            try
            {
                System.Diagnostics.Process.Start(this.direc + "\\IfcGeoRefChecker\\buildingLocator\\index.html");
            }
            catch
            {
                MessageBox.Show("No html-map file available. Please check application directory for file buildingLocator.index.html");
            }
        }

        /// <summary>
        /// Button for starting of updating IFC-file
        /// </summary>
        private void bt_update_ifc_Click(object sender, RoutedEventArgs e)
        {
            var showExport2IFC = new Export2IFC(this.direc, ifcModels.Text, NameFromPath(ifcModels.Text));
            showExport2IFC.Show();
        }

        private string NameFromPath(string filePath)
        {
            var splits = filePath.Split('\\');

            return splits[splits.Length - 1];
        }
    }
}