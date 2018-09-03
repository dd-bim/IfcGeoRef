using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IfcGeoRefChecker.Appl
{
    public class UpperPlcmReader
    {
        public IList<IIfcProduct> ProdList { get; set; }

        public UpperPlcmReader(IfcStore model)
        {
            this.ProdList = model.Instances.Where<IIfcLocalPlacement>(e => e.PlacementRelTo == null)
                    .SelectMany(e => e.PlacesObject).ToList();
        }
    }
}