using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using IfcGeoRefChecker.Appl;
using Newtonsoft.Json;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IfcGeoRefChecker.IO
{
    public class JsonOutput
    {
        public string GlobalID { get; set; }
        public string IFCSchema { get; set; }
        public string TimeCreation { get; set; }
        public string TimeCheck { get; set; }

        public List<Level10> LoGeoRef10 { get; set; } = new List<Level10>();

        public List<Level20> LoGeoRef20 { get; set; } = new List<Level20>();

        public List<Level30> LoGeoRef30 { get; set; } = new List<Level30>();

        public List<Level40> LoGeoRef40 { get; set; } = new List<Level40>();

        public List<Level50> LoGeoRef50 { get; set; } = new List<Level50>();

        public void WriteJSONfile(IfcStore model, string file, string direc)
        {
            using(var writeJson = File.CreateText((direc + "\\" + file + ".json")))
            {
                try
                {
                    var proj = model.Instances.FirstOrDefault<IIfcProject>();
                    this.GlobalID = proj.GlobalId.ToString();

                    this.IFCSchema = model.IfcSchemaVersion.ToString();
                    this.TimeCreation = model.Header.TimeStamp;
                    this.TimeCheck = DateTime.UtcNow.ToString("s", System.Globalization.CultureInfo.InvariantCulture);      //UTC timestamp

                    string jsonobj = JsonConvert.SerializeObject(this, Formatting.Indented);

                    writeJson.WriteLine(jsonobj);
                    this.LoGeoRef10.Clear();
                    this.LoGeoRef20.Clear();
                    this.LoGeoRef30.Clear();
                    this.LoGeoRef40.Clear();
                    this.LoGeoRef50.Clear();
                }

                catch(Exception ex)
                {
                    MessageBox.Show($"Error occured while writing JSON-file. \r\n Message: {ex.Message}");
                }
            }
        }

        public void GetGeoRefElements10(Level10 georef10)
        {
            if(georef10 == null)
            {
                georef10.GeoRef10 = false;
            }

            this.LoGeoRef10.Add(georef10);
        }

        public void GetGeoRefElements20(Level20 georef20)
        {
            if(georef20 == null)
            {
                georef20.GeoRef20 = false;
            }

            this.LoGeoRef20.Add(georef20);
        }

        public void GetGeoRefElements30(Level30 georef30)
        {
            if(georef30 == null)
            {
                georef30.GeoRef30 = false;
            }

            this.LoGeoRef30.Add(georef30);
        }

        public void GetGeoRefElements40(Level40 georef40)
        {
            if(georef40 == null)
            {
                georef40.GeoRef40 = false;
            }

            this.LoGeoRef40.Add(georef40);
        }

        public void GetGeoRefElements50(Level50 georef50)
        {
            if (georef50 == null)
            {
                georef50.GeoRef50 = false;
            }

            this.LoGeoRef50.Add(georef50);
        }
    }
}