using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.RepresentationResource;

namespace IfcGeoRefChecker.Appl
{
    public class Level50 : IEquatable<Level50>
    {
        public bool GeoRef50 { get; set; }

        public IList<string> Reference_Object { get; set; }

        public IList<string> Instance_Object_Project { get; set; }

        public IList<string> Instance_Object_CRS { get; set; }

        public double Translation_Eastings { get; set; }

        public double Translation_Northings { get; set; }

        public double Translation_Orth_Height { get; set; }

        public IList<double> RotationXY { get; set; }

        public double Scale { get; set; }

        public string CRS_Name { get; set; } = "n/a";

        public string CRS_Description { get; set; } = "n/a";

        public string CRS_Geodetic_Datum { get; set; } = "n/a";

        public string CRS_Vertical_Datum { get; set; } = "n/a";

        public string CRS_Projection_Name { get; set; } = "n/a";

        public string CRS_Projection_Zone { get; set; } = "n/a";

        public bool Equals(Level50 other)
        {
            if(other == null)
                return false;
            if(Translation_Eastings == other.Translation_Eastings &&
                Translation_Northings == other.Translation_Northings &&
                Translation_Orth_Height == other.Translation_Orth_Height &&
                RotationXY[0] == other.RotationXY[0] &&
                RotationXY[1] == other.RotationXY[1] &&
                Scale == other.Scale &&
                string.Equals(CRS_Name, other.CRS_Name) &&
                string.Equals(CRS_Description, other.CRS_Description) &&
                string.Equals(CRS_Geodetic_Datum, other.CRS_Geodetic_Datum) &&
                string.Equals(CRS_Vertical_Datum, other.CRS_Vertical_Datum) &&
                string.Equals(CRS_Projection_Name, other.CRS_Projection_Name) &&
                string.Equals(CRS_Projection_Zone, other.CRS_Projection_Zone))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private IfcStore model;

        private IIfcMapConversion mapCvs;

        private IIfcProjectedCRS mapCRS;

        private IIfcGeometricRepresentationContext prjCtx;

        //GeoRef 50: read MapConversion, if referenced by IfcGeometricRepresentationContext (only in scope of IFC4 schema)
        //-----------------------------------------------------------------------------------------------------------------

        public Level50(IfcStore model, int ifcInstance)

        {
            try
            {
                this.model = model;

                this.prjCtx = model.Instances.Where<IIfcGeometricRepresentationContext>(s => s.GetHashCode() == ifcInstance).Single();

                this.Reference_Object = new List<string>
                                        {
                        {"IfcMapConversion"},
                        {"n/a"}
                    };

                this.Instance_Object_Project = new List<string>
                    {
                        {"#" + prjCtx.GetHashCode()},
                        {prjCtx.GetType().Name}
                    };

                var maps = model.Instances.OfType<IIfcMapConversion>();

                foreach(var map in maps)
                {
                    if(map.SourceCRS is IIfcCoordinateReferenceSystem)
                    {
                        MessageBox.Show("This instance of MapConversion references a conversion between two CRS. That case is not covered by the tool yet." +
                            "For changing of the conversion between those CRS please refer to the IFC-file at instance number: #" + this.mapCvs.GetHashCode() +
                            " and the corresponding attributes for SourceCRS and TargetCRS (attributes 1 and 2)");
                    }

                    if(map.SourceCRS is null)
                    {
                        MessageBox.Show("MapConversion does not contain a reference to the project context or to any other CRS. Thus the checked IFC-file is not valid.");
                    }

                    if(map.SourceCRS is IfcGeometricRepresentationContext)
                    {
                        this.mapCvs = map;
                    }
                }

                this.Instance_Object_CRS = new List<string>
                    {
                        {"IfcProjectedCRS"},
                        {"n/a"}
                    };

                this.RotationXY = new List<double>();
            }
            catch(Exception e)
            {
                MessageBox.Show("Error occured while initializing LoGeoRef50 instance. \r\nError message: " + e.Message);
            }
        }

        public void GetLevel50()
        {
            try
            {
                //restriction on IfcMapConversion objects which references (or inverse referenced by) IfcGeometricRepresentationContext
                if(prjCtx.HasCoordinateOperation.Count() != 0)
                {
                    //this.mapCvs = (IfcMapConversion)prjCtx.HasCoordinateOperation;

                    this.Reference_Object[0] = "#" + mapCvs.GetHashCode();
                    this.Reference_Object[1] = mapCvs.GetType().Name;

                    this.Translation_Eastings = (mapCvs.Eastings != null) ? mapCvs.Eastings : 0;
                    this.Translation_Northings = (mapCvs.Northings != null) ? mapCvs.Northings : 0;
                    this.Translation_Orth_Height = (mapCvs.OrthogonalHeight != null) ? mapCvs.OrthogonalHeight : 0;

                    if(mapCvs.XAxisAbscissa != null && mapCvs.XAxisOrdinate != null)
                    {
                        this.RotationXY.Add(mapCvs.XAxisOrdinate.Value);
                        this.RotationXY.Add(mapCvs.XAxisAbscissa.Value);
                    }
                    else
                    {
                        //if omitted, values for no rotation (angle = 0) applied (consider difference to True North)

                        this.RotationXY.Add(0);
                        this.RotationXY.Add(1);
                    }

                    this.Scale = (mapCvs.Scale != null) ? mapCvs.Scale.Value : 1;

                    this.mapCRS = (IIfcProjectedCRS)mapCvs.TargetCRS;

                    if(mapCRS != null)
                    {
                        this.Instance_Object_CRS[0] = "#" + mapCRS.GetHashCode();
                        this.Instance_Object_CRS[1] = mapCRS.GetType().Name;

                        this.CRS_Name = (mapCRS.Name != null) ? mapCRS.Name.ToString() : "n/a";
                        this.CRS_Description = (mapCRS.Description != null) ? mapCRS.Description.ToString() : "n/a";
                        this.CRS_Geodetic_Datum = (mapCRS.GeodeticDatum != null) ? mapCRS.GeodeticDatum.ToString() : "n/a";
                        this.CRS_Vertical_Datum = (mapCRS.VerticalDatum != null) ? mapCRS.VerticalDatum.ToString() : "n/a";
                        this.CRS_Projection_Name = (mapCRS.MapProjection != null) ? mapCRS.MapProjection.ToString() : "n/a";
                        this.CRS_Projection_Zone = (mapCRS.MapZone != null) ? mapCRS.MapZone.ToString() : "n/a";
                    }

                    this.GeoRef50 = true;
                }
                else
                {
                    this.GeoRef50 = false;

                    this.Reference_Object.Add("IfcMapConversion");
                    this.Reference_Object.Add("n/a");

                    this.Instance_Object_CRS.Add("IfcProjectedCRS");
                    this.Instance_Object_CRS.Add("n/a");

                    this.Translation_Eastings = 0;
                    this.Translation_Northings = 0;
                    this.Translation_Orth_Height = 0;

                    this.RotationXY.Add(0);
                    this.RotationXY.Add(1);

                    this.Scale = 1;

                    this.CRS_Name = "n/a";
                    this.CRS_Description = "n/a";
                    this.CRS_Geodetic_Datum = "n/a";
                    this.CRS_Vertical_Datum = "n/a";
                    this.CRS_Projection_Name = "n/a";
                    this.CRS_Projection_Zone = "n/a";
                }
            }
            catch(Exception e)
            {
                MessageBox.Show("Error occured while reading LoGeoRef50 attribute values. \r\nError message: " + e.Message);
            }
        }

        public void UpdateLevel50()
        {
            try
            {
                using(var txn = this.model.BeginTransaction(model.FileName + "_transedit"))
                {
                    if(mapCRS == null)
                    {
                        this.mapCRS = model.Instances.New<IfcProjectedCRS>();
                    }

                    this.mapCRS.Name = this.CRS_Name;
                    this.mapCRS.Description = this.CRS_Description;
                    this.mapCRS.GeodeticDatum = this.CRS_Geodetic_Datum;
                    this.mapCRS.VerticalDatum = this.CRS_Vertical_Datum;
                    this.mapCRS.MapProjection = this.CRS_Projection_Name;
                    this.mapCRS.MapZone = this.CRS_Projection_Zone;

                    var unit = model.Instances.New<IfcSIUnit>(u =>
                    {
                        u.UnitType = IfcUnitEnum.LENGTHUNIT;
                        u.Name = IfcSIUnitName.METRE;
                    });

                    this.mapCRS.MapUnit = unit;

                    if(mapCvs == null)
                    {
                        this.mapCvs = model.Instances.New<IfcMapConversion>(m =>
                        {
                            m.SourceCRS = prjCtx;
                        });
                    }

                    this.mapCvs.TargetCRS = (IfcProjectedCRS)this.mapCRS;

                    this.mapCvs.Eastings = this.Translation_Eastings;
                    this.mapCvs.Northings = this.Translation_Northings;
                    this.mapCvs.OrthogonalHeight = this.Translation_Orth_Height;
                    this.mapCvs.XAxisOrdinate = this.RotationXY[0];
                    this.mapCvs.XAxisAbscissa = this.RotationXY[1];
                    this.mapCvs.Scale = this.Scale;

                    // timestamp for last modifiedDate in OwnerHistory
                    long timestamp = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                    var proj = model.Instances.OfType<IIfcProject>().Single();

                    if(proj.OwnerHistory != null)
                    {
                        proj.OwnerHistory.LastModifiedDate = new Xbim.Ifc4.DateTimeResource.IfcTimeStamp(timestamp);
                        proj.OwnerHistory.ChangeAction = IfcChangeActionEnum.MODIFIED;
                    }

                    txn.Commit();
                }

                var pos = model.FileName.LastIndexOf(".");
                var file = model.FileName.Substring(0, pos);

                model.SaveAs(file + "_edit");
            }
            catch(Exception e)
            {
                MessageBox.Show("Error occured while updating LoGeoRef50 attribute values to IfcFile. \r\nError message: " + e.Message);
            }
        }

        public string LogOutput()
        {
            string logLevel50 = "";
            string line = "\r\n________________________________________________________________________________________________________________________________________";
            string dashline = "\r\n----------------------------------------------------------------------------------------------------------------------------------------";

            logLevel50 += "\r\n \r\nSpecific entities for georeferencing (only in scope of IFC4; IfcMapConversion references IfcGeometricRepresenationContext)" + dashline + "\r\n";

            if(this.Reference_Object != null && prjCtx.HasCoordinateOperation.Count() != 0)

            {
                logLevel50 += " Project Context element which is referenced by IfcMapConversion: " + this.Instance_Object_Project[0] + "=" + this.Instance_Object_Project[1]
                + "\r\n MapConversion element: " + this.Reference_Object[0] + "=" + this.Reference_Object[1]
                + "\r\n  Translation Eastings:" + this.Translation_Eastings
                + "\r\n  Translation Northings:" + this.Translation_Northings
                + "\r\n  Translation Height:" + this.Translation_Orth_Height
                + "\r\n  Rotation X-axis(Abscissa):" + this.RotationXY[0]
                + "\r\n  Rotation X-axis(Ordinate):" + this.RotationXY[1]
                + "\r\n  Scale:" + this.Scale
                + "\r\n CRS element: " + this.Instance_Object_CRS[0] + "=" + this.Instance_Object_CRS[1]
                + "\r\n  Name:" + this.CRS_Name
                + "\r\n  Description:" + this.CRS_Description
                + "\r\n  Geodetic Datum:" + this.CRS_Geodetic_Datum
                + "\r\n  Vertical Datum:" + this.CRS_Vertical_Datum
                + "\r\n  Projection Name:" + this.CRS_Projection_Name
                + "\r\n  Projection Zone:" + this.CRS_Projection_Zone;
            }
            else
            {
                logLevel50 += "\r\n No conversion of the world coordinate system (WCS) in a coordinate reference system (CRS) applicable.";
            }

            logLevel50 += "\r\n \r\n LoGeoRef 50 = " + this.GeoRef50 + "\r\n" + line;

            return logLevel50;
        }
    }
}