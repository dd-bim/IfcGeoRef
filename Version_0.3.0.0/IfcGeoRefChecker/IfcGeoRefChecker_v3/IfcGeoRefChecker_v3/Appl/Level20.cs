using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Xbim.Ifc;
using Xbim.Ifc4.DateTimeResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;

namespace IfcGeoRefChecker.Appl
{
    public class Level20 : IEquatable<Level20>
    {
        public bool GeoRef20 { get; set; }

        public IList<string> Instance_Object { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public double Elevation { get; set; }

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

        private IIfcSite site;

        private IfcStore model;

        public Level20() { }

        public Level20(IfcStore model, int ifcInstance)
        {
            try
            {
                this.model = model;

                this.site = model.Instances.Where<IIfcSite>(s => s.GetHashCode() == ifcInstance).Single();

                this.Instance_Object = new List<string>
                    {
                        {"#" + site.GetHashCode() },
                        {site.GetType().Name }
                    };
            }
            catch(Exception e)
            {
                MessageBox.Show("Error occured while initializing LoGeoRef20 instance. \r\nError message: " + e.Message);
            }
        }

        public void GetLevel20()
        {
            try
            {
                if(site.RefLatitude.HasValue == false || site.RefLongitude.HasValue == false)
                {
                    this.Latitude = -999999;
                    this.Longitude = -999999;

                    this.GeoRef20 = false;
                }
                else
                {
                    this.Latitude = site.RefLatitude.Value.AsDouble;
                    this.Longitude = site.RefLongitude.Value.AsDouble;

                    this.GeoRef20 = true;
                }

                this.Elevation = (site.RefElevation.HasValue == true) ? site.RefElevation.Value : -999999;
            }
            catch(Exception e)
            {
                MessageBox.Show("Error occured while reading LoGeoRef20 attribute values. \r\nError message: " + e.Message);
            }
        }

        public void UpdateLevel20(IfcStore newModel)
        {
            try
            {
                using(var txn = newModel.BeginTransaction(model.FileName + "_transedit"))
                {
                    IfcTimeStamp create;

                    if(this.site.OwnerHistory != null)
                    {
                        // timestamp for element before reference is added
                        create = this.site.OwnerHistory.CreationDate;
                    }
                    else
                    {
                        create = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                    }

                    var dms = new Calc().DDtoCompound(this.Latitude);

                    var list = new List<long>
                {
                    { Convert.ToInt64(dms[0]) },
                    { Convert.ToInt64(dms[1]) },
                    { Convert.ToInt64(dms[2]) },
                    { Convert.ToInt64(dms[3]) }
                };

                    this.site.RefLatitude = new IfcCompoundPlaneAngleMeasure(list);

                    var dms1 = new Calc().DDtoCompound(this.Longitude);

                    var list1 = new List<long>
                {
                    { Convert.ToInt64(dms1[0]) },
                    { Convert.ToInt64(dms1[1]) },
                    { Convert.ToInt64(dms1[2]) },
                    { Convert.ToInt64(dms1[3]) }
                };

                    this.site.RefLongitude = new IfcCompoundPlaneAngleMeasure(list1);

                    this.site.RefElevation = this.Elevation;

                    if(this.site.OwnerHistory != null)
                    {

                        // set timestamp back (xBim creates a new OwnerHistory object)
                        this.site.OwnerHistory.CreationDate = create;

                        // timestamp for last modifiedDate in OwnerHistory
                        long timestamp = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                        this.site.OwnerHistory.LastModifiedDate = new Xbim.Ifc4.DateTimeResource.IfcTimeStamp(timestamp);
                        this.site.OwnerHistory.ChangeAction = IfcChangeActionEnum.MODIFIED;
                    }

                    txn.Commit();
                }

                var pos = newModel.FileName.LastIndexOf(".");
                var file = newModel.FileName.Substring(0, pos);

                newModel.SaveAs(file + "_edit");
            }
            catch(Exception e)
            {
                MessageBox.Show("Error occured while updating LoGeoRef20 attribute values to IfcFile. \r\nError message: " + e.Message);
            }
        }

        public string LogOutput()
        {
            string logLevel20 = "";
            string line = "\r\n________________________________________________________________________________________________________________________________________";
            string dashline = "\r\n----------------------------------------------------------------------------------------------------------------------------------------";

            logLevel20 += "\r\n \r\nGeographic coordinates referenced by IfcSite (Latitude / Longitude / Elevation)" + dashline + "\r\n";

            if((this.Longitude == -999999) || (this.Latitude == -999999))
            {
                logLevel20 += "\r\n " + this.Instance_Object[0] + "=" + this.Instance_Object[1] + " has no geographic coordinates.";
            }
            else
            {
                logLevel20 += "Referenced in " + this.Instance_Object[0] + "=" + this.Instance_Object[1] + ":\r\n Latitude: " + this.Latitude + "\r\n Longitude: " + this.Longitude;
            }

            if(this.Elevation == -999999)
            {
                logLevel20 += "\r\n " + this.Instance_Object[0] + "=" + this.Instance_Object[1] + " has no Elevation.";
            }
            else
            {
                logLevel20 += "\r\n Elevation: " + this.Elevation;
            }

            logLevel20 += "\r\n \r\n LoGeoRef 20 = " + this.GeoRef20 + line;

            return logLevel20;
        }
    }
}