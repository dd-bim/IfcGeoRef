using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IfcGeoRefChecker.Appl
{
    public class Level10
    {
        public bool GeoRef10 { get; set; }

        public IList<string> Reference_Object { get; set; }

        public IList<string> Instance_Object { get; set; }

        public IList<string> AddressLines { get; set; }

        public string Postalcode { get; set; }

        public string Town { get; set; }

        public string Region { get; set; }

        public string Country { get; set; }

        private IIfcPostalAddress address;

        private IfcStore model;

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
                    //get all IfcPostalAddress-objects, referenced by IfcSite:
                    address = model.Instances.Where<IIfcSite>(s => s.GetHashCode().ToString() == ifcInstance).Select(s => s.SiteAddress).Single();
                    //addr = elem.SiteAddress;
                }
                else if(ifcType == "IfcBuilding")
                {
                    //get all IfcPostalAddress-objects, referenced by IfcSite:
                    address = model.Instances.Where<IIfcBuilding>(s => s.GetHashCode().ToString() == ifcInstance).Select(s => s.BuildingAddress).Single();
                    //addr = elem.BuildingAddress;
                }
                else
                { address = null; }

                //statement for LoGeoRef_10 decision
                if(address == null)
                {
                    this.GeoRef10 = false;
                }
                else
                {
                    this.GeoRef10 = true;
                }
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

                if(address.AddressLines.Count > 3)
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
                ;
            };
        }

        public void UpdateLevel10()
        {
            using(var txn = this.model.BeginTransaction(model.FileName + "_transedit"))
            {
                var p = this.address;

                p.AddressLines.Clear();
                p.AddressLines.Add(this.AddressLines[0]);
                p.AddressLines.Add(this.AddressLines[1]);
                p.AddressLines.Add(this.AddressLines[2]);

                p.PostalCode = this.Postalcode;
                p.Town = this.Town;
                p.Region = this.Region;
                p.Country = this.Country;

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
                            logLevel10 += "  " + this.AddressLines[i];
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