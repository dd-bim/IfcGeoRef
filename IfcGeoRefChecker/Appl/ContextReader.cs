using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IfcGeoRefChecker.Appl
{
    public class ContextReader
    {
        public IList<IIfcGeometricRepresentationContext> CtxList { get; set; }

        //function for reading of the IfcGeometricRepresentationContext of the project
        public ContextReader(IfcStore model)
        {
            //read of the project context objects
            //via IFC schema definition there should be at least one context (type: model)
            //there can be optionally also be a context for 2D (type: plan)

            //name contraint for GetType().Name because otherwise also SubContext objects will be read by xBIM functionality, they are inherited from parent context

            this.CtxList = model.Instances.OfType<IIfcGeometricRepresentationContext>().Where(ctx => ctx.GetType().Name == "IfcGeometricRepresentationContext").ToList();

            // --> in a normal valif IFC-file the list should contain at max 2 objects (context for model and optionally plan)
        }
    }
}