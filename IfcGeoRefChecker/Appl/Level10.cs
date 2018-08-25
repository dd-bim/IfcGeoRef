using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Xbim.Ifc;
using Xbim.Ifc4.ActorResource;
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

        private IIfcSpatialStructureElement elem;

        private IIfcPostalAddress address;

        private IfcStore model;

        public bool Equals(Level10 other)
        {
            if(other == null)
                return false;
            return string.Equals(AddressLines[0], other.AddressLines[0]) &&
                string.Equals(AddressLines[1], other.AddressLines[1]) &&
                string.Equals(AddressLines[2], other.AddressLines[2]) &&
                   string.Equals(Postalcode, other.Postalcode) &&
                   string.Equals(Town, other.Town) &&
                   string.Equals(Region, other.Region) &&
                   string.Equals(Country, other.Country);
        }

        //GeoRef 10: read all IfcPostalAddress-objects which are referenced by IfcSite or IfcBuilding
        //--------------------------------------------------------------------------------------------

        public Level10(IfcStore model, string ifcInstance, string ifcType)
        {
            try
            {
                this.model = model;

                this.Reference_Object = new List<string>
                    {
                        {"#" + ifcInstance},
                        {ifcType }
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

                if(ifcType == "IfcSite")
                {
                    elem = model.Instances.OfType<IIfcSite>().Where(s => s.GetHashCode().ToString() == ifcInstance).Single();

                    address = (elem as IIfcSite).SiteAddress;
                }
                else if(ifcType == "IfcBuilding")
                {
                    elem = model.Instances.OfType<IIfcBuilding>().Where(s => s.GetHashCode().ToString() == ifcInstance).Single();

                    address = (elem as IIfcBuilding).BuildingAddress;
                }
                else
                { address = null; }
            }

            catch(Exception e)
            {
                MessageBox.Show("Error occured while checking for LoGeoRef10: \r\n" + e.Message + e.StackTrace);
            }
        }

        public void GetLevel10()
        {
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

                if(address.AddressLines.Count > 3)
                {
                    MessageBox.Show("There are more than 3 address lines. Program only reads up to 3 lines.");
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

        public void UpdateLevel10()
        {
            using(var txn = this.model.BeginTransaction(model.FileName + "_transedit"))
            {
                if(this.address == null)
                {
                    this.address = this.model.Instances.New<IfcPostalAddress>();

                    // timestamp for element before reference is added
                    var create = this.elem.OwnerHistory.CreationDate;

                    if(this.elem is IIfcSite)
                    {
                        (this.elem as IIfcSite).SiteAddress = this.address;
                    }
                    else
                    {
                        (this.elem as IIfcBuilding).BuildingAddress = this.address;
                    }

                    // set timestamp back (xBim creates a new OwnerHistory object)
                    this.elem.OwnerHistory.CreationDate = create;
                }

                var p = this.address;

                p.AddressLines.Clear();
                p.AddressLines.Add(this.AddressLines[0]);
                p.AddressLines.Add(this.AddressLines[1]);
                p.AddressLines.Add(this.AddressLines[2]);

                p.PostalCode = this.Postalcode;
                p.Town = this.Town;
                p.Region = this.Region;
                p.Country = this.Country;

                // timestamp for last modifiedDate in OwnerHistory
                long timestamp = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                this.elem.OwnerHistory.LastModifiedDate = new Xbim.Ifc4.DateTimeResource.IfcTimeStamp(timestamp);
                this.elem.OwnerHistory.ChangeAction = IfcChangeActionEnum.MODIFIED;

                txn.Commit();
            }

            model.SaveAs(model.FileName + "_edit");
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