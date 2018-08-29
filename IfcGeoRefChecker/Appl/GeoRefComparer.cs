using System;
using System.Collections.Generic;
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

            foreach(var compModel in compList)
            {
                try
                {
                    FillGeoref(compModel);

                    Dictionary<string, bool> equality = new Dictionary<string, bool>
                {
                    { "site10", refSiteAddress.Equals(siteAddress) },
                    { "bldg10", refBldgAddress.Equals(bldgAddress) },
                    { "site20", refLatlon.Equals(latlon) },
                    { "site30", refSitePlcm.Equals(siteplcm) },
                    { "proj40", refProjPlcm.Equals(projplcm) },
                    { "proj50", refProjCRS.Equals(projCRS) }
                };
                    string a = "Vergleich " + refModel.FileName + " mit " + compModel.FileName + ":";

                    if(equality.ContainsValue(false))
                    {
                        a += "\r\nThe georeferencing of the files is NOT equal.";

                        a += "\r\nSite address =" + (refSiteAddress.Equals(siteAddress)).ToString();
                        a += "\r\nBuilding address =" + (refBldgAddress.Equals(bldgAddress)).ToString();
                        a += "\r\nSite latlon =" + (refLatlon.Equals(latlon)).ToString();
                        a += "\r\nSite placement =" + (refSitePlcm.Equals(siteplcm)).ToString();
                        a += "\r\nProject placement =" + (refProjPlcm.Equals(projplcm)).ToString();
                        a += "\r\nProject conversion =" + (refProjCRS.Equals(projCRS)).ToString();
                    }
                    else
                    {
                        a += "\r\nThe georeferencing of the files is exactly equal.";
                    }

                    MessageBox.Show(a);
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Error occured while comparing Ifc-files. \r\nError message: " + ex.Message);
                }
            }
        }

        public void FillGeoref(IfcStore model)
        {
            try
            {
                var siteReading = new Appl.SiteReader(model).SiteList[0];
                var bldgReading = new Appl.BldgReader(model).BldgList[0];
                var prodReading = new Appl.UpperPlcmReader(model).ProdList;
                var ctxReading = new Appl.ContextReader(model).CtxList;

                var site10 = new Level10(model, siteReading.GetHashCode(), siteReading.GetType().ToString());
                site10.GetLevel10();
                //siteAddressList.Add(site10);
                siteAddress = site10;

                var bldg10 = new Level10(model, bldgReading.GetHashCode(), bldgReading.GetType().ToString());
                bldg10.GetLevel10();
                //bldgAddressList.Add(bldg10);
                bldgAddress = bldg10;

                var site20 = new Level20(model, siteReading.GetHashCode());
                site20.GetLevel20();
                //latlonList.Add(site20);
                latlon = site20;

                for(var i = 0; i < prodReading.Count; i++)
                {
                    if(prodReading[i].GetType().Name == "IfcSite")
                    {
                        var site30 = new Level30(model, prodReading[i].GetHashCode(), prodReading[i].ToString());
                        site30.GetLevel30();
                        //siteplcmList.Add(site30);
                        siteplcm = site30;
                    }
                }
                for(var i = 0; i < ctxReading.Count; i++)
                {
                    if(ctxReading[i].ContextType == "Model")
                    {
                        var cont40 = new Level40(model, ctxReading[i].GetHashCode());
                        cont40.GetLevel40();
                        //projplcmList.Add(cont40);
                        projplcm = cont40;
                    }

                    if(ctxReading[i].ContextType == "Model" && ctxReading[i].HasCoordinateOperation != null)
                    {
                        var map50 = new Level50(model, ctxReading[i].GetHashCode());
                        map50.GetLevel50();
                        //projCRSList.Add(map50);
                        projCRS = map50;
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error occured while comparing Ifc-files. \r\nError message: " + ex.Message);
            }
        }
    }
}