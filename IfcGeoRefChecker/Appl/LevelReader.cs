//using Xbim.Ifc;

//namespace IfcGeoRefChecker.Appl
//{
//    internal class LevelReader
//    {
//        public Level10 geoRef10 { get; set; }
//        public Level20 geoRef20 { get; set; }
//        public Level30 geoRef30 { get; set; }
//        public Level40 geoRef40 { get; set; }
//        public Level50 geoRef50 { get; set; }

//        public int ifcHash { get; set; }

//        public string ifcType { get; set; }

//        public LevelReader(IfcStore model)
//        {
//            var SiteReader siteReading = new Appl.SiteReader(model).SiteList;       //for Level 10 and 20
//            var bldgReading = new Appl.BldgReader(model).BldgList;       //for Level 10
//            var prodReading = new Appl.UpperPlcmReader(model).ProdList;  //for Level 30
//            var ctxReading = new Appl.ContextReader(model).CtxList;     //for Level 40 and 50
//        }

//        public void ReadLevel10()
//        {
//            for(int i = 0; i < siteReading.Count; i++)
//            {
//                var ifcHash = siteReading[i].GetHashCode();
//                var ifcType = siteReading[i].GetType().Name;

//                string listbox = "#" + ifcHash + "=" + ifcType;

//                geoRef10 = new Appl.Level10(model, ifcHash, ifcType);
//                geoRef10.GetLevel10();

//                geoRef20 = new Appl.Level20(model, ifcHash);
//                geoRef20.GetLevel20();
//            }
//        }

//        public void ReadLevel20()
//        {

//            for(int i = 0; i<bldgReading.Count; i++)
//            {
//                private var ifcHash = bldgReading[i].GetHashCode();
//        private var ifcType = bldgReading[i].GetType().Name;
//        private string listbox = "#" + ifcHash + "=" + ifcType;

//        geoRef10 = new Appl.Level10(model, ifcHash, ifcType);
//                geoRef10.GetLevel10();
//            }

//            for(int i = 0; i<prodReading.Count; i++)
//            {
//                var ifcHash = prodReading[i].GetHashCode();
//    var ifcType = prodReading[i].GetType().Name;

//    string listbox = "#" + ifcHash + "=" + ifcType;

//    // get values for specific element
//    geoRef30 = new Appl.Level30(model, ifcHash, ifcType);
//                geoRef30.GetLevel30();
//            }

//            for(int i = 0; i<ctxReading.Count; i++)
//            {
//                var ifcHash = ctxReading[i].GetHashCode();
//var ifcType = ctxReading[i].GetType().Name;

//string listbox = "#" + ifcHash + "=" + ifcType;

//// get values for specific element
//geoRef40 = new Appl.Level40(model, ifcHash);
//                geoRef40.GetLevel40();
//;

//                // get values for specific element
//                geoRef50 = new Appl.Level50(model, ifcHash);
//                geoRef50.GetLevel50();
//            }
//        }
//    }
//}