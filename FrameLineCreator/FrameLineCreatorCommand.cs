using System;
using Rhino;
using Rhino.Commands;
using Rhino.Collections;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI;
using Eto.Drawing;
using Eto.Forms;

namespace FrameLineCreator
{
    public class FrameLineCreatorCommand : Rhino.Commands.Command
    {
        public override string EnglishName => "CreateFrameLine";

        protected override Result RunCommand(Rhino.RhinoDoc doc, RunMode mode)
        {
            // Data from user
            int startFrame = 0;
            int endFrame = 0;
            int spacing = 0;
            bool modify = false;

            // Variables to capture user input
            int startMod = 0;
            int endMod = 0;
            int spacMod = 0;

            // List to capture modifications in frameline spacing
            RhinoList<int> startModList = new RhinoList<int>();
            RhinoList<int> endModList = new RhinoList<int>();
            RhinoList<int> spacModList = new RhinoList<int>();

            // Lists: spacing, frame numbers, if frame to be labeled
            RhinoList<int> spacingList = new RhinoList<int>();
            RhinoList<int> framesList = new RhinoList<int>();
            RhinoList<bool> ifLabelList = new RhinoList<bool>();

            // Move distance in x-axis for zero to be at zero
            int zeroMove = 0;

            // Points for main line of frameline
            RhinoList<Point3d> polyPoints = new RhinoList<Point3d>();

            int tempSum = 0;

            // Vertical lines height
            int frameHeight = 400;

            RhinoGet.GetInteger("Enter starting frame number: ", false, ref startFrame);
            RhinoGet.GetInteger("Enter end frame number: ", false, ref endFrame);
            RhinoGet.GetInteger("Enter typical spacing: ", false, ref spacing);
            RhinoGet.GetBool("Do you want to insert local modification of spacing?", true, "No", "Yes", ref modify);

            while (modify)
            {
                RhinoGet.GetInteger("Enter starting frame of modification: ", false, ref startMod);
                RhinoGet.GetInteger("Enter end frame of modification: ", false, ref endMod);
                RhinoGet.GetInteger("Enter spacing of modification: ", false, ref spacMod);

                startModList.Add(startMod);
                endModList.Add(endMod);
                spacModList.Add(spacMod);

                RhinoGet.GetBool("Do you want to add another local modification?", true, "No", "Yes", ref modify);
            }

            for (int i = 0; i < (endFrame - startFrame) + 1; i++)
            {
                spacingList.Add(spacing);
                framesList.Add(startFrame + i);
                if ((startFrame + i) % 5 == 0)
                {
                    ifLabelList.Add(true);
                }
                else
                {
                    ifLabelList.Add(false);
                }
            }

            if (startModList.Count > 0)
            {
                for (int i = 0; i < startModList.Count; i++)
                {
                    for (int j = 0; j < (endModList[i] - startModList[i]); j++)
                    {
                        spacingList[startModList[i] - startFrame + j] = spacModList[i];
                    }
                    ifLabelList[startModList[i] - startFrame] = true;
                    ifLabelList[endModList[i] - startFrame] = true;
                }
            }

            // First and last frame always with label
            ifLabelList[0] = true;
            ifLabelList[ifLabelList.Count - 1] = true;

            for (int i = 0; i < framesList.Count; i++)
            {
                if (framesList[i] < 0)
                {
                    zeroMove += spacingList[i];
                }
                else
                {
                    break;
                }
            }
                        
            polyPoints.Add(new Point3d(-zeroMove, 0, 0));

            for (int i = 0; i < framesList.Count; i++)
            {
                polyPoints.Add(new Point3d(spacingList[i] + tempSum - zeroMove, 0.0, 0.0));
                tempSum += spacingList[i];
            }
                        
            // Removing last point (end+1)
            polyPoints.RemoveAt(polyPoints.Count - 1);

            // Backup of current layer
            Rhino.DocObjects.Layer layerBackUp = RhinoDoc.ActiveDoc.Layers.CurrentLayer;
            int indexDef = layerBackUp.Index;

            //Creating layer for frameline
            Rhino.DocObjects.Layer flineLayer = new Rhino.DocObjects.Layer
            {
                Name = "Frameline"
            };
            int indexFL = doc.Layers.Add(flineLayer);         
            RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(indexFL, true);

            int textHeight = 150;

            for (int i = 0; i < polyPoints.Count; i++)
            {
                doc.Objects.AddLine(new Line(new Point3d(polyPoints[i][0], polyPoints[i][1] - frameHeight / 2, 0),
                                             new Point3d(polyPoints[i][0], polyPoints[i][1] + frameHeight / 2, 0)));
                doc.Objects.AddLine(new Line(new Point3d(polyPoints[i][0], 0, polyPoints[i][1] - frameHeight / 2),
                                             new Point3d(polyPoints[i][0], 0, polyPoints[i][1] + frameHeight / 2)));
                if (ifLabelList[i])
                {
                    Text3d tkst = new Text3d("Fr " + framesList[i].ToString());
                    Text3d tkstRotated = new Text3d("Fr " + framesList[i].ToString());

                    tkst.Height = textHeight;
                    tkstRotated.Height = textHeight;

                    tkst.HorizontalAlignment = Rhino.DocObjects.TextHorizontalAlignment.Center;
                    tkst.VerticalAlignment = Rhino.DocObjects.TextVerticalAlignment.Middle;

                    tkstRotated.HorizontalAlignment = Rhino.DocObjects.TextHorizontalAlignment.Center;
                    tkstRotated.VerticalAlignment = Rhino.DocObjects.TextVerticalAlignment.Middle;

                    tkst.TextPlane = new Plane(new Point3d(polyPoints[i][0], - frameHeight, 0), new Vector3d(0.0, 0.0, 1.0));
                    Plane rotPlane = new Plane(new Point3d(polyPoints[i][0], 0, -frameHeight), new Vector3d(0.0, 0.0, 1.0));
                    rotPlane.Rotate(Math.PI / 2, new Vector3d(1.0, 0.0, 0.0));
                    tkstRotated.TextPlane = rotPlane;
                    
                    doc.Objects.AddText(tkst);
                    doc.Objects.AddText(tkstRotated); 
                }
            }

            // Adding polyline to document
            doc.Objects.AddPolyline(polyPoints);
            RhinoDoc.ActiveDoc.Views.Redraw();

            // Backing up to previous layer and locking frameline layer
            RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(indexDef, true);
            RhinoDoc.ActiveDoc.Layers.FindIndex(indexFL).IsLocked = true;
            
            return Result.Success;
        }
    }
}