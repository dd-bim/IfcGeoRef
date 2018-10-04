using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IfcGeoRefChecker.Appl
{
    //reads all buildings in the committed IfcModel

    public class BldgReader
    {
        public IList<IIfcBuilding> BldgList { get; set; }

        public BldgReader()
        {
        }

        public BldgReader(IfcStore model)
        {
            this.BldgList = model.Instances.OfType<IIfcBuilding>().ToList();
        }

        public void ReadSlab(IfcStore model)
        {
            //read slabs (not of type landing or roof) which could be the baseslab
            //type baseslab

            var slabList = model.Instances.OfType<IIfcSlab>().Where(x => x.PredefinedType != IfcSlabTypeEnum.ROOF).Where(x => x.PredefinedType != IfcSlabTypeEnum.LANDING);

            foreach(var slab in slabList)
            {
                MessageBox.Show(slab.Name+ "..." +slab.ObjectType + "..." +slab.PredefinedType);
            }
        }

        public void ReadLocalPlacements(IfcStore model)
        {
            var plcmList = model.Instances.OfType<IIfcLocalPlacement>().ToList();

            foreach(var plcm in plcmList)
            {
            }
        }
    }
}