using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using Xbim.Ifc;

namespace IfcGeoRefChecker.IO
{
    public class IfcImport
    {
        public Dictionary<string, IfcStore> ImportModels { get; set; }

        public List<string> FilePath { get; set; }

        private string fileName;

        public IfcImport()
        {
            try
            {
                var fd = new OpenFileDialog();

                fd.Filter = "IFC-files (*.ifc)|*.ifc|All Files (*.*)|*.*";
                fd.Multiselect = true;

                fd.ShowDialog();

                this.ImportModels = new Dictionary<string, IfcStore>();

                for(int i = 0; i < fd.FileNames.Length; i++)
                {
                    this.fileName = Path.GetFileNameWithoutExtension(fd.FileNames[i]);

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
                        var model = IfcStore.Open(fd.FileNames[i], editor);
                        this.ImportModels.Add(fileName, model);
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
    }
}