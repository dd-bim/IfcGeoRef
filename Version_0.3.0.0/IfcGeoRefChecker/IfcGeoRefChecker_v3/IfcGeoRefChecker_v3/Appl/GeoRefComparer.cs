using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Serilog;

namespace IfcGeoRefChecker.Appl
{
    internal class GeoRefComparer
    {
        private Level10 siteAddress;  //adresses for Site
        private Level10 bldgAddress;    //adresses for Building
        private Level20 latlon;       //latlon for Site
        private Level30 siteplcm;       //placement for Site
        private Level40 projplcm;        //WCS placement (model)
        private Level50 projCRS;        //Map conversion (model)

        private string refModel;
        private Dictionary<string, string> compList = new Dictionary<string, string>();

        private List<string> logList = new List<string>();
        private string direc;
        private string refFile;
        //private string compFile;

        public GeoRefComparer(string direc, KeyValuePair<string, string> jsonRef, Dictionary<string, string> jsonComp)
        {
            this.direc = direc;
            this.refFile = jsonRef.Key;
            this.refModel = jsonRef.Value;
            this.compList = jsonComp;
        }

        public void CompareIFC()
        {
            try
            {
                Log.Information("GeoRefComparer: Initialized...");

                Log.Information("GeoRefComparer: Get GeoRef-Elements for Reference model...");

                FillGeoref(refFile, refModel);

                var refSiteAddress = siteAddress;
                var refBldgAddress = bldgAddress;
                var refLatlon = latlon;
                var refSitePlcm = siteplcm;
                var refProjPlcm = projplcm;
                var refProjCRS = projCRS;

                foreach(var compModel in compList)
                {
                    Log.Information("GeoRefComparer: Get GeoRef-Elements for Comparison model...");

                    FillGeoref(compModel.Key, compModel.Value);

                    Log.Information("GeoRefComparer: Comparison of reference with comparison model started...");

                    bool eq10site, eq10bldg, eq20site, eq30site, eq40proj, eq50proj;

                    try
                    {
                        if(refSiteAddress == null || siteAddress == null)
                        {
                            eq10site = false;
                            if(refSiteAddress == null && siteAddress == null)
                                eq10site = true;
                        }
                        else
                        {
                            eq10site = refSiteAddress.Equals(siteAddress);
                        }

                        if(refBldgAddress == null || bldgAddress == null)
                        {
                            eq10bldg = false;
                            if(refBldgAddress == null && bldgAddress == null)
                                eq10bldg = true;
                        }
                        else
                        {
                            eq10bldg = refBldgAddress.Equals(bldgAddress);
                        }

                        if(refLatlon == null || latlon == null)
                        {
                            eq20site = false;
                            if(refLatlon == null && latlon == null)
                                eq20site = true;
                        }
                        else
                        {
                            eq20site = refLatlon.Equals(latlon);
                        }

                        if(refSitePlcm == null || siteplcm == null)
                        {
                            eq30site = false;
                            if(refSitePlcm == null && siteplcm == null)
                                eq30site = true;
                        }
                        else
                        {
                            eq30site = refSitePlcm.Equals(siteplcm);
                        }

                        if(refProjPlcm == null || projplcm == null)
                        {
                            eq40proj = false;
                            if(refProjPlcm == null && projplcm == null)
                                eq40proj = true;
                        }
                        else
                        {
                            eq40proj = refProjPlcm.Equals(projplcm);
                        }

                        if(refProjCRS == null || projCRS == null)
                        {
                            eq50proj = false;
                            if(refProjCRS == null && projCRS == null)
                                eq50proj = true;
                        }
                        else
                        {
                            eq50proj = refProjCRS.Equals(projCRS);
                        }
                    }
                    catch(Exception ex)
                    {
                        Log.Error("GeoRefComparer: Error occured while comparing GeoRef levels: " + ex.Message);
                        continue;
                    }

                    Dictionary<string, bool> equality = new Dictionary<string, bool>
                            {
                                { "GeoRef10 (IfcSite address) ", eq10site },
                                { "GeoRef10 (IfcBuilding address) ", eq10bldg },
                                { "GeoRef20 (IfcSite Lat/Lon/Elevation) ", eq20site },
                                { "GeoRef30 (IfcSite Placement) ", eq30site },
                                { "GeoRef40 (IfcProject WCS/True North) ", eq40proj },
                                { "GeoRef50 (IfcProject Map Conversion) ", eq50proj }
                            };
                    string a = "\r\nComparison to " + compModel.Key + ":";

                    if(equality.ContainsValue(false))
                    {
                        a += "\r\n The georeferencing of the files is NOT equal.";

                        var keys = from entry in equality
                                   where entry.Value == false
                                   select entry.Key;

                        foreach(var key in keys)
                        {
                            a += "\r\n  A difference was detected at " + key;
                        }
                    }
                    else
                    {
                        a += "\r\n The georeferencing of the files is exactly equal.";
                    }

                    logList.Add(a);

                    Log.Information("GeoRefComparer: Comparison with " + compModel.Key + " sucessfully finished.");
                }

                WriteCompareLog();

                MessageBox.Show("GeoRefComparer: Comparison was successful. See file.");
            }
            catch(Exception ex)
            {
                MessageBox.Show("GeoRefComparer: Error occured. Error: " + ex.Message);
            }
        }

        private void WriteCompareLog()
        {
            Log.Information("GeoRefComparer: Write CompareLog file...");

            using(var writeCompareLog = File.CreateText(this.direc + refFile + "_compare.txt"))
            {
                try
                {
                    writeCompareLog.WriteLine("Results of Comparison regarding Georeferencing for reference model: " + refFile);

                    foreach(var entry in logList)
                    {
                        writeCompareLog.WriteLine(entry);
                    }

                    Log.Information("GeoRefComparer: Write CompareLog file successful.");
                }

                catch(Exception ex)
                {
                    Log.Error($"GeoRefComparer: Error occured while writing Compare-Logfile. \r\n Message: {ex.Message}");
                }
            };
        }

        public void ShowCompareLog()
        {
            try
            {
                Log.Information("GeoRefComparer: Show Compare file...");

                var splits = refFile.Split('\\');
                var name = splits[splits.Length - 1];

                var path = this.direc + "\\IfcGeoRefChecker\\export\\" + name + "_compare.txt";

                System.Diagnostics.Process.Start(path);
            }
            catch(Exception ex)
            {
                var str = "GeoRefComparer: Error occured while open compare file. Error:" + ex.Message;

                Log.Error(str);
                MessageBox.Show(str);
            }
        }

        private void FillGeoref(string name, string jsonModel)
        {
            try
            {
                var json = new GeoRefChecker(jsonModel);

                this.siteAddress = (from l10Site in json.LoGeoRef10
                                    where l10Site.Reference_Object[1].Equals("IfcSite")
                                    select l10Site).Single();

                this.bldgAddress = (from l10Bldg in json.LoGeoRef10
                                    where l10Bldg.Reference_Object[1].Equals("IfcBuilding")
                                    select l10Bldg).Single();

                this.latlon = (from l20site in json.LoGeoRef20
                               where l20site.Reference_Object[1].Equals("IfcSite")
                               select l20site).Single();

                this.siteplcm = (from l30site in json.LoGeoRef30
                                 where l30site.Reference_Object[1].Equals("IfcSite")
                                 select l30site).Single();

                this.projplcm = (from l40 in json.LoGeoRef40
                                 where l40.Reference_Object[1].Equals("IfcProject")
                                 select l40).Single();

                this.projCRS = (from l50 in json.LoGeoRef50
                                where l50.Reference_Object[1].Equals("IfcProject")
                                select l50).Single();

                Log.Information("GeoRefComparer: Get GeoRef attributes successful.");
            }
            catch(Exception ex)
            {
                Log.Error("GeoRefComparer: Error occured while getting GeoRef attributes. Error:" + ex.Message);
            }
        }
    }
}