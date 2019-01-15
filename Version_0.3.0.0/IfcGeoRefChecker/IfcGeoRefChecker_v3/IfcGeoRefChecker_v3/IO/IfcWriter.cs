using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Xbim.Ifc;
using Xbim.Ifc4.DateTimeResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.RepresentationResource;

namespace IfcGeoRefChecker.IO
{
    internal class IfcWriter
    {
        //private IfcStore model;

        public IfcWriter(string file, string jsonObj)
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

            var fileIFC = file + ".ifc";

            using(var model = IfcStore.Open(fileIFC, editor))
            {
                try
                {
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

                            p.AddressLines.Add(lev10.AddressLines[0]);// .Add(this.AddressLines[0]);
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
                            var refObj = GetRefObj(model, lev20.Instance_Object[0]);
                            var creation = GetCreationDate(refObj);

                            var dmsLat = new Appl.Calc().DDtoCompound(lev20.Latitude);
                            var dmsLon = new Appl.Calc().DDtoCompound(lev20.Longitude);

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

                            plcm3D.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(lev30.ObjectLocationXYZ[0], lev30.ObjectLocationXYZ[1], lev30.ObjectLocationXYZ[2]));
                            plcm3D.RefDirection = model.Instances.New<IfcDirection>(d => d.SetXYZ(lev30.ObjectRotationX[0], lev30.ObjectRotationX[1], lev30.ObjectRotationX[2]));
                            plcm3D.Axis = model.Instances.New<IfcDirection>(d => d.SetXYZ(lev30.ObjectRotationZ[0], lev30.ObjectRotationZ[1], lev30.ObjectRotationZ[2]));

                            refObj = UpdateOwnerHistory(refObj, creation);
                        }

                        foreach(var lev40 in json.LoGeoRef40)
                        {
                            var refObj = GetRefObj(model, lev40.Reference_Object[0]);

                            var project = model.Instances.OfType<IIfcProject>().Where(c => c.RepresentationContexts.Contains((refObj as IIfcGeometricRepresentationContext))).Single();
                            var creation = GetCreationDate(project);

                            var elemPlcm = (IIfcLocalPlacement)(refObj as IIfcProduct).ObjectPlacement;
                            var plcm = elemPlcm.RelativePlacement;

                            if(plcm is IIfcAxis2Placement3D)
                            {
                                (plcm as IIfcAxis2Placement3D).Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(lev40.ProjectLocation[0], lev40.ProjectLocation[1], lev40.ProjectLocation[2]));
                                (plcm as IIfcAxis2Placement3D).RefDirection = model.Instances.New<IfcDirection>(d => d.SetXYZ(lev40.ProjectRotationX[0], lev40.ProjectRotationX[1], lev40.ProjectRotationX[2]));
                                (plcm as IIfcAxis2Placement3D).Axis = model.Instances.New<IfcDirection>(d => d.SetXYZ(lev40.ProjectRotationZ[0], lev40.ProjectRotationZ[1], lev40.ProjectRotationZ[2]));
                            }
                            else
                            {
                                (plcm as IIfcAxis2Placement2D).Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXY(lev40.ProjectLocation[0], lev40.ProjectLocation[1]));
                                (plcm as IIfcAxis2Placement2D).RefDirection = model.Instances.New<IfcDirection>(d => d.SetXY(lev40.ProjectRotationX[0], lev40.ProjectRotationX[1]));
                            }

                            //(refObj as IIfcGeometricRepresentationContext).WorldCoordinateSystem = plcm;
                            (refObj as IIfcGeometricRepresentationContext).TrueNorth = model.Instances.New<IfcDirection>(d => d.SetXY(lev40.TrueNorthXY[0], lev40.TrueNorthXY[1]));

                            refObj = UpdateOwnerHistory(project, creation);
                        }

                        foreach(var lev50 in json.LoGeoRef50)
                        {
                            var refObj = GetRefObj(model, lev50.Reference_Object[0]);

                            var project = model.Instances.OfType<IIfcProject>().Where(c => c.RepresentationContexts.Contains((refObj as IIfcGeometricRepresentationContext))).Single();

                            var creation = GetCreationDate(refObj);

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

                            mapConv.Eastings = lev50.Translation_Eastings;
                            mapConv.Northings = lev50.Translation_Northings;
                            mapConv.OrthogonalHeight = lev50.Translation_Orth_Height;
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

                            refObj = UpdateOwnerHistory(project, creation);
                        }

                        txn.Commit();
                    }

                    model.SaveAs(file + "_updated");
                }
                catch(Exception e)
                {
                    MessageBox.Show("Error occured while updating LoGeoRef10 attribute values to IfcFile. \r\nError message: " + e.Message);
                }

                //foreach (var lev10 in json.LoGeoRef10)
                //{
                //    var l10 = new Appl.Level10(model, lev10.)

                //    lev20.UpdateLevel20();
                //}

                //get existing door from the model
                //var id = "3cUkl32yn9qRSPvBJVyWYp";
                //var theDoor = model.Instances.FirstOrDefault<IfcDoor>(d => d.GlobalId == id);

                ////open transaction for changes
                //using(var txn = model.BeginTransaction("Doors modification"))
                //{
                //    //create new property set with two properties
                //    var pSetRel = model.Instances.New<IfcRelDefinesByProperties>(r =>
                //    {
                //        r.GlobalId = Guid.NewGuid();
                //        r.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pSet =>
                //        {
                //            pSet.Name = "New property set";
                //            //all collections are always initialized
                //            pSet.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                //            {
                //                p.Name = "First property";
                //                p.NominalValue = new IfcLabel("First value");
                //            }));
                //            pSet.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                //            {
                //                p.Name = "Second property";
                //                p.NominalValue = new IfcLengthMeasure(156.5);
                //            }));
                //        });
                //    });

                //    //change the name of the door
                //    theDoor.Name += "_checked";
                //    //add properties to the door
                //    pSetRel.RelatedObjects.Add(theDoor);

                //    //commit changes
                //    txn.Commit();
                //}
            }
        }

        private IIfcObjectDefinition GetRefObj(IfcStore model, string refNo)
        {
            var refObj = (IIfcObjectDefinition)model.Instances.Where(o => ("#" + o.GetHashCode()).Equals(refNo)).Single();

            return refObj;
        }

        private IfcTimeStamp GetCreationDate(IIfcObjectDefinition refObj)
        {
            IfcTimeStamp creation;

            if(refObj.OwnerHistory == null || refObj.OwnerHistory.CreationDate == null)
            {
                creation = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            }
            else
                creation = refObj.OwnerHistory.CreationDate;

            return creation;
        }

        private IIfcObjectDefinition UpdateOwnerHistory(IIfcObjectDefinition refObj, IfcTimeStamp creation)
        {
            refObj.OwnerHistory.CreationDate = creation;
            long timestamp = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            refObj.OwnerHistory.LastModifiedDate = new IfcTimeStamp(timestamp);
            refObj.OwnerHistory.ChangeAction = IfcChangeActionEnum.MODIFIED;

            return refObj;
        }

        //private void UpdatePlacementXYZ(IfcStore model, IfcAxis2Placement plcm, Appl.Level30? Lev30,  )
        //{
        //    var schema = json.IFCSchema;

        //    if(plcm is IIfcAxis2Placement3D)
        //    {
        //        //if(schema == "Ifc2X3")
        //        //{
        //        //    (plcm as IIfcAxis2Placement3D).Location = model.Instances.New<Xbim.Ifc2x3.GeometryResource.IfcCartesianPoint>(p => p.SetXYZ(this.LocationXYZ[0], this.LocationXYZ[1], this.LocationXYZ[2]));
        //        //    plcm3D.RefDirection = model.Instances.New<Xbim.Ifc2x3.GeometryResource.IfcDirection>(d => d.SetXYZ(this.RotationX[0], this.RotationX[1], this.RotationX[2]));
        //        //    plcm3D.Axis = model.Instances.New<Xbim.Ifc2x3.GeometryResource.IfcDirection>(d => d.SetXYZ(this.RotationZ[0], this.RotationZ[1], this.RotationZ[2]));
        //        //}
        //        //else
        //        //{
        //        (plcm as IIfcAxis2Placement3D).Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(le  this.LocationXYZ[0], this.LocationXYZ[1], this.LocationXYZ[2]));
        //        (plcm as IIfcAxis2Placement3D).RefDirection = model.Instances.New<IfcDirection>(d => d.SetXYZ(this.RotationX[0], this.RotationX[1], this.RotationX[2]));
        //        (plcm as IIfcAxis2Placement3D).Axis = model.Instances.New<IfcDirection>(d => d.SetXYZ(this.RotationZ[0], this.RotationZ[1], this.RotationZ[2]));
        //        //}
        //    }

        //    if(plcm is IIfcAxis2Placement2D)
        //    {
        //        if(schema == "Ifc2X3")
        //        {
        //            plcm2D.Location = model.Instances.New<Xbim.Ifc2x3.GeometryResource.IfcCartesianPoint>(p => p.SetXY(this.LocationXYZ[0], this.LocationXYZ[1]));
        //            plcm2D.RefDirection = model.Instances.New<Xbim.Ifc2x3.GeometryResource.IfcDirection>(d => d.SetXY(this.RotationX[0], this.RotationX[1]));
        //        }
        //        else
        //        {
        //            plcm2D.Location = model.Instances.New<Xbim.Ifc4.GeometryResource.IfcCartesianPoint>(p => p.SetXY(this.LocationXYZ[0], this.LocationXYZ[1]));
        //            plcm2D.RefDirection = model.Instances.New<Xbim.Ifc4.GeometryResource.IfcDirection>(d => d.SetXY(this.RotationX[0], this.RotationX[1]));
        //        }
        //    }
        //}
    }
}