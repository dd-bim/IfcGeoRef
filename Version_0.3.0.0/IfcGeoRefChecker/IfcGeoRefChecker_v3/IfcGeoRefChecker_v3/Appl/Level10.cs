using System;
using System.Collections.Generic;

namespace IfcGeoRefChecker.Appl
{
    public class Level10 : IEquatable<Level10>
    {
        public bool GeoRef10 { get; set; }

        public IList<string> Reference_Object { get; set; } = new List<string>();

        public IList<string> Instance_Object { get; set; } = new List<string>();

        public List<string> AddressLines { get; set; } = new List<string>();

        public string Postalcode { get; set; }

        public string Town { get; set; }

        public string Region { get; set; }

        public string Country { get; set; }

        public bool Equals(Level10 other)
        {
            if(other == null)
                return false;
            if(string.Equals(AddressLines[0], other.AddressLines[0]) == true &&
                string.Equals(AddressLines[1], other.AddressLines[1]) == true &&
                string.Equals(AddressLines[2], other.AddressLines[2]) == true &&
                   string.Equals(Postalcode, other.Postalcode) == true &&
                   string.Equals(Town, other.Town) == true &&
                   string.Equals(Region, other.Region) == true &&
                   string.Equals(Country, other.Country) == true)
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