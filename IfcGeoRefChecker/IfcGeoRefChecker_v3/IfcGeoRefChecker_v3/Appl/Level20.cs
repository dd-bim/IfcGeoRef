using System;
using System.Collections.Generic;

namespace IfcGeoRefChecker.Appl
{
    public class Level20 : IEquatable<Level20>
    {
        public bool GeoRef20 { get; set; }

        public IList<string> Reference_Object { get; set; } = new List<string>();

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public double? Elevation { get; set; }

        public bool Equals(Level20 other)
        {
            if(other == null)
                return false;
            if(Latitude == other.Latitude &&
               Longitude == other.Longitude &&
               Elevation == other.Elevation)
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