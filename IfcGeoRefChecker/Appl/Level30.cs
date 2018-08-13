using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IfcGeoRefChecker.Appl
{
    public class Level30 : IEquatable<Level30>
    {
        public bool GeoRef30 { get; set; }

        public IList<string> Reference_Object { get; set; }

        public IList<string> Instance_Object { get; set; }

        public IList<double> ObjectLocationXYZ { get; set; }

        public IList<double> ObjectRotationX { get; set; }

        public IList<double> ObjectRotationZ { get; set; }

        public bool Equals(Level30 other)
        {
            if(other == null)
                return false;
            return ObjectLocationXYZ[0] == other.ObjectLocationXYZ[0] &&
                ObjectLocationXYZ[1] == other.ObjectLocationXYZ[1] &&
                ObjectLocationXYZ[2] == other.ObjectLocationXYZ[2] &&
                ObjectRotationX[0] == other.ObjectRotationX[0] &&
                ObjectRotationX[1] == other.ObjectRotationX[1] &&
                ObjectRotationX[2] == other.ObjectRotationX[2] &&
                ObjectRotationZ[0] == other.ObjectRotationZ[0] &&
                ObjectRotationZ[1] == other.ObjectRotationZ[1] &&
                ObjectRotationZ[2] == other.ObjectRotationZ[2];
        }



        private PlacementXYZ plcm = new PlacementXYZ();

        private IIfcAxis2Placement3D plcm3D;

        private IfcStore model;

        //GeoRef 30: read all Spatial Structure Elements with the "highest" Local Placement --> that means their placment is not relative to an other elements placement
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------

        public Level30(IfcStore model, string ifcInstance, string ifcType)
        {
            try
            {
                this.model = model;

                var elem = model.Instances.Where<IIfcProduct>(s => s.GetHashCode().ToString() == ifcInstance).Single();

                var elemPlcm = (IIfcLocalPlacement)elem.ObjectPlacement;
                this.plcm3D = (IIfcAxis2Placement3D)elemPlcm.RelativePlacement;

                this.Reference_Object = new List<string>
                    {
                        {"#" + elem.GetHashCode() },
                        {elem.GetType().Name }
                    };

                this.Instance_Object = new List<string>
                    {
                        {"#" + plcm3D.GetHashCode() },
                        {plcm3D.GetType().Name }
                    };
            }

            catch(Exception e)
            {
                MessageBox.Show("Error occured while checking for LoGeoRef30: \r\n" + e.Message);
            }
        }

        public void GetLevel30()
        {
            this.plcm.GetPlacementXYZ(this.plcm3D);

            this.GeoRef30 = this.plcm.GeoRefPlcm;
            this.ObjectLocationXYZ = this.plcm.LocationXYZ;
            this.ObjectRotationX = this.plcm.RotationX;
            this.ObjectRotationZ = this.plcm.RotationZ;
        }

        public void UpdateLevel30()
        {
            using(var txn = this.model.BeginTransaction(model.FileName + "_transedit"))
            {
                this.plcm.LocationXYZ = this.ObjectLocationXYZ;
                this.plcm.RotationX = this.ObjectRotationX;
                this.plcm.RotationZ = this.ObjectRotationZ;

                this.plcm.UpdatePlacementXYZ(model);

                txn.Commit();
            }

            model.SaveAs(model.FileName + "_edit");
        }

        public string LogOutput()
        {
            string logLevel30 = "";
            string line = "\r\n________________________________________________________________________________________________________________________________________";
            string dashline = "\r\n----------------------------------------------------------------------------------------------------------------------------------------";

            logLevel30 += "\r\n \r\nLocal placement for uppermost Elements (usually an instance of IfcSite or IfcBuilding)"
                + "\r\nThe placement of those elements is only relative to the WorldCoordinateSystem (see LoGeoRef 40) but not to other IFC-Elements"
                + dashline
                + "\r\n Referencing Element:" + this.Reference_Object[0] + "=" + this.Reference_Object[1]
                + "\r\n Placement referenced in " + this.Instance_Object[0] + "=" + this.Instance_Object[1];

            logLevel30 += "\r\n  X = " + this.ObjectLocationXYZ[0] + "\r\n  Y = " + this.ObjectLocationXYZ[1] + "\r\n  Z = " + this.ObjectLocationXYZ[2];

            logLevel30 += $"\r\n Rotation X-axis = ({this.ObjectRotationX[0]}/{this.ObjectRotationX[1]}/{this.ObjectRotationX[2]})";

            logLevel30 += $"\r\n Rotation Z-axis = ({this.ObjectRotationZ[0]}/{this.ObjectRotationZ[1]}/{this.ObjectRotationZ[2]})";

            logLevel30 += "\r\n \r\n LoGeoRef 30 = " + this.GeoRef30 + "\r\n" + line;

            return logLevel30;
        }
    }
}