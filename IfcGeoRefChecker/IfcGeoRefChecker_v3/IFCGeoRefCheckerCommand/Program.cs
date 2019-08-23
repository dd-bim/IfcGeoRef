using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using IfcGeoRefChecker.Appl;
using Xbim.Ifc4.Interfaces;
using Newtonsoft.Json;
using IfcGeoRefChecker.IO;
using System.IO;

namespace IFCGeoRefCheckerCommand
{
    static class Program
    {
        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Dictionary<string, GeoRefChecker> CheckObjList = new Dictionary<string, GeoRefChecker>();
            Dictionary<string, IList<IIfcBuildingElement>> GroundWallObjects;
            Dictionary<string, string> NamePathDict;

            //string testpath = @"C:\Users\possner\Desktop\BeispieldatenGeoRefChecker\Input\InputTest.json";
            //string jPath = testpath;

            string jPath = args[0];
            string jText = System.IO.File.ReadAllText(jPath);

            InputGroup inputGroup = JsonConvert.DeserializeObject<InputGroup>(jText);
            
            var importObj = new IfcImport(inputGroup);

            NamePathDict = importObj.NamePathDict;
            CheckObjList = importObj.CheckObjs;
            GroundWallObjects = importObj.GroundWallObjects;

            foreach (var checkObj in CheckObjList)
            {
                string[] lDirectory = { inputGroup.outputDirectory, "IfcGeoRefChecker\\CheckExport" };
                Directory.CreateDirectory(System.IO.Path.Combine(lDirectory));

                string[] paths = { inputGroup.outputDirectory, "IfcGeoRefChecker\\CheckExport", checkObj.Key };
                var path = System.IO.Path.Combine(paths);
                
                if (inputGroup.outLog)
                {
                    try
                    {
                        //Log.Information("Export checking-log...");

                        var log = new LogOutput(checkObj.Value, path, checkObj.Key);

                        //Log.Information("Export successful to: " + path);
                    }
                    catch (Exception exIO)
                    {
                        //Log.Error("Not able to export log. Error: " + exIO);
                    }
                }

                if (inputGroup.outJson)
                {
                    try
                    {
                        //Log.Information("Export JSON-file...");

                        var js = new JsonOutput();
                        js.JsonOutputFile(checkObj.Value, path);

                        //Log.Information("Export successful to: " + path);
                    }
                    catch (Exception exIO)
                    {
                        //Log.Error("Not able to export json. Error: " + exIO);
                    }
                }
            } 
        }
    }
}
