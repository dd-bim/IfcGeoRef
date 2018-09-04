using System.Collections.Generic;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IfcGeoRefChecker.Appl
{
    internal class PlacementXYZ
    {
        public IList<double> LocationXYZ { get; set; }

        public IList<double> RotationX { get; set; }

        public IList<double> RotationZ { get; set; }

        public bool GeoRefPlcm { get; set; }

        private IIfcAxis2Placement3D plcm3D;

        public void GetPlacementXYZ(IIfcAxis2Placement plcm)
        {
            this.plcm3D = (IIfcAxis2Placement3D)plcm;

            this.LocationXYZ = new List<double> //must be given, if IfcAxis2Placment3D exists
            {
                plcm3D.Location.X,
                plcm3D.Location.Y,
                plcm3D.Location.Z,
            };

            this.RotationX = new List<double>();

            if(plcm3D.RefDirection != null)

            {
                this.RotationX.Add(plcm3D.RefDirection.DirectionRatios[0]);
                this.RotationX.Add(plcm3D.RefDirection.DirectionRatios[1]);
                this.RotationX.Add(plcm3D.RefDirection.DirectionRatios[2]);
            }
            else  //if omitted, default values (see IFC schema for IfcAxis2Placment3D)
            {
                this.RotationX.Add(1);
                this.RotationX.Add(0);
                this.RotationX.Add(0);
            }

            this.RotationZ = new List<double>();

            if(plcm3D.Axis != null)

            {
                this.RotationZ.Add(plcm3D.Axis.DirectionRatios[0]);
                this.RotationZ.Add(plcm3D.Axis.DirectionRatios[1]);
                this.RotationZ.Add(plcm3D.Axis.DirectionRatios[2]);
            }
            else  //if omitted, default values (see IFC schema for IfcAxis2Placment3D)
            {
                this.RotationZ.Add(0);
                this.RotationZ.Add(0);
                this.RotationZ.Add(1);
            }

            if((plcm3D.Location.X > 0) || (plcm3D.Location.Y > 0) || (plcm3D.Location.Z > 0))
            {
                //by definition: ONLY in this case there could be an georeferencing
                this.GeoRefPlcm = true;
            }
            else
            {
                this.GeoRefPlcm = false;
            }
        }

        public void UpdatePlacementXYZ(IfcStore model)
        {
            var schema = model.IfcSchemaVersion.ToString();

            if(schema == "Ifc2X3")
            {
                plcm3D.Location = model.Instances.New<Xbim.Ifc2x3.GeometryResource.IfcCartesianPoint>(p => p.SetXYZ(this.LocationXYZ[0], this.LocationXYZ[1], this.LocationXYZ[2]));
                plcm3D.RefDirection = model.Instances.New<Xbim.Ifc2x3.GeometryResource.IfcDirection>(d => d.SetXYZ(this.RotationX[0], this.RotationX[1], this.RotationX[2]));
                plcm3D.Axis = model.Instances.New<Xbim.Ifc2x3.GeometryResource.IfcDirection>(d => d.SetXYZ(this.RotationZ[0], this.RotationZ[1], this.RotationZ[2]));
            }
            else
            {
                plcm3D.Location = model.Instances.New<Xbim.Ifc4.GeometryResource.IfcCartesianPoint>(p => p.SetXYZ(this.LocationXYZ[0], this.LocationXYZ[1], this.LocationXYZ[2]));
                plcm3D.RefDirection = model.Instances.New<Xbim.Ifc4.GeometryResource.IfcDirection>(d => d.SetXYZ(this.RotationX[0], this.RotationX[1], this.RotationX[2]));
                plcm3D.Axis = model.Instances.New<Xbim.Ifc4.GeometryResource.IfcDirection>(d => d.SetXYZ(this.RotationZ[0], this.RotationZ[1], this.RotationZ[2]));
            }
        }
    }
}