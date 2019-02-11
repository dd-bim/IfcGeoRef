using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Xbim.Ifc4.Interfaces;

namespace IfcGeoRefChecker.Appl
{
    public class Level10 : IEquatable<Level10>
    {
        public bool GeoRef10 { get; set; }

        public IList<string> Reference_Object { get; set; }

        public IList<string> Instance_Object { get; set; }

        public List<string> AddressLines { get; set; }

        public string Postalcode { get; set; }

        public string Town { get; set; }

        public string Region { get; set; }

        public string Country { get; set; }

        private IIfcPostalAddress address;

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

        //GeoRef 10: read all IfcPostalAddress-objects which are referenced by IfcSite or IfcBuilding
        //--------------------------------------------------------------------------------------------
        public Level10() { }


        public Level10(IIfcSpatialStructureElement spatialElement)
        {
            if(spatialElement is IIfcSite)
            {
                address = (spatialElement as IIfcSite).SiteAddress;
            }
            else if(spatialElement is IIfcBuilding)
            {
                address = (spatialElement as IIfcBuilding).BuildingAddress;
            }
            else
            { address = null; }
        }

        public void GetLevel10(IIfcSpatialStructureElement spatialElement)
        {
            try
            {

                this.Reference_Object = new List<string>
                    {
                        {"#" + spatialElement.GetHashCode()},
                        {spatialElement.ExpressType.ToString()},
                        {spatialElement.GlobalId},
                    };

                this.Instance_Object = new List<string>
                    {
                        {"IfcPostalAddress"},
                        {"n/a"}
                    };

                this.AddressLines = new List<string>();
                this.AddressLines.Add("n/a");
                this.AddressLines.Add("n/a");
                this.AddressLines.Add("n/a");

                this.Postalcode = "n/a";
                this.Town = "n/a";
                this.Region = "n/a";
                this.Country = "n/a";

                if(address != null)
                {
                    this.GeoRef10 = true;

                    this.Instance_Object[0] = "#" + address.GetHashCode();
                    this.Instance_Object[1] = address.GetType().Name;

                    this.AddressLines.Clear();

                    if(address.AddressLines.Count == 0)
                    {
                        this.AddressLines.Add("n/a");
                        this.AddressLines.Add("n/a");
                        this.AddressLines.Add("n/a");
                    }

                    if(address.AddressLines.Count == 1)
                    {
                        this.AddressLines.Add(address.AddressLines[0]);
                        this.AddressLines.Add("n/a");
                        this.AddressLines.Add("n/a");
                    }

                    if(address.AddressLines.Count == 2)
                    {
                        this.AddressLines.Add(address.AddressLines[0]);
                        this.AddressLines.Add(address.AddressLines[1]);
                        this.AddressLines.Add("n/a");
                    }

                    if(address.AddressLines.Count >= 3)
                    {
                        this.AddressLines.Add(address.AddressLines[0]);
                        this.AddressLines.Add(address.AddressLines[1]);
                        this.AddressLines.Add(address.AddressLines[2]);
                    }

                    this.Postalcode = (address.PostalCode.HasValue == true) ? address.PostalCode.ToString() : "n/a";
                    this.Town = (address.Town.HasValue == true) ? address.Town.ToString() : "n/a";
                    this.Region = (address.Region.HasValue == true) ? address.Region.ToString() : "n/a";
                    this.Country = (address.Country.HasValue == true) ? address.Country.ToString() : "n/a";
                }
                else
                {
                    this.AddressLines.Add("n/a");
                    this.AddressLines.Add("n/a");
                    this.AddressLines.Add("n/a");

                    this.GeoRef10 = false;
                }
            }

            catch(Exception e)
            {
                MessageBox.Show("Error occured while reading LoGeoRef10 attribute values. \r\nError message: " + e.Message);
            }
        }

        public string LogOutput()
        {
            string logLevel10 = "";
            string line = "\r\n________________________________________________________________________________________________________________________________________";
            string dashline = "\r\n----------------------------------------------------------------------------------------------------------------------------------------";

            logLevel10 += "Existing addresses referenced by IfcSite or IfcBuilding" + dashline + "\r\n";

            if(this.Instance_Object.Contains("n/a"))

            {
                logLevel10 += "\r\n " + this.Reference_Object[0] + "=" + this.Reference_Object[1] + " references no address.";
            }
            else
            {
                logLevel10 += "Found address referenced by " + this.Reference_Object[0] + "=" + this.Reference_Object[1] + ":\r\n" + this.Instance_Object[0] + "=" + this.Instance_Object[1] + "\r\n Address: \r\n";

                for(int i = 0; i < this.AddressLines.Count; i++)
                {
                    if(this.AddressLines[0] == "n/a")
                    {
                        logLevel10 += " n/a";
                        break;
                    }
                    else
                    {
                        if(this.AddressLines[i] == "n/a")
                        {
                            break;
                        }
                        else
                        {
                            logLevel10 += "  " + this.AddressLines[i] + "\r\n";
                        }
                    }
                }

                logLevel10 += "\r\n Postal code: " + this.Postalcode + "\r\n Town: " + this.Town + "\r\n Region: " + this.Region + "\r\n Country: " + this.Country;
            }

            logLevel10 += "\r\n \r\n LoGeoRef 10 = " + this.GeoRef10 + line;

            return logLevel10;
        }
    }
}