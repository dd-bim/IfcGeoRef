using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Xbim.Ifc;

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

        private IfcStore refModel;
        private List<IfcStore> compList = new List<IfcStore>();

        private List<string> logList = new List<string>();
        private string refDirec;
        private string refFile;
        private string compFile;

        public GeoRefComparer(IfcStore refModel, List<IfcStore> compModels)
        {
            this.refModel = refModel;
            this.compList = compModels;
        }

        public void CompareIFC()
        {
            FillGeoref(refModel);

            var refSiteAddress = siteAddress;
            var refBldgAddress = bldgAddress;
            var refLatlon = latlon;
            var refSitePlcm = siteplcm;
            var refProjPlcm = projplcm;
            var refProjCRS = projCRS;

            var pos = refModel.FileName.LastIndexOf("\\");
            refFile = refModel.FileName.Substring(pos + 1);
            refDirec = refModel.FileName.Substring(0, pos);

            foreach(var compModel in compList)
            {
                try
                {
                    FillGeoref(compModel);

                    var pos2 = compModel.FileName.LastIndexOf("\\");
                    compFile = compModel.FileName.Substring(pos2 + 1);

                    var eq10site = refSiteAddress.Equals(siteAddress);

                    bool eq10bldg;

                    if(refBldgAddress != null)
                    {
                        eq10bldg = refBldgAddress.Equals(bldgAddress);
                    }
                    else
                    {
                        eq10bldg = false;
                    }

                    var eq20site = refLatlon.Equals(latlon);
                    var eq30site = refSitePlcm.Equals(siteplcm);
                    var eq40proj = refProjPlcm.Equals(projplcm);
                    var eq50proj = refProjCRS.Equals(projCRS);

                    Dictionary<string, bool> equality = new Dictionary<string, bool>
                {
                    { "GeoRef10 (IfcSite address) ", eq10site },
                    { "GeoRef10 (IfcBuilding address) ", eq10bldg },
                    { "GeoRef20 (IfcSite Lat/Lon/Elevation) ", eq20site },
                    { "GeoRef30 (IfcSite Placement) ", eq30site },
                    { "GeoRef40 (IfcProject WCS/True North) ", eq40proj },
                    { "GeoRef50 (IfcProject Map Conversion) ", eq50proj }
                };
                    string a = "\r\nComparison to " + compFile + ":";

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
                }

                catch(Exception ex)
                {
                    MessageBox.Show("Error occured while comparing Ifc-files at file: " + compFile + "\r\nError message: " + ex.Message);
                }
            }

            WriteCompareLog();
        }

        public void WriteCompareLog()
        {
            using(var writeCompareLog = File.CreateText((refDirec + "\\Comparison_" + refFile + ".txt")))
            {
                try
                {
                    writeCompareLog.WriteLine("Results of Comparison regarding Georeferencing for reference model: " + refFile);

                    foreach(var entry in logList)
                    {
                        writeCompareLog.WriteLine(entry);
                    }
                }

                catch(Exception ex)
                {
                    writeCompareLog.WriteLine($"Error occured while writing Compare-Logfile. \r\n Message: {ex.Message}");
                }
            };
        }

        public void ShowCompareLog()
        {
            var path = refDirec + "\\Comparison_" + refFile + ".txt";

            System.Diagnostics.Process.Start(path);
        }

        public void FillGeoref(IfcStore model)
        {
            try
            {
                var siteReading = new Appl.SiteReader(model).SiteList;

                if(siteReading.Count != 0)
                {
                    var site10 = new Level10(model, siteReading[0].GetHashCode(), siteReading[0].GetType().Name);
                    site10.GetLevel10();
                    siteAddress = site10;

                    var site20 = new Level20(model, siteReading[0].GetHashCode());
                    site20.GetLevel20();
                    latlon = site20;
                }

                var bldgReading = new Appl.BldgReader(model).BldgList;

                if(bldgReading.Count != 0)
                {
                    var bldg10 = new Level10(model, bldgReading[0].GetHashCode(), bldgReading[0].GetType().Name);
                    bldg10.GetLevel10();
                    bldgAddress = bldg10;
                }

                var prodReading = new Appl.UpperPlcmReader(model).ProdList;
                var ctxReading = new Appl.ContextReader(model).CtxList;

                for(var i = 0; i < prodReading.Count; i++)
                {
                    if(prodReading[i].GetType().Name == "IfcSite")
                    {
                        var site30 = new Level30(model, prodReading[i].GetHashCode(), prodReading[i].GetType().Name);
                        site30.GetLevel30();
                        siteplcm = site30;
                        break;
                    }
                }
                for(var i = 0; i < ctxReading.Count; i++)
                {
                    if(ctxReading[i].ContextType == "Model")
                    {
                        var cont40 = new Level40(model, ctxReading[i].GetHashCode());
                        cont40.GetLevel40();
                        projplcm = cont40;
                        break;
                    }
                }
                for(var i = 0; i < ctxReading.Count; i++)
                {
                    if(ctxReading[i].ContextType == "Model" && ctxReading[i].HasCoordinateOperation != null)
                    {
                        var map50 = new Level50(model, ctxReading[i].GetHashCode());
                        map50.GetLevel50();
                        projCRS = map50;
                        break;
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error occured while comparing Ifc-files at file: " + compFile + "\r\nError message: " + ex.Message);
            }
        }
    }
}