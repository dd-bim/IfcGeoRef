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
                //Log.Logger = new LoggerConfiguration()
                //    .WriteTo.File(this.direc, rollingInterval: RollingInterval.Day)
                //    //.MinimumLevel.Debug()
                //    .CreateLogger();

                //Log.Logger = new LoggerConfiguration()
                //    .WriteTo.File("C:\\Users\\goerne\\Desktop\\logtest\\log.txt", rollingInterval: RollingInterval.Day)
                //    //.MinimumLevel.Debug()
                //    .CreateLogger();

                //Log.Information("Start of IfcGeoRefChecker");

                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                InitializeComponent();

                tb_direc.Text = this.direc;

                bt_log.IsEnabled = false;
                bt_json.IsEnabled = false;
            }

            catch (Exception ex)
            {
                //Log.Error("Start of IfcGeoRefChecker failed. Error message: " + ex);
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
                //Log.Information("Start of Checking Georef...");
                //Log.Debug("Files loaded: " + this.CheckObjList.Count);

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
                        //Log.Error(exStr + aex.Message);
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
                    var path = direc + checkObj.Key;

                    if (check_log.IsChecked == true)
                    {
                        try
                        {
                            //Log.Information("Export checking-log...");

                            var log = new LogOutput(checkObj.Value, path, checkObj.Key);
                            bt_log.IsEnabled = true;

                            //Log.Information("Export successful to: " + path);
                        }
                        catch (IOException exIO)
                        {
                            //Log.Error("Not able to export log. Error: " + exIO);
                        }
                    }

                    if (check_json.IsChecked == true)
                    {
                        try
                        {
                            //Log.Information("Export JSON-file...");

                            var js = new JsonOutput();
                            js.JsonOutputFile(checkObj.Value, path);
                            bt_json.IsEnabled = true;

                            //Log.Information("Export successful to: " + path);
                        }
                        catch (IOException exIO)
                        {
                            //Log.Error("Not able to export json. Error: " + exIO);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //Log.Error("Unknown error occured. Error: " + ex.Message);
                System.Windows.MessageBox.Show("Unknown error occured. Error: " + ex.Message);
            }
        }
    }
}
