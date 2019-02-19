using System;
using System.Globalization;
using System.IO;
using System.Threading;

namespace IfcGeoRefChecker.IO
{
    public class LogOutput
    {
        public LogOutput(Appl.GeoRefChecker checkObj, string fileDirec, string file)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            string dashline = "\r\n----------------------------------------------------------------------------------------------------------------------------------------";
            var headline = $"\r\nExamination of {file}.ifc regarding georeferencing content ({DateTime.Now.ToShortDateString()}, {DateTime.Now.ToLongTimeString()})" + dashline + dashline + "\r\n";

            using(var writeLog = File.CreateText((fileDirec + ".txt")))
            {
                try
                {
                    writeLog.WriteLine(headline);
                    writeLog.WriteLine("IfcVersion: " + checkObj.IFCSchema);
                    writeLog.WriteLine("LengthUnit: " + checkObj.LengthUnit);

                    foreach(var lev in checkObj.LoGeoRef10)
                    {
                        writeLog.WriteLine(LogLevel10(lev));
                    }

                    foreach(var lev in checkObj.LoGeoRef20)
                    {
                        writeLog.WriteLine(LogLevel20(lev));
                    }

                    foreach(var lev in checkObj.LoGeoRef30)
                    {
                        writeLog.WriteLine(LogLevel30(lev));
                    }

                    foreach(var lev in checkObj.LoGeoRef40)
                    {
                        writeLog.WriteLine(LogLevel40(lev));
                    }

                    foreach(var lev in checkObj.LoGeoRef50)
                    {
                        writeLog.WriteLine(LogLevel50(lev));
                    }
                }

                catch(Exception ex)
                {
                    writeLog.WriteLine($"Error occured while writing Logfile. \r\n Message: {ex.Message}");
                }
            };
        }

        public string LogLevel10(Appl.Level10 lev)
        {
            string logLevel10 = "";
            string line = "\r\n________________________________________________________________________________________________________________________________________";
            string dashline = "\r\n----------------------------------------------------------------------------------------------------------------------------------------";

            logLevel10 += "Existing addresses referenced by IfcSite or IfcBuilding" + dashline + "\r\n";

            if(lev.Instance_Object.Count == 0)

            {
                logLevel10 += "\r\n " + lev.Reference_Object[0] + "=" + lev.Reference_Object[1] + " references no address.";
            }
            else
            {
                logLevel10 += "Found address referenced by " + lev.Reference_Object[0] + "=" + lev.Reference_Object[1] + ":\r\n" + lev.Instance_Object[0] + "=" + lev.Instance_Object[1] + "\r\n Address: \r\n";

                foreach(var a in lev.AddressLines)
                {
                    logLevel10 += a;
                }

                logLevel10 += "\r\n Postal code: " + lev.Postalcode + "\r\n Town: " + lev.Town + "\r\n Region: " + lev.Region + "\r\n Country: " + lev.Country;
            }

            logLevel10 += "\r\n \r\n LoGeoRef 10 = " + lev.GeoRef10 + line;

            return logLevel10;
        }

        public string LogLevel20(Appl.Level20 lev)

        {
            string logLevel20 = "";
            string line = "\r\n________________________________________________________________________________________________________________________________________";
            string dashline = "\r\n----------------------------------------------------------------------------------------------------------------------------------------";

            logLevel20 += "\r\n \r\nGeographic coordinates referenced by IfcSite (Latitude / Longitude / Elevation)" + dashline + "\r\n";

            if((lev.Longitude == null) || (lev.Latitude == null))
            {
                logLevel20 += "\r\n " + lev.Reference_Object[0] + "=" + lev.Reference_Object[1] + " has no geographic coordinates.";
            }
            else
            {
                logLevel20 += "Referenced in " + lev.Reference_Object[0] + "=" + lev.Reference_Object[1] + ":\r\n Latitude: " + lev.Latitude + "\r\n Longitude: " + lev.Longitude;
            }

            if(lev.Elevation == null)
            {
                logLevel20 += "\r\n " + lev.Reference_Object[0] + "=" + lev.Reference_Object[1] + " has no Elevation.";
            }
            else
            {
                logLevel20 += "\r\n Elevation: " + lev.Elevation;
            }

            logLevel20 += "\r\n \r\n LoGeoRef 20 = " + lev.GeoRef20 + line;

            return logLevel20;
        }

        public string LogLevel30(Appl.Level30 lev)

        {
            string logLevel30 = "";
            string line = "\r\n________________________________________________________________________________________________________________________________________";
            string dashline = "\r\n----------------------------------------------------------------------------------------------------------------------------------------";

            logLevel30 += "\r\n \r\nLocal placement for uppermost Elements (usually an instance of IfcSite or IfcBuilding)"
                + "\r\nThe placement of those elements is only relative to the WorldCoordinateSystem (see LoGeoRef 40) but not to other IFC-Elements"
                + dashline
                + "\r\n Referencing Element:" + lev.Reference_Object[0] + "=" + lev.Reference_Object[1]
                + "\r\n Placement referenced in " + lev.Instance_Object[0] + "=" + lev.Instance_Object[1];

            logLevel30 += "\r\n  X = " + lev.ObjectLocationXYZ[0] + "\r\n  Y = " + lev.ObjectLocationXYZ[1] + "\r\n  Z = " + lev.ObjectLocationXYZ[2];

            logLevel30 += $"\r\n Rotation X-axis = ({lev.ObjectRotationX[0]}/{lev.ObjectRotationX[1]}/{lev.ObjectRotationX[2]})";

            logLevel30 += $"\r\n Rotation Z-axis = ({lev.ObjectRotationZ[0]}/{lev.ObjectRotationZ[1]}/{lev.ObjectRotationZ[2]})";

            logLevel30 += "\r\n \r\n LoGeoRef 30 = " + lev.GeoRef30 + "\r\n" + line;

            return logLevel30;
        }

        public string LogLevel40(Appl.Level40 lev)

        {
            string logLevel40 = "";
            string line = "\r\n________________________________________________________________________________________________________________________________________";
            string dashline = "\r\n----------------------------------------------------------------------------------------------------------------------------------------";

            logLevel40 += "\r\n \r\nProject context attributes for georeferencing (Location: WorldCoordinateSystem / Rotation: TrueNorth)"
            + dashline + "\r\n Project:" + lev.Reference_Object[0] + "=" + lev.Reference_Object[1]
            + "\r\n Project context element: " + lev.Instance_Object[0] + "=" + lev.Instance_Object[1];

            logLevel40 += "\r\n  X = " + lev.ProjectLocation[0] + "\r\n  Y = " + lev.ProjectLocation[1] + "\r\n  Z = " + lev.ProjectLocation[2];
            logLevel40 += $"\r\n  Rotation X-axis = ({lev.ProjectRotationX[0]}/{lev.ProjectRotationX[1]}/{lev.ProjectRotationX[2]})";
            logLevel40 += $"\r\n  Rotation Z-axis = ({lev.ProjectRotationZ[0]}/{lev.ProjectRotationZ[1]}/{lev.ProjectRotationZ[2]})";

            logLevel40 += "\r\n \r\n True North:"
                + "\r\n  X-component =" + lev.TrueNorthXY[0]
                + "\r\n  Y-component =" + lev.TrueNorthXY[1];

            logLevel40 += "\r\n \r\n LoGeoRef 40 = " + lev.GeoRef40 + "\r\n" + line;

            return logLevel40;
        }

        public string LogLevel50(Appl.Level50 lev)

        {
            string logLevel50 = "";
            string line = "\r\n________________________________________________________________________________________________________________________________________";
            string dashline = "\r\n----------------------------------------------------------------------------------------------------------------------------------------";

            logLevel50 += "\r\n \r\nSpecific entities for georeferencing" + dashline + "\r\n";

            if(lev.Instance_Object.Count == 0)
            {
                logLevel50 += "\r\n No conversion of the world coordinate system (WCS) in a coordinate reference system (CRS) applicable.";
            }
            else
            {
                logLevel50 += " Project for which IfcMapConversion applies: " + lev.Reference_Object[0] + "=" + lev.Reference_Object[1]
                + "\r\n MapConversion element: " + lev.Instance_Object[0] + "=" + lev.Instance_Object[1]
                + "\r\n  Translation Eastings:" + lev.Translation_Eastings
                + "\r\n  Translation Northings:" + lev.Translation_Northings
                + "\r\n  Translation Height:" + lev.Translation_Orth_Height
                + "\r\n  Rotation X-axis(Abscissa):" + lev.RotationXY[0]
                + "\r\n  Rotation X-axis(Ordinate):" + lev.RotationXY[1]
                + "\r\n  Scale:" + lev.Scale
                + "\r\n CRS element: "
                + "\r\n  Name:" + lev.CRS_Name
                + "\r\n  Description:" + lev.CRS_Description
                + "\r\n  Geodetic Datum:" + lev.CRS_Geodetic_Datum
                + "\r\n  Vertical Datum:" + lev.CRS_Vertical_Datum
                + "\r\n  Projection Name:" + lev.CRS_Projection_Name
                + "\r\n  Projection Zone:" + lev.CRS_Projection_Zone;
            }

            logLevel50 += "\r\n \r\n LoGeoRef 50 = " + lev.GeoRef50 + "\r\n" + line;

            return logLevel50;
        }
    }
}