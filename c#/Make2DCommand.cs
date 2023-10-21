using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;

namespace Make2D
{
    public class Make2DCommand : Command
    {
        public Make2DCommand()
        {
            Instance = this;
        }
        public static Make2DCommand Instance { get; private set; }
        public override string EnglishName => "Make2DCommand";
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            //选择要make2d的几何物体（点、面、多重曲面）
            var go = new GetObject();
            go.SetCommandPrompt("Select objects to test Make2D Points");
            go.GeometryFilter = ObjectType.Point | ObjectType.Surface | ObjectType.PolysrfFilter;
            go.GroupSelect = true;
            go.GetMultiple(1, 0);
            if (go.CommandResult() != Result.Success)
                return go.CommandResult();


            //激活当前视口
            var view = doc.Views.ActiveView;
            if (null == view)
                return Result.Failure;

            //得到视口中的物体
            var obj_refs = go.Objects();


            //创建线绘制参数实例，
            var hld_params = new HiddenLineDrawingParameters
            {
                AbsoluteTolerance = doc.ModelAbsoluteTolerance,
                IncludeTangentEdges = false,
                IncludeHiddenCurves = true
            };

            //将当前视口设置为线绘制视口
            hld_params.SetViewport(view.ActiveViewport);

            //将当前视口中的物体添加至线绘制所需物体中
            foreach (var obj_ref in obj_refs)
            {
                var obj = obj_ref?.Object();
                if (obj != null)
                    hld_params.AddGeometry(obj.Geometry, Transform.Identity, obj.Id);
            }

            //利用前述线绘制参数，创建线绘制实例，允许多线程计算
            var hld = HiddenLineDrawing.Compute(hld_params, true);


            if (hld != null)
            {
                //创建拍平的transform动作，可将物体拍平至世界坐标xy平面
                var flatten = Transform.PlanarProjection(Plane.WorldXY);

                //得到投影线至世界坐标原点的向量，使线flatten后可移至坐标原点
                BoundingBox page_box = hld.BoundingBox(true);
                var delta_2d = new Vector2d(0, 0);
                delta_2d = delta_2d - new Vector2d(page_box.Min.X, page_box.Min.Y);
                var delta_3d = Transform.Translation(new Vector3d(delta_2d.X, delta_2d.Y, 0.0));
                flatten = delta_3d * flatten;

                //创建两个名称分别为可见（V）和不可见（H）的两个属性
                var h_attribs = new ObjectAttributes { Name = "H" };
                var v_attribs = new ObjectAttributes { Name = "V" };


                
                foreach (var hld_curve in hld.Segments)
                {
                    //对绘制完每条线，如果线不是空或且它轮廓线不是空，就复制一条线
                    if (hld_curve?.ParentCurve == null || hld_curve.ParentCurve.SilhouetteType == SilhouetteType.None)
                        continue;

                    var crv = hld_curve.CurveGeometry.DuplicateCurve();

                    //如果该线不是空，对它执行拍平动作，并且根据原来线的可见性不同设置不同的属性，添加到rhino文档中
                    if (crv != null)
                    {
                        crv.Transform(flatten);
                        switch (hld_curve.SegmentVisibility)
                        {
                            case HiddenLineDrawingSegment.Visibility.Visible:
                                doc.Objects.AddCurve(crv, v_attribs);
                                break;
                            case HiddenLineDrawingSegment.Visibility.Hidden:
                                doc.Objects.AddCurve(crv, h_attribs);
                                break;
                        }
                    }
                }


                foreach (var hld_pt in hld.Points)
                {
                    //如果make2d的物体为点，对于绘制结果的每个点，如果点不是空，则根据该点的位置创建一个3d点
                    if (hld_pt == null)
                        continue;

                    var pt = hld_pt.Location;

                    //如果该点有效，对其进行拍平操作，并根据该点的可见性不同，设置不同属性，添加到rhino文档中
                    if (pt.IsValid)
                    {
                        pt.Transform(flatten);
                        switch (hld_pt.PointVisibility)
                        {
                            case HiddenLineDrawingPoint.Visibility.Visible:
                                doc.Objects.AddPoint(pt, v_attribs);
                                break;
                            case HiddenLineDrawingPoint.Visibility.Hidden:
                                doc.Objects.AddPoint(pt, h_attribs);
                                break;
                        }
                    }
                }
            }
            //更新视口
            doc.Views.Redraw();
            return Result.Success;
        }
    }
}
