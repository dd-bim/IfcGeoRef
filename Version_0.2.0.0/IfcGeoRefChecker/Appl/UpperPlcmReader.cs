using System.Collections.Generic;
using System.Linq;
using System.Windows;
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

        /*/ IFC conventions, refer IfcLocalPlacement:
         * IfcSite shall be placed absolutely within the world coordinate system established by the geometric representation context of the IfcProject
         * IfcBuilding shall be placed relative to the local placement of IfcSite
         * IfcBuildingStorey shall be placed relative to the local placement of IfcBuilding
         /*/


    }
}