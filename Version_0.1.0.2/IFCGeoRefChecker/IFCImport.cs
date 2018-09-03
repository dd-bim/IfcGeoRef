using System.IO;
using System.Windows.Forms;
using Xbim.Ifc;

namespace IFCGeoRefChecker
{
    public class Import
    {
        public Import()
        {
            try
            {
                var fd = new OpenFileDialog
                {
                    Filter = "IFC-files (*.ifc)|*.ifc|All Files (*.*)|*.*"
                };
                fd.ShowDialog();

                this.IfcModel = IfcStore.Open(fd.FileName);

                this.IfcFile = Path.GetFileNameWithoutExtension(fd.FileName);
            }
            catch
            {
            }
        }

        public IfcStore IfcModel { get; set; }
        public string IfcFile { get; set; }
    }
}
