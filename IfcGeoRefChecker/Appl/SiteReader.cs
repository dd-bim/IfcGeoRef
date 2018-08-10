using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IfcGeoRefChecker.Appl
{
    public class SiteReader
    {
        public IList<IIfcSite> SiteList { get; set; }

        public SiteReader(IfcStore model)
        {
            this.SiteList = model.Instances.OfType<IIfcSite>().ToList();
        }
    }
}