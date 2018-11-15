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

                foreach(var wl in wallLined)
                {
                    var dX = wl.segmentA.X - wl.segmentB.X;
                    var dY = wl.segmentA.Y - wl.segmentB.Y;

                    if((dX == 0.0) && (dY == 0.0))
                        Console.WriteLine("Segmente Pkt gleich: " + wl.segmentA.X + " / " + wl.segmentA.Y);

                    WallToDXF(wl, "0_detectedWalls", DXFcolor.yellow);
                    PointsToDXF(wl.segmentA, "0_detectedWalls", DXFcolor.yellow);
                    PointsToDXF(wl.segmentB, "0_detectedWalls", DXFcolor.yellow);
                }
                //--------------------------------------------------------------------------

                Console.ReadKey();

                CompareCvxWalls();

                FindOuterIntersectionPts(wallLined);

                CreateRealIntersecPts();

                var globalPts = GetGlobalPlacement(realIntersecPts);

                globalPts.Add(globalPts[0]); //geschlossener Ring

                foreach(var pt in globalPts)
                {
                    PointsToDXF(pt, "globalSecs", DXFcolor.red);

                    Console.WriteLine(pt.X + " , " + pt.Y);
                }

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

        public static List<Point2> GetGlobalPlacement(IList<Point2> intersecs)
        {
            var globalPts = new List<Point2>();

            var globPlcmts = new List<Axis2Placement3D>();

            var ctx = model.Instances.OfType<IIfcGeometricRepresentationContext>().     //enthält Projektkoordinatensystem
                Where(m => m.CoordinateSpaceDimension == 3).                            //Beschränkung auf Model-Context
                First();                                                                //erstgefundenes Objekt (sollte sowieso nur einmal in IFC vorkommen)

            var ifcWcs = (IIfcAxis2Placement3D)ctx.WorldCoordinateSystem;               //IfcAxis2Placement3D des Projektes (Projektkoordinatensystem)

            var libWcs = ConvertAxis2Plcm(ifcWcs);                                      //Umwandlung in Library-Klasse

            globPlcmts.Add(libWcs);

            var north = ctx.TrueNorth;                                                  //Winkel zwischen Projektnord und geograph. Nord

            var libTN = Axis2Placement3D.Create();                                      //Axis2Placement für True North, basierend auf WCS

            Direction3.Create(north.Y, north.X, 0.0, out var libDirTN);                 //True North Verdrehung als neue X-Achse

            libTN.RefDirection = libDirTN;

            globPlcmts.Add(libTN);

            var map = model.Instances.OfType<IIfcMapConversion>().                      //MapConversion zu CRS vorhanden
                Where(g => g.SourceCRS is IIfcGeometricRepresentationContext).
                FirstOrDefault();

            if(map != null)
            {
                var vecMap = BimGisCad.Representation.Geometry.Elementary.Vector3.Create(map.Eastings, map.Northings, map.OrthogonalHeight);
                Direction3.Create(map.XAxisAbscissa.Value, map.XAxisOrdinate.Value, 0.0, out var libDirMap);

                var libMap = Axis2Placement3D.Create(vecMap);
                libMap.RefDirection = libDirMap;

                globPlcmts.Add(libMap);
            }

            var plcmGlobal = Axis2Placement3D.Combine(globPlcmts.ToArray());               //Kombinieren der Systeme zu globalem System

            foreach(var sec in intersecs)
            {
                Axis2Placement3D.ToGlobal(plcmGlobal, sec, out var gbSec);

                globalPts.Add(Point2.Create(gbSec.X, gbSec.Y));
            }

            return globalPts;
        }

        //Methoden, die von jeder einzelnen Wand aufgerufen werden:
        //----------------------------------------------------------
        //----------------------------------------------------------

        //Methoden zur Transformation der relativen Koordinaten in absolutes Bauwerkssystem
        //-----------------------------------------------------------------------------------

        public static Axis2Placement3D GetAbsolutePlacement(IIfcObjectPlacement plcmRelObj)
        {
            var plcmts = new List<Axis2Placement3D>();
            var relPlcmts = GetRelativePlacements(plcmRelObj, plcmts);

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
                else
                {
                    //Console.WriteLine("No more relative Placements existent. Please refer now to the World Coordinate System.");

                    //TO DO: Umwandlung ins WCS vornehmen
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

                //WallToDXF(wallSegment, "WallsBody", DXFcolor.yellow);
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

                //WallToDXF(wallLine, "WallsAxis", DXFcolor.green);
            }

            return wallLine;
        }

        public static void GetFootprintGeometry(IIfcRepresentation rep)
        { }

        //------------------------------------------neuer Ansatz zum Finden----------------------------------------

        public static IList<LinePoints> CalcConvexHull(IList<Point2> absPts)
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

                PointsToDXF(pt1, "convex", DXFcolor.magenta);

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

                cvxLines.Add(cvxLineSeg);
            }

            return cvxLines;
        }

        public static void CompareCvxWalls()
        {
            try
            {
                var absPts = new List<Point2>();

                absPts.AddRange(from lines in wallLined select lines.segmentA);
                absPts.AddRange(from lines in wallLined select lines.segmentB);

                var cvxLines = CalcConvexHull(absPts); //konvexe Hülle

                var cvxMatchLines = new List<LinePoints>();

                foreach(var cvx in cvxLines)
                {
                    var cvxMatch = from wall in wallLined                                                 //Vergleich Convex Hull mit Wandkanten
                                   where wall.wallLine.Direction.Equals(cvx.wallLine.Direction)
                                   select cvx;

                    cvxMatchLines.AddRange(cvxMatch.Distinct());

                    extWallLines.AddRange(cvxMatch.Distinct());  //Listen vereinigen ?!
                }

                foreach(var cvx in cvxLines)
                {
                    if(!cvxMatchLines.Contains(cvx))
                        cvxOuterLines.Add(cvx);
                }

                foreach(var cvxM in cvxMatchLines)
                {
                    WallToDXF(cvxM, "convexWallLine", DXFcolor.blue);
                }

                DensifyRayOrigins(1);

                //foreach(var cvxO in cvxOuterLines)
                //{
                //    WallToDXF(cvxO, "convexLineOutside", DXFcolor.yellow);

                //    var mpt = Point2.Create((cvxO.segmentB.X + (cvxO.segmentA.X - cvxO.segmentB.X) / 2), (cvxO.segmentB.Y + (cvxO.segmentA.Y - cvxO.segmentB.Y) / 2));

                //    PointsToDXF(mpt, "mittelpunkte", DXFcolor.magenta);

                //    var rayB = new RayBundle(cvxO.wallLine.GetHashCode().ToString(), mpt);  //RayBundle-Klasse wahrscheinlich übertrieben (Identifier notwendig?)

                //    bundleList.Add(rayB);
                //}
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

                                var sectCSV = Point2.ToCSVString(intersec);

                                WriteCoords(sectCSV, "intersectionCoords");

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

                            extPts.Add(extSec);                             //prüfen, ob diese Liste noch benötigt wird (außer Visualisierung)

                            PointsToDXF(extSec, "ExternalWallIntersects", DXFcolor.cyan);
                            //WallToDXF(extWall, "ExternalWalls", DXFcolor.blue);
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
                Console.WriteLine(extWallLines.Count + "vorher");

                foreach(var wall in extWallLines)
                {
                    WallToDXF(wall, "externeWallsVorVereinigung", DXFcolor.red);
                }

                CalcSameLines();

                Console.WriteLine(extWallLines.Count + "nachher");

                foreach(var wall in extWallLines)
                {
                    WallToDXF(wall, "externeWallsBereinigt", DXFcolor.magenta);
                }
            }
            catch(Exception ex) { Console.WriteLine(ex); }
        }

        public static void CalcSameLines()  //Versuch, Linien vorher zu bereinigen (evtl vor Outerlines mit remove durchführen)
        {
            var realIntersecPts = new List<Point2>();

            //var nextLine = new LinePoints();
            //var unitedList = new List<LinePoints>();

            var removedWalls = new List<LinePoints>();

            for(var j = 0; j < extWallLines.Count; j++)
            {
                for(var k = (j + 1); k < extWallLines.Count; k++)
                {
                    if(extWallLines[j] != extWallLines[k]) //nicht gleiche Linien vergleichen
                    {
                        var dirAx = Math.Round(Math.Abs(extWallLines[j].wallLine.Direction.X), 4);
                        var dirAy = Math.Round(Math.Abs(extWallLines[j].wallLine.Direction.Y), 4);
                        var dirBx = Math.Round(Math.Abs(extWallLines[k].wallLine.Direction.X), 4);
                        var dirBy = Math.Round(Math.Abs(extWallLines[k].wallLine.Direction.Y), 4);

                        if(dirAx.Equals(dirBx) && dirAy.Equals(dirBy))          //nur Linien gleicher Richtung wählen

                        {
                            if(ValidIntersecPt(extWallLines[j].segmentA, extWallLines[j].segmentB, extWallLines[k].segmentA) ||
                                ValidIntersecPt(extWallLines[j].segmentA, extWallLines[j].segmentB, extWallLines[k].segmentB))
                            {
                                var pts = new List<Point2>() { extWallLines[j].segmentA, extWallLines[j].segmentB, extWallLines[k].segmentA, extWallLines[k].segmentB };

                                var segA = UnitedLine(pts, 0);
                                var segB = UnitedLine(pts, 1);

                                if(!segA.Equals(segB))
                                {
                                    Line2.Create(segA, segB, out var newline);

                                    Console.WriteLine(segA.X + "  " + segA.Y);
                                    Console.WriteLine(segB.X + "  " + segB.Y);

                                    var unitedWallLine = new LinePoints();

                                    unitedWallLine.wallLine = newline;
                                    unitedWallLine.segmentA = segA;
                                    unitedWallLine.segmentB = segB;
                                    //Line2.Create(unitedWallLine.segmentA, unitedWallLine.segmentB, out var newline);

                                    WallToDXF(unitedWallLine, "AvereinigteLinien", DXFcolor.green);

                                    removedWalls.Add(extWallLines[j]);
                                    removedWalls.Add(extWallLines[k]);

                                    if(!extWallLines.Contains(unitedWallLine))
                                        extUniWalls.Add(unitedWallLine);
                                }
                            }
                            else if(ValidDistToIntersect(extWallLines[j].segmentA, extWallLines[k].segmentA) ||
                                ValidDistToIntersect(extWallLines[j].segmentA, extWallLines[k].segmentB) ||
                                ValidDistToIntersect(extWallLines[j].segmentB, extWallLines[k].segmentA) ||
                                ValidDistToIntersect(extWallLines[j].segmentB, extWallLines[k].segmentB))
                            {
                                //var unitedWallLine = new LinePoints();
                                var pts = new List<Point2>() { extWallLines[j].segmentA, extWallLines[j].segmentB, extWallLines[k].segmentA, extWallLines[k].segmentB };

                                var segA = UnitedLine(pts, 0);
                                var segB = UnitedLine(pts, 1);

                                if(!segA.Equals(segB))
                                {
                                    Line2.Create(segA, segB, out var newline);

                                    Console.WriteLine(segA.X + "  " + segA.Y);
                                    Console.WriteLine(segB.X + "  " + segB.Y);

                                    var unitedWallLine = new LinePoints();

                                    unitedWallLine.wallLine = newline;
                                    unitedWallLine.segmentA = segA;
                                    unitedWallLine.segmentB = segB;
                                    //Line2.Create(unitedWallLine.segmentA, unitedWallLine.segmentB, out var newline);

                                    WallToDXF(unitedWallLine, "AvereinigteLinien", DXFcolor.green);

                                    removedWalls.Add(extWallLines[j]);
                                    removedWalls.Add(extWallLines[k]);

                                    if(!extWallLines.Contains(unitedWallLine))
                                        extUniWalls.Add(unitedWallLine);

                                    //extUniWalls.Add(unitedWallLine);
                                }
                            }
                        }
                    }
                }
            }

            foreach(var wallR in removedWalls)
            {
                WallToDXF(wallR, "removed", DXFcolor.red);

                if(extWallLines.Contains(wallR))
                    extWallLines.Remove(wallR);
            }

            extUniWalls.AddRange(extWallLines);

            foreach(var nextLine in extUniWalls)
            {
                // Console.WriteLine("NextLine: " + nextLine.wallLine.ToString() + ", SegA: " + (nextLine.segmentA.X - nextLine.segmentB.X) + ", SegB: " + (nextLine.segmentA.Y - nextLine.segmentB.Y));

                WallToDXF(nextLine, "extuniwalls", DXFcolor.green);
            }

            firstLine = extUniWalls[0];

            NextIntersectionPt(firstLine);
        }

        public static void NextIntersectionPt(LinePoints startLine)
        {
            try
            {
                var nextLine = new LinePoints();

                for(var k = 0; k < extUniWalls.Count; k++)
                {
                    LinePoints followLine;

                    if(extUniWalls.Count == 1)
                    {
                        followLine = firstLine;
                    }
                    else
                    {
                        followLine = extUniWalls[k];
                    }

                    if(startLine != followLine)
                    {
                        if(Point2.Create(startLine.wallLine, followLine.wallLine, out var realIntersec))  //nur wenn Schnittpunkt zw Linien gefunden wird

                        {
                            if(realIntersec.X != 0 && realIntersec.Y != 0)
                            {
                                var lineAsegA = startLine.segmentA;
                                var lineAsegB = startLine.segmentB;
                                var lineBsegA = followLine.segmentA;
                                var lineBsegB = followLine.segmentB;

                                if(ValidIntersecPt(lineAsegA, lineAsegB, realIntersec) &&                     //Fall 1
                                    ValidIntersecPt(lineBsegA, lineBsegB, realIntersec))
                                {
                                    PointsToDXF(realIntersec, "RealIntersecsCase1", DXFcolor.yellow);

                                    if(!realIntersecPts.Contains(realIntersec))

                                    {                                       //keine doppelten Punkte
                                        realIntersecPts.Add(realIntersec);

                                        nextLine = followLine;
                                        //extUniWalls.Remove(startLine);
                                    }

                                    //if(!startLine.Equals(firstLine))
                                    break;
                                }
                                else if((ValidDistToIntersect(realIntersec, lineAsegA) || ValidDistToIntersect(realIntersec, lineAsegB)) &&                     //Fall 2a
                                    !ValidIntersecPt(lineAsegA, lineAsegB, realIntersec) &&
                                    ValidIntersecPt(lineBsegA, lineBsegB, realIntersec))
                                {
                                    PointsToDXF(realIntersec, "RealIntersecsCase2a", DXFcolor.cyan);

                                    if(!realIntersecPts.Contains(realIntersec))

                                    {                                       //keine doppelten Punkte
                                        realIntersecPts.Add(realIntersec);

                                        nextLine = followLine;
                                        //extUniWalls.Remove(startLine);
                                    }

                                    //if(!startLine.Equals(firstLine))
                                    break;
                                }
                                else if((ValidDistToIntersect(realIntersec, lineBsegA) || ValidDistToIntersect(realIntersec, lineBsegB)) &&                     //Fall 2b
                                    !ValidIntersecPt(lineBsegA, lineBsegB, realIntersec) &&
                                    ValidIntersecPt(lineAsegA, lineAsegB, realIntersec))
                                {
                                    PointsToDXF(realIntersec, "RealIntersecsCase2b", DXFcolor.cyan);

                                    if(!realIntersecPts.Contains(realIntersec))

                                    {                                       //keine doppelten Punkte
                                        realIntersecPts.Add(realIntersec);

                                        nextLine = followLine;
                                        //extUniWalls.Remove(startLine);
                                    }

                                    if(!startLine.Equals(firstLine))
                                        break;
                                }
                                else if(((ValidDistToIntersect(realIntersec, lineAsegA) || ValidDistToIntersect(realIntersec, lineAsegB)) &&       //Fall 3
                                    (ValidDistToIntersect(realIntersec, lineBsegA) || ValidDistToIntersect(realIntersec, lineBsegB)) &&
                                    !ValidIntersecPt(lineAsegA, lineAsegB, realIntersec) &&
                                    !ValidIntersecPt(lineBsegA, lineBsegB, realIntersec)))
                                {
                                    PointsToDXF(realIntersec, "RealIntersecsCase3", DXFcolor.green);

                                    if(!realIntersecPts.Contains(realIntersec))

                                    {                                       //keine doppelten Punkte
                                        realIntersecPts.Add(realIntersec);

                                        nextLine = followLine;
                                        //extUniWalls.Remove(startLine);
                                    }
                                    //if(!startLine.Equals(firstLine))
                                    break;
                                }
                                else
                                    continue;
                                //neue Berechnung Strahlen (Schnittberechnung möglich, aber außerhalb der Kanten)
                            }
                            else
                                continue;
                            //neue Berechnung Strahlen (Schnittberechnung nicht möglich, parallele Kanten)
                        }
                    }
                }

                //Console.WriteLine("NextLine: " + nextLine.wallLine.ToString() + ", SegA: " + (nextLine.segmentA.X - nextLine.segmentB.X) + ", SegB: " + (nextLine.segmentA.Y - nextLine.segmentB.Y));

                extUniWalls.Remove(startLine);

                Console.WriteLine(extUniWalls.Count);

                if(extUniWalls.Count > 0)
                    NextIntersectionPt(nextLine);

                //if(extUniWalls.Count == 1)
                //    extUniWalls.Remove(followLine);

                //if(extUniWalls.Count == 0)
                //    NextIntersectionPt(firstLine);
            }

            catch(Exception ex) { Console.WriteLine(ex); }
        }

        //----------------------------------------------------------------------------------------------

        //Hilfsmethoden und Klassen

        public static void DensifyRayOrigins(int densLev) // TO DO: Ändern zu neuem Ansatz (fortlaufendes Halbieren der Cvx-kKnte)
        {
            bundleList.Clear();

            foreach(var cvxO in cvxOuterLines)
            {
                WallToDXF(cvxO, "convexLineOutside", DXFcolor.yellow);

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

            var XsegA = Math.Round(segmentA.X, 4);
            var YsegA = Math.Round(segmentA.Y, 4);
            var XsegB = Math.Round(segmentB.X, 4);
            var YsegB = Math.Round(segmentB.Y, 4);

            var Xintersec = Math.Round(intersec.X, 4);
            var Yintersec = Math.Round(intersec.Y, 4);

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

            if(Xintersec <= xMax && Xintersec >= xMin && Yintersec <= yMax && Yintersec >= yMin)
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
            var deg = 5;

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