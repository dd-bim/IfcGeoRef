using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.PropertyResource;

namespace IfcGeoRefChecker.Appl
{
    public class GeoRefChecker
    {
        public string GlobalID { get; set; }
        public string IFCSchema { get; set; }
        public string TimeCreation { get; set; }
        public string TimeCheck { get; set; }
        public string LengthUnit { get; set; }
        public string WKTRep { get; set; }
        public List<Level10> LoGeoRef10 { get; set; } = new List<Level10>();
        public List<Level20> LoGeoRef20 { get; set; } = new List<Level20>();
        public List<Level30> LoGeoRef30 { get; set; } = new List<Level30>();
        public List<Level40> LoGeoRef40 { get; set; } = new List<Level40>();
        public List<Level50> LoGeoRef50 { get; set; } = new List<Level50>();

        private IO.IfcReader obj;

        public GeoRefChecker(string jsonString)
        {
            try
            {
                JObject jsonObj = JObject.Parse(jsonString);

                this.GlobalID = jsonObj["GlobalID"].ToString();
                this.IFCSchema = jsonObj["IFCSchema"].ToString();
                this.TimeCheck = jsonObj["TimeCheck"].ToString();
                this.TimeCreation = jsonObj["TimeCreation"].ToString();

                this.LengthUnit = jsonObj["LengthUnit"].ToString();

                var lev10 = jsonObj["LoGeoRef10"].Children();

                foreach(var res in lev10)
                {
                    var l10 = new Level10();
                    JsonConvert.PopulateObject(res.ToString(), l10);
                    this.LoGeoRef10.Add(l10);
                }

                var lev20 = jsonObj["LoGeoRef20"].Children();

                foreach(var res in lev20)
                {
                    var l20 = new Level20();
                    JsonConvert.PopulateObject(res.ToString(), l20);
                    this.LoGeoRef20.Add(l20);
                }

                var lev30 = jsonObj["LoGeoRef30"].Children();

                foreach(var res in lev30)
                {
                    var l30 = new Level30();
                    JsonConvert.PopulateObject(res.ToString(), l30);
                    this.LoGeoRef30.Add(l30);
                }

                var lev40 = jsonObj["LoGeoRef40"].Children();

                foreach(var res in lev40)
                {
                    var l40 = new Level40();
                    JsonConvert.PopulateObject(res.ToString(), l40);
                    this.LoGeoRef40.Add(l40);
                }

                var lev50 = jsonObj["LoGeoRef50"].Children();

                foreach(var res in lev50)
                {
                    var l50 = new Level50();
                    JsonConvert.PopulateObject(res.ToString(), l50);
                    this.LoGeoRef50.Add(l50);
                }
            }
            catch(Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        public GeoRefChecker(IfcStore model)
        {
            this.obj = new IO.IfcReader(model);

            var bldg = obj.BldgReader().First();
            var site = obj.SiteReader().First();
            var proj = obj.ProjReader().First();
            var prods = obj.UpperPlcmProdReader();

            this.GlobalID = proj.GlobalId;
            this.IFCSchema = model.SchemaVersion.ToString();
            this.TimeCreation = model.Header.TimeStamp;
            this.TimeCheck = DateTime.UtcNow.ToString("s", System.Globalization.CultureInfo.InvariantCulture);      //UTC timestamp
            this.LengthUnit = obj.LengthUnitReader();

            this.LoGeoRef10.Add(GetLevel10(bldg));
            this.LoGeoRef10.Add(GetLevel10(site));

            this.LoGeoRef20.Add(GetLevel20(site));

            foreach(var prod in prods)
            {
                this.LoGeoRef30.Add(GetLevel30(prod));
            }

            this.LoGeoRef40.Add(GetLevel40(proj));

            if(model.SchemaVersion.ToString() != "Ifc2X3")
            {
                this.LoGeoRef50.Add(GetLevel50(proj));
            }
            else
            {
                var psetMap = obj.PSetReaderMap();
                var psetCrs = obj.PSetReaderCRS();

                if(psetMap.Count > 0 && psetCrs.Count > 0)
                {
                    this.LoGeoRef50.Add(GetLevel50(psetMap.First(), psetCrs.First()));
                }
                else
                {
                    var l50f = new Level50();
                    l50f.GeoRef50 = false;
                    l50f.Reference_Object = GetInfo(proj);
                    this.LoGeoRef50.Add(l50f);
                }
            }

            var jsonObj = JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public Level10 GetLevel10(IIfcSpatialStructureElement spatialElement)
        {
            var l10 = new Appl.Level10();

            try
            {
                IIfcPostalAddress address = null;

                if(spatialElement is IIfcSite)
                {
                    address = (spatialElement as IIfcSite).SiteAddress;
                }
                else
                {
                    address = (spatialElement as IIfcBuilding).BuildingAddress;
                }

                l10.Reference_Object = GetInfo(spatialElement);

                //l10.Reference_Object = new List<string>
                //    {
                //        {"#" + spatialElement.GetHashCode()},
                //        {spatialElement.ExpressType.ToString()},
                //        {spatialElement.GlobalId},
                //    };

                if(address != null)
                {
                    l10.GeoRef10 = true;

                    l10.Instance_Object = GetInfo(address);

                    //l10.Instance_Object[0] = "#" + address.GetHashCode();
                    //l10.Instance_Object[1] = address.GetType().Name;

                    //l10.AddressLines.Clear();

                    var alines = address.AddressLines;

                    if(alines != null)
                    {
                        foreach(var a in alines)
                        {
                            l10.AddressLines.Add(a);
                        }
                    }
                    else
                    {
                        l10.AddressLines.Add("n/a");
                    }

                    //    l10.AddressLines.Add("n/a");
                    //    l10.AddressLines.Add("n/a");
                    //    l10.AddressLines.Add("n/a");
                    //}

                    //if(address.AddressLines.Count == 1)
                    //{
                    //    l10.AddressLines.Add(address.AddressLines[0]);
                    //    l10.AddressLines.Add("n/a");
                    //    l10.AddressLines.Add("n/a");
                    //}

                    //if(address.AddressLines.Count == 2)
                    //{
                    //    l10.AddressLines.Add(address.AddressLines[0]);
                    //    l10.AddressLines.Add(address.AddressLines[1]);
                    //    l10.AddressLines.Add("n/a");
                    //}

                    //if(address.AddressLines.Count >= 3)
                    //{
                    //    l10.AddressLines.Add(address.AddressLines[0]);
                    //    l10.AddressLines.Add(address.AddressLines[1]);
                    //    l10.AddressLines.Add(address.AddressLines[2]);
                    //}

                    l10.Postalcode = (address.PostalCode.HasValue) ? address.PostalCode.ToString() : "n/a";
                    l10.Town = (address.Town.HasValue) ? address.Town.ToString() : "n/a";
                    l10.Region = (address.Region.HasValue) ? address.Region.ToString() : "n/a";
                    l10.Country = (address.Country.HasValue) ? address.Country.ToString() : "n/a";
                }
                else
                {
                    //l10.AddressLines.Add("n/a");
                    //l10.AddressLines.Add("n/a");
                    //l10.AddressLines.Add("n/a");

                    l10.GeoRef10 = false;
                }
            }

            catch(Exception e)
            {
                MessageBox.Show("Error occured while reading LoGeoRef10 attribute values. \r\nError message: " + e.Message);
            }

            return l10;
        }

        public Level20 GetLevel20(IIfcSite site)
        {
            var l20 = new Level20();

            try
            {
                l20.Reference_Object = GetInfo(site);

                if(site.RefLatitude.HasValue || site.RefLongitude.HasValue)
                {
                    l20.Latitude = site.RefLatitude.Value.AsDouble;
                    l20.Longitude = site.RefLongitude.Value.AsDouble;

                    l20.GeoRef20 = true;
                }
                else
                {
                    l20.Latitude = null;
                    l20.Longitude = null;

                    l20.GeoRef20 = false;
                }

                l20.Elevation = site.RefElevation.Value;
            }
            catch(Exception e)
            {
                MessageBox.Show("Error occured while reading LoGeoRef20 attribute values. \r\nError message: " + e.Message);
            }

            return l20;
        }

        public Level30 GetLevel30(IIfcProduct elem)
        {
            var l30 = new Level30();

            try
            {
                var elemPlcm = (IIfcLocalPlacement)elem.ObjectPlacement;
                var plcm3D = (IIfcAxis2Placement3D)elemPlcm.RelativePlacement;

                l30.Reference_Object = GetInfo(elem);

                l30.Instance_Object = GetInfo(plcm3D);

                var plcm = new PlacementXYZ(plcm3D);

                l30.GeoRef30 = plcm.GeoRefPlcm;
                l30.ObjectLocationXYZ = plcm.LocationXYZ;
                l30.ObjectRotationX = plcm.RotationX;
                l30.ObjectRotationZ = plcm.RotationZ;
            }
            catch(Exception e)
            {
                MessageBox.Show("Error occured while reading LoGeoRef30 attribute values. \r\nError message: " + e.Message);
            }

            return l30;
        }

        public Level40 GetLevel40(IIfcProject proj)
        {
            var l40 = new Level40();

            try
            {
                l40.Reference_Object = GetInfo(proj);

                var prjCtx = obj.ContextReader(proj).First();

                l40.Instance_Object = GetInfo(prjCtx);

                //variable for the WorldCoordinatesystem attribute
                var plcm = prjCtx.WorldCoordinateSystem;
                var plcmXYZ = new PlacementXYZ(plcm);

                l40.GeoRef40 = plcmXYZ.GeoRefPlcm;
                l40.ProjectLocation = plcmXYZ.LocationXYZ;
                l40.ProjectRotationX = plcmXYZ.RotationX;

                if(plcm is IIfcAxis2Placement3D)
                {
                    l40.ProjectRotationZ = plcmXYZ.RotationZ;
                }

                //variable for the TrueNorth attribute
                var dir = prjCtx.TrueNorth;

                l40.TrueNorthXY = new List<double>();

                if(dir != null)
                {
                    l40.TrueNorthXY.Add(dir.DirectionRatios[0]);
                    l40.TrueNorthXY.Add(dir.DirectionRatios[1]);
                }
                else
                {
                    //if omitted, default values (see IFC schema for IfcGeometricRepresentationContext):

                    l40.TrueNorthXY.Add(0);
                    l40.TrueNorthXY.Add(1);
                }
            }
            catch(Exception e)
            {
                MessageBox.Show("Error occured while reading LoGeoRef40 attribute values. \r\nError message: " + e.Message);
            }

            return l40;
        }

        public Level50 GetLevel50(IIfcProject proj)
        {
            var l50 = new Level50();

            try
            {
                l50.Reference_Object = GetInfo(proj);

                var prjCtx = obj.ContextReader(proj).First();

                var mapCvs = obj.MapReader(prjCtx);

                if(mapCvs.Count > 0)
                {
                    var map = mapCvs.First();

                    l50.Instance_Object = GetInfo(map);

                    l50.RotationXY = new List<double>();

                    l50.Translation_Eastings = map.Eastings;
                    l50.Translation_Northings = map.Northings;
                    l50.Translation_Orth_Height = map.OrthogonalHeight;

                    if(map.XAxisAbscissa.HasValue && map.XAxisOrdinate.HasValue)
                    {
                        l50.RotationXY[0] = map.XAxisOrdinate.Value;
                        l50.RotationXY[1] = map.XAxisAbscissa.Value;
                    }
                    //else
                    //{
                    //    //if omitted, values for no rotation (angle = 0) applied (consider difference to True North)

                    //    l50.RotationXY.Add(0);
                    //    l50.RotationXY.Add(1);
                    //}

                    l50.Scale = (map.Scale.HasValue) ? map.Scale.Value : 1;

                    var mapCRS = (IIfcProjectedCRS)map.TargetCRS;

                    if(mapCRS != null)
                    {
                        l50.CRS_Name = (mapCRS.Name != null) ? mapCRS.Name.ToString() : "n/a";
                        l50.CRS_Description = (mapCRS.Description != null) ? mapCRS.Description.ToString() : "n/a";
                        l50.CRS_Geodetic_Datum = (mapCRS.GeodeticDatum != null) ? mapCRS.GeodeticDatum.ToString() : "n/a";
                        l50.CRS_Vertical_Datum = (mapCRS.VerticalDatum != null) ? mapCRS.VerticalDatum.ToString() : "n/a";
                        l50.CRS_Projection_Name = (mapCRS.MapProjection != null) ? mapCRS.MapProjection.ToString() : "n/a";
                        l50.CRS_Projection_Zone = (mapCRS.MapZone != null) ? mapCRS.MapZone.ToString() : "n/a";
                    }

                    l50.GeoRef50 = true;
                }
                else
                {
                    l50.GeoRef50 = false;
                }
            }

            catch(Exception e)
            {
                MessageBox.Show("Error occured while reading LoGeoRef50 attribute values. \r\nError message: " + e.Message);
            }

            return l50;
        }

        public Level50 GetLevel50(IIfcPropertySet psetMap, IIfcPropertySet psetCrs)
        {
            var l50 = new Level50();

            try
            {
                l50.Translation_Eastings = ((IfcLengthMeasure)((psetMap.HasProperties.Where(p => p.Name == "Eastings").Single()) as IfcPropertySingleValue).NominalValue);

                l50.Translation_Northings = ((IfcLengthMeasure)((psetMap.HasProperties.Where(p => p.Name == "Northings").Single()) as IfcPropertySingleValue).NominalValue);

                l50.Translation_Orth_Height = ((IfcLengthMeasure)((psetMap.HasProperties.Where(p => p.Name == "OrthogonalHeight").Single()) as IfcPropertySingleValue).NominalValue);

                l50.RotationXY.Add(((IfcReal)((psetMap.HasProperties.Where(p => p.Name == "XAxisAbscissa").Single()) as IfcPropertySingleValue).NominalValue));

                l50.RotationXY.Add(((IfcReal)((psetMap.HasProperties.Where(p => p.Name == "XAxisOrdinate").Single()) as IfcPropertySingleValue).NominalValue));

                l50.Scale = ((IfcReal)((psetMap.HasProperties.Where(p => p.Name == "Scale").Single()) as IfcPropertySingleValue).NominalValue);

                l50.CRS_Name = (IfcLabel)((psetCrs.HasProperties.Where(p => p.Name == "Name").Single()) as IfcPropertySingleValue).NominalValue;
                l50.CRS_Description = (IfcText)((psetCrs.HasProperties.Where(p => p.Name == "Description").Single()) as IfcPropertySingleValue).NominalValue;
                l50.CRS_Geodetic_Datum = (IfcIdentifier)((psetCrs.HasProperties.Where(p => p.Name == "GeodeticDatum").Single()) as IfcPropertySingleValue).NominalValue;
                l50.CRS_Vertical_Datum = (IfcIdentifier)((psetCrs.HasProperties.Where(p => p.Name == "VerticalDatum").Single()) as IfcPropertySingleValue).NominalValue;
                l50.CRS_Projection_Name = (IfcIdentifier)((psetCrs.HasProperties.Where(p => p.Name == "MapProjection").Single()) as IfcPropertySingleValue).NominalValue;
                l50.CRS_Projection_Zone = (IfcIdentifier)((psetCrs.HasProperties.Where(p => p.Name == "MapZone").Single()) as IfcPropertySingleValue).NominalValue;
            }

            catch(Exception e)
            {
                MessageBox.Show("Error occured while reading LoGeoRef50 attribute values. \r\nError message: " + e.Message);
            }

            return l50;
        }

        private List<string> GetInfo(IPersistEntity entity)
        {
            var info = new List<string>
                    {
                        {"#" + entity.GetHashCode() },
                        {entity.GetType().Name },
                };

            if(entity is IIfcRoot)
            {
                info.Add((entity as IIfcRoot).GlobalId);
            }

            return info;
        }
    }
}