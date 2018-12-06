using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BimGisCad.Representation.Geometry;
using BimGisCad.Representation.Geometry.Elementary;
using MIConvexHull;
using netDxf;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IfcGeometryExtractor
{
    internal class Program
    {
        //temporär zum Testen / Visualisieren:
        //---------------------------------------------------
        private static string nr;                                                       //Variable für IFC Modell - Auswahl

        private static DxfDocument dxf = new DxfDocument();                             //Variable für DXF-Datei (netDXF-Lib)

        //---------------------------------------------------
        //IFC-Modell
        private static IfcStore model;

        //-------------------------------

        //absolutes Bauwerkssystem (i.d.R. nicht global):
        private static Axis2Placement3D siteSystem;                                   //globales Placement der Wand (ohne Geometrie-Koords)

        //---------------------------------------------------

        private static List<LinePoints> wallLined = new List<LinePoints>();                         //Wandlinien für Schnittpunktberechnung mit Paaren der globalen Wandkoords
        private static List<LinePoints> wallLinedClean = new List<LinePoints>();
        private static List<LinePoints> cvxLinesClean = new List<LinePoints>();
        private static List<LinePoints> extWallLinesClean = new List<LinePoints>();
        private static IList<LinePoints> cvxOuterLines = new List<LinePoints>();
        private static List<LinePoints> extUniWalls = new List<LinePoints>();
        private static LinePoints firstLine;

        private static IList<Point2> realIntersecPts = new List<Point2>();

        private static IList<Point2> extPts = new List<Point2>();                  //Schnittpunkte (Ray-Wandachse)

        private static List<LinePoints> extWallLines = new List<LinePoints>();                   //Wandlinien, auf denen äußere Schnittpunkte liegen

        private static BboxIFC bbox;
        private static IList<RayBundle> bundleList = new List<RayBundle>();

        private static void Main(string[] args)
        {
            Console.WriteLine("Building geometry extractor");
            Console.WriteLine("Please choose a IFC model:");
            Console.WriteLine("...Schependomlaan --> 1");
            Console.WriteLine("...Haus_CG_ifc2x3_coordination_view --> 2");
            Console.WriteLine("...FZK-Haus --> 3 or default");
            Console.WriteLine("...Buerogebaeude --> 4");
            Console.WriteLine("...Haus-Constr --> 5");
            nr = Console.ReadLine();

            //Testmodelle:

            var source = "D:\\1_CityBIM\\1_Programmierung\\IfcGeoRef\\zzz_IFC_Testdateien\\";
            var path = source + "Projekt1.ifc";

            try
            {
                switch(nr)
                {
                    case "1":
                        path = source + "schependomlaan.-.IFC\\Design model IFC\\IFC_Schependomlaan.ifc";
                        break;

                    case "2":
                        path = source + "Haus_CG_ifc2x3_coordination_view.ifc";
                        break;

                    case "3":
                        path = source + "FZK_Haus.ifc";
                        break;

                    case "4":
                        path = source + "Buerogebauede.ifc";
                        break;

                    case "5":
                        path = source + "Haus-Constrerw.ifc";
                        break;

                    case "6":
                        path = source + "Haus-Constrtn25.ifc";
                        break;

                    case "7":
                        path = source + "Haus-Constr2intern.ifc";
                        break;

                    case "8":
                        path = source + "IFCSDLV4.ifc";
                        break;

                    default:
                        Console.WriteLine("Default model: Projekt1");
                        break;
                }
                Console.WriteLine();

                //Lade Modell

                model = IfcStore.Open(path);

                if(model != null)
                {
                    Console.WriteLine("Model successfully loaded.");
                }
                else
                {
                    Console.WriteLine("NO model loaded.");
                }
                Console.WriteLine();

                //Extrahiere alle Walls, die im (vermutlich) Erdgeschoss liegen
                var walls = GetWallsOnGround();

                Console.WriteLine("There are {0} walls at ground storey.", walls.Count());

                //Schleife für alle Wände:

                foreach(var singleWall in walls)
                {
                    //Ermitteln der Werte für Local Placement
                    var plcmt = singleWall.ObjectPlacement;

                    //derzeit nur Fall IfcLocalPlacement (Erweiterung für Grid,... nötig)
                    siteSystem = GetAbsolutePlacement(plcmt);                                            //globales Bauwerkssystem wird ermittelt

                    //Auslesen der Repräsentationstypen
                    //-----------------------------------

                    var repTypes = singleWall.Representation.Representations;

                    var repList = new List<IIfcRepresentation>();

                    var repBody = from rep in repTypes
                                  where rep.RepresentationIdentifier == "Body"
                                  select rep;

                    var wallDetec = GetBodyGeometry(repBody.FirstOrDefault());

                    if(wallDetec.Count > 0)
                    {
                        wallLined.AddRange(wallDetec);
                        Console.WriteLine("For " + singleWall.GetHashCode() + " " + wallDetec.Count + " walllines were detected.");

                        foreach(var wl in wallDetec)
                        {
                            var dX = wl.segmentA.X - wl.segmentB.X;
                            var dY = wl.segmentA.Y - wl.segmentB.Y;

                            if((dX == 0.0) && (dY == 0.0))
                            {
                                Console.WriteLine("Segmente Pkt gleich: " + wl.segmentA.X + " / " + wl.segmentA.Y);

                                Console.WriteLine(repBody.FirstOrDefault().RepresentationType.ToString());
                            }
                        }

                        Console.WriteLine(repBody.FirstOrDefault().RepresentationType.ToString());
                    }
                    else
                    {
                        var repAxis = from rep in repTypes
                                      where rep.RepresentationIdentifier == "Axis"
                                      select rep;

                        wallLined.Add(GetAxisGeometry(repAxis.FirstOrDefault()));
                        Console.WriteLine("For " + singleWall.GetHashCode() + " Body geometry detection was not successful. There will be an unprecise Axisline-Representation instead.");
                    }
                }

                //Ausgabe:
                //--------------------------------------------------------------------------
                Console.WriteLine("Gefundene Wandsegmente: " + wallLined.Count);
                Console.WriteLine();

                //-------------------Einheiten-------------------------

                var unit = model.Instances.OfType<IIfcUnitAssignment>().Select(u => u.Units).First().OfType<IIfcNamedUnit>()
                    .Where(s => s.UnitType.ToString().Equals("LENGTHUNIT")).Select(un => un.Symbol).Single();

                wallLined = ConvertToMeter(wallLined, unit);

                foreach(var wl in wallLined)
                {
                    var dX = wl.segmentA.X - wl.segmentB.X;
                    var dY = wl.segmentA.Y - wl.segmentB.Y;

                    if((dX == 0.0) && (dY == 0.0))
                        Console.WriteLine("Segmente Pkt gleich: " + wl.segmentA.X + " / " + wl.segmentA.Y);

                    WallToDXF(wl, "0_detectedWalls", DXFcolor.yellow);
                    PointsToDXF(wl.segmentA, "0_detectedWallPts", DXFcolor.yellow);
                    PointsToDXF(wl.segmentB, "0_detectedWallPts", DXFcolor.yellow);
                }
                //--------------------------------------------------------------------------

                Console.ReadKey();

                CleanUpWallLines(wallLined, "wall");

                Console.WriteLine("cleans= " + wallLinedClean.Count);

                foreach(var w in wallLinedClean)
                {
                    WallToDXF(w, "cleanLines", DXFcolor.red);

                    PointsToDXF(w.segmentA, "cleanPts", DXFcolor.red);
                    PointsToDXF(w.segmentB, "cleanPts", DXFcolor.red);
                }

                CompareCvxWalls();

                FindOuterIntersectionPts(wallLinedClean);

                CreateRealIntersecPts();

                foreach(var pt in realIntersecPts)
                {
                    PointsToDXF(pt, "RealIntersecs", DXFcolor.yellow);

                    Console.WriteLine(pt.X + " / " + pt.Y);

                    //"WKTRep": "POLYGON((10 10, 100 10, 100 80, 70 80, 70 30, 30 30, 30 55, 10 55, 10 10))",
                }

                ////var globalPts = GetGlobalPlacement(realIntersecPts);

                ////globalPts.Add(globalPts[0]); //geschlossener Ring

                //foreach(var pt in realIntersecPts)
                //{
                //    PointsToDXF(pt, "finalWKTpts", DXFcolor.red);

                //    Console.WriteLine(pt.X + " , " + pt.Y);
                //}

                dxf.Save(path + ".dxf");

                System.Diagnostics.Process.Start(path + ".dxf");

                Console.ReadKey();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);

                dxf.Save(path + ".dxf");

                System.Diagnostics.Process.Start(path + ".dxf");

                Console.ReadLine();
            }
        }

        //------------------------------------------------------------------------------------------------------------------------------------------
        //Methodendeklarationen
        //-----------------------
        //-----------------------

        public static List<LinePoints> ConvertToMeter(List<LinePoints> walls, string unit)
        {
            var wallLined = new List<LinePoints>();

            switch(unit)
            {
                case "m":
                    foreach(var w in walls)
                    {
                        var segA = Point2.Create(/*RoundCoords*/(w.segmentA.X), /*RoundCoords*/(w.segmentA.Y));
                        var segB = Point2.Create(/*RoundCoords*/(w.segmentB.X), /*RoundCoords*/(w.segmentB.Y));

                        var wall = new LinePoints();
                        wall.segmentA = segA;
                        wall.segmentB = segB;
                        Line2.Create(wall.segmentA, wall.segmentB, out var wallL);
                        wall.wallLine = wallL;
                        wallLined.Add(wall);
                    }
                    break;

                case "mm":

                    foreach(var w in walls)
                    {
                        var segA = Point2.Create(/*RoundCoords*/(w.segmentA.X / 1000), /*RoundCoords*/(w.segmentA.Y / 1000));
                        var segB = Point2.Create(/*RoundCoords*/(w.segmentB.X / 1000), /*RoundCoords*/(w.segmentB.Y / 1000));

                        var wall = new LinePoints();
                        wall.segmentA = segA;
                        wall.segmentB = segB;
                        Line2.Create(wall.segmentA, wall.segmentB, out var wallL);
                        wall.wallLine = wallL;
                        wallLined.Add(wall);
                    }
                    break;

                case "ft":

                    foreach(var w in walls)
                    {
                        var segA = Point2.Create(/*RoundCoords*/(w.segmentA.X * 0.3048), /*RoundCoords*/(w.segmentA.Y * 0.3048));
                        var segB = Point2.Create(/*RoundCoords*/(w.segmentB.X * 0.3048), /*RoundCoords*/(w.segmentB.Y * 0.3048));

                        var wall = new LinePoints();
                        wall.segmentA = segA;
                        wall.segmentB = segB;
                        Line2.Create(wall.segmentA, wall.segmentB, out var wallL);
                        wall.wallLine = wallL;
                        wallLined.Add(wall);
                    }
                    break;
            }

            return wallLined;
        }

        public static double RoundCoords(double coord)
        {
            coord = Math.Round(coord, 3);

            return coord;
        }

        public static double RoundDirection(double direc)
        {
            direc = Math.Round(direc, 8);

            return direc;
        }

        public static void CleanUpWallLines(List<LinePoints> ifcWallLines, string task)
        {
            var indices = new List<int>();
            var interimWalls = new List<LinePoints>();

            for(var j = 0; j < ifcWallLines.Count; j++)
            {
                var relatedWallsLoc = new List<LinePoints>();

                var overlaps = new List<bool>();

                //if(relatedWalls.Contains(ifcWallLines[j]))
                //    continue;

                //relatedWalls.Clear();

                for(var k = 0; k < ifcWallLines.Count; k++)
                {
                    //if(j == k)
                    //    continue;

                    if(IdentifyOverlapLines(ifcWallLines[j], ifcWallLines[k]))
                    {
                        if(!j.Equals(k))
                            overlaps.Add(true);

                        if(!indices.Contains(k))
                        {
                            indices.Add(k);

                            relatedWallsLoc.Add(ifcWallLines[k]);
                        }
                    }
                    else
                    {
                        overlaps.Add(false);
                    }
                }

                if(!overlaps.Contains(true))
                {
                    if(task.Equals("wall"))
                        wallLinedClean.Add(ifcWallLines[j]);               //Wandlinie ist einzeln, nicht überlappend, und nicht benachbart in gleicher Richtung vorhanden
                                                                           //wird daher direkt in "saubere" Wandlinien geschrieben

                    if(task.Equals("cvx"))
                        cvxLinesClean.Add(ifcWallLines[j]);

                    if(task.Equals("ext"))
                        extWallLinesClean.Add(ifcWallLines[j]);
                }
                else
                {
                    var unifiedWall = UnifyWallLines(relatedWallsLoc);

                    if(!unifiedWall.segmentA.Equals(unifiedWall.segmentB))
                        interimWalls.Add(unifiedWall);          //übergibt in Beziehung stehende Linien und erstellt neue Linie mit weit auseinander ligendsten Punkten
                }

                Console.WriteLine("counter interims, loop, int j: " + j + "Count= " + interimWalls.Count);
            }

            foreach(var w in interimWalls)
            {
                WallToDXF(w, "interim", DXFcolor.blue);
            }

            Console.WriteLine("counter interims, all: " + interimWalls.Count);

            //Wiederholung bis alle Linien "sauber" sind

            if(interimWalls.Count > 0)
            {
                Console.WriteLine("Neuer Durchlauf!");
                Console.WriteLine("counter cleans: " + wallLinedClean.Count);

                var interims = new List<LinePoints>();
                interims.AddRange(interimWalls);

                if(task.Equals("wall"))
                    CleanUpWallLines(interims, "wall");

                if(task.Equals("cvx"))
                    CleanUpWallLines(interims, "cvx");
            }
            else
            {
                Console.WriteLine("Vorgang beendet!");
            }
        }

        public static LinePoints UnifyWallLines(List<LinePoints> relWalls)
        {
            var unitedWall = new LinePoints();

            var ptsA = from wall in relWalls
                       select wall.segmentA;

            var ptsB = from wall in relWalls
                       select wall.segmentB;

            var pts = new List<Point2>();
            pts.AddRange(ptsA);
            pts.AddRange(ptsB);

            var dist = 0.0;

            for(var i = 0; i < pts.Count; i++)
            {
                for(var j = 0; j < pts.Count; j++)
                {
                    var dx = pts[i].X - pts[j].X;
                    var dy = pts[i].Y - pts[j].Y;

                    var distL = Math.Sqrt((dx * dx + dy * dy));

                    if(distL > dist)
                    {
                        unitedWall.segmentA = pts[i];
                        unitedWall.segmentB = pts[j];

                        Line2.Create(pts[i], pts[j], out var wall);

                        unitedWall.wallLine = wall;

                        dist = distL;
                    }
                }
            }

            return unitedWall;
        }

        //Methode zur Extraktion von Wänden aus der IFC-Datei
        //----------------------------------------------------------

        public static IEnumerable<IIfcBuildingElement> GetWallsOnGround()
        {
            var bldg = model.Instances.OfType<IIfcBuilding>().FirstOrDefault();                                     //NUR erstes IfcBuilding
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

        //Methode zur Umrechnung der absoluten Koordinaten, bezogen auf die Aggregationshierarchie ins (globale) Projektsystem
        //----------------------------------------------------------

        //Deprecated: keine Übergabe zur Map von global berechneten Koordinaten

        //public static List<Point2> GetGlobalPlacement(IList<Point2> intersecs)
        //{
        //    var globalPts = new List<Point2>();

        //    var globPlcmts = new List<Axis2Placement3D>();

        //    var ctx = model.Instances.OfType<IIfcGeometricRepresentationContext>().     //enthält Projektkoordinatensystem
        //        Where(m => m.CoordinateSpaceDimension == 3).                            //Beschränkung auf Model-Context
        //        First();                                                                //erstgefundenes Objekt (sollte sowieso nur einmal in IFC vorkommen)

        //    var ifcWcs = (IIfcAxis2Placement3D)ctx.WorldCoordinateSystem;               //IfcAxis2Placement3D des Projektes (Projektkoordinatensystem)

        //    var libWcs = ConvertAxis2Plcm(ifcWcs);                                      //Umwandlung in Library-Klasse

        //    globPlcmts.Add(libWcs);

        //    var north = ctx.TrueNorth;                                                  //Winkel zwischen Projektnord und geograph. Nord

        //    var libTN = Axis2Placement3D.Create();                                      //Axis2Placement für True North, basierend auf WCS

        //    Direction3.Create(north.Y, north.X, 0.0, out var libDirTN);                 //True North Verdrehung als neue X-Achse

        //    libTN.RefDirection = libDirTN;

        //    globPlcmts.Add(libTN);

        //    var map = model.Instances.OfType<IIfcMapConversion>().                      //MapConversion zu CRS vorhanden
        //        Where(g => g.SourceCRS is IIfcGeometricRepresentationContext).
        //        FirstOrDefault();

        //    if(map != null)
        //    {
        //        var vecMap = BimGisCad.Representation.Geometry.Elementary.Vector3.Create(map.Eastings, map.Northings, map.OrthogonalHeight);
        //        Direction3.Create(map.XAxisAbscissa.Value, map.XAxisOrdinate.Value, 0.0, out var libDirMap);

        //        var libMap = Axis2Placement3D.Create(vecMap);
        //        libMap.RefDirection = libDirMap;

        //        globPlcmts.Add(libMap);
        //    }

        //    var plcmGlobal = Axis2Placement3D.Combine(globPlcmts.ToArray());               //Kombinieren der Systeme zu globalem System

        //    foreach(var sec in intersecs)
        //    {
        //        Axis2Placement3D.ToGlobal(plcmGlobal, sec, out var gbSec);

        //        globalPts.Add(Point2.Create(gbSec.X, gbSec.Y));
        //    }

        //    return globalPts;
        //}

        //Methoden, die von jeder einzelnen Wand aufgerufen werden:
        //----------------------------------------------------------
        //----------------------------------------------------------

        //Methoden zur Transformation der relativen Koordinaten in absolutes Bauwerkssystem
        //-----------------------------------------------------------------------------------

        public static Axis2Placement3D GetAbsolutePlacement(IIfcObjectPlacement plcmRelObj)
        {
            var plcmts = new List<Axis2Placement3D>();
            var relPlcmts = GetRelativePlacements(plcmRelObj, plcmts);

            //Achtung: folgende Zeile löscht LocalPlacement vom letzten Objekt in der Liste
            //Dies ist das Placement vom (höchstgelegenen) IfcSite-Objekt (Annahme: korrekte Aggregation in IFC-Datei)
            //Rotation und Translation von IfcSite werden absichtlich übergangen (=> Vermeidung großer Koordinaten für Map-Platzierung)
            relPlcmts.RemoveAt(relPlcmts.Count - 1);

            siteSystem = Axis2Placement3D.Combine(relPlcmts.ToArray());

            return siteSystem;
        }

        public static List<Axis2Placement3D> GetRelativePlacements(IIfcObjectPlacement plcmRelObj, List<Axis2Placement3D> plcmts)
        {
            if(plcmRelObj is IIfcLocalPlacement)
            {
                //Auslesen des lokaleren Elementes (Start:Wand)
                //--------------------------------------------------------------------------------------------------------------------------------------

                // (relative) Platzierung der Wand

                //Geometrische Platzierung:

                var axisPlcm = (IIfcAxis2Placement3D)(plcmRelObj as IIfcLocalPlacement).RelativePlacement;                                              //Relatives Positionierungssystem der Wand

                plcmts.Add(ConvertAxis2Plcm(axisPlcm));

                //--------------------------------------------------------------------------------------------------------------------------------------

                //Auslesen des globaleren Elementes (Start: i.d.R. BuildingStorey)
                //--------------------------------------------------------------------------------------------------------------------------------------

                //Local Placement, zu welchem die Wand relativ steht (idR Placement von IfcBuildingStorey)

                var higherPlcm = (plcmRelObj as IIfcLocalPlacement).PlacementRelTo;

                if(higherPlcm != null)
                {
                    //Methode ruft sich selber auf bis keine relativen Platzierungen mehr vorhanden sind
                    GetRelativePlacements(higherPlcm, plcmts);
                }
            }
            else
            {
                Console.WriteLine("Currently only IfcLocalPlacement in scope.");
            }

            return plcmts;
        }

        //Methode zur Umwandlung des IFC-Placements für BimGisCad-Library
        //------------------------------------------------------------------
        public static Axis2Placement3D ConvertAxis2Plcm(IIfcAxis2Placement3D ifcPlcm)
        {
            var libVec3 = BimGisCad.Representation.Geometry.Elementary.Vector3.Create(ifcPlcm.Location.X, ifcPlcm.Location.Y, ifcPlcm.Location.Z);

            Axis2Placement3D libPlcm;

            if(ifcPlcm.RefDirection != null && ifcPlcm.Axis != null)              //Directions auf IFC-Datei
            {
                Direction3.Create(BimGisCad.Representation.Geometry.Elementary.Vector3.Create(ifcPlcm.RefDirection.X, ifcPlcm.RefDirection.Y, ifcPlcm.RefDirection.Z), out var libDirX);
                Direction3.Create(BimGisCad.Representation.Geometry.Elementary.Vector3.Create(ifcPlcm.Axis.X, ifcPlcm.Axis.Y, ifcPlcm.Axis.Z), out var libDirZ);

                libPlcm = Axis2Placement3D.Create(libVec3, libDirZ, libDirX);
            }
            else
            {
                libPlcm = Axis2Placement3D.Create(libVec3);                       //sonst: Standard-Direction für X- und Z-Achse
            }

            return libPlcm;
        }

        //------------------------------------------------------------------

        //Methoden zur Ermittlung der Wandgeometrie
        //-------------------------------------------

        public static List<LinePoints> GetBodyGeometry(IIfcRepresentation rep)
        {
            var wallLines = new List<LinePoints>();

            var localPts = new List<Point3>();

            switch(rep.RepresentationType)
            {
                case "SweptSolid":

                    var extrSld = rep.Items.Single() as IIfcSweptAreaSolid;

                    //für Trafo (3D-Position + opt. Rotation):

                    var extrPlcm = extrSld.Position;
                    var extrPos = extrPlcm.Location;
                    var ptExtr = BimGisCad.Representation.Geometry.Elementary.Vector3.Create(extrPos.X, extrPos.Y, extrPos.Z);

                    var plcmExtr = ConvertAxis2Plcm(extrPlcm);

                    siteSystem = Axis2Placement3D.Combine(siteSystem, plcmExtr);    //siteSystem erweitert um System der Extrusion

                    //für Geometrie:
                    var extrArea = extrSld.SweptArea;                               //Auslesen der Profillinien (Footprint des Sweeps)

                    //Geometrie in IfcProfileDef sehr vielschichtig
                    if(extrArea is IIfcRectangleProfileDef)                         //rechteckiger Wandgrundriss
                    {
                        //für Trafo! (2D-Postion + opt. Rotation): optional
                        var profilePlcm = (extrArea as IIfcRectangleProfileDef).Position;

                        //für Geometrie (Breite und Länge des rechteckigen Profiles): mandatory
                        var width = (extrArea as IIfcRectangleProfileDef).XDim;
                        var height = (extrArea as IIfcRectangleProfileDef).YDim;

                        //IFC-Def: Position liegt bei halber Länge und halber Breite der Profilgeometrie

                        var profilePos = profilePlcm.Location;

                        var transX = 0.5 * width;
                        var transY = 0.5 * height;

                        //lokale Koordinaten des Profiles (geschlossenes Polygon)
                        localPts.Add(Point3.Create(profilePos.X + transX, profilePos.Y + transY, 0)); // rechts oben
                        localPts.Add(Point3.Create(profilePos.X + transX, profilePos.Y - transY, 0)); // rechts unten
                        localPts.Add(Point3.Create(profilePos.X - transX, profilePos.Y - transY, 0)); // links unten
                        localPts.Add(Point3.Create(profilePos.X - transX, profilePos.Y + transY, 0)); // links oben
                        localPts.Add(Point3.Create(profilePos.X + transX, profilePos.Y + transY, 0)); // rechts oben
                    }

                    if(extrArea is IIfcArbitraryClosedProfileDef)                   //beliebiger Wandgrundriss mit beliebig vielen Knickpunkten
                    {
                        var polyline = (extrArea as IIfcArbitraryClosedProfileDef).OuterCurve;

                        var ifcPoints = (polyline as IIfcPolyline).Points;

                        foreach(var cartPt in ifcPoints)
                        {
                            localPts.Add(Point3.Create(cartPt.X, cartPt.Y, 0));
                        }
                    }

                    //........weitere Möglichkeiten?!

                    break;

                case "Brep":

                    var breps = rep.Items.FirstOrDefault();       //Single() wirft teilweise Exception --> prüfen

                    //foreach(var brep in breps)
                    //{
                    if(breps is IIfcFacetedBrep)                //Konzept-Festlegung "Body Brep Geometry"
                    {
                        var faces = (breps as IIfcFacetedBrep).Outer.CfsFaces;

                        var facePts = new List<IIfcCartesianPoint>();

                        var faceListOU = new List<List<IIfcCartesianPoint>>();

                        foreach(var face in faces)
                        {
                            var faceBounds = face.Bounds; //.Single();

                            var loopPts = face.Bounds.Select(a => a.Bound as IIfcPolyLoop).SelectMany(i => i.Polygon);//.OrderByDescending(p => p.Z);

                            foreach(var bnd in faceBounds)
                            {
                                var polyPts = (bnd.Bound as IIfcPolyLoop).Polygon;

                                var polyZ = polyPts.Select(z => z.Z);

                                if(polyZ.Distinct().Count() == 1)          //planar oben oder unten
                                    facePts.AddRange(polyPts.ToList());
                            }
                        }

                        //for (var i = 0; i < faceListOU.Count; i++)
                        //{
                        //   var fcZ = faceListOU[i].Select(f => f.Z).Distinct().Single();

                        //    for(var j = i+1 ; j < faceListOU.Count; i++)
                        //    {
                        //        var fcZ1 = faceListOU[i].Select(f => f.Z).Distinct().Single();
                        //        var fcZ2 = faceListOU[j].Select(f => f.Z).Distinct().Single();

                        //    }

                        //var btFacePts = from fc in faceListOU
                        //                where  new { ab = from f in fc where f.Z

                        var zMin = (from p in facePts select p).Min(pt => pt.Z);

                        var facePtFoot = facePts.Where(z => z.Z == zMin).ToList();

                        for(var i = 0; i < facePtFoot.Count; i++)
                        {
                            var libPt = Point3.Create(facePtFoot[i].X, facePtFoot[i].Y, facePtFoot[i].Z);

                            localPts.Add(libPt);
                        }

                        localPts.Add(Point3.Create(facePtFoot[0].X, facePtFoot[0].Y, facePtFoot[0].Z));
                    }
                    //}

                    break;

                case "Clipping":
                    break;

                case "CSG":
                    break;

                default:
                    break;
            }

            var globalPtList = new List<Point2>();

            for(var i = 0; i < localPts.Count; i++)
            {
                Axis2Placement3D.ToGlobal(siteSystem, localPts[i], out var ptxyz);
                var ptxy = Point2.Create(ptxyz.X, ptxyz.Y);
                globalPtList.Add(ptxy);         //evtl globalPtList und absIFCPoints zusammenfassen (Axis-Ausgabe muss angepasst werden)
            }

            for(var i = 0; i < globalPtList.Count; i++)
            {
                if((i + 1) >= globalPtList.Count)
                    break;

                var wallSegment = new LinePoints();

                wallSegment.segmentA = globalPtList.ElementAt(i);
                wallSegment.segmentB = globalPtList.ElementAt(i + 1);

                Line2.Create(wallSegment.segmentA, wallSegment.segmentB, out var wallAxis);

                wallSegment.wallLine = wallAxis;

                wallLines.Add(wallSegment);

                //WallToDXF(wallSegment, "partsWallsBody", DXFcolor.cyan);
            }

            return wallLines;
        }

        public static LinePoints GetAxisGeometry(IIfcRepresentation rep)
        {
            var wallLine = new LinePoints();

            if(rep is IIfcShapeRepresentation) //nur für Geometrie (!= TopologyRep or StyledRep)
            {
                var lines = (rep as IIfcShapeRepresentation).Items.OfType<IIfcPolyline>().Single(); //zunächst nur Polylines mit CartesianPoints

                var coords = lines.Points;

                List<Point2> ptPair = new List<Point2>();

                for(var i = 0; i < coords.Count; i++)
                {
                    //Console.WriteLine("  -> Coords-Axis: " + coords[i].X + " , " + coords[i].Y);

                    //transformiere lokale Achskoordinaten in globales System:
                    Axis2Placement3D.ToGlobal(siteSystem, Point2.Create(coords[i].X, coords[i].Y), out var globPt);

                    //Console.WriteLine(globPt.X + " / " + globPt.Y + " / " + globPt.Z);

                    //erzeuge 2D-Punkt und füge diesen einer 2D-Punktliste hinzu
                    var pt2D = Point2.Create(globPt.X, globPt.Y);
                    //absIFCPts.Add(pt2D);

                    //Console.WriteLine("  -> Coords-Axis (global): " + globPt.X + " , " + globPt.Y);

                    //schreibe Koordinaten in Textdatei (Kommata-getrennt):
                    WriteCoords(Point2.ToCSVString(Point2.Create(globPt.X, globPt.Y)), "wallAxisCoords"); //verebnet

                    //WriteCoords(Point3.ToCSVString(globPt)); // mit Höhen

                    //Rekonstruktion der Wandachsen:
                    ptPair.Add(pt2D);

                    //PointsToDXF(pt2D, "AxisPoints", DXFcolor.red);
                }

                wallLine.segmentA = ptPair[0];
                wallLine.segmentB = ptPair[1];

                //für Weiterverarbeitung im Programm:
                Line2.Create(ptPair[0], ptPair[1], out var wallAxis);
                wallLine.wallLine = wallAxis;

                //WallToDXF(wallLine, "partsWallsAxis", DXFcolor.green);
            }

            return wallLine;
        }

        public static void GetFootprintGeometry(IIfcRepresentation rep)
        { } // To Be implemented

        //------------------------------------------neuer Ansatz zum Finden----------------------------------------

        public static List<LinePoints> CalcConvexHull(IList<Point2> absPts)
        {
            Console.WriteLine("Ready? Push Return/Enter to start.");
            Console.ReadLine();

            var vertices = new VtxForHull[absPts.Count];

            for(var i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new VtxForHull(absPts[i].X, absPts[i].Y);
            }

            Console.WriteLine("Running...");
            var now = DateTime.Now;
            var convexHull = ConvexHull.Create(vertices).Points.ToList();
            var interval = DateTime.Now - now;
            Console.WriteLine("Out of the {0} 2D vertices, there are {1} on the convex hull.", vertices.Length, convexHull.Count());
            Console.WriteLine("time = " + interval);

            var cvxLines = new List<LinePoints>();

            for(var i = 0; i < convexHull.Count(); i++)
            {
                var pt1 = Point2.Create(convexHull[i].Position[0], convexHull[i].Position[1]);

                Point2 pt2;

                if(i != convexHull.Count - 1)
                {
                    pt2 = Point2.Create(convexHull[i + 1].Position[0], convexHull[i + 1].Position[1]);
                }
                else
                {
                    pt2 = Point2.Create(convexHull[0].Position[0], convexHull[0].Position[1]);
                }

                Line2.Create(pt1, pt2, out var cvxLine);

                var cvxLineSeg = new LinePoints();
                cvxLineSeg.segmentA = pt1;
                cvxLineSeg.segmentB = pt2;
                cvxLineSeg.wallLine = cvxLine;

                realIntersecPts.Add(pt1);

                cvxLines.Add(cvxLineSeg);
            }

            return cvxLines;
        }

        public static void CompareCvxWalls()
        {
            try
            {
                var absPts = new List<Point2>();

                absPts.AddRange(from lines in wallLinedClean select lines.segmentA);
                absPts.AddRange(from lines in wallLinedClean select lines.segmentB);

                var cvxLines = CalcConvexHull(absPts); //konvexe Hülle

                CleanUpWallLines(cvxLines, "cvx");

                foreach(var cvxH in cvxLines)
                {
                    WallToDXF(cvxH, "convexHull", DXFcolor.yellow);
                }

                foreach(var cvxH in cvxLinesClean)
                {
                    PointsToDXF(cvxH.segmentA, "convexHullPts", DXFcolor.magenta);
                    PointsToDXF(cvxH.segmentB, "convexHullPts", DXFcolor.magenta);

                    WallToDXF(cvxH, "convexHullClean", DXFcolor.magenta);
                }

                var cvxMatchLines = new List<LinePoints>();

                //foreach (var cvx in cvxLines)
                //{
                //    var wallMatch = from cvx in cvxLines                                                 //Vergleich Convex Hull mit Wandkanten
                //                   where cvx.wallLine.Direction.Equals(wall.wallLine.Direction)
                //                   select wall;

                //    extWallLines.AddRange(wallMatch.Distinct());
                //}

                //List<LinePoints> cvxMatches = null;

                foreach(var cvx in cvxLinesClean)
                {
                    var dirCvxX = /*RoundDirection*/(Math.Abs(cvx.wallLine.Direction.X));
                    var dirCvxY = /*RoundDirection*/(Math.Abs(cvx.wallLine.Direction.Y));

                    var cvxMatches = (from wall in wallLinedClean                                                 //Vergleich Convex Hull mit Wandkanten
                                      where
                                      /*RoundDirection*/(Math.Abs(wall.wallLine.Direction.X)).Equals(dirCvxX) &&
                                      /*RoundDirection*/(Math.Abs(wall.wallLine.Direction.Y)).Equals(dirCvxY)
                                      select cvx);

                    //var cvxMatch= from wall in wallLinedClean                                                 //Vergleich Convex Hull mit Wandkanten
                    //               where
                    //               (Math.Round(Math.Abs(wall.wallLine.Direction.X), 4)).Equals(dirCvxX) &&
                    //               (Math.Round(Math.Abs(wall.wallLine.Direction.Y), 4)).Equals(dirCvxY) &&
                    //               (wall.segmentA.Equals(cvx.segmentA) || wall.segmentA.Equals(cvx.segmentB)) &&      //alt. Prüfbedingung für 1 Wand (direktMatch)
                    //               (wall.segmentB.Equals(cvx.segmentB) || wall.segmentB.Equals(cvx.segmentA))
                    //              select cvx;                                                                   //"saubere" ConvexHull-Lines

                    //var cvxExamine = cvxMD.Except(cvxMatch);  //ConvexHull-Lines, wo Nischen vorhanden sind

                    ////Touches - Test
                    ////wenn 2 => directMatch
                    ////wenn > 2 => more than one line => Nische vorhanden
                    ////dann Punkte extrahieren, die nicht Start- und Endpunkt der cvx-Linie sind
                    ////jede zweite Punktkombination ist eine Match-Line
                    ////dazwischen sind Nische(n)
                    ////dort muss mindestens ein RayOrigin in die Mitte
                    ////problematisch wird es, falls sich die Nische weiter verzweigt

                    //foreach(var w in cvxMatch)
                    //{
                    //    WallToDXF(w, "matchedLinesStrikt", DXFcolor.green);

                    //}

                    //foreach(var w in cvxMD)
                    //{
                    //    WallToDXF(w, "matchedLinesDir", DXFcolor.blue);

                    //}

                    //foreach(var w in cvxExamine)
                    //{
                    //    WallToDXF(w, "matchedLinesExamine", DXFcolor.magenta);

                    //}

                    //cvxMatchLines.AddRange(cvxMatch);

                    //extWallLines.AddRange(cvxMatch);  //Listen vereinigen ?!

                    //var cvxMD = cvxMatches.Distinct();       //Bereinigte ConvexHull-Lines ("sauber" und mit Nischen)

                    //IEnumerable<LinePoints> cvxMatch = null;

                    //foreach (var cvx in cvxMD)
                    //{
                    //cvxMatch = from wall in wallLinedClean                                                 //Vergleich Convex Hull mit Wandkanten
                    //               where
                    //               (wall.segmentA.Equals(cvx.segmentA) || wall.segmentA.Equals(cvx.segmentB)) &&      //alt. Prüfbedingung für 1 Wand (direktMatch)
                    //               (wall.segmentB.Equals(cvx.segmentB) || wall.segmentB.Equals(cvx.segmentA))
                    //               select cvx;                                                                   //"saubere" ConvexHull-Lines

                    //}

                    //var cvxExamine = cvxMD.Except(cvxMatch);  //ConvexHull-Lines, wo Nischen vorhanden sind

                    //Touches - Test
                    //wenn 2 => directMatch
                    //wenn > 2 => more than one line => Nische vorhanden
                    //dann Punkte extrahieren, die nicht Start- und Endpunkt der cvx-Linie sind
                    //jede zweite Punktkombination ist eine Match-Line
                    //dazwischen sind Nische(n)
                    //dort muss mindestens ein RayOrigin in die Mitte
                    //problematisch wird es, falls sich die Nische weiter verzweigt

                    //foreach(var w in cvxMatch)
                    //{
                    //    WallToDXF(w, "matchedLinesStrikt", DXFcolor.green);

                    //}

                    //foreach(var w in cvxMD)
                    //{
                    //    WallToDXF(w, "matchedLinesDir", DXFcolor.blue);

                    //}

                    //foreach(var w in cvxExamine)
                    //{
                    //    WallToDXF(w, "matchedLinesExamine", DXFcolor.magenta);

                    //}

                    cvxMatchLines.AddRange(cvxMatches);

                    //extWallLines.AddRange(cvxMatches);  //Listen vereinigen ?!
                }

                foreach(var cvxL in cvxLinesClean)
                {
                    if(!cvxMatchLines.Contains(cvxL))
                        cvxOuterLines.Add(cvxL);
                }

                var cvxMatch = cvxMatchLines.Distinct();

                var cvxCt = cvxLinesClean.Count - cvxMatch.Count();

                if(cvxCt > 0)
                {
                    realIntersecPts.Clear();

                    foreach(var cvxM in cvxMatch)
                    {
                        WallToDXF(cvxM, "convexHullMatchLine", DXFcolor.blue);
                        extWallLines.Add(cvxM);
                    }

                    foreach(var cvxO in cvxOuterLines)
                    {
                        WallToDXF(cvxO, "convexLineOutside", DXFcolor.cyan);
                    }

                    DensifyRayOrigins(1);
                }
            }
            catch { }
        }

        //Variante mit OuterRayOrigins:

        public static void FindOuterIntersectionPts(IList<LinePoints> wallLines)
        {
            foreach(var rayBundle in bundleList)
            {
                foreach(var ray in rayBundle.rays)
                {
                    var distances = new Dictionary<Point2, double>();
                    var distList = new List<double>();
                    var intersecs = new List<IntersectionPoints>();
                    var intersecPts = new List<Point2>();

                    foreach(var wallLine in wallLines)
                    {
                        //Erzeugen der Schnittpunkte (Gerade-Gerade), bool für Entscheidung, ob es Schnittpunkt gibt
                        var boolIntersec = Point2.Create(ray, wallLine.wallLine, out var intersec);

                        //wenn Schnittpunkt vorhanden
                        if(boolIntersec)
                        {
                            // Filter, es sind nur Schnittpunkte auf Segment von wallLine erwünscht (Segment=Wandachse=begrenzte Strecke))
                            if(ValidIntersecPt(wallLine.segmentA, wallLine.segmentB, intersec))
                            {
                                intersecPts.Add(intersec);

                                //Console.WriteLine("Intersection points detected: " + intersec.X + " / " + intersec.Y);

                                //var sectCSV = Point2.ToCSVString(intersec);

                                //WriteCoords(sectCSV, "intersectionCoords");

                                var dx = intersec.X - rayBundle.rayOrigin.X;
                                var dy = intersec.Y - rayBundle.rayOrigin.Y;

                                //Console.WriteLine("realSec. " + intersec.X + " / " + intersec.Y);

                                var sectPt = new IntersectionPoints();
                                sectPt.intersection = intersec;
                                sectPt.distToRayOrigin = Math.Sqrt((dx * dx + dy * dy));

                                intersecs.Add(sectPt);
                            }
                        }
                    }

                    if(intersecs.Count > 0)
                    {
                        var intersecOrd = intersecs.OrderBy(d => d.distToRayOrigin); //Schnittpunkte werden nach deren Abstand zum Ursprung der Spinne sortiert
                        var extSec = intersecOrd.First().intersection;              //nahster Schnittpunkt wird selektiert

                        var extWalls = (from wall in wallLines
                                        where Line2.Touches(wall.wallLine, extSec)
                                        select wall);                               //externe Walls werden identifiziert

                        foreach(var extWall in extWalls)
                        {
                            var extA = extWall.segmentA;
                            var extB = extWall.segmentB;

                            if(!ValidIntersecPt(extA, extB, extSec))            //Abbruch, wenn Schnittpunkt nicht auf DIESEM Wandsegment liegt
                                continue;                                       //verhindert Wandsegmente im Inneren, die auf gleicher Wandgerade, wie ein Außenwandsegment liegen

                            if(extWallLines.Contains(extWall))                  //Abbruch, wenn Liste externer Wände schon diese Wand enthält
                                continue;                                       //verhindert Linien und Punkte Overhead

                            extWallLines.Add(extWall);

                            //extPts.Add(extSec);                             //prüfen, ob diese Liste noch benötigt wird (außer Visualisierung)

                            PointsToDXF(extSec, "RayWallIntersects", DXFcolor.cyan);
                            WallToDXF(extWall, "FoundedExternalWalls", DXFcolor.blue);
                        }
                    }
                }
            }
        }

        //Schnittpunkte der Wandachsen
        public static void CreateRealIntersecPts()
        {
            //drei Varianten implementieren
            //1: Schnittpunkt befindet sich auf beiden Segmenten (best Case), Prüfung mit ValidIntersect()
            //2: Schnittpunkt auf einem Segment (max.Abstand zu anderem Segment = Parameter, 0.5 m?)
            //3: Schnittpunkt auf keinem Segment (max.Abstand zu beiden Segmenten = Parameter, 0.5 m?)
            //nichts gefunden: neue Strahlenspinne zum Finden externer Kanten

            try
            {
                if(extWallLines.Count > 0)
                {
                    Console.WriteLine(extWallLines.Count + "vorher");

                    //foreach(var wall in extWallLines)
                    //{
                    //    WallToDXF(wall, "externeWallsVorVereinigung", DXFcolor.red);
                    //}

                    firstLine = extWallLines[0];

                    WallToDXF(firstLine, "FirstExternalLine", DXFcolor.green);

                    CleanUpWallLines(extWallLines, "ext");

                    NextIntersectionPt(firstLine);

                    //CalcSameLines();

                    Console.WriteLine(extWallLines.Count + "nachher");

                    foreach(var wall in extWallLinesClean)
                    {
                        WallToDXF(wall, "FoundedExternalWallsAfterCalcSameLines", DXFcolor.magenta);
                    }
                }
            }
            catch(Exception ex) { Console.WriteLine(ex); }
        }

        public static bool IdentifyOverlapLines(LinePoints a, LinePoints b)
        {
            //Möglichkeiten für Überlappungen/Nachbarschaft:
            ////b liegt komplett in a
            ////b, segA grenzt an a
            ////b, segB grenzt an a
            ////a liegt komplett in b
            ////a, segA grenzt an b
            ////a, segB grenzt an b

            if(
            RoundDirection(Math.Abs(a.wallLine.Direction.X)).Equals(RoundDirection(Math.Abs(b.wallLine.Direction.X))) && //P1: gleiche Richtung
            (ValidIntersecPt(a.segmentA, a.segmentB, b.segmentA) ||     //P2: innerhalb Segmente (Bbox-Calc)
            ValidIntersecPt(a.segmentA, a.segmentB, b.segmentB) ||
            ValidIntersecPt(b.segmentA, b.segmentB, a.segmentA) ||
            ValidIntersecPt(b.segmentA, b.segmentB, a.segmentB))
            )
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //public static void CalcSameLines()  //Versuch, Linien vorher zu bereinigen (evtl vor Outerlines mit remove durchführen)
        //{
        //    var realIntersecPts = new List<Point2>();

        //    //var nextLine = new LinePoints();
        //    //var unitedList = new List<LinePoints>();

        //    var removedWalls = new List<LinePoints>();

        //    for(var j = 0; j < extWallLines.Count; j++)
        //    {
        //        for(var k = (j + 1); k < extWallLines.Count; k++)
        //        {
        //            if(extWallLines[j] != extWallLines[k]) //nicht gleiche Linien vergleichen
        //            {
        //                var dirAx = Math.Round(Math.Abs(extWallLines[j].wallLine.Direction.X), 4);
        //                var dirAy = Math.Round(Math.Abs(extWallLines[j].wallLine.Direction.Y), 4);
        //                var dirBx = Math.Round(Math.Abs(extWallLines[k].wallLine.Direction.X), 4);
        //                var dirBy = Math.Round(Math.Abs(extWallLines[k].wallLine.Direction.Y), 4);

        //                if(dirAx.Equals(dirBx) && dirAy.Equals(dirBy))          //nur Linien gleicher Richtung wählen

        //                {
        //                    if(ValidIntersecPt(extWallLines[j].segmentA, extWallLines[j].segmentB, extWallLines[k].segmentA) ||
        //                        ValidIntersecPt(extWallLines[j].segmentA, extWallLines[j].segmentB, extWallLines[k].segmentB))
        //                    {
        //                        var pts = new List<Point2>() { extWallLines[j].segmentA, extWallLines[j].segmentB, extWallLines[k].segmentA, extWallLines[k].segmentB };

        //                        var segA = UnitedLine(pts, 0);
        //                        var segB = UnitedLine(pts, 1);

        //                        if(!segA.Equals(segB))
        //                        {
        //                            Line2.Create(segA, segB, out var newline);

        //                            Console.WriteLine(segA.X + "  " + segA.Y);
        //                            Console.WriteLine(segB.X + "  " + segB.Y);

        //                            var unitedWallLine = new LinePoints();

        //                            unitedWallLine.wallLine = newline;
        //                            unitedWallLine.segmentA = segA;
        //                            unitedWallLine.segmentB = segB;
        //                            //Line2.Create(unitedWallLine.segmentA, unitedWallLine.segmentB, out var newline);

        //                            //WallToDXF(unitedWallLine, "AvereinigteLinien", DXFcolor.green);

        //                            removedWalls.Add(extWallLines[j]);
        //                            removedWalls.Add(extWallLines[k]);

        //                            if(!extWallLines.Contains(unitedWallLine))
        //                                extWallLines.Add(unitedWallLine);
        //                        }
        //                    }
        //                    else if(ValidDistToIntersect(extWallLines[j].segmentA, extWallLines[k].segmentA) ||
        //                        ValidDistToIntersect(extWallLines[j].segmentA, extWallLines[k].segmentB) ||
        //                        ValidDistToIntersect(extWallLines[j].segmentB, extWallLines[k].segmentA) ||
        //                        ValidDistToIntersect(extWallLines[j].segmentB, extWallLines[k].segmentB))
        //                    {
        //                        //var unitedWallLine = new LinePoints();
        //                        var pts = new List<Point2>() { extWallLines[j].segmentA, extWallLines[j].segmentB, extWallLines[k].segmentA, extWallLines[k].segmentB };

        //                        var segA = UnitedLine(pts, 0);
        //                        var segB = UnitedLine(pts, 1);

        //                        if(!segA.Equals(segB))
        //                        {
        //                            Line2.Create(segA, segB, out var newline);

        //                            Console.WriteLine(segA.X + "  " + segA.Y);
        //                            Console.WriteLine(segB.X + "  " + segB.Y);

        //                            var unitedWallLine = new LinePoints();

        //                            unitedWallLine.wallLine = newline;
        //                            unitedWallLine.segmentA = segA;
        //                            unitedWallLine.segmentB = segB;
        //                            //Line2.Create(unitedWallLine.segmentA, unitedWallLine.segmentB, out var newline);

        //                            //WallToDXF(unitedWallLine, "AvereinigteLinien", DXFcolor.green);

        //                            removedWalls.Add(extWallLines[j]);
        //                            removedWalls.Add(extWallLines[k]);

        //                            if(!extWallLines.Contains(unitedWallLine))
        //                                extWallLines.Add(unitedWallLine);

        //                            //extUniWalls.Add(unitedWallLine);
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    foreach(var wallR in removedWalls)
        //    {
        //        WallToDXF(wallR, "RemovedWallsinCalcSameLines", DXFcolor.red);

        //        if(extWallLines.Contains(wallR))
        //            extWallLines.Remove(wallR);
        //    }

        //    extUniWalls.AddRange(extWallLines);

        //    foreach(var nextLine in extUniWalls)
        //    {
        //        // Console.WriteLine("NextLine: " + nextLine.wallLine.ToString() + ", SegA: " + (nextLine.segmentA.X - nextLine.segmentB.X) + ", SegB: " + (nextLine.segmentA.Y - nextLine.segmentB.Y));

        //        WallToDXF(nextLine, "BereinigteExternalWallsAfterCalcSameLines", DXFcolor.green);
        //    }

        //    firstLine = extUniWalls[0];

        //    NextIntersectionPt(firstLine);
        //}

        public static void NextIntersectionPt(LinePoints startLine)
        {
            try
            {
                var nextLine = new LinePoints();

                var distances = new Dictionary<LinePoints, double>();
                bool flag = false;
                var tempIntersecExactly = new Dictionary<LinePoints, Point2>();
                var tempIntersecNearby = new Dictionary<LinePoints, Point2>();

                for(var k = 0; k < extWallLinesClean.Count; k++)
                {
                    distances.Clear();
                    flag = false;

                    LinePoints followLine;

                    if(extWallLinesClean.Count == 1)
                    {
                        followLine = firstLine;         //klappt nur bei korrekten Linien in ExtWallLines
                    }
                    else
                    {
                        followLine = extWallLinesClean[k];
                    }

                    if(startLine != followLine)
                    {
                        if(Point2.Create(startLine.wallLine, followLine.wallLine, out var realIntersec))  //nur wenn Schnittpunkt zw Linien gefunden wird

                        {
                            if(realIntersec.X != 0 && realIntersec.Y != 0)
                            {
                                //potentieller Schnittpunkt auf Geraden

                                var lineAsegA = startLine.segmentA;
                                var lineAsegB = startLine.segmentB;
                                var lineBsegA = followLine.segmentA;
                                var lineBsegB = followLine.segmentB;

                                if(
                                    (ValidIntersecPt(lineAsegA, lineAsegB, realIntersec) &&                     //Fall 1
                                    ValidIntersecPt(lineBsegA, lineBsegB, realIntersec))

                                    )

                                //realer Schnittpunkt auf Segmenten

                                {
                                    if(!realIntersecPts.Contains(realIntersec))

                                    {                                       //keine doppelten Punkte
                                        tempIntersecExactly.Add(followLine, realIntersec);

                                        //nextLine = followLine;
                                        //extWallLines.Remove(startLine);
                                    }
                                    else
                                    {
                                        Console.WriteLine("intersec schon vorhanden");
                                    }

                                    //flag = true;

                                    //if(!startLine.Equals(firstLine))
                                    //break;                                  //Abbruch der Schleife, wenn exakter Schnittpunkt gefunden wurde
                                }
                                else
                                {
                                    if(((ValidDistToIntersect(realIntersec, lineAsegA) || ValidDistToIntersect(realIntersec, lineAsegB)) &&                     //Fall 2a
                                    !ValidIntersecPt(lineAsegA, lineAsegB, realIntersec) &&
                                    ValidIntersecPt(lineBsegA, lineBsegB, realIntersec))
                                    ||
                                    ((ValidDistToIntersect(realIntersec, lineBsegA) || ValidDistToIntersect(realIntersec, lineBsegB)) &&                     //Fall 2b
                                    !ValidIntersecPt(lineBsegA, lineBsegB, realIntersec) &&
                                    ValidIntersecPt(lineAsegA, lineAsegB, realIntersec)))
                                    {
                                        if(!realIntersecPts.Contains(realIntersec))

                                        {                                       //keine doppelten Punkte
                                            tempIntersecNearby.Add(followLine, realIntersec);
                                        }
                                        else
                                        {
                                            Console.WriteLine("intersec schon vorhanden");
                                        }
                                    }
                                    continue;                                   //Schleife weiter durchlaufen (evtl. wird noch realer Punkt gefunden)
                                }

                                //else
                                //{
                                //    //Schnittberechnung möglich, aber außerhalb der Kanten)

                                //    Console.WriteLine("innere else" + realIntersec.X + "   /  " + realIntersec.Y);

                                //    //Berechne Lot-Abstand von potentieller Follow - Line zum letztgefundenen Punkt
                                //    //  von allen Lotabständen den geringsten nehmen
                                //    //  Lotfußpunkt wird nächster RealIntersec
                                //    //  Kante, auf welcher Lotfußpunkt liegt, wird Follow-Line

                                //    var localDist = new List<double>();

                                //    var dx1 = lineAsegA.X - lineBsegA.X;
                                //    var dy1 = lineAsegA.Y - lineBsegA.Y;
                                //    localDist.Add(Math.Sqrt((dx1 * dx1 + dy1 * dy1)));

                                //    var dx2 = lineAsegA.X - lineBsegB.X;
                                //    var dy2 = lineAsegA.Y - lineBsegB.Y;
                                //    localDist.Add(Math.Sqrt((dx2 * dx2 + dy2 * dy2)));

                                //    var dx3 = lineAsegB.X - lineBsegA.X;
                                //    var dy3 = lineAsegB.Y - lineBsegA.Y;
                                //    localDist.Add(Math.Sqrt((dx3 * dx3 + dy3 * dy3)));

                                //    var dx4 = lineAsegB.X - lineBsegB.X;
                                //    var dy4 = lineAsegB.Y - lineBsegB.Y;
                                //    localDist.Add(Math.Sqrt((dx4 * dx4 + dy4 * dy4)));

                                //    distances.Add(followLine, localDist.Min());

                                //    flag = false;

                                //continue;
                                //}
                            }
                            else
                            {
                                Console.WriteLine("äußere else");

                                continue;
                            }

                            //neue Berechnung Strahlen (Schnittberechnung nicht möglich, parallele Kanten)
                        }
                    }
                }

                if(tempIntersecExactly.Count == 1)                             //Standardfall: nur ein exakter Schnittpunkt gefunden
                {
                    realIntersecPts.Add(tempIntersecExactly.First().Value);         //Schnittpunkt ist Punkt von Polygon
                    nextLine = tempIntersecExactly.First().Key;                     //nächste Polygonlinie ist followline
                    extWallLinesClean.Remove(startLine);                            //Vorgängerlinie wird gelöscht (keine weitere Betrachtung)
                }

                if(tempIntersecExactly.Count > 1)                               //Fall tritt ein, wenn entlang Hauptlinie Vorsprünge vorhanden sind, welche die
                {                                                               //die Hauptlinie (hier startline) schneiden
                    var lastIntersec = realIntersecPts.LastOrDefault();

                    var keys = from kp in tempIntersecExactly
                               select kp.Key;

                    var dists = from val in tempIntersecExactly.Values
                                select (CalcDistance(val, lastIntersec));

                    var minDist = dists.Min();

                    var pt = (from val in tempIntersecExactly.Values
                              where ((CalcDistance(val, lastIntersec)) == minDist)
                              select val).First();

                    var line = (from kp in tempIntersecExactly
                                where kp.Value.Equals(pt)
                                select kp.Key).First();

                    realIntersecPts.Add(pt);
                    nextLine = line;
                    //extWallLinesClean.Remove(startLine);

                    //var newline = startLine;

                    //newline.segmentB = Line2.PointOnLine(startLine.wallLine, minDist);

                    //extWallLinesClean.Add(newline);
                }

                if(tempIntersecExactly.Count == 0)                               //Fall tritt ein, kein sinnvoller Schnitt möglich ist (Linie fehlt) oder Lücke vorhanden (Axis-Rep)
                {
                    if(tempIntersecNearby.Count == 1)                           //wahrscheinlich echte Schnittpunkte, allerdings mit Lücken (Festlegung, zulässige Lücke <= 50 cm)
                    {
                        realIntersecPts.Add(tempIntersecNearby.First().Value);         //Schnittpunkt ist (wahrscheinlich) Punkt von Polygon
                        nextLine = tempIntersecNearby.First().Key;                     //nächste Polygonlinie ist followline
                        extWallLinesClean.Remove(startLine);                            //Vorgängerlinie wird gelöscht (keine weitere Betrachtung)
                    }

                    if(tempIntersecNearby.Count > 1)                               //Fall tritt ein, wenn entlang Hauptlinie Vorsprünge vorhanden sind, welche die
                    {                                                               //die Hauptlinie (hier startline) schneiden
                        var lastIntersec = realIntersecPts.LastOrDefault();

                        var keys = from kp in tempIntersecNearby
                                   select kp.Key;

                        var dists = from val in tempIntersecNearby.Values
                                    select (CalcDistance(val, lastIntersec));

                        var minDist = dists.Min();

                        var pt = (from val in tempIntersecNearby.Values
                                  where ((CalcDistance(val, lastIntersec)) == minDist)
                                  select val).First();

                        var line = (from kp in tempIntersecNearby
                                    where kp.Value.Equals(pt)
                                    select kp.Key).First();

                        realIntersecPts.Add(pt);
                        nextLine = line;
                    }

                    if(tempIntersecNearby.Count == 0)
                    {
                        Console.WriteLine("to be implemented, kein sinnvoller Schnittpunkt gefunden");

                        var lastIntersec = realIntersecPts.LastOrDefault();

                        var perpPt = startLine.segmentB;

                        if(CalcDistance(startLine.segmentA, lastIntersec) > CalcDistance(startLine.segmentB, lastIntersec))
                        {
                            perpPt = startLine.segmentA;
                        }

                        var dists = from w in extWallLinesClean
                                    select (Line2.DistanceToLine(w.wallLine, perpPt));

                        var minDist = dists.Min();

                        var line = (from w in extWallLinesClean
                                    where Line2.DistanceToLine(w.wallLine, perpPt) == minDist
                                    select w).First();

                        var pt = Line2.PerpendicularFoot(line.wallLine, perpPt);

                        realIntersecPts.Add(perpPt);
                        realIntersecPts.Add(pt);
                        nextLine = line;
                        extWallLinesClean.Remove(startLine);
                    }
                }

                //realIntersecPts.Add(tempIntersec);

                //nextLine = followline; //followline auf dict

                //if(!flag)
                //{
                //var ordDist = distances.OrderBy(d => d.Value);

                //nextLine = (from line in ordDist
                //            select line.Key).First();
                //}

                //Console.WriteLine("NextLine: " + nextLine.wallLine.ToString() + ", SegA: " + (nextLine.segmentA.X - nextLine.segmentB.X) + ", SegB: " + (nextLine.segmentA.Y - nextLine.segmentB.Y));

                //extWallLinesClean.Remove(startLine);

                Console.WriteLine(extWallLinesClean.Count);

                if(extWallLinesClean.Count > 0)
                    NextIntersectionPt(nextLine);
            }

            catch(Exception ex) { Console.WriteLine(ex); }
        }

        //----------------------------------------------------------------------------------------------

        //Hilfsmethoden und Klassen

        public static double CalcDistance(Point2 a, Point2 b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;

            return (Math.Sqrt((dx * dx) + (dy * dy)));
        }

        public static void DensifyRayOrigins(int densLev) // TO DO: Ändern zu neuem Ansatz (fortlaufendes Halbieren der Cvx-kKnte)
        {
            bundleList.Clear();

            foreach(var cvxO in cvxOuterLines)
            {
                //WallToDXF(cvxO, "convexLineOutside", DXFcolor.yellow);

                var deltax = cvxO.segmentA.X - cvxO.segmentB.X;
                var deltay = cvxO.segmentA.Y - cvxO.segmentB.Y;

                switch(densLev)
                {
                    case 0:
                        {
                            var mpt = Point2.Create((cvxO.segmentB.X + deltax / 2), (cvxO.segmentB.Y + deltay / 2));

                            PointsToDXF(mpt, "RayOrigins", DXFcolor.magenta);

                            var rayB = new RayBundle(cvxO.wallLine.GetHashCode().ToString(), mpt);  //RayBundle-Klasse wahrscheinlich übertrieben (Identifier notwendig?)
                            bundleList.Add(rayB);

                            break;
                        }

                    case 1:
                        {
                            var mpt1 = Point2.Create((cvxO.segmentB.X + deltax / 4), (cvxO.segmentB.Y + deltay / 4));
                            var mpt2 = Point2.Create((cvxO.segmentA.X - deltax / 4), (cvxO.segmentA.Y - deltay / 4));

                            PointsToDXF(mpt1, "RayOrigins", DXFcolor.magenta);
                            PointsToDXF(mpt2, "RayOrigins", DXFcolor.magenta);

                            var rayB1 = new RayBundle(cvxO.wallLine.GetHashCode().ToString(), mpt1);  //RayBundle-Klasse wahrscheinlich übertrieben (Identifier notwendig?)
                            bundleList.Add(rayB1);
                            var rayB2 = new RayBundle(cvxO.wallLine.GetHashCode().ToString(), mpt2);
                            bundleList.Add(rayB2);

                            break;
                        }

                    case 2:
                        {
                            var mpt1 = Point2.Create((cvxO.segmentB.X + deltax / 8), (cvxO.segmentB.Y + deltay / 8));
                            var mpt2 = Point2.Create((cvxO.segmentB.X + deltax / 2 - deltax / 8), (cvxO.segmentB.Y + deltay / 2 - deltay / 8));
                            var mpt3 = Point2.Create((cvxO.segmentA.X - deltax / 2 + deltax / 8), (cvxO.segmentA.Y - deltay / 2 + deltay / 8));
                            var mpt4 = Point2.Create((cvxO.segmentA.X - deltax / 8), (cvxO.segmentA.Y - deltay / 8));

                            PointsToDXF(mpt1, "RayOrigins", DXFcolor.magenta);
                            PointsToDXF(mpt2, "RayOrigins", DXFcolor.magenta);
                            PointsToDXF(mpt3, "RayOrigins", DXFcolor.magenta);
                            PointsToDXF(mpt4, "RayOrigins", DXFcolor.magenta);

                            var rayB1 = new RayBundle(cvxO.wallLine.GetHashCode().ToString(), mpt1);  //RayBundle-Klasse wahrscheinlich übertrieben (Identifier notwendig?)
                            bundleList.Add(rayB1);
                            var rayB2 = new RayBundle(cvxO.wallLine.GetHashCode().ToString(), mpt2);
                            bundleList.Add(rayB2);
                            var rayB3 = new RayBundle(cvxO.wallLine.GetHashCode().ToString(), mpt3);
                            bundleList.Add(rayB3);
                            var rayB4 = new RayBundle(cvxO.wallLine.GetHashCode().ToString(), mpt4);
                            bundleList.Add(rayB4);

                            break;
                        }
                }
            }
        }

        public static Point2 UnitedLine(List<Point2> pts, int begOrEnd)
        {
            var dist = 0.0;

            Point2 start = pts[0];
            Point2 end = pts[3];

            for(var i = 0; i < pts.Count; i++)
            {
                for(var j = 0; j < pts.Count; j++)
                {
                    var dx = pts[i].X - pts[j].X;
                    var dy = pts[i].Y - pts[j].Y;

                    var distL = Math.Sqrt((dx * dx + dy * dy));

                    if(distL > dist)
                    {
                        start = pts[i];
                        end = pts[j];
                        dist = distL;
                    }
                }
            }

            if(begOrEnd == 0)
                return start;
            else if(begOrEnd == 1)
                return end;
            else
                return start;
        }

        public static bool ValidDistToIntersect(Point2 segment, Point2 intersec)
        {
            const double dist = 0.5; //selbst gewählter Parameter

            var dx = intersec.X - segment.X;
            var dy = intersec.Y - segment.Y;

            //Console.WriteLine("realSec. " + intersec.X + " / " + intersec.Y);

            var realDist = Math.Sqrt((dx * dx + dy * dy));

            if(realDist <= dist)
                return true;
            else
                return false;
        }

        //Bbox-Filter für Schnittpunkte nur auf Wandkante (nicht außerhalb des Hauses)
        public static bool ValidIntersecPt(Point2 segmentA, Point2 segmentB, Point2 intersec)
        {
            double xMin, yMin, xMax, yMax;

            var XsegA = RoundCoords(segmentA.X);
            var YsegA = RoundCoords(segmentA.Y);
            var XsegB = RoundCoords(segmentB.X);
            var YsegB = RoundCoords(segmentB.Y);

            var Xintersec = RoundCoords(intersec.X);
            var Yintersec = RoundCoords(intersec.Y);

            if(segmentA.X <= segmentB.X)
            {
                xMin = XsegA;
                xMax = XsegB;
            }
            else
            {
                xMin = XsegB;
                xMax = XsegA;
            }

            if(segmentA.Y <= segmentB.Y)
            {
                yMin = YsegA;
                yMax = YsegB;
            }
            else
            {
                yMin = YsegB;
                yMax = YsegA;
            }

            Line2.Create(segmentA, segmentB, out var checkL);
            var dst = Line2.DistanceToLine(checkL, intersec);

            if(Xintersec <= xMax && Xintersec >= xMin && Yintersec <= yMax && Yintersec >= yMin && dst < 0.01)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void WriteCoords(string coord, string task)
        {
            var time = DateTime.Now.ToString("yyyy-dd-M--HH-mm");

            using(var writeLog = File.AppendText(("D:\\1_CityBIM\\1_Programmierung\\IfcGeoRef\\" + task + nr + "_" + time + ".txt")))
            {
                try
                {
                    writeLog.WriteLine(coord);
                }

                catch(Exception ex)
                {
                    writeLog.WriteLine($"Error occured while writing Logfile. \r\n Message: {ex.Message}");
                }
            };
        }

        public static void CalcBbox(IList<Point2> ifcPoints)
        {
            //Bbox:
            var xValuesIFC = ifcPoints.Select(x => x.X);
            var yValuesIFC = ifcPoints.Select(y => y.Y);

            var xMinIFC = xValuesIFC.Min();
            var xMaxIFC = xValuesIFC.Max();

            var yMinIFC = yValuesIFC.Min();
            var yMaxIFC = yValuesIFC.Max();

            bbox = new BboxIFC()
            {
                lowerLeftPt = Point2.Create(xMinIFC, yMinIFC),
                upperRightPt = Point2.Create(xMaxIFC, yMaxIFC),
                widthX = xMaxIFC - xMinIFC,
                widthY = yMaxIFC - yMinIFC
            };
        }

        public static IList<Line2> InitializeRays(Point2 rayOrigin)
        {
            //Initialiseren der Spinne
            IList<Line2> rays = new List<Line2>();

            //Variable deg gibt implizit Anzahl der Strahlen vor (für Test beliebig, später auswählbar oder 0.1)
            var deg = 1;

            var rad = deg * Math.PI / 180;

            //Anlegen der Strahlen und Ablage in rays-Liste
            for(double i = 0; i < (Math.PI); i += rad)
            {
                var ray = Line2.Create(rayOrigin, Direction2.Create(i));

                rays.Add(ray);
            }

            return rays;
        }

        public static void PointsToDXF(Point2 ptXY, string layerName, DXFcolor color)
        {
            netDxf.Tables.Layer layer = new netDxf.Tables.Layer(layerName);

            netDxf.Entities.Point pt = new netDxf.Entities.Point(ptXY.X, ptXY.Y, 0);

            netDxf.Entities.Circle circle = new netDxf.Entities.Circle(new netDxf.Vector2(ptXY.X, ptXY.Y), 0.2);

            layer.Color = GetDXFColor(color);

            pt.Layer = layer;
            circle.Layer = layer;

            dxf.AddEntity(pt);
            dxf.AddEntity(circle);
        }

        public static void WallToDXF(LinePoints wallLine, string layerName, DXFcolor color)
        {
            netDxf.Vector2 vec1 = new netDxf.Vector2(wallLine.segmentA.X, wallLine.segmentA.Y);
            netDxf.Vector2 vec2 = new netDxf.Vector2(wallLine.segmentB.X, wallLine.segmentB.Y);

            netDxf.Entities.Line wallLineDXF = new netDxf.Entities.Line(vec1, vec2);

            wallLineDXF.Layer = new netDxf.Tables.Layer(layerName);

            wallLineDXF.Layer.Color = GetDXFColor(color);

            dxf.AddEntity(wallLineDXF);
        }

        public static AciColor GetDXFColor(DXFcolor color)
        {
            switch(color)
            {
                case DXFcolor.red:
                    return new AciColor(255, 0, 0);

                case DXFcolor.green:
                    return new AciColor(0, 255, 0);

                case DXFcolor.blue:
                    return new AciColor(0, 0, 255);

                case DXFcolor.yellow:
                    return new AciColor(255, 255, 0);

                case DXFcolor.magenta:
                    return new AciColor(255, 0, 255);

                case DXFcolor.cyan:
                    return new AciColor(0, 255, 255);

                default:
                    return new AciColor(255, 255, 255);
            }
        }

        public enum DXFcolor { red, green, blue, cyan, yellow, magenta }

        public class LinePoints
        {
            public Line2 wallLine { get; set; }
            public Point2 segmentA { get; set; }
            public Point2 segmentB { get; set; }
        }

        public class IntersectionPoints
        {
            public Point2 intersection { get; set; }
            public double distToRayOrigin { get; set; }
        }

        public class RayBundle
        {
            public IList<Line2> rays { get; set; }
            public Point2 rayOrigin { get; set; }
            public string rayIdentifier { get; set; }

            public RayBundle(string rayIdentifier, Point2 rayOrigin)
            {
                this.rayIdentifier = rayIdentifier;
                this.rayOrigin = rayOrigin;

                this.rays = InitializeRays(rayOrigin);
            }
        }

        public class BboxIFC
        {
            public Point2 lowerLeftPt { get; set; }
            public Point2 upperRightPt { get; set; }

            public double widthX { get; set; }

            public double widthY { get; set; }
        }
    }
}