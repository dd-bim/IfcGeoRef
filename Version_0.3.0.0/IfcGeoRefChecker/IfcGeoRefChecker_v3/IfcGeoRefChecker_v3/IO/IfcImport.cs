using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
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
                var fd = new Microsoft.Win32.OpenFileDialog();

                fd.Filter = "IFC-files (*.ifc)|*.ifc|All Files (*.*)|*.*";
                fd.Multiselect = true;

                fd.ShowDialog();

                for(int i = 0; i < fd.FileNames.Length; i++)
                {
                    this.fileName = Path.ChangeExtension(fd.FileNames[i], null);

                    var editor = new XbimEditorCredentials
                    {
                        ApplicationDevelopersName = "HTW Dresden",
                        ApplicationFullName = "IfcGeoRefChecker",
                        ApplicationIdentifier = "IfcGeoRef",
                        ApplicationVersion = "0.2.0.0",
                        EditorsFamilyName = Environment.UserName,
                    };

                    try
                    {
                        using(var model = IfcStore.Open(fd.FileNames[i], editor))
                        {
                            //this.ImportModels.Add(fileName, model);

                            var checkObj = new Appl.GeoRefChecker(model);
                            this.CheckObjs.Add(fileName, checkObj);

                            var reader = new IfcReader(model);
                            var bldgs = reader.BldgReader();
                            var groundWalls = reader.GroundFloorWallReader(bldgs[0]).ToList();   //nur Wände des ersten Gebäudes derzeit in scope

                            this.GroundWallObjects.Add(fileName, groundWalls);
                        }
                    }
                    catch(Exception e)
                    {
                        MessageBox.Show("Failed to import " + fileName + ".ifc \r\nError message: " + e.Message +
    "\r\n \r\n Xbim is not able to import the selected IfcModel." +
    "\r\n Please check your IfcFile for bad syntax errors.");
                    }
                }
            }
            catch
            {
            }
        }

        //public void CloseIfcStore(List<IfcStore> ifcModels)
        //{
        //    foreach(var m in ifcModels)
        //    {
        //        m.Close();
        //    }
        //}
    }
}