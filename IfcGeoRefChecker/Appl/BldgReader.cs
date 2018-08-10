using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IfcGeoRefChecker.Appl
{
    //reads all buildings in the committed IfcModel

    public class BldgReader
    {
        public IList<IIfcBuilding> BldgList { get; set; }

        public BldgReader(IfcStore model)
        {
            this.BldgList = model.Instances.OfType<IIfcBuilding>().ToList();
        }
    }
}