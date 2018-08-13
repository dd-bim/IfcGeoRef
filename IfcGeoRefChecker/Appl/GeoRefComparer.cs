using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Xbim.Ifc;

namespace IfcGeoRefChecker.Appl
{
    //public class Foo : IEquatable<Foo>
    //{
    //    public int MyNum { get; set; }
    //    public string MyStr { get; set; }

    //    #region Equality

    //    public bool Equals(Foo other)
    //    {
    //        if(other == null)
    //            return false;
    //        return MyNum == other.MyNum &&
    //               string.Equals(MyStr, other.MyStr);
    //    }


        //#endregion
    //}



    internal class GeoRefComparer
    {
        private List<Level10> addressList = new List<Level10>();
        private List<Level20> latlonList = new List<Level20>();
        private List<Level30> siteplcmList = new List<Level30>();
        private List<Level40> projplcmList = new List<Level40>();
        private List<Level50> projCRSList = new List<Level50>();

        

        //private Foo a = new Foo();
        //private Foo b = new Foo();
        //private Foo c = new Foo();

        //public void compareinstances()
        //{
        //    a.MyNum = 111;
        //    a.MyStr = "aaa";

        //    b.MyNum = 222;compList
        //    b.MyStr = "bbb";

        //    c.MyNum = 111;
        //    c.MyStr = "aaa";

        //}

        public void comparetest(Dictionary<string, IfcStore> compList)
        {
            IfcStore model, modeleq, modelch;



            for (var i = 0; i < compList.Count; i++)
            {
                foreach(var compModel in compList.Values)
                {
                    var abc = new Level10(compModel, "7220", "IfcSite");
                    abc.GetLevel10();


                }
            }

            var instA = compList.TryGetValue("comp1", out model);
            var instC = compList.TryGetValue("comp1equal", out modeleq);
            var instB = compList.TryGetValue("comp1change", out modelch);


            var a = new Level10(model, "7220", "IfcSite");
            a.GetLevel10();
            var c = new Level10(modeleq, "7220", "IfcSite");
            c.GetLevel10();
            var b = new Level10(modelch, "7220", "IfcSite");
            b.GetLevel10();

            MessageBox.Show("Test a und b" + (a.Equals(b)).ToString());
            MessageBox.Show("Test a und c" + (a.Equals(c)).ToString());
            MessageBox.Show("Test c und b" + (c.Equals(b)).ToString());
            MessageBox.Show("Test c und a" + (c.Equals(a)).ToString());
        }

        public void CompareIFCModels(List<IfcStore> compList, List<string> fileList)
        {
            for(int i = 0; i < compList.Count; i++)
            {
                var a = "123";
                var b = "ifcsite";

                //Level-Listen für Speicherung der Ergebnisse aus dem IFC-Model auslesen
                addressList.Add(new Level10(compList[i], a, b));
                latlonList.Add(new Level20(compList[i], a));
                siteplcmList.Add(new Level30(compList[i], a, b));
                projplcmList.Add(new Level40(compList[i]));
                projCRSList.Add(new Level50(compList[i]));
            }

            foreach (var elem in addressList)
            {
               
            }

            for(int i = 0; i < compList.Count; i++)                                            // Listen auslesen fehlt bei List-Attributen
            {
                for(int j = (i + 1); j < compList.Count; j++)
                {
                    using(var writeRep = File.CreateText((@".\results\Comparison_" + fileList[i] + " with " + fileList[j] + ".txt")))
                    {
                        string dashline = "\r\n----------------------------------------------------------------------------------------------------------------------------------------";

                        bool cAddr = false;

                        if(addressList[i].AddressLines.Count == addressList[j].AddressLines.Count)
                        {
                            for(var k = 0; k < addressList[i].AddressLines.Count; k++)
                            {
                                if(addressList[i].AddressLines[k] == addressList[j].AddressLines[k])
                                {
                                    cAddr = true;
                                }
                            }
                        }

                        bool cPost = addressList[i].Postalcode == addressList[j].Postalcode ? true : false;
                        bool cTown = addressList[i].Town == addressList[j].Town ? true : false;
                        bool cReg = addressList[i].Region == addressList[j].Region ? true : false;
                        bool cCtry = addressList[i].Country == addressList[j].Country ? true : false;

                        bool cLon = latlonList[i].Longitude == latlonList[j].Longitude ? true : false;
                        bool cLat = latlonList[i].Latitude == latlonList[j].Latitude ? true : false;
                        bool cElev = latlonList[i].Elevation == latlonList[j].Elevation ? true : false;

                        bool cXYZS = false;

                        for(var k = 0; k < siteplcmList[i].ObjectLocationXYZ.Count; k++)
                        {
                            cXYZS = siteplcmList[i].ObjectLocationXYZ[k] == siteplcmList[j].ObjectLocationXYZ[k] ? true : false;
                        }

                        bool cXRotS = false;

                        for(var k = 0; k < siteplcmList[i].ObjectRotationX.Count; k++)
                        {
                            cXRotS = siteplcmList[i].ObjectRotationX[k] == siteplcmList[i].ObjectRotationX[k] ? true : false;
                        }

                        bool cZRotS = false;

                        for(var k = 0; k < siteplcmList[i].ObjectRotationZ.Count; k++)
                        {
                            cZRotS = siteplcmList[i].ObjectRotationZ[k] == siteplcmList[i].ObjectRotationZ[k] ? true : false;
                        }

                        bool cXYZP = false;

                        for(var k = 0; k < projplcmList[i].ProjectLocationXYZ.Count; k++)
                        {
                            cXYZP = projplcmList[i].ProjectLocationXYZ[k] == projplcmList[j].ProjectLocationXYZ[k] ? true : false;
                        }

                        bool cXRotP = false;

                        if(projplcmList[i].ProjectRotationX != null)
                        {
                            for(var k = 0; k < projplcmList[i].ProjectRotationX.Count; k++)
                            {
                                cXRotP = projplcmList[i].ProjectRotationX[k] == projplcmList[j].ProjectRotationX[k] ? true : false;
                            }
                        }

                        bool cZRotP = false;

                        if(projplcmList[i].ProjectRotationZ != null)
                        {
                            for(var k = 0; k < projplcmList[i].ProjectRotationZ.Count; k++)
                            {
                                cZRotP = projplcmList[i].ProjectRotationZ[k] == projplcmList[j].ProjectRotationZ[k] ? true : false;
                            }
                        }

                        bool cTNo = false;

                        for(var k = 0; k < projplcmList[i].TrueNorthXY[k]; k++)
                        {
                            cTNo = projplcmList[i].TrueNorthXY[k] == projplcmList[j].TrueNorthXY[k] ? true : false;
                        }

                        bool cTrEa = projCRSList[i].Translation_Eastings == projCRSList[j].Translation_Eastings ? true : false;
                        bool cTrNo = projCRSList[i].Translation_Northings == projCRSList[j].Translation_Northings ? true : false;
                        bool cTrHe = projCRSList[i].Translation_Orth_Height == projCRSList[j].Translation_Orth_Height ? true : false;

                        bool cRotXY = false;

                        if(projCRSList[i].RotationXY != null)
                        {
                            for(var k = 0; k < projCRSList[i].RotationXY[k]; k++)
                            {
                                cRotXY = projCRSList[i].RotationXY[k] == projCRSList[j].RotationXY[k] ? true : false;
                            }
                        }
                        bool cScl = projCRSList[i].Scale == projCRSList[j].Scale ? true : false;
                        bool cCRS = projCRSList[i].CRS_Name == projCRSList[j].CRS_Name ? true : false;

                        //-----------------------------------------------------------------------------------

                        writeRep.WriteLine(
               $"\r\nComparison of {fileList[i]}.ifc and {fileList[j]}.ifc regarding their georeferencing content ({DateTime.Now.ToShortDateString()}, {DateTime.Now.ToLongTimeString()})" + dashline + dashline);

                        writeRep.WriteLine("LoGeoRef10" + dashline);

                        if(cAddr == true && cPost == true && cTown == true && cReg == true && cCtry == true)
                        {
                            writeRep.WriteLine("LoGeoRef10 is identical" + dashline);
                        }
                        else
                        {
                            writeRep.WriteLine("LoGeoRef10 is NOT identical" + cAddr + cPost + cTown + cReg + cCtry + dashline);
                        }

                        writeRep.WriteLine("LoGeoRef20" + dashline);
                        if(cLat == true && cLon == true && cElev == true)
                        {
                            writeRep.WriteLine("LoGeoRef20 is identical" + dashline);
                        }
                        else
                        {
                            writeRep.WriteLine("LoGeoRef20 is NOT identical" + cLat + cLon + cElev + dashline);
                        }

                        writeRep.WriteLine("LoGeoRef30" + dashline);

                        if(cXYZS == true && cXRotS == true && cZRotS == true)
                        {
                            writeRep.WriteLine("LoGeoRef30 is identical" + dashline);
                        }
                        else
                        {
                            writeRep.WriteLine("LoGeoRef30 is NOT identical" + cXYZS + cXRotS + cZRotS + dashline);
                        }
                        writeRep.WriteLine("LoGeoRef40" + dashline);

                        if(cXYZP == true && cXRotP == true && cZRotP == true && cTNo == true)
                        {
                            writeRep.WriteLine("LoGeoRef40 is identical" + dashline);
                        }
                        else
                        {
                            writeRep.WriteLine("LoGeoRef40 is NOT identical" + cXYZP + cXRotP + cZRotP + cTNo + dashline);
                        }

                        writeRep.WriteLine("LoGeoRef50" + dashline);

                        if(cTrEa == true && cTrNo == true && cTrHe == true && cRotXY == true && cScl == true && cCRS == true)
                        {
                            writeRep.WriteLine("LoGeoRef50 is identical" + dashline);
                        }
                        else
                        {
                            writeRep.WriteLine("LoGeoRef50 is NOT identical" + cTrEa + cTrNo + cTrHe + cRotXY + cScl + cCRS + dashline);
                        }
                    }
                }
            }
        }
    }
}