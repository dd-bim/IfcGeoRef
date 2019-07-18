using System;
using System.Collections.Generic;
using System.Linq;
using BimGisCad.Representation.Geometry.Composed;
using IfcGeometryExtractor;
using Xbim.Ifc4.Interfaces;

namespace IfcGeoRefChecker.Appl
{
    public class BldgContourCalculator
    {
        public string GetBldgContour(IList<IIfcBuildingElement> elems, string unit)
        {
            string WKTstring = "";

            try
            {
                var calc = new Calculation();
                var wallLined = new List<Segment2>();

                //-------------------Auslesen der Geometrie pro Slab oder Wall, Geometrie je nach Typ--------------------------------------
                foreach(var singleElem in elems)
                {
                    //Ermitteln der Werte für Local Placement
                    var plcmt = singleElem.ObjectPlacement;

                    //derzeit nur Fall IfcLocalPlacement (Erweiterung für Grid,... nötig)
                    var bldgSystem = calc.GetAbsolutePlacement(plcmt);                                  //globales Bauwerkssystem wird ermittelt (ohne SiteSystem)

                    //Auslesen der Repräsentationstypen
                    //-----------------------------------

                    var repTypes = singleElem.Representation.Representations;

                    var repBody = from rep in repTypes
                                  where rep.RepresentationIdentifier == "Body"
                                  select rep;

                    var wallDetec = calc.GetBodyGeometry(repBody.FirstOrDefault());              //wenn vorhanden. ist Body-Geometrie immer maßgebend

                    if(wallDetec.Count > 0)
                    {
                        wallLined.AddRange(wallDetec);

                        foreach(var wl in wallDetec)
                        {
                            double dX = wl.Start.X - wl.End.X;
                            double dY = wl.Start.Y - wl.End.Y;
                        }
                    }
                    else
                    {       //keine Body-Geometrie gefunden bzw. Auslesen der Repräsentation derzeit nicht implementiert
                        var repFootprint = from rep in repTypes
                                           where rep.RepresentationIdentifier == "Footprint"
                                           select rep;

                        var repBox = from rep in repTypes
                                     where rep.RepresentationIdentifier == "Box"
                                     select rep;

                        if(repFootprint.Any())          //erste Wahl: Footprint
                        {
                            wallLined.AddRange(calc.GetFootprintGeometry(repFootprint.FirstOrDefault()));
                        }
                        else if(repBox.Any())           //zweite Wahl: BoundingBox
                        {
                            wallLined.AddRange(calc.GetBboxGeometry(repBox.FirstOrDefault()));
                        }
                        else
                        {                               //dritte Wahl: Axis (vermutlich nur bei Walls, ultima ratio)
                            var repAxis = from rep in repTypes
                                          where rep.RepresentationIdentifier == "Axis"
                                          select rep;

                            var axisWall = calc.GetAxisGeometry(repAxis.FirstOrDefault());

                            wallLined.AddRange(axisWall);
                        }
                    }
                }

                //------------------------------------------------------------------------------------------------------------------------

                wallLined = calc.ConvertToMeter(wallLined, unit);

                //-----------------Zusammenführen gleichartiger Segmente-------------------------------------------------------------------
                calc.CleanUpWallLines(wallLined, 0);
                var uniqueWalls = new List<Segment2>();

                foreach(var w in calc.WallLinedClean)
                    uniqueWalls.Add(w);
                //-------------------------------------------------------------------------------------------------------------------------

                //-----------------Berechnung der Polygonpunkte (Hauptbestandteil) + Ausgabe als WKT-string--------------------------------
                var polyPts = calc.GetPolygonPts(uniqueWalls);

                var output = new Output();

                WKTstring = output.CreateWKTstring(polyPts);
            }
            catch(Exception ex)
            {
                WKTstring = ex.Message;
            }

            return WKTstring;
        }
    }
}