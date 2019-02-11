using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IfcGeoRefChecker.IO
{
    public class IfcReader
    {
        private IfcStore model;
        
        public IfcReader(IfcStore model)
        {
            this.model = model;
        }

        public List<IIfcSite> SiteReader()
        {
            return model.Instances.OfType<IIfcSite>().ToList();
        }

        public List<IIfcBuilding> BldgReader()
        {
            return model.Instances.OfType<IIfcBuilding>().ToList();
        }

        public List<IIfcGeometricRepresentationContext> ContextReader()
        {
            var allCtx = model.Instances.OfType<IIfcGeometricRepresentationContext>();

            var noSubCtx = allCtx.Where(ctx => ctx.ExpressType.ToString() != "IfcGeometricRepresentationSubContext"); //avoid subs (unneccessary overhead)
            
            return noSubCtx.ToList();
        }

        public string LengthUnitReader()
        {
            return model.Instances.OfType<IIfcUnitAssignment>().Select(u => u.Units).First().OfType<IIfcNamedUnit>()
                .Where(s => s.UnitType.ToString().Equals("LENGTHUNIT")).Select(un => un.Symbol).Single();
        }

        public List<IIfcProduct> UpperPlcmProdReader()
        {
            return model.Instances.Where<IIfcLocalPlacement>(e => e.PlacementRelTo == null)
                    .SelectMany(e => e.PlacesObject).ToList();
        }

        public IEnumerable<IIfcBuildingElement> GroundFloorWallReader(IIfcBuilding bldg)
        {
            var bldgRefHeight = (bldg.ElevationOfRefHeight != null) ? (double)bldg.ElevationOfRefHeight : 0.0;      //Selektion BuildingRefHeight (wenn NULL -> 0.0)

            var storeys = bldg.BuildingStoreys;                                                                     //alle Stockwerke

            var dictStorey = new Dictionary<IIfcBuildingStorey, double>();

            foreach(var storey in storeys)
            {
                dictStorey.Add(storey, Math.Abs(bldgRefHeight - (double)storey.Elevation));                         //für jedes Stockwerk Differenz zur BuildingrefHeight ermitteln
            }

            var minVal = dictStorey.Values.Min();
            var groundStorey = dictStorey.Where(s => s.Value == minVal).Select(s => s.Key).FirstOrDefault();        //Auswahl Stockwerk, wo Differenz minimal ist (mutmaßlich Erdgeschoss)

            var walls = model.Instances.OfType<IIfcBuildingElement>()                                               //Selektion aller IfcWalls im Erdgeschoss
                .Where(s => s is IIfcWall /*|| s is IIfcCurtainWall*/)                                                  //TO DO: IfcCurtainWall berücksichtigen (besteht im Bsp Burogebäude aus IfcPlate....)
                .Where(b => b.IsContainedIn == groundStorey);                                                           //evtl. TO DO: IfcWall-Objekte im Außenbereich ausschließen?

            return walls;
        }

    
    }
}
