using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IfcGeoRefChecker.Appl
{
    public class Level40 : IEquatable<Level40>
    {
        public bool GeoRef40 { get; set; }

        public IList<string> Reference_Object { get; set; }

        public IList<string> Instance_Object_WCS { get; set; }

        public IList<string> Instance_Object_North { get; set; }

        public IList<double> ProjectLocation { get; set; }

        public IList<double> ProjectRotationX { get; set; }

        public IList<double> ProjectRotationZ { get; set; }

        public IList<double> TrueNorthXY { get; set; }

        public bool Equals(Level40 other)
        {
            if(other == null)
                return false;
            if(ProjectLocation[0] == other.ProjectLocation[0] &&
                ProjectLocation[1] == other.ProjectLocation[1] &&
                ProjectLocation[2] == other.ProjectLocation[2] &&
                ProjectRotationX[0] == other.ProjectRotationX[0] &&
                ProjectRotationX[1] == other.ProjectRotationX[1] &&
                ProjectRotationX[2] == other.ProjectRotationX[2] &&
                ProjectRotationZ[0] == other.ProjectRotationZ[0] &&
                ProjectRotationZ[1] == other.ProjectRotationZ[1] &&
                ProjectRotationZ[2] == other.ProjectRotationZ[2] &&
                TrueNorthXY[0] == other.TrueNorthXY[0] &&
                TrueNorthXY[1] == other.TrueNorthXY[1])

            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private PlacementXYZ plcmXYZ = new PlacementXYZ();

        private IIfcAxis2Placement plcm;

        //private IIfcDirection dir;

        private IIfcGeometricRepresentationContext prjCtx;

        private IfcStore model;

        //GeoRef 40: read the WorldCoordinateSystem and TrueNorth attribute of IfcGeometricRepresentationContext
        //-------------------------------------------------------------------------------------------------------

        public Level40(IfcStore model, int ifcInstance)
        {
            try
            {
                this.model = model;

                this.prjCtx = model.Instances.Where<IIfcGeometricRepresentationContext>(s => s.GetHashCode() == ifcInstance).Single();

                this.Reference_Object = new List<string>
                    {
                        {"#" + prjCtx.GetHashCode() },
                        {prjCtx.GetType().Name }
                    };

                //variable for the WorldCoordinatesystem attribute
                this.plcm = prjCtx.WorldCoordinateSystem;

                this.Instance_Object_WCS = new List<string>();
            }
            catch(Exception e)
            {
                MessageBox.Show("Error occured while initializing LoGeoRef40 instance. \r\nError message: " + e.Message);
            }
        }

        public void GetLevel40()
        {
            try
            {
                if(this.plcm != null)
                {
                    this.Instance_Object_WCS.Add("#" + this.plcm.GetHashCode());
                    this.Instance_Object_WCS.Add(this.plcm.GetType().Name);
                }
                else
                {
                    this.Instance_Object_WCS.Add("IfcAxis2Placement");
                    this.Instance_Object_WCS.Add("n/a");
                }

                if(this.plcm is IIfcAxis2Placement3D)
                {
                    this.plcmXYZ.GetPlacementXYZ(this.plcm);

                    this.GeoRef40 = this.plcmXYZ.GeoRefPlcm;
                    this.ProjectLocation = this.plcmXYZ.LocationXYZ;
                    this.ProjectRotationX = this.plcmXYZ.RotationX;
                    this.ProjectRotationZ = this.plcmXYZ.RotationZ;
                }

                if(this.plcm is IIfcAxis2Placement2D)
                {
                    this.plcmXYZ.GetPlacementXYZ(this.plcm);

                    this.GeoRef40 = this.plcmXYZ.GeoRefPlcm;
                    this.ProjectLocation = this.plcmXYZ.LocationXYZ;
                    this.ProjectRotationX = this.plcmXYZ.RotationX;
                }

                //variable for the TrueNorth attribute
                var dir = prjCtx.TrueNorth;

                this.Instance_Object_North = new List<string>();
                this.TrueNorthXY = new List<double>();

                if(dir != null)
                {
                    this.Instance_Object_North.Add("#" + dir.GetHashCode());
                    this.Instance_Object_North.Add(dir.GetType().Name);

                    this.TrueNorthXY.Add(dir.DirectionRatios[0]);
                    this.TrueNorthXY.Add(dir.DirectionRatios[1]);
                }
                else
                {
                    this.Instance_Object_North.Add("IfcDirection");
                    this.Instance_Object_North.Add("n/a");

                    //if omitted, default values (see IFC schema for IfcGeometricRepresentationContext):

                    this.TrueNorthXY.Add(0);
                    this.TrueNorthXY.Add(1);
                }
            }
            catch(Exception e)
            {
                MessageBox.Show("Error occured while reading LoGeoRef40 attribute values. \r\nError message: " + e.Message);
            }
        }

        public void UpdateLevel40()
        {
            try
            {
                using(var txn = this.model.BeginTransaction(model.FileName + "_transedit"))
                {
                    this.plcmXYZ.LocationXYZ = this.ProjectLocation;
                    this.plcmXYZ.RotationX = this.ProjectRotationX;
                    this.plcmXYZ.RotationZ = this.ProjectRotationZ;

                    this.plcmXYZ.UpdatePlacementXYZ(model);

                    var schema = model.IfcSchemaVersion.ToString();

                    if(schema == "Ifc2X3")
                    {
                        this.prjCtx.TrueNorth = model.Instances.New<Xbim.Ifc2x3.GeometryResource.IfcDirection>(d => d.SetXY(this.TrueNorthXY[0], this.TrueNorthXY[1]));
                    }
                    else
                    {
                        this.prjCtx.TrueNorth = model.Instances.New<Xbim.Ifc4.GeometryResource.IfcDirection>(d => d.SetXY(this.TrueNorthXY[0], this.TrueNorthXY[1]));
                    }

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
                MessageBox.Show("Error occured while updating LoGeoRef40 attribute values to IfcFile. \r\nError message: " + e.Message);
            }
        }

        public string LogOutput()
        {
            string logLevel40 = "";
            string line = "\r\n________________________________________________________________________________________________________________________________________";
            string dashline = "\r\n----------------------------------------------------------------------------------------------------------------------------------------";

            logLevel40 += "\r\n \r\nProject context attributes for georeferencing (Location: WorldCoordinateSystem / Rotation: TrueNorth)"
            + dashline + "\r\n Project context element:" + this.Reference_Object[0] + "=" + this.Reference_Object[1]
            + "\r\n Placement referenced in " + this.Instance_Object_WCS[0] + "=" + this.Instance_Object_WCS[1];

            if(plcm is IIfcAxis2Placement2D)
            {
                logLevel40 += "\r\n  X = " + this.ProjectLocation[0] + "\r\n  Y = " + this.ProjectLocation[1];
                logLevel40 += $"\r\n  Rotation X-axis = ({this.ProjectRotationX[0]}/{this.ProjectRotationX[1]})";
            }

            if(plcm is IIfcAxis2Placement3D)
            {
                logLevel40 += "\r\n  X = " + this.ProjectLocation[0] + "\r\n  Y = " + this.ProjectLocation[1] + "\r\n  Z = " + this.ProjectLocation[2];
                logLevel40 += $"\r\n  Rotation X-axis = ({this.ProjectRotationX[0]}/{this.ProjectRotationX[1]}/{this.ProjectRotationX[2]})";
                logLevel40 += $"\r\n  Rotation Z-axis = ({this.ProjectRotationZ[0]}/{this.ProjectRotationZ[1]}/{this.ProjectRotationZ[2]})";
            }

            if(this.Instance_Object_North.Contains("n/a"))

            {
                logLevel40 += "\r\n \r\n No rotation regarding True North mentioned.";
            }
            else
            {
                logLevel40 += "\r\n \r\n True North referenced in " + this.Instance_Object_North[0] + "=" + this.Instance_Object_North[1]
                    + "\r\n  X-component =" + this.TrueNorthXY[0]
                    + "\r\n  Y-component =" + this.TrueNorthXY[1];
            }

            logLevel40 += "\r\n \r\n LoGeoRef 40 = " + this.GeoRef40 + "\r\n" + line;

            return logLevel40;
        }
    }
}