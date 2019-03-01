using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IfcGeoRefChecker.IO
{
    public class IfcReader
    {
        private IfcStore model;

        /// <summary>
        /// Initialized IfcReader with Xbim IfcStore
        /// </summary>
        public IfcReader(IfcStore model)
        {
            this.model = model;

            Log.Information("IFC reader initialized...");
        }

        /// <summary>
        /// Reads all IfcProject-Entities (should be one for valid IFC-file)
        /// </summary>
        public IIfcProject ProjReader()
        {
            var res = model.Instances.OfType<IIfcProject>().ToList();

            Log.Information("IFC reader: Projects found = " + res.Count);

            if(res.Count == 0)
                Log.Error("IFC reader: No IfcProject present. IFC file is NOT valid.");

            if(res.Count > 1)
                Log.Error("IFC reader: More than one IfcProject present. IFC file is NOT valid.");

            return res.FirstOrDefault();
        }

        /// <summary>
        /// Reads all IfcSite-Entities (should be one for correct IFC-implementation of exproting BIM-software)
        /// </summary>
        public IIfcSite SiteReader()
        {
            var res = model.Instances.OfType<IIfcSite>().ToList();

            Log.Information("IFC reader: Sites found = " + res.Count);

            if(res.Count == 0)
                Log.Warning("IFC reader: No IfcSite present. IFC file is valid but violated implementation agreements. Some LoGeoRef could not be checked.");

            if(res.Count > 1)
                Log.Warning("IFC reader: More than one IfcSite present. This application supports currently only one (the first) site.");

            return res.FirstOrDefault();
        }

        /// <summary>
        /// Reads all IfcBuilding-Entities (mostly one, sometimes more but not handled in this application)
        /// </summary>
        public IIfcBuilding BldgReader()
        {
            var res = model.Instances.OfType<IIfcBuilding>().ToList();

            Log.Information("IFC reader: Buildings found = " + res.Count());

            if(res.Count == 0)
                Log.Error("IFC reader: No IfcBuilding present. IFC file is valid but violated implementation agreements.");

            if(res.Count > 1)
                Log.Warning("IFC reader: More than one IfcBuilding present. This application supports currently only one (the first) building.");

            return res.FirstOrDefault();
        }

        /// <summary>
        /// Reads all IfcGeometricRepresentationContext (should be one 1 (model view) or 2 (model and plan view) for valid IFC-file)
        /// </summary>
        public List<IIfcGeometricRepresentationContext> ContextReader(IIfcProject proj)
        {
            var allCtx = proj.RepresentationContexts.OfType<IIfcGeometricRepresentationContext>();  //includes also inherited SubContexts (not necessary for this application)

            var noSubCtx = allCtx.Where(ctx => ctx.ExpressType.ToString() != "IfcGeometricRepresentationSubContext").ToList(); //avoid subs (unneccessary overhead)

            Log.Information("IFC reader: GeometricRepresentationContext found = " + noSubCtx.Count());

            if(noSubCtx.Count == 0)
                Log.Error("IFC reader: No IfcGeometricRepresentationContext present. IFC file is NOT valid.");

            if(noSubCtx.Count > 2)
                Log.Error("IFC reader: More than two IfcGeometricRepresentationContext present. IFC file is NOT valid.");

            return noSubCtx;
        }

        /// <summary>
        /// Reads all IfcMapConversion associated to IfcProject via context (should be not more than one 1 for valid IFC-file)
        /// </summary>
        public IIfcMapConversion MapReader(IIfcGeometricRepresentationContext ctx)
        {
            var map = model.Instances.OfType<IIfcMapConversion>().Where(m => m.SourceCRS == ctx).ToList();

            Log.Information("IFC reader: MapConversion with connection to project found = " + map.Count());

            if(map.Count > 1)
                Log.Error("IFC reader: More than one IfcMapConversion connected to IfcProject present. IFC file is valid but violated implementation agreements.");

            return map.FirstOrDefault();
        }

        /// <summary>
        /// Reads PropertySet with ProjectedCRS in its name (maybe present when no IfcProjectedCRS is available)
        /// </summary>
        public IIfcPropertySet PSetReaderCRS()
        {
            var pset = model.Instances.OfType<IIfcPropertySet>()
                .Where(p => p.Name.ToString().Contains("ProjectedCRS")).ToList();

            Log.Information("IFC reader: PsetCRS found = " + pset.Count);

            if(pset.Count > 1)
                Log.Warning("IFC reader: More than one PropertySet for ProjectedCRS present. IFC file is valid but information could be redundant.");

            return pset.FirstOrDefault();
        }

        /// <summary>
        /// Reads PropertySet with MapConversion in its name (maybe present when no IfcMapConversion is available)
        /// </summary>
        public IIfcPropertySet PSetReaderMap()
        {
            var pset = model.Instances.OfType<IIfcPropertySet>()
                .Where(p => p.Name.ToString().Contains("MapConversion")).ToList();

            Log.Information("IFC reader: PsetMapConversion found = " + pset.Count());

            if(pset.Count > 1)
                Log.Warning("IFC reader: More than one PropertySet for MapConversion present. IFC file is valid but information could be redundant.");

            return pset.FirstOrDefault();
        }

        /// <summary>
        /// Reads Project Length Unit associsated to the projects unit assignment (sholud be one for valid file)
        /// </summary>
        public string LengthUnitReader()
        {
            try
            {
                var unit = model.Instances.OfType<IIfcUnitAssignment>().Select(u => u.Units).First().OfType<IIfcNamedUnit>()
                    .Where(s => s.UnitType.ToString().Equals("LENGTHUNIT")).Select(un => un.Symbol).Single();

                Log.Information("IFC Reader: LengthUnit = " + unit);

                return unit;
            }
            catch
            {
                Log.Error("IFC Reader: no or more than one LengthUnit was found. IFC-file is NOT valid. Return metre as unit.");
                return "m";
            }
        }

        /// <summary>
        /// Reads Products with global placement (at least one, mostly IfcSite, is necessary)
        /// </summary>
        public List<IIfcProduct> UpperPlcmProdReader()
        {
            var res = model.Instances.Where<IIfcLocalPlacement>(e => e.PlacementRelTo == null)
                    .SelectMany(e => e.PlacesObject).ToList();

            Log.Information("IFC reader: Objects with global placement found = " + res.Count());

            if(res.Count == 0)
                Log.Error("IFC reader: No products with global placement present. IFC file is NOT valid.");

            return res;
        }

        /// <summary>
        /// Reads walls and calculates ground floor walls out of building and building storey height attributes
        /// </summary>
        public IEnumerable<IIfcBuildingElement> GroundFloorWallReader(IIfcBuilding bldg)
        {
            Log.Information("IFC reader: Ground floor wall detecting...");

            var bldgRefHeight = (bldg.ElevationOfRefHeight != null) ? (double)bldg.ElevationOfRefHeight : 0.0;      //Selektion BuildingRefHeight (wenn NULL -> 0.0)

            Log.Information("IFC reader: Building Ref Height = " + bldgRefHeight);

            var storeys = bldg.BuildingStoreys;                                                                     //alle Stockwerke

            Log.Information("IFC reader: Number of Building storeys = " + storeys.Count());

            var dictStorey = new Dictionary<IIfcBuildingStorey, double>();

            foreach(var storey in storeys)
            {
                var delta = Math.Abs(bldgRefHeight - (double)storey.Elevation);

                dictStorey.Add(storey, delta);                         //für jedes Stockwerk Differenz zur BuildingrefHeight ermitteln

                Log.Information("IFC reader: Height difference between storey and Building Ref Height = " + delta);
            }

            var minVal = dictStorey.Values.Min();
            var groundStorey = dictStorey.Where(s => s.Value == minVal).Select(s => s.Key).FirstOrDefault();        //Auswahl Stockwerk, wo Differenz minimal ist (mutmaßlich Erdgeschoss)

            var walls = model.Instances.OfType<IIfcBuildingElement>()                                               //Selektion aller IfcWalls im Erdgeschoss
                .Where(s => s is IIfcWall /*|| s is IIfcCurtainWall*/)                                                  //TO DO: IfcCurtainWall berücksichtigen (besteht im Bsp Burogebäude aus IfcPlate....)
                .Where(b => b.IsContainedIn == groundStorey);                                                           //evtl. TO DO: IfcWall-Objekte im Außenbereich ausschließen?

            Log.Information("IFC reader: Ground floor walls found = " + walls.Count());

            return walls;
        }
    }
}