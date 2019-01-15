using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using IfcGeoRefChecker.Appl;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        public string LengthUnit { get; set; }

        public string WKTRep { get; set; }

        public List<Level10> LoGeoRef10 { get; set; } = new List<Level10>();

        public List<Level20> LoGeoRef20 { get; set; } = new List<Level20>();

        public List<Level30> LoGeoRef30 { get; set; } = new List<Level30>();

        public List<Level40> LoGeoRef40 { get; set; } = new List<Level40>();

        public List<Level50> LoGeoRef50 { get; set; } = new List<Level50>();

        public string CreateJSON(IfcStore model)
        {
            try
            {
                var proj = model.Instances.FirstOrDefault<IIfcProject>();
                this.GlobalID = proj.GlobalId.ToString();

                this.IFCSchema = model.SchemaVersion.ToString();
                this.TimeCreation = model.Header.TimeStamp;
                this.TimeCheck = DateTime.UtcNow.ToString("s", System.Globalization.CultureInfo.InvariantCulture);      //UTC timestamp

                this.LengthUnit = new IfcReader(model).LengthUnitReader();

                //this.WKTRep = //Referenz zu WallExtractor-Output

                //Beispiel
                this.WKTRep = null;

                return JsonConvert.SerializeObject(this, Formatting.Indented);
            }

            catch(Exception ex)
            {
                MessageBox.Show($"Error occured while writing JSON-file. \r\n Message: {ex.Message}");
                return null;
            }
        }

        public string AddWKTtoJSON(string wkt, string jsonobj)
        {
            PopulateJson(jsonobj);

            this.WKTRep = wkt;

            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public void WriteJSONfile(string jsonObj, string file/*, string direc*/)
        {
            using(var writeJson = File.CreateText((/*direc + "\\" + */file + ".json")))
            {
                try
                {
                    writeJson.WriteLine(jsonObj);
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
            if(georef50 == null)
            {
                georef50.GeoRef50 = false;
            }

            this.LoGeoRef50.Add(georef50);
        }

        public void ReadJson()  //später hier löschen und nur in GUI aufrufen
        {
            var fd = new OpenFileDialog();

            fd.Filter = "JSON-files (*.json)|*.json|All Files (*.*)|*.*";
            fd.Multiselect = false;

            fd.ShowDialog();

            PopulateJson(File.ReadAllText(fd.FileName));
        }

        public void PopulateJson(string file)
        {
            try
            {
                JObject jsonObj = JObject.Parse(file);

                this.GlobalID = jsonObj["GlobalID"].ToString();
                this.IFCSchema = jsonObj["IFCSchema"].ToString();
                this.TimeCheck = jsonObj["TimeCheck"].ToString();
                this.TimeCreation = jsonObj["TimeCreation"].ToString();

                this.LengthUnit = jsonObj["LengthUnit"].ToString();

                var lev10 = jsonObj["LoGeoRef10"].Children();

                foreach(var res in lev10)
                {
                    var l10 = new Level10();
                    JsonConvert.PopulateObject(res.ToString(), l10);
                    this.LoGeoRef10.Add(l10);
                }

                var lev20 = jsonObj["LoGeoRef20"].Children();

                foreach(var res in lev20)
                {
                    var l20 = new Level20();
                    JsonConvert.PopulateObject(res.ToString(), l20);
                    this.LoGeoRef20.Add(l20);
                }

                var lev30 = jsonObj["LoGeoRef30"].Children();

                foreach(var res in lev30)
                {
                    var l30 = new Level30();
                    JsonConvert.PopulateObject(res.ToString(), l30);
                    this.LoGeoRef30.Add(l30);
                }

                var lev40 = jsonObj["LoGeoRef40"].Children();

                foreach(var res in lev40)
                {
                    var l40 = new Level40();
                    JsonConvert.PopulateObject(res.ToString(), l40);
                    this.LoGeoRef40.Add(l40);
                }

                var lev50 = jsonObj["LoGeoRef50"].Children();

                foreach(var res in lev40)
                {
                    var l50 = new Level50();
                    JsonConvert.PopulateObject(res.ToString(), l50);
                    this.LoGeoRef50.Add(l50);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }
    }
}