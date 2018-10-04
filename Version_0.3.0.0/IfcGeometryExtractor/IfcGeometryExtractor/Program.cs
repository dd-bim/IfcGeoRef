using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BimGisCad.Representation.Geometry;
using BimGisCad.Representation.Geometry.Elementary;
using netDxf;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IfcGeometryExtractor
{
    internal class Program
    {
        private static string nr;                                                       //Variable für IFC Modell - Auswahl
        private static DxfDocument dxf = new DxfDocument();                             //Variable für DXF-Datei (netDXF-Lib)

        private static List<Axis2Placement3D> plcmts = new List<Axis2Placement3D>();    //Liste von Placement-Objekten in Hierarchie PRO Wand
        private static Axis2Placement3D plcmCombined;                                   //globales Placement der Wand (ohne Geometrie-Koords)
        private static IList<Point2> absIFCPts = new List<Point2>();                    //globale Koordinaten der Wand (ohne WCS und MapConversion)
        private static Point2 ifcPtsCentroid;                                           //Schwerpunkt (Zentrum der Strahlenspinne)
        private static IList<List<Point2>> pairList = new List<List<Point2>>();         //Paare von Punkten, welche zu einer Wandachse gehören
        private static IList<LinePoints> wallLined = new List<LinePoints>();                         //Wandlinien für Schnittpunktberechnung mit Paaren der globalen Wandkoords

        private static IList<Line2> rays = new List<Line2>();                           //Strahlenspinne
        private static IList<Point2> extPts = new List<Point2>();                  //Schnittpunkte (Ray-Wandachse)

        private static IList<Point2> doubleList = new List<Point2>();

        //private static IList<Point2> extIntersecPts = new List<Point2>();               //äußere Schnittpunkte (Ray-Wandachse)
        private static IList<LinePoints> extWallLines = new List<LinePoints>();                   //Wandlinien, auf denen äußere Schnittpunkte liegen

        private static IList<Point2> realPts = new List<Point2>();                      //Schnittpunkte der gefundenen Wandachsen (sollten reale Wandecken sein)

        private static BboxIFC bbox;
        private static IList<RayBundle> bundleList = new List<RayBundle>();

        private static void Main(string[] args)
        {
            Console.WriteLine("Building geometry extractor");
            Console.WriteLine("Please choose a IFC model:");
            Console.WriteLine("...Schependomlaan --> 1");
            Console.WriteLine("...Haus_CG_ifc2x3_coordination_view --> 2");
            Console.WriteLine("...FZK-Haus --> 3 or default");
            Console.WriteLine("...schultz_residence --> 4");
            Console.WriteLine("...Haus-Constr --> 5");
            nr = Console.ReadLine();

            //Testmodelle:

            var source = "D:\\1_CityBIM\\1_Programmierung\\IfcGeoRef\\zzz_IFC_Testdateien\\";
            var path = source + "Projekt1.ifc";

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
                    path = source + "schultz_residence.ifc";
                    break;

                case "5":
                    path = source + "Haus-Constr.ifc";
                    break;

                default:
                    Console.WriteLine("Default model: Projekt1");
                    break;
            }
            Console.WriteLine();

            //Lade Modell

            var model = IfcStore.Open(path);

            if(model != null)
            {
                Console.WriteLine("Model successfully loaded.");
            }
            else
            {
                Console.WriteLine("NO model loaded.");
            }
            Console.WriteLine();

            //Extrahiere alle Walls

            var walls = model.Instances.OfType<IIfcWall>();

            Console.WriteLine("There are {0} walls in the choosen model.", walls.Count());

            //Schleife für alle Wände:

            foreach(var singleWall in walls)
            {
                Console.WriteLine(singleWall.Name + " , " + singleWall.PredefinedType);

                //Ermitteln der Werte für Local Placement

                var plcmt = singleWall.ObjectPlacement;

                //derzeit nur Fall IfcLocalPlacement (Erweiterung für Grid,... nötig)

                GetRelativePlacements(plcmt);

                plcmCombined = Axis2Placement3D.Combine(plcmts.ToArray());

                plcmts.Clear();

                //Ermitteln der Geometrie

                var reps = singleWall.Representation.Representations.Where(x => x.RepresentationIdentifier == "Axis");// .Select(x => x.Representation);

                GetAxisGeometry(reps); //theoretisch mehrere Repräsentationen einer Wand als Achse möglich
            }
            WriteDXF(absIFCPts, "wallAxisPoints", 1);

            var wallCentroid = CalcCentroidIFCPts(absIFCPts);
            WriteCoords(Point2.ToCSVString(wallCentroid), "centroid");
            IList<Point2> centr = new List<Point2>() { wallCentroid };
            WriteDXF(centr, "centroid", 3);

            CalcBbox(absIFCPts);

            DensifyRayOrigins(2);

            FindOuterIntersectionPts(wallLined);

            //FindIntersectionPts(wallLined);

            //FindExternalPoints();

            //FindExternalWalls();

            //CreateRealIntersecPts();

            //WriteDXF(intersecPts, "wallIntersectionPtsAll", 4);

            //WriteDXF(extPts, "wallIntersectonsPtsExt", 5);

            WriteDXF(bundleList.Select(o => o.rayOrigin).ToList(), "bundlePts", 6);

            WriteDXF(extPts, "wallIntersectonsPtsExt", 5);

            WriteDXF(doubleList, "doubleIntersecs", 4);

            //WriteDXF(realPts, "realPoints", 6);

            dxf.Save(path + ".dxf");

            System.Diagnostics.Process.Start(path + ".dxf");

            Console.ReadKey();
        }

        public static void GetRelativePlacements(IIfcObjectPlacement plcmRelObj)
        {
            if(plcmRelObj is IIfcLocalPlacement)
            {
                //Auslesen des lokaleren Elementes (Start:Wand)
                //--------------------------------------------------------------------------------------------------------------------------------------

                // (relative) Platzierung der Wand

                //Semantische Zuordnung zum IfcProduct:

                var prods = plcmRelObj.PlacesObject;

                foreach(var prod in prods)  //ein ObjectPlacement kann theoretisch mehrere Produkte plazieren
                {
                    Console.WriteLine(prod.GetHashCode() + "=" + prod.GetType().Name);
                }

                //Geometrische Platzierung:

                var axisPlcm = (IIfcAxis2Placement3D)(plcmRelObj as IIfcLocalPlacement).RelativePlacement; //axis2Placement

                var ptLocal = BimGisCad.Representation.Geometry.Elementary.Vector3.Create(axisPlcm.Location.X, axisPlcm.Location.Y, axisPlcm.Location.Z);

                Axis2Placement3D plcmLocal;

                if(axisPlcm.RefDirection != null && axisPlcm.Axis != null)
                {
                    Direction3.Create(BimGisCad.Representation.Geometry.Elementary.Vector3.Create(axisPlcm.RefDirection.X, axisPlcm.RefDirection.Y, axisPlcm.RefDirection.Z), out var refDirXLocal);
                    Direction3.Create(BimGisCad.Representation.Geometry.Elementary.Vector3.Create(axisPlcm.Axis.X, axisPlcm.Axis.Y, axisPlcm.Axis.Z), out var refDirZLocal);

                    plcmLocal = Axis2Placement3D.Create(ptLocal, refDirZLocal, refDirXLocal);
                }
                else
                {
                    plcmLocal = Axis2Placement3D.Create(ptLocal);
                }

                plcmts.Add(plcmLocal);

                //--------------------------------------------------------------------------------------------------------------------------------------

                //Auslesen des globaleren Elementes (Start: i.d.R. BuildingStorey)
                //--------------------------------------------------------------------------------------------------------------------------------------

                //Local Placement, zu welchem die Wand relativ steht (idR Placement von IfcBuildingStorey)

                var higherPlcm = (plcmRelObj as IIfcLocalPlacement).PlacementRelTo;

                if(higherPlcm != null)
                {
                    //Methode ruft sich selber auf bis keine relativen Platzierungen mehr vorhanden sind
                    GetRelativePlacements(higherPlcm);
                }
                else
                {
                    Console.WriteLine("No more relative Placements existent. Please refer now to the World Coordinate System.");
                }
            }
            else
            {
                Console.WriteLine("Currently only IfcLocalPlacement in scope.");
            }
        }

        public static void GetAxisGeometry(IEnumerable<IIfcRepresentation> axisRep)
        {
            foreach(var rep in axisRep)
            {
                if(rep is IIfcShapeRepresentation) //nur für Geometrie (!= TopologyRep or StyledRep)
                {
                    var lines = (rep as IIfcShapeRepresentation).Items.OfType<IIfcPolyline>().Single(); //zunächst nur Polylines mit CartesianPoints

                    var coords = lines.Points;

                    List<Point2> ptPair = new List<Point2>();

                    for(var i = 0; i < coords.Count; i++)
                    {
                        //Console.WriteLine("  -> Coords-Axis: " + coords[i].X + " , " + coords[i].Y);

                        //transformiere lokale Achskoordinaten in globales System:
                        Axis2Placement3D.ToGlobal(plcmCombined, Point2.Create(coords[i].X, coords[i].Y), out var globPt);

                        //erzeuge 2D-Punkt und füge diesen einer 2D-Punktliste hinzu
                        var pt2D = Point2.Create(globPt.X, globPt.Y);
                        absIFCPts.Add(pt2D);

                        //Console.WriteLine("  -> Coords-Axis (global): " + globPt.X + " , " + globPt.Y);

                        //schreibe Koordinaten in Textdatei (Kommata-getrennt):
                        WriteCoords(Point2.ToCSVString(Point2.Create(globPt.X, globPt.Y)), "wallAxisCoords"); //verebnet

                        //WriteCoords(Point3.ToCSVString(globPt)); // mit Höhen

                        //Rekonstruktion der Wandachsen:
                        ptPair.Add(pt2D);
                    }

                    //für Darstellung in DXF-Datei:
                    netDxf.Vector2 vec1 = new netDxf.Vector2(ptPair[0].X, ptPair[0].Y);
                    netDxf.Vector2 vec2 = new netDxf.Vector2(ptPair[1].X, ptPair[1].Y);

                    netDxf.Entities.Line wallLineDXF = new netDxf.Entities.Line(vec1, vec2);

                    wallLineDXF.Layer = new netDxf.Tables.Layer("IFCwallLines");

                    dxf.AddEntity(wallLineDXF);
                    //--------------------------------

                    var wallLine = new LinePoints();

                    wallLine.segmentA = ptPair[0];
                    wallLine.segmentB = ptPair[1];

                    //für Weiterverarbeitung im Programm:
                    Line2.Create(ptPair[0], ptPair[1], out var wallAxis);
                    wallLine.wallLine = wallAxis;

                    wallLined.Add(wallLine);

                    //pairList.Add(ptPair);
                }
            }
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
                        var intersecOrd = intersecs.OrderBy(d => d.distToRayOrigin);
                        var extSec = intersecOrd.First().intersection;

                        var extWalls = (from wall in wallLines
                                        where Line2.Touches(wall.wallLine, extSec)
                                        select wall);

                        foreach(var extWall in extWalls)
                        {
                            var extA = extWall.segmentA;
                            var extB = extWall.segmentB;

                            if(!ValidIntersecPt(extA, extB, extSec))            //Abbruch, wenn Schnittpunkt nicht auf DIESEM Wandsegment liegt
                                continue;                                       //verhindert Wandsegmente im Inneren, die auf gleicher Wandgerade, wie ein Außenwandsegment liegen

                            if(extWallLines.Contains(extWall))                  //Abbruch, wenn Liste externer Wände schon diese Wand enthält
                                continue;                                       //verhindert Linien und Punkte Overhead

                            extWallLines.Add(extWall);

                            netDxf.Vector2 extWallA = new netDxf.Vector2(extA.X, extA.Y);
                            netDxf.Vector2 extWallB = new netDxf.Vector2(extB.X, extB.Y);

                            netDxf.Entities.Line extWallDXF = new netDxf.Entities.Line(extWallA, extWallB);
                            extWallDXF.Layer = new netDxf.Tables.Layer("externalWalls");

                            dxf.AddEntity(extWallDXF);

                            extPts.Add(extSec);
                        }
                    }
                }
            }
        }

        //public static void FindExternalWalls()
        //{
        //    foreach(var pt in extPts)
        //    {
        //        for(var i = 0; i < wallLined.Count; i++)
        //        {
        //            var wall = wallLined[i].wallLine;

        //            if(Line2.Touches(wall, pt))
        //            {
        //                //Löscht gefundene Wandachse aus Menge aller Wandachsen
        //                //otherWallLines.Add(wall);

        //                //Fügt gefundene Wandachse zu Liste der äußerende Wandachsen hinzu
        //                extWallLines.Add(wall);

        //                netDxf.Vector2 extWallA = new netDxf.Vector2(wallLined[i].segmentA.X, wallLined[i].segmentA.Y);
        //                netDxf.Vector2 extWallB = new netDxf.Vector2(wallLined[i].segmentB.X, wallLined[i].segmentB.Y);

        //                netDxf.Entities.Line extWall = new netDxf.Entities.Line(extWallA, extWallB);
        //                extWall.Layer = new netDxf.Tables.Layer("externalWalls");

        //                dxf.AddEntity(extWall);
        //            }
        //        }
        //    }

        //extWallLines = extWallLines.Distinct().ToList();  //verringert im Bsp von 988 auf 380
        //Console.WriteLine("alte wandlinien = " + wallLined.Count);
        //    //Console.WriteLine("alte wandlinien (should be removed) = " + otherWallLines.Count);
        //    Console.WriteLine("neue wandlinien (external) = " + extWallLines.Count);
        //}

        //Variante mit Schwerpunkt:
        /*/
        public static void FindIntersectionPts(IList<LinePoints> wallLines)
        {
            var wallsSelect = new List<Line2>();

            rays = InitializeRays(ifcPtsCentroid);

            Console.WriteLine("pairs count" + wallLines.Count);

            foreach(var wallLine in wallLines)
            {
                foreach(var ray in rays)
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

                            Console.WriteLine("Intersection points detected: " + intersec.X + " / " + intersec.Y);

                            var sectCSV = Point2.ToCSVString(intersec);

                            WriteCoords(sectCSV, "intersectionCoords");

                            //wallsSelect.Add(wallLine.wallLine);

                            //break; // pro Wandachse wird ein Punkt (idR) gefunden, danach wird abgebrochen
                        }
                    }
                }
            }

            Console.Write("anzahl schnittpkt: " + intersecPts.Count);
        }

        public static void FindExternalPoints()
        {
            foreach(var ray in rays)
            {
                var distListA = new Dictionary<Point2, double>();
                var distListB = new Dictionary<Point2, double>();

                foreach(var sect in intersecPts)
                {
                    if(Line2.Touches(ray, sect))
                    {
                        var dx = sect.X - ifcPtsCentroid.X;
                        var dy = sect.Y - ifcPtsCentroid.Y;

                        //var dist = Math.Sqrt((dx * dx + dy * dy));

                        try
                        {
                            //A-Liste:
                            if((dx == 0 && dy > 0) || (dx > 0 && dy > 0) || (dx > 0 && dy == 0) || (dx > 0 && dy < 0))
                            {
                                var distA = Math.Sqrt((dx * dx + dy * dy));
                                distListA.Add(sect, distA);
                            }

                            //B-Liste:
                            if((dx == 0 && dy < 0) || (dx < 0 && dy < 0) || (dx < 0 && dy == 0) || (dx < 0 && dy > 0))
                            {
                                var distB = Math.Sqrt((dx * dx + dy * dy));
                                distListB.Add(sect, distB);
                            }

                            if(dx == 0 && dy == 0)
                            {
                                Console.WriteLine("Centroid equal to Intersec");
                            }
                        }
                        catch
                        {
                            Console.WriteLine("double Point by " + sect.X + " / " + sect.Y);
                            continue;
                        }
                    }
                }
                Console.WriteLine("A-side " + distListA.Count + " / B-side " + distListB.Count);
                //var pt = distList.Select(x => x.Values).Where(y => y.)

                try
                {
                    var maxDistA = distListA.Values.Max();
                    var maxDistB = distListB.Values.Max();

                    var maxPtsA = distListA.Where(x => x.Value == maxDistA).Select(y => y.Key);
                    var maxPtsB = distListB.Where(x => x.Value == maxDistB).Select(y => y.Key);

                    foreach(var maxPt in maxPtsA)
                    {
                        extIntersecPts.Add(maxPt);
                        WriteCoords(Point2.ToCSVString(maxPt), "ExternalPts");
                    }

                    foreach(var maxPt in maxPtsB)
                    {
                        extIntersecPts.Add(maxPt);
                        WriteCoords(Point2.ToCSVString(maxPt), "ExternalPts");
                    }
                }
                catch { }
            }
        }
        /*/

        /*/
        //Schnittpunkte der Wandachsen
        public static void CreateRealIntersecPts()
        {
            List<Point2> realIntersecPts = new List<Point2>();

            for(var i = 0; i < extWallLines.Count; i++)
            {
                for(var j = 0; j < extWallLines.Count; j++)
                {
                    Point2.Create(extWallLines[i], extWallLines[j], out var realIntersec);

                    realIntersecPts.Add(realIntersec);
                    //Ergebnis enthält alle Schnittpunkte aller Wandlinien miteinander --> führt ggf. zu falschen Schnittpunkten außerhalb des Hauses
                }
            }

            Console.WriteLine("realIntersecPoints, no filter" + realIntersecPts.Count);

            var clean = realIntersecPts.Distinct(); //filtert Wandpunkte, sodass jeder Punkt nur noch einmal vorkommt

            Console.WriteLine("realIntersecPoints, distinct" + clean.Count());

            foreach(var pt in clean)
            {
                var dx = Math.Abs(ifcPtsCentroid.X - pt.X);
                var dy = Math.Abs(ifcPtsCentroid.Y - pt.Y);

                if(!(dy > 100000 || dx > 100000))
                {                              //sinnvolle Schwelle?
                    WriteCoords(Point2.ToCSVString(pt), "realintersec");
                    realPts.Add(pt);
                }
            }
        }

     /*/

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

        public static void WriteDXF(IList<Point2> ptsXY, string layerName, int colorInt)
        {
            netDxf.Tables.Layer layer = new netDxf.Tables.Layer(layerName);

            foreach(var ptXY in ptsXY)
            {
                netDxf.Entities.Point pt = new netDxf.Entities.Point(ptXY.X, ptXY.Y, 0);

                netDxf.Entities.Circle circle = new netDxf.Entities.Circle(new netDxf.Vector2(ptXY.X, ptXY.Y), colorInt * 0.1);

                pt.Layer = layer;
                circle.Layer = layer;

                switch(colorInt)
                {
                    case 1:
                        layer.Color = new AciColor(255, 0, 0);
                        break;

                    case 2:
                        layer.Color = new AciColor(0, 255, 0);
                        break;

                    case 3:
                        layer.Color = new AciColor(0, 0, 255);
                        break;

                    case 4:
                        layer.Color = new AciColor(100, 200, 50);
                        break;

                    case 5:
                        layer.Color = new AciColor(200, 100, 200);
                        break;

                    case 6:
                        layer.Color = new AciColor(50, 50, 200);
                        break;
                }

                dxf.AddEntity(pt);
                dxf.AddEntity(circle);
            }
        }

        public static Point2 CalcCentroidIFCPts(IList<Point2> points2D)
        {
            ifcPtsCentroid = Point2.Centroid(points2D);
            return ifcPtsCentroid;
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

        //public static RayBundle CalcRayOrigins(IList<Point2> ifcPoints)
        //{
        //    var rayBundle = new RayBundle();

        //    var dictRayOrigins = new Dictionary<string, Point2>()
        //    {
        //        {"rayLL", Point2.Create(xMaxIFC - widthX, yMaxIFC - widthY) },
        //        {"rayUR", Point2.Create(xMaxIFC + widthX, yMaxIFC + widthY) },
        //        {"rayLR", Point2.Create(xMaxIFC + widthX, yMaxIFC - widthY) },
        //        {"rayUL", Point2.Create(xMaxIFC - widthX, yMaxIFC + widthY) },
        //        {"rayUL", Point2.Create(xMaxIFC - widthX, yMaxIFC + widthY) },
        //        {"centroid", Point2.Centroid(ifcPoints) }
        //    };

        //    return rayBundle;
        //}

        public static void DensifyRayOrigins(int densLev)
        {
            //var bundleList = new List<RayBundle>();

            switch(densLev)
            {
                case 0:
                    {
                        bundleList.Add(new RayBundle("LU", Point2.Create(bbox.lowerLeftPt.X - bbox.widthX, bbox.lowerLeftPt.Y - bbox.widthY)));
                        bundleList.Add(new RayBundle("RO", Point2.Create(bbox.upperRightPt.X + bbox.widthX, bbox.upperRightPt.Y + bbox.widthY)));
                        break;
                    }

                case 1:
                    {
                        bundleList.Add(new RayBundle("LO", Point2.Create(bbox.lowerLeftPt.X - bbox.widthX, bbox.upperRightPt.Y + bbox.widthY)));
                        bundleList.Add(new RayBundle("RU", Point2.Create(bbox.upperRightPt.X + bbox.widthX, bbox.lowerLeftPt.Y - bbox.widthY)));
                        goto case 0;
                    }

                case 2:
                    {
                        bundleList.Add(new RayBundle("ML", Point2.Create(bbox.lowerLeftPt.X - bbox.widthX / 2, bbox.lowerLeftPt.Y + bbox.widthY / 2)));
                        bundleList.Add(new RayBundle("MR", Point2.Create(bbox.upperRightPt.X + bbox.widthX / 2, bbox.upperRightPt.Y - bbox.widthY / 2)));
                        bundleList.Add(new RayBundle("MU", Point2.Create(bbox.lowerLeftPt.X + bbox.widthX / 2, bbox.lowerLeftPt.Y - bbox.widthY / 2)));
                        bundleList.Add(new RayBundle("MO", Point2.Create(bbox.upperRightPt.X - bbox.widthX / 2, bbox.upperRightPt.Y + bbox.widthY / 2)));
                        goto case 1;
                    }
            }
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