using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using IfcGeometryExtractor;
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

                Mouse.OverrideCursor = Cursors.Wait;

                try
                {
                    for(int i = 0; i < fd.FileNames.Length; i++)
                    {
                        importProcess(fd.FileNames[i]);
                    }
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                }
            }
            catch(IOException exIO)
            {
                Log.Error("Not able to open file dialog. Error: " + exIO);
                MessageBox.Show("Not able to open file dialog. Error: " + exIO);
            }
        }

        public IfcImport(InputGroup iGroup)
        {
            for(int i = 0; i < iGroup.InputObjects.Count; i++)
            {
                importProcess(iGroup.InputObjects[i].Filename);
            }
        }

        private void importProcess(string filename)
        {
            var filePath = Path.ChangeExtension(filename, null);
            var fileExtensionless = Path.GetFileNameWithoutExtension(filename);

            try
            {
                Log.Information("Import of " + filename);

                using (var model = IfcStore.Open(filename))
                {
                    Log.Information("Start GeoRef-Check for " + filename);

                    this.NamePathDict.Add(fileExtensionless, filePath);

                    var checkObj = new Appl.GeoRefChecker(model);
                    this.CheckObjs.Add(fileExtensionless, checkObj);

                    Log.Information("Start calculating of ground floor walls for " + filename);

                    try
                    {
                        var reader = new IfcReader(model);
                        var bldg = reader.BldgReader();

                        var ifc = new Extraction(model);
                        var calc = new Calculation();
                        var elems = new List<IIfcBuildingElement>();

                        //Extrahiere alle Slabs, die im (vermutlich) Erdgeschoss liegen
                        var slabs = ifc.GetSlabsOnGround();

                        //-------------------------------------
                        //Untersuchung der in IFC-Datei vorhandenen Geometrie-Repräsentationen für die slabs
                        //wenn einer der GeometrieTypen je Slab unterstützt wird, werden slabs berechnet
                        //wenn nicht, werden die Wände untersucht --> diese sollten je Wand mindestens eine unterstützte Axis-Geometrie enthalten
                        //wenn weder Slabs noch Walls vorhanden sind, untersuche IFC auf Proxys
                        if (slabs.Any() && calc.CompareSupportedRepTypes(slabs))
                        {
                            elems = slabs.ToList();
                        }
                        else
                        {
                            var walls = ifc.GetWallsOnGround();

                            if (walls.Any() && calc.CompareSupportedRepTypes(walls))
                            {
                                elems = walls.ToList();
                            }
                            else
                            {
                                elems = ifc.GetProxysOnGround().ToList();
                            }
                        }
                        this.GroundWallObjects.Add(fileExtensionless, elems);
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show("No building elements found. Trying to proceed without. Problem:" + ex);
                    }

                }
            }
            catch (FileLoadException exL)
            {
                var exStr = "Import of " + filename + " failed. Error: " + exL.Message;

                Log.Error(exStr);
                MessageBox.Show(exStr);
                Log.Information(filename + " will be ignored.");
            }
        }
    }
}