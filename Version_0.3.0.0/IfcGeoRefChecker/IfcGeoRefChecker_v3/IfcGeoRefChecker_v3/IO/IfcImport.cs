using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Serilog;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IfcGeoRefChecker.IO
{
    public class IfcImport
    {
        public Dictionary<string, Appl.GeoRefChecker> CheckObjs { get; set; } = new Dictionary<string, Appl.GeoRefChecker>();
        public Dictionary<string, IList<IIfcBuildingElement>> GroundWallObjects { get; set; } = new Dictionary<string, IList<IIfcBuildingElement>>();
        public Dictionary<string, string> NamePathDict { get; set; } = new Dictionary<string, string>();

        //private string fileName;

        public IfcImport(string direc)
        {
            try
            {
                Log.Information("Start of file import.");

                var fd = new Microsoft.Win32.OpenFileDialog();

                fd.InitialDirectory = direc;
                fd.Filter = "IFC-files (*.ifc)|*.ifc|All Files (*.*)|*.*";
                fd.Multiselect = true;

                fd.ShowDialog();

                Log.Debug("Selected files for Import: " + fd.FileNames.Length);

                for(int i = 0; i < fd.FileNames.Length; i++)
                {
                    var filePath = Path.ChangeExtension(fd.FileNames[i], null);
                    var fileName = Path.GetFileNameWithoutExtension(fd.FileNames[i]);

                    try
                    {
                        Log.Information("Import of " + fd.FileNames[i]);

                        using(var model = IfcStore.Open(fd.FileNames[i]))
                        {
                            Log.Information("Start GeoRef-Check for " + fd.FileNames[i]);

                            this.NamePathDict.Add(fileName, filePath);

                            var checkObj = new Appl.GeoRefChecker(model);
                            this.CheckObjs.Add(fileName, checkObj);

                            Log.Information("Start calculating of ground floor walls for " + fd.FileNames[i]);

                            var reader = new IfcReader(model);
                            var bldg = reader.BldgReader();
                            var groundWalls = reader.GroundFloorWallReader(bldg).ToList();   //nur Wände des ersten Gebäudes derzeit in scope

                            this.GroundWallObjects.Add(fileName, groundWalls);
                        }
                    }
                    catch(FileLoadException exL)
                    {
                        var exStr = "Import of " + fd.FileNames[i] + " failed. Error: " + exL.Message;

                        Log.Error(exStr);
                        MessageBox.Show(exStr);
                        Log.Information(fd.FileNames[i] + " will be ignored.");
                    }
                }
            }
            catch(IOException exIO)
            {
                Log.Error("Not able to open file dialog. Error: " + exIO);
                MessageBox.Show("Not able to open file dialog. Error: " + exIO);
            }
        }
    }
}