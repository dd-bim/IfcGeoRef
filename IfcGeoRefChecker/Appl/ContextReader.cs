using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IfcGeoRefChecker.Appl
{
    public class ContextReader

    {
        public IIfcGeometricRepresentationContext ProjCtx { get; set; }


        public IList<IIfcGeometricRepresentationContext> CtxList { get; set; }

        //function for reading of the IfcGeometricRepresentationContext of the project
        public ContextReader(IfcStore model)
        {
            //read only the context where the type is a model (not plan) and the dimension is 3D (not 2D) --> context for the 3D model/geometry of the project
            //in a valid ifc file such an context can only occure once
            //also contraint for GetType().Name because otherwise also SubContexts will be read by xBIM functionality

            this.CtxList = model.Instances.OfType<IIfcGeometricRepresentationContext>().Where(ctx => ctx.GetType().Name == "IfcGeometricRepresentationContext").ToList();

            //this.ProjCtx = model.Instances
            //.Where<IIfcGeometricRepresentationContext>(ctx => ctx.GetType().Name == "IfcGeometricRepresentationContext" && ctx.ContextType == "Model" && ctx.CoordinateSpaceDimension == 3);
        }
    }
}