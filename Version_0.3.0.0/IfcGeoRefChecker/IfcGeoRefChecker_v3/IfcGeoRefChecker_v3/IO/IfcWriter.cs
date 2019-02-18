using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.RepresentationResource;

namespace IfcGeoRefChecker.IO
{
    internal class IfcWriter
    {

        public IfcWriter(string newDirec, string ifcPath, string fileName, string jsonObj)
        {
            var json = new JsonOutput();
            json.PopulateJson(jsonObj);

            var editor = new XbimEditorCredentials
            {
                ApplicationDevelopersName = "HTW Dresden",
                ApplicationFullName = "IfcGeoRefChecker",
                ApplicationIdentifier = "IfcGeoRef",
                ApplicationVersion = "0.3.0.0",
                EditorsFamilyName = Environment.UserName,
            };

            var fileIFC = ifcPath + ".ifc";

            using(var model = IfcStore.Open(fileIFC, editor))
            {
                //try
                //{
                using(var txn = model.BeginTransaction(model.FileName + "_transedit"))
                {
                    //Level 10

                    foreach(var lev10 in json.LoGeoRef10)
                    {
                        var refObj = GetRefObj(model, lev10.Reference_Object[0]);
                        var creation = GetCreationDate(refObj);

                        IIfcPostalAddress p;

                        if(json.IFCSchema == "Ifc2X3")
                        {
                            p = model.Instances.New<Xbim.Ifc2x3.ActorResource.IfcPostalAddress>();
                        }
                        else
                        {
                            p = model.Instances.New<Xbim.Ifc4.ActorResource.IfcPostalAddress>();
                        }

                        p.AddressLines.Add(lev10.AddressLines[0]);
                        p.AddressLines.Add(lev10.AddressLines[1]);
                        p.AddressLines.Add(lev10.AddressLines[2]);

                        p.PostalCode = lev10.Postalcode;
                        p.Town = lev10.Town;
                        p.Region = lev10.Region;
                        p.Country = lev10.Country;

                        if(refObj is IIfcSite)
                        {
                            (refObj as IIfcSite).SiteAddress = p;
                        }
                        else
                        {
                            (refObj as IIfcBuilding).BuildingAddress = p;
                        }

                        refObj = UpdateOwnerHistory(refObj, creation);
                    }

                    foreach(var lev20 in json.LoGeoRef20)
                    {
                        var refObj = GetRefObj(model, lev20.Reference_Object[0]);
                        var creation = GetCreationDate(refObj);

                        var dmsLat = new Appl.Calc().DDtoCompound((double)lev20.Latitude);
                        var dmsLon = new Appl.Calc().DDtoCompound((double)lev20.Longitude);

                        var listLat = new List<long>
                            {
                                { Convert.ToInt64(dmsLat[0]) },
                                { Convert.ToInt64(dmsLat[1]) },
                                { Convert.ToInt64(dmsLat[2]) },
                                { Convert.ToInt64(dmsLat[3]) }
                            };

                        var listLon = new List<long>
                            {
                                { Convert.ToInt64(dmsLon[0]) },
                                { Convert.ToInt64(dmsLon[1]) },
                                { Convert.ToInt64(dmsLon[2]) },
                                { Convert.ToInt64(dmsLon[3]) }
                            };

                        (refObj as IIfcSite).RefLatitude = new IfcCompoundPlaneAngleMeasure(listLat);
                        (refObj as IIfcSite).RefLongitude = new IfcCompoundPlaneAngleMeasure(listLon);

                        (refObj as IIfcSite).RefElevation = lev20.Elevation;

                        refObj = UpdateOwnerHistory(refObj, creation);
                    }

                    foreach(var lev30 in json.LoGeoRef30)
                    {
                        var refObj = GetRefObj(model, lev30.Reference_Object[0]);
                        var creation = GetCreationDate(refObj);

                        var elemPlcm = (IIfcLocalPlacement)(refObj as IIfcProduct).ObjectPlacement;
                        var plcm3D = (IIfcAxis2Placement3D)elemPlcm.RelativePlacement;

                        if(json.IFCSchema == "Ifc2X3")
                        {
                            plcm3D.Location = model.Instances.New<Xbim.Ifc2x3.GeometryResource.IfcCartesianPoint>(p => p.SetXYZ(lev30.ObjectLocationXYZ[0], lev30.ObjectLocationXYZ[1], lev30.ObjectLocationXYZ[2]));
                            plcm3D.RefDirection = model.Instances.New<Xbim.Ifc2x3.GeometryResource.IfcDirection>(d => d.SetXYZ(lev30.ObjectRotationX[0], lev30.ObjectRotationX[1], lev30.ObjectRotationX[2]));
                            plcm3D.Axis = model.Instances.New<Xbim.Ifc2x3.GeometryResource.IfcDirection>(d => d.SetXYZ(lev30.ObjectRotationZ[0], lev30.ObjectRotationZ[1], lev30.ObjectRotationZ[2]));
                        }
                        else
                        {
                            plcm3D.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(lev30.ObjectLocationXYZ[0], lev30.ObjectLocationXYZ[1], lev30.ObjectLocationXYZ[2]));
                            plcm3D.RefDirection = model.Instances.New<IfcDirection>(d => d.SetXYZ(lev30.ObjectRotationX[0], lev30.ObjectRotationX[1], lev30.ObjectRotationX[2]));
                            plcm3D.Axis = model.Instances.New<IfcDirection>(d => d.SetXYZ(lev30.ObjectRotationZ[0], lev30.ObjectRotationZ[1], lev30.ObjectRotationZ[2]));
                        }

                        refObj = UpdateOwnerHistory(refObj, creation);
                    }

                    foreach(var lev40 in json.LoGeoRef40)
                    {
                        var refObj = GetRefCtx(model, lev40.Instance_Object[0]);

                        var project = model.Instances.OfType<IIfcProject>().Where(c => c.RepresentationContexts.Contains((refObj as IIfcGeometricRepresentationContext))).Single();
                        var creation = GetCreationDate(project);

                        var plcm = refObj.WorldCoordinateSystem;

                        if(json.IFCSchema == "Ifc2X3")
                        {
                            (refObj as IIfcGeometricRepresentationContext).TrueNorth = model.Instances.New<Xbim.Ifc2x3.GeometryResource.IfcDirection>(d => d.SetXY(lev40.TrueNorthXY[0], lev40.TrueNorthXY[1]));

                            if(plcm is IIfcAxis2Placement3D)
                            {
                                (plcm as IIfcAxis2Placement3D).Location = model.Instances.New<Xbim.Ifc2x3.GeometryResource.IfcCartesianPoint>(p => p.SetXYZ(lev40.ProjectLocation[0], lev40.ProjectLocation[1], lev40.ProjectLocation[2]));
                                (plcm as IIfcAxis2Placement3D).RefDirection = model.Instances.New<Xbim.Ifc2x3.GeometryResource.IfcDirection>(d => d.SetXYZ(lev40.ProjectRotationX[0], lev40.ProjectRotationX[1], lev40.ProjectRotationX[2]));
                                (plcm as IIfcAxis2Placement3D).Axis = model.Instances.New<Xbim.Ifc2x3.GeometryResource.IfcDirection>(d => d.SetXYZ(lev40.ProjectRotationZ[0], lev40.ProjectRotationZ[1], lev40.ProjectRotationZ[2]));
                            }
                            else
                            {
                                (plcm as IIfcAxis2Placement2D).Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXY(lev40.ProjectLocation[0], lev40.ProjectLocation[1]));
                                (plcm as IIfcAxis2Placement2D).RefDirection = model.Instances.New<IfcDirection>(d => d.SetXY(lev40.ProjectRotationX[0], lev40.ProjectRotationX[1]));
                            }
                        }
                        else
                        {
                            (refObj as IIfcGeometricRepresentationContext).TrueNorth = model.Instances.New<IfcDirection>(d => d.SetXY(lev40.TrueNorthXY[0], lev40.TrueNorthXY[1]));

                            if(plcm is IIfcAxis2Placement3D)
                            {
                                (plcm as IIfcAxis2Placement3D).Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(lev40.ProjectLocation[0], lev40.ProjectLocation[1], lev40.ProjectLocation[2]));
                                (plcm as IIfcAxis2Placement3D).RefDirection = model.Instances.New<IfcDirection>(d => d.SetXYZ(lev40.ProjectRotationX[0], lev40.ProjectRotationX[1], lev40.ProjectRotationX[2]));
                                (plcm as IIfcAxis2Placement3D).Axis = model.Instances.New<IfcDirection>(d => d.SetXYZ(lev40.ProjectRotationZ[0], lev40.ProjectRotationZ[1], lev40.ProjectRotationZ[2]));
                            }
                            else
                            {
                                (plcm as IIfcAxis2Placement2D).Location = model.Instances.New<Xbim.Ifc2x3.GeometryResource.IfcCartesianPoint>(p => p.SetXY(lev40.ProjectLocation[0], lev40.ProjectLocation[1]));
                                (plcm as IIfcAxis2Placement2D).RefDirection = model.Instances.New<Xbim.Ifc2x3.GeometryResource.IfcDirection>(d => d.SetXY(lev40.ProjectRotationX[0], lev40.ProjectRotationX[1]));
                            }
                        }

                        //(refObj as IIfcGeometricRepresentationContext).WorldCoordinateSystem = plcm;

                        var proj = (IIfcObjectDefinition)project;

                        proj = UpdateOwnerHistory((project as IIfcObjectDefinition), creation);
                    }

                    foreach(var lev50 in json.LoGeoRef50)
                    {
                        var refObj = GetRefCtx(model, lev50.Instance_Object[0]);

                        var project = model.Instances.OfType<IIfcProject>().Where(c => c.RepresentationContexts.Contains((refObj as IIfcGeometricRepresentationContext))).Single();

                        var creation = GetCreationDate(project);

                        if(json.IFCSchema != "Ifc2X3" && lev50.Translation_Eastings == 0 && lev50.Translation_Northings == 0)
                        {
                            var pSetMapCRS = model.Instances.New<Xbim.Ifc4.Kernel.IfcRelDefinesByProperties>(r =>
                            {
                                r.RelatingPropertyDefinition = model.Instances.New<Xbim.Ifc4.Kernel.IfcPropertySet>(pSet =>
                                {
                                    pSet.Name = "ePset_ProjectedCRS";
                                    pSet.Description = "Definition of the coordinate reference system";                                                                   //all collections are always initialized

                                    pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc4.PropertyResource.IfcPropertySingleValue>(p =>
                                    {
                                        p.Name = "Name";
                                        p.Description = "Name by which the coordinate reference system is defined, shall be EPSG-Code";
                                        p.NominalValue = new Xbim.Ifc4.MeasureResource.IfcLabel(lev50.CRS_Name);
                                    }));
                                    pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc4.PropertyResource.IfcPropertySingleValue>(p =>
                                    {
                                        p.Name = "Description";
                                        p.Description = "Informal description";
                                        p.NominalValue = new Xbim.Ifc4.MeasureResource.IfcText(lev50.CRS_Description);
                                    }));
                                    pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc4.PropertyResource.IfcPropertySingleValue>(p =>
                                    {
                                        p.Name = "GeodeticDatum";
                                        p.Description = "Name by which this datum is identified. " +
                                        "The geodetic datum is associated with the coordinate reference system and " +
                                        "indicates the shape and size of the rotation ellipsoid and this ellipsoid's " +
                                        "connection and orientation to the actual globe/earth.";

                                        p.NominalValue = new Xbim.Ifc4.MeasureResource.IfcIdentifier(lev50.CRS_Geodetic_Datum);
                                    }));
                                    pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc4.PropertyResource.IfcPropertySingleValue>(p =>
                                    {
                                        p.Name = "VerticalDatum";
                                        p.Description = "Name by which the vertical datum is identified. " +
                                        "The vertical datum is associated with the height axis of the " +
                                        "coordinate reference system and indicates the reference plane and " +
                                        "fundamental point defining the origin of a height system.";

                                        p.NominalValue = new Xbim.Ifc4.MeasureResource.IfcIdentifier(lev50.CRS_Vertical_Datum);
                                    }));
                                    pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc4.PropertyResource.IfcPropertySingleValue>(p =>
                                    {
                                        p.Name = "MapProjection";
                                        p.Description = "Name by which the map projection is identified";
                                        p.NominalValue = new Xbim.Ifc4.MeasureResource.IfcIdentifier(lev50.CRS_Projection_Name);
                                    }));
                                    pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc4.PropertyResource.IfcPropertySingleValue>(p =>
                                    {
                                        p.Name = "MapZone";
                                        p.Description = "Name by which the map zone is identified";
                                        p.NominalValue = new Xbim.Ifc4.MeasureResource.IfcIdentifier(lev50.CRS_Projection_Zone);
                                    }));
                                });
                            });

                            pSetMapCRS.RelatedObjects.Add((Xbim.Ifc4.Kernel.IfcProject)project);
                        }

                        if (json.IFCSchema == "Ifc2X3")
                        {
                            var pSetMapCRS = model.Instances.New<Xbim.Ifc2x3.Kernel.IfcRelDefinesByProperties>(r =>
                            {
                                r.RelatingPropertyDefinition = model.Instances.New<Xbim.Ifc2x3.Kernel.IfcPropertySet>(pSet =>
                                {
                                    pSet.Name = "ePset_ProjectedCRS";
                                    pSet.Description = "Definition of the coordinate reference system";                                                                   //all collections are always initialized

                                    pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue>(p =>
                                    {
                                        p.Name = "Name";
                                        p.Description = "Name by which the coordinate reference system is defined, shall be EPSG-Code";
                                        p.NominalValue = new Xbim.Ifc2x3.MeasureResource.IfcLabel(lev50.CRS_Name);
                                    }));
                                    pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue>(p =>
                                    {
                                        p.Name = "Description";
                                        p.Description = "Informal description";
                                        p.NominalValue = new Xbim.Ifc2x3.MeasureResource.IfcText(lev50.CRS_Description);
                                    }));
                                    pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue>(p =>
                                    {
                                        p.Name = "GeodeticDatum";
                                        p.Description = "Name by which this datum is identified. " +
                                        "The geodetic datum is associated with the coordinate reference system and " +
                                        "indicates the shape and size of the rotation ellipsoid and this ellipsoid's " +
                                        "connection and orientation to the actual globe/earth.";

                                        p.NominalValue = new Xbim.Ifc2x3.MeasureResource.IfcIdentifier(lev50.CRS_Geodetic_Datum);
                                    }));
                                    pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue>(p =>
                                    {
                                        p.Name = "VerticalDatum";
                                        p.Description = "Name by which the vertical datum is identified. " +
                                        "The vertical datum is associated with the height axis of the " +
                                        "coordinate reference system and indicates the reference plane and " +
                                        "fundamental point defining the origin of a height system.";

                                        p.NominalValue = new Xbim.Ifc2x3.MeasureResource.IfcIdentifier(lev50.CRS_Vertical_Datum);
                                    }));
                                    pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue>(p =>
                                    {
                                        p.Name = "MapProjection";
                                        p.Description = "Name by which the map projection is identified";
                                        p.NominalValue = new Xbim.Ifc2x3.MeasureResource.IfcIdentifier(lev50.CRS_Projection_Name);
                                    }));
                                    pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue>(p =>
                                    {
                                        p.Name = "MapZone";
                                        p.Description = "Name by which the map zone is identified";
                                        p.NominalValue = new Xbim.Ifc2x3.MeasureResource.IfcIdentifier(lev50.CRS_Projection_Zone);
                                    }));
                                });
                            });

                            pSetMapCRS.RelatedObjects.Add((Xbim.Ifc2x3.Kernel.IfcObject)project);
                        }


                        if(json.IFCSchema == "Ifc2X3" && lev50.Translation_Eastings != 0 && lev50.Translation_Northings != 0)
                        {
                            var unit = model.Instances.New<Xbim.Ifc2x3.MeasureResource.IfcSIUnit>();

                            unit.UnitType = Xbim.Ifc2x3.MeasureResource.IfcUnitEnum.LENGTHUNIT;
                            unit.Name = Xbim.Ifc2x3.MeasureResource.IfcSIUnitName.METRE;

                            var pSetMapConv = model.Instances.New<Xbim.Ifc2x3.Kernel.IfcRelDefinesByProperties>(r =>
                            {
                                //r.GlobalId = Guid.NewGuid();
                                r.RelatingPropertyDefinition = model.Instances.New<Xbim.Ifc2x3.Kernel.IfcPropertySet>(pSet =>
                                {
                                    pSet.Name = "ePset_MapConversion";
                                    pSet.Description = "Specification of the transformation between the local grid coordinate system and a map coordinate system";                                                                   //all collections are always initialized

                                    pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue>(p =>
                                        {
                                            p.Name = "Eastings";
                                            p.Description = "The translation in X between the two coordinate systems";
                                            p.NominalValue = new Xbim.Ifc2x3.MeasureResource.IfcLengthMeasure((double)lev50.Translation_Eastings);
                                            p.Unit = unit;
                                        }));
                                    pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue>(p =>
                                    {
                                        p.Name = "Northings";
                                        p.Description = "The translation in Y between the two coordinate systems";
                                        p.NominalValue = new Xbim.Ifc2x3.MeasureResource.IfcLengthMeasure((double)lev50.Translation_Northings);
                                        p.Unit = unit;
                                    }));
                                    pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue>(p =>
                                    {
                                        p.Name = "OrthogonalHeight";
                                        p.Description = "The translation in Z between the two coordinate systems";
                                        p.NominalValue = new Xbim.Ifc2x3.MeasureResource.IfcLengthMeasure((double)lev50.Translation_Orth_Height);
                                        p.Unit = unit;
                                    }));
                                    pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue>(p =>
                                    {
                                        p.Name = "XAxisAbscissa";
                                        p.Description = "The X component of the rotation between the two coordinate systems";
                                        p.NominalValue = new Xbim.Ifc2x3.MeasureResource.IfcReal(lev50.RotationXY[0]);
                                    }));
                                    pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue>(p =>
                                    {
                                        p.Name = "XAxisOrdinate";
                                        p.Description = "The Y component of the rotation between the two coordinate systems";
                                        p.NominalValue = new Xbim.Ifc2x3.MeasureResource.IfcReal(lev50.RotationXY[1]);
                                    }));
                                    pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue>(p =>
                                    {
                                        p.Name = "Scale";
                                        p.Description = "The scale in X, Y between the two coordinate systems";
                                        p.NominalValue = new Xbim.Ifc2x3.MeasureResource.IfcReal(lev50.Scale);
                                    }));
                                });
                            });

                            pSetMapConv.RelatedObjects.Add((Xbim.Ifc2x3.Kernel.IfcObject)project);

                            
                        }
                        else if (json.IFCSchema != "Ifc2X3" && lev50.Translation_Eastings != 0 && lev50.Translation_Northings != 0)
                        {
                            var mapConv = model.Instances.OfType<IIfcMapConversion>().Where(c => c.SourceCRS is IfcGeometricRepresentationContext).Single();

                            var mapCRS = (IfcProjectedCRS)mapConv.TargetCRS;

                            var mapUnit = mapCRS.MapUnit;

                            if(mapConv == null)
                            {
                                mapConv = model.Instances.New<IfcMapConversion>(m =>
                                {
                                    m.SourceCRS = (refObj as IIfcGeometricRepresentationContext);
                                    m.TargetCRS = model.Instances.New<IfcProjectedCRS>();

                                    mapCRS = (IfcProjectedCRS)m.TargetCRS;
                                });
                            }

                            mapConv.Eastings = (double)lev50.Translation_Eastings;
                            mapConv.Northings = (double)lev50.Translation_Northings;
                            mapConv.OrthogonalHeight = (double)lev50.Translation_Orth_Height;
                            mapConv.XAxisAbscissa = lev50.RotationXY[0];
                            mapConv.XAxisOrdinate = lev50.RotationXY[1];
                            mapConv.Scale = lev50.Scale;

                            mapCRS.Name = lev50.CRS_Name;
                            mapCRS.Description = lev50.CRS_Description;
                            mapCRS.GeodeticDatum = lev50.CRS_Geodetic_Datum;
                            mapCRS.VerticalDatum = lev50.CRS_Vertical_Datum;
                            mapCRS.MapProjection = lev50.CRS_Projection_Name;
                            mapCRS.MapZone = lev50.CRS_Projection_Zone;

                            var unit = model.Instances.New<IfcSIUnit>(u =>
                            {
                                u.UnitType = IfcUnitEnum.LENGTHUNIT;
                                u.Name = IfcSIUnitName.METRE;
                            });

                            mapCRS.MapUnit = unit;
                        }

                        var proj = (IIfcObjectDefinition)project;

                        proj = UpdateOwnerHistory((project as IIfcObjectDefinition), creation);
                    }

                    txn.Commit();
                }
                model.SaveAs(newDirec + fileName + "_updated");

                //catch(Exception e)
                //{
                //    MessageBox.Show("Error occured while updating LoGeoRef10 attribute values to IfcFile. \r\nError message: " + e.Message);
                //}
            }
        }

        private IIfcObjectDefinition GetRefObj(IfcStore model, string refNo)
        {
            var refObj = (IIfcObjectDefinition)model.Instances.Where(o => ("#" + o.GetHashCode()).Equals(refNo)).Single();

            return refObj;
        }

        private IIfcGeometricRepresentationContext GetRefCtx(IfcStore model, string refNo)
        {
            var refCtx = (IIfcGeometricRepresentationContext)model.Instances.Where(o => ("#" + o.GetHashCode()).Equals(refNo)).Single();

            return refCtx;
        }

        private Xbim.Ifc4.DateTimeResource.IfcTimeStamp GetCreationDate(IIfcObjectDefinition refObj)
        {
            Xbim.Ifc4.DateTimeResource.IfcTimeStamp creation;

            if(refObj.OwnerHistory == null || refObj.OwnerHistory.CreationDate == null)
            {
                creation = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            }
            else
                creation = refObj.OwnerHistory.CreationDate;

            return creation;
        }

        private IIfcObjectDefinition UpdateOwnerHistory(IIfcObjectDefinition refObj, Xbim.Ifc4.DateTimeResource.IfcTimeStamp creation)
        {
            refObj.OwnerHistory.CreationDate = creation;
            long timestamp = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            refObj.OwnerHistory.LastModifiedDate = new Xbim.Ifc4.DateTimeResource.IfcTimeStamp(timestamp);
            refObj.OwnerHistory.ChangeAction = IfcChangeActionEnum.MODIFIED;

            return refObj;
        }
    }
}