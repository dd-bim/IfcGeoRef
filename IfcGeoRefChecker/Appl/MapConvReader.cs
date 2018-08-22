using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IfcGeoRefChecker.Appl
{
    public class MapConvReader
    {
        public IList<IIfcMapConversion> MapList { get; set; }

        public MapConvReader(IfcStore model)
        {
            this.MapList = model.Instances.OfType<IIfcMapConversion>().ToList();
        }
    }
}