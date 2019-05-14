﻿using System;
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
        private Dictionary<string, Appl.GeoRefChecker> CheckObjList = new Dictionary<string, Appl.GeoRefChecker>();
        private string direc = Environment.CurrentDirectory;
        private Dictionary<string, IList<IIfcBuildingElement>> GroundWallObjects;
        private Dictionary<string, string> NamePathDict;

        public MainWindow()
        {
            try
            {
                //Log.Logger = new LoggerConfiguration()
                //    .WriteTo.File(this.direc, rollingInterval: RollingInterval.Day)
                //    //.MinimumLevel.Debug()
                //    .CreateLogger();

                Log.Logger = new LoggerConfiguration()
                    .WriteTo.File("C:\\Users\\goerne\\Desktop\\logtest\\log.txt", rollingInterval: RollingInterval.Day)
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
            try
            {
                using(var fbd = new System.Windows.Forms.FolderBrowserDialog())
                {
                    Log.Information("Start of changing directory. Dialogue opened.");

                    fbd.RootFolder = Environment.SpecialFolder.Desktop;
                    fbd.Description = "Select folder";

                    fbd.ShowNewFolderButton = true;

                    var result = fbd.ShowDialog();

                    if(result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                    {
                        this.direc = fbd.SelectedPath;
                        Log.Information("Directory changed to " + this.direc);
                    }
                    else
                    {
                        Log.Information("Directory not changed.");
                    }
                }
            }
            catch(Exception ex)
            {
                Log.Error("Not able to change directory. Error: " + ex);
                Log.Information("Program will use the default path (install folder).");
            }

            tb_direc.Text = this.direc;

            //try
            //{
            //    if(!tb_direc.Text.Equals(this.direc))
            //    {
            //        //Log.Information("Copy required program files to new directory.");

            //        // Copy from the current directory, include subdirectories.
            //        //DirectoryCopy(@".\IfcGeoRefChecker", this.direc + "\\IfcGeoRefChecker\\", true);
            //    }
            //}
            //catch(Exception ex)
            //{
            //    Log.Error("Not able to copy files and subfolders to new directory. Error: " + ex);
            //    this.direc = Environment.CurrentDirectory;
            //    Log.Information("Program will use the default path (install folder).");
            //}
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
                Log.Information("Open Terms of Use window.");
                var info = new Info();
                info.Show();
            }
            catch(Exception ex)
            {
                Log.Error("Not able to open Terms of Use window. Error: " + ex);
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
                Log.Information("Start of Checking Georef...");
                Log.Debug("Files loaded: " + this.CheckObjList.Count);

                if(this.CheckObjList.Count == 0)  //default at start of program
                {
                    var importObj = new IO.IfcImport(this.direc);

                    this.NamePathDict = importObj.NamePathDict;
                    this.CheckObjList = importObj.CheckObjs;
                    this.GroundWallObjects = importObj.GroundWallObjects;
                }
                else
                {
                    var addedObj = new IO.IfcImport(this.direc);

                    var addCheckObjs = addedObj.CheckObjs;
                    var addGroundWalls = addedObj.GroundWallObjects;
                    var addedPath = addedObj.NamePathDict;

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

                        foreach(var kp in addedPath)
                        {
                            this.NamePathDict.Add(kp.Key, kp.Value);
                        }
                    }
                    catch(ArgumentException aex)
                    {
                        var exStr = "Ifc-file already imported.";
                        Log.Error(exStr + aex.Message);
                        MessageBox.Show(exStr);
                    }
                }

                foreach(string fileName in this.CheckObjList.Keys)
                {
                    if(importFiles.Items.Contains(fileName))
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

                foreach(var checkObj in this.CheckObjList)
                {
                    var path = direc + checkObj.Key;

                    if(check_log.IsChecked == true)
                    {
                        try
                        {
                            Log.Information("Export checking-log...");

                            var log = new IO.LogOutput(checkObj.Value, path, checkObj.Key);
                            bt_log.IsEnabled = true;

                            Log.Information("Export successful to: " + path);
                        }
                        catch(IOException exIO)
                        {
                            Log.Error("Not able to export log. Error: " + exIO);
                        }
                    }

                    if(check_json.IsChecked == true)
                    {
                        try
                        {
                            Log.Information("Export JSON-file...");

                            var js = new IO.JsonOutput();
                            js.JsonOutputFile(checkObj.Value, path);
                            bt_json.IsEnabled = true;

                            Log.Information("Export successful to: " + path);
                        }
                        catch(IOException exIO)
                        {
                            Log.Error("Not able to export json. Error: " + exIO);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Log.Error("Unknown error occured. Error: " + ex.Message);
                MessageBox.Show("Unknown error occured. Error: " + ex.Message);
            }
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
            if(this.CheckObjList.Count == 0)
            {
                Log.Information("Showing log not possible. No model imported.");
                MessageBox.Show("Please import at least 1 Ifc-file.");
            }
            else if(importFiles.SelectedItem == null)
            {
                Log.Information("Showing log not possible. No model selected.");
                MessageBox.Show("Please select the ifc file, which should be updated, in the box above.");
            }
            else
            {
                try
                {
                    System.Diagnostics.Process.Start(this.direc + importFiles.SelectedItem.ToString() + ".txt");
                    Log.Information("Checking log opened.");
                }
                catch(Exception ex)
                {
                    Log.Error("Not able to open log-file. Error: " + ex);
                    MessageBox.Show("Error occured. Please check directory of your IFC-file for the corresponding GeoRef log file." + ex);
                }
            }
        }

        /// <summary>
        /// Button for displaying of Json-file
        /// </summary>
        private void bt_json_Click(object sender, RoutedEventArgs e)
        {
            if(this.CheckObjList.Count == 0)
            {
                Log.Information("Showing JSON not possible. No model imported.");
                MessageBox.Show("Please import at least 1 Ifc-file.");
            }
            else if(importFiles.SelectedItem == null)
            {
                Log.Information("Showing JSON not possible. No model selected.");
                MessageBox.Show("Please select the ifc file, which should be updated, in the box above.");
            }
            else
            {
                try
                {
                    System.Diagnostics.Process.Start(this.direc + importFiles.SelectedItem.ToString() + ".json");
                    Log.Information("Checking json opened.");
                }
                catch(Exception ex)
                {
                    Log.Error("Not able to open JSON-file. Error: " + ex);
                    MessageBox.Show("Error occured. Please check directory of your IFC-file for the corresponding GeoRef JSON-file." + ex.Message);
                }
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
            try
            {
                if(this.CheckObjList != null && this.CheckObjList.Count > 1)
                {
                    var comp = new Compare(this.direc, this.CheckObjList);
                    comp.Show();

                    Log.Information("GeoRefComparer started.");
                }
                else
                {
                    Log.Information("Starting GeoRefComparer not possible. Not enough models imported.");
                    MessageBox.Show("Please import at least 2 Ifc-files for comparison.");
                }
            }
            catch(Exception ex)
            {
                Log.Error("Starting GeoRefComparer failed. Error: " + ex.Message);
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
            Log.Information("Application terminated.");
            Application.Current.Shutdown();
        }

        //--------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------
        // Link to documentation
        //--------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Button for opening of quick guide HTML-file
        /// </summary>
        private void bt_guide_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(@"Quick_Guide.html");
                Log.Information("Quick Guide HTML opened.");
            }
            catch(Exception ex)
            {
                Log.Error("Not able to open Documentation. Error: " + ex.Message);
                MessageBox.Show("Error occured. Please check directory for Quick_Guide HTML-file." + ex.Message);
            }
        }

        /// <summary>
        /// Button for opening of documentation HTML-file
        /// </summary>
        private void bt_docu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(@"Documentation.html");
                
                Log.Information("Documentation HTML opened.");
            }
            catch(Exception ex)
            {
                Log.Error("Not able to open Documentation. Error: " + ex.Message);
                MessageBox.Show("Error occured. Please check directory for Documentation HTML-file." + ex.Message);
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
            if(this.CheckObjList.Count == 0)
            {
                Log.Information("Starting Updating not possible. No model imported.");
                MessageBox.Show("Please import at least 1 Ifc-file.");
            }
            else if(importFiles.SelectedItem == null)
            {
                Log.Information("Starting Updating not possible. No model selected.");
                MessageBox.Show("Please select the ifc file, which should be updated, in the box above.");
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
                catch(Exception ex)
                {
                    var str = "Not able to start manual update process. Maybe necessary JSON-Export to local directory failed. Please check local directory! Error: " + ex.Message;

                    Log.Error(str);
                    MessageBox.Show(str);
                }
            }
        }

        /// <summary>
        /// Button for starting of updating Georef via map browser window
        /// </summary>
        private void bt_update_map_Click(object sender, RoutedEventArgs e)
        {
            if(this.CheckObjList.Count == 0)
            {
                Log.Information("Starting Updating not possible. No model imported.");
                MessageBox.Show("Please import at least 1 Ifc-file.");
            }
            else if(importFiles.SelectedItem == null)
            {
                Log.Information("Starting Updating not possible. No model selected.");
                MessageBox.Show("Please select the ifc file, which should be updated, in the box above.");
            }
            else
            {
                Log.Information("Updating via map started...");

                try
                {
                    GroundWallObjects.TryGetValue(importFiles.SelectedItem.ToString(), out var groundWalls);
                    CheckObjList.TryGetValue(importFiles.SelectedItem.ToString(), out var checkObj);

                    MessageBox.Show("Start of calculating the BuildingFootprint and writing it into json file.\r\n \r\n" +
                        "Please save the file and continue with the Building Locator in web browser.\r\n" +
                        "You will need Internet connection to display the required web map service.\r\n \r\n" +
                        "After changing the georef via map, please continue with step 2 \"Export Updates to IFC\" in this application. \r\n" +
                        "You will then need to import the updated JSON file which was exported by the Building Locator web tool.", "Important Information");

                    Log.Information("Calculate building perimeter...");

                    var unit = checkObj.LengthUnit;

                    var wkt = new Appl.BldgFootprintExtraxtor().CalcBuildingFootprint(groundWalls, unit);

                    Log.Information("Calculation finished.");

                    Log.Information("Write JSON-check file with WKTZ-string for perimeter to local 'buildingLocator\\json' directory...");

                    checkObj.WKTRep = wkt;

                    var jsonWkt = new IO.JsonOutput();
                    jsonWkt.JsonOutputDialog(checkObj, this.direc, importFiles.SelectedItem.ToString());

                    Log.Information("Done.");
                }
                catch(Exception ex)
                {
                    if(GroundWallObjects == null)
                    {
                        MessageBox.Show("Error: Not able to select GroundWalls. Please make sure, " +
                            "you checked the required file before and that your IFC file contains walls.");
                    }
                    else
                    {
                        MessageBox.Show("Error: Not able to calculate required building footprint. Message: " + ex.Message);
                    }
                }

                try
                {
                    Log.Information("Opening of HTML-Site for updating via map...");

                    System.Diagnostics.Process.Start(Environment.CurrentDirectory + "\\buildingLocator\\index.html");

                    Log.Information("Done.");
                }
                catch(Exception ex)
                {
                    var str = "No html-map file available. Please check local directory 'buildingLocator' for 'index.html'. Error: " + ex;

                    Log.Error(str);
                    MessageBox.Show(str);
                }
            }
        }

        /// <summary>
        /// Button for starting of updating IFC-file
        /// </summary>
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
            catch(Exception ex)
            {
                var str = "Not able to open Export window. Error: " + ex;

                Log.Error(str);
                MessageBox.Show(str);
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------
        // Short results (true / false) regarding GeoRef concept
        //--------------------------------------------------------------------------------------------------------------------------------------

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