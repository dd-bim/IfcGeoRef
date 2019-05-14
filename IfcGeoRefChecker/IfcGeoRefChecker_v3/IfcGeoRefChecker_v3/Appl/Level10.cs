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

            var a = AddressLines.Count;
            var b = other.AddressLines.Count;
            if(a == b)
            {
                for(int i = 0; i < a; i++)
                {
                    if(!string.Equals(AddressLines[i], other.AddressLines[i]))
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }
            if(string.Equals(Postalcode, other.Postalcode) &&
                   string.Equals(Town, other.Town) &&
                   string.Equals(Region, other.Region) &&
                   string.Equals(Country, other.Country))
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