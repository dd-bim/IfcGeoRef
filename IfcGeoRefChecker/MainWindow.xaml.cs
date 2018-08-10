using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Xbim.Ifc;

namespace IfcGeoRefChecker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dictionary<string, IfcStore> ModelList;
        private Dictionary<string, Dictionary<string, bool>> Dict;

        public MainWindow()
        {
            InitializeComponent();

            bt_log.IsEnabled = false;
            bt_json.IsEnabled = false;
            bt_update.IsEnabled = false;
        }

        private void BtInfo_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Noch hinzufügen");
        }

        private void Bt_Import(object sender, RoutedEventArgs e)
        {
            this.ModelList = new IO.IfcImport().ImportModels;

            foreach(string file in this.ModelList.Keys)
            {
                importFiles.Items.Add(file);
            }


        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void CheckGeoRef_Click(object sender, RoutedEventArgs e)
        {
            this.Dict = new Dictionary<string, Dictionary<string, bool>>();
            var dictBool = new Dictionary<string, bool>();

            var logOutput = new List<string>();

            var boolList10 = new List<bool>();
            var boolList20 = new List<bool>();
            var boolList30 = new List<bool>();

            var output = new IO.LogOutput();
            var jsonout = new IO.JsonOutput();

            foreach(var kvpModel in this.ModelList)
            {
                var file = kvpModel.Key;
                var model = kvpModel.Value;

                string dashline = "\r\n----------------------------------------------------------------------------------------------------------------------------------------";
                logOutput.Add($"\r\nExamination of {file}.ifc regarding georeferencing content ({DateTime.Now.ToShortDateString()}, {DateTime.Now.ToLongTimeString()})" + dashline + dashline + "\r\n");

                ifcModels.Items.Add(file);

                var siteReading = new Appl.SiteReader(model);       //for Level 10 and 20
                var bldgReading = new Appl.BldgReader(model);       //for Level 10
                var prodReading = new Appl.UpperPlcmReader(model);  //for Level 30

                for(var i = 0; i < bldgReading.BldgList.Count; i++)
                {
                    var bNo = bldgReading.BldgList[i].GetHashCode().ToString();
                    var bNa = bldgReading.BldgList[i].GetType().Name;

                    var georef10 = new Appl.Level10(model, bNo, bNa);
                    georef10.GetLevel10();
                    MessageBox.Show(georef10.Instance_Object[0]);

                    jsonout.GetGeoRefElements10(georef10);
                    boolList10.Add(georef10.GeoRef10);
                    logOutput.Add(georef10.LogOutput());
                }

                for(var i = 0; i < siteReading.SiteList.Count; i++)
                {
                    var sNo = siteReading.SiteList[i].GetHashCode().ToString();
                    var sNa = siteReading.SiteList[i].GetType().Name;

                    var georef10 = new Appl.Level10(model, sNo, sNa);
                    georef10.GetLevel10();
                    jsonout.GetGeoRefElements10(georef10);
                    boolList10.Add(georef10.GeoRef10);
                    logOutput.Add(georef10.LogOutput());

                    var georef20 = new Appl.Level20(model, sNo);
                    georef20.GetLevel20();
                    jsonout.GetGeoRefElements20(georef20);
                    boolList20.Add(georef20.GeoRef20);
                    logOutput.Add(georef20.LogOutput());
                }

                for(var i = 0; i < prodReading.ProdList.Count; i++)
                {
                    var pNo = prodReading.ProdList[i].GetHashCode().ToString();
                    var pNa = prodReading.ProdList[i].GetType().Name;

                    var georef30 = new Appl.Level30(model, pNo, pNa);
                    georef30.GetLevel30();
                    jsonout.GetGeoRefElements30(georef30);
                    boolList30.Add(georef30.GeoRef30);
                    logOutput.Add(georef30.LogOutput());
                }

                var georef40 = new Appl.Level40(model);
                georef40.GetLevel40();
                logOutput.Add(georef40.LogOutput());

                var georef50 = new Appl.Level50(model);
                georef50.GetLevel50();
                logOutput.Add(georef50.LogOutput());

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

                dictBool.Add(file + "georef40", georef40.GeoRef40);
                dictBool.Add(file + "georef50", georef50.GeoRef50);

                this.Dict.Add(file, dictBool);

                output.WriteLogfile(logOutput, file);
                jsonout.WriteJSONfile(model, file);
                logOutput.Clear();

                bt_log.IsEnabled = true;
                bt_json.IsEnabled = true;
                bt_update.IsEnabled = true;
            }
            ReadBool();
        }

        private void ifcModels_SelectedIndexChanged(object sender, EventArgs e)

        {
            ReadBool();
        }

        public void ReadBool()
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

        private void bt_log_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(@".\results\GeoRef_" + ifcModels.Text + ".txt");
            }
            catch
            {
                MessageBox.Show("Error occured. Please check application directory for the folder \"results\" and the corresponding file.");
            }
        }

        private void bt_json_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(@".\results\GeoRef_" + ifcModels.Text + ".json");
            }
            catch
            {
                MessageBox.Show("Error occured. Please check application directory for the folder \"results\" and the corresponding json file.");
            }
        }

        private void bt_update_Click(object sender, RoutedEventArgs e)
        {
            var showResults = new Results(this.ModelList[ifcModels.Text]);
            showResults.Show();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Coming soon.");
        }
    }
}