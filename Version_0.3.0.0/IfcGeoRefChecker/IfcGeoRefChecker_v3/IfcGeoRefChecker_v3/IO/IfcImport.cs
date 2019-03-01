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

        private string fileName;

        public IfcImport()
        {
            try
            {
                Log.Information("Start of file import.");

                var fd = new Microsoft.Win32.OpenFileDialog();

                fd.Filter = "IFC-files (*.ifc)|*.ifc|All Files (*.*)|*.*";
                fd.Multiselect = true;

                fd.ShowDialog();

                Log.Debug("Selected files for Import: " + fd.FileNames.Length);

                for(int i = 0; i < fd.FileNames.Length; i++)
                {
                    this.fileName = Path.ChangeExtension(fd.FileNames[i], null);

                    try
                    {
                        Log.Information("Import of " + fd.FileNames[i]);

                        using(var model = IfcStore.Open(fd.FileNames[i]))
                        {
                            Log.Information("Start GeoRef-Check for " + fd.FileNames[i]);

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
            catch (IOException exIO)
            {
                Log.Error("Not able to open file dialog. Error: " + exIO);
                MessageBox.Show("Not able to open file dialog. Error: " + exIO);
            }
        }
    }
}