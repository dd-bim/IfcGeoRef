using System;
using System.Collections.Generic;

namespace IfcGeoRefChecker.Appl
{
    public class Level30 : IEquatable<Level30>
    {
        public bool GeoRef30 { get; set; }

        public IList<string> Reference_Object { get; set; } = new List<string>();

        public IList<string> Instance_Object { get; set; } = new List<string>();

        public IList<double> ObjectLocationXYZ { get; set; } = new List<double>();

        public IList<double> ObjectRotationX { get; set; } = new List<double>();

        public IList<double> ObjectRotationZ { get; set; } = new List<double>();

        public bool Equals(Level30 other)
        {
            if(other == null)
                return false;
            if(ObjectLocationXYZ[0] == other.ObjectLocationXYZ[0] &&
                ObjectLocationXYZ[1] == other.ObjectLocationXYZ[1] &&
                ObjectLocationXYZ[2] == other.ObjectLocationXYZ[2] &&
                ObjectRotationX[0] == other.ObjectRotationX[0] &&
                ObjectRotationX[1] == other.ObjectRotationX[1] &&
                ObjectRotationX[2] == other.ObjectRotationX[2] &&
                ObjectRotationZ[0] == other.ObjectRotationZ[0] &&
                ObjectRotationZ[1] == other.ObjectRotationZ[1] &&
                ObjectRotationZ[2] == other.ObjectRotationZ[2])

            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}