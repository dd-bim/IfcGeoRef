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

                    var fileName = Path.GetFileNameWithoutExtension(fd.FileNames[i]);

                    var editor = new XbimEditorCredentials
                    {
                        ApplicationDevelopersName = "HTW Dresden",
                        ApplicationFullName = "IfcGeoRefChecker",
                        ApplicationIdentifier = "IfcGeoRef",
                        ApplicationVersion = "1.0",
                        EditorsFamilyName = Environment.UserName,
                    };

                    var model = IfcStore.Open(fd.FileNames[i], editor);

                    this.ImportModels.Add(fileName, model);
                }
            }
            catch(Exception e)
            {
                MessageBox.Show("fail" + e.Message + e.StackTrace);
            }
        }
    }
}