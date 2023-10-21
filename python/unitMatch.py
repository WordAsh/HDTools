import System
import System.Collections.Generic.IEnumerable as IEnumerable

import copy
import Rhino.Geometry as rg
import Rhino.RhinoDoc as doc
import math
import threading
from collections import defaultdict

tol = doc.ModelAbsoluteTolerance

class UnitFrame:
    def __init__(self,plane,frame,srf):
        self.plane = plane
        self.frame = frame
        self.srf = srf
        self.w=0
        self.h=0

    #得到物体中心点
    def get_centerPt(self):
        Frame=rg.Curve.JoinCurves(self.frame)#组合边框线
        bbox=Frame[0].GetBoundingBox(rg.Plane.WorldXY)
        centerPt=bbox.Center
        return centerPt
    
    #得到unit的尺寸
    def get_unit_size(self):
        for crv in self.frame:
            vec=crv.TangentAtStart
            result=rg.Vector3d.CrossProduct(vec,rg.Vector3d(0,0,1))
            if result.Length<0.001:
                self.h=crv.GetLength()
            else:
                self.w=crv.GetLength()

class PlaneTest:
    @staticmethod
    #检查两平面是否共面
    def is_coplanar(plane1,plane2):
        #先判断两平面是否平行
        norm1=plane1.Normal
        norm2=plane2.Normal
        vec=rg.Vector3d.CrossProduct(norm1,norm2)
        if vec.Length<0.001:#若平行，再判断是否共面
            pt=plane1.Origin
            if plane2.DistanceTo(pt)<0.001:
                return True

class GetBrepFace:
    @staticmethod
    #得到一组breps的包围盒
    def get_breps_bbox(breps,plane):
        bbox=breps[0].GetBoundingBox(plane)
        if len(breps)>1:
            for i in range(len(breps)-1):
                b=breps[i].GetBoundingBox(plane)
                bbox=rg.BoundingBox.Union(bbox,b)


        return bbox


    @staticmethod
    #在brep的face中找与给定平面共平面的面
    def get_common_face(brep,plane):
        for face in brep.Faces:
            p=face.TryGetPlane()[1]
            if PlaneTest.is_coplanar(p,plane):
                return face

    # 求brep的基准平面
    @staticmethod
    def get_brep_plane(brep):
        vertices = brep.Vertices
        points = [vertice.Location for vertice in vertices]
        return rg.Plane.FitPlaneToPoints(points)[1]

    # 找竖向面
    def get_vertical_faces(self,brep):
        faces = []
        for face in brep.Faces:
            normal = self.get_brep_plane(face.ToBrep()).Normal
            ang = rg.Vector3d.VectorAngle(rg.Vector3d(0,0,1),normal)
            if ang == math.pi/2:
                faces.append(face.ToBrep())
        return faces

class SetFrames:
    @staticmethod
    #在竖向面上按生成定位单元
    def generate_frames(face,cutter):
        frames=[]
        cutters=cutter.get_face_cutters(face)

        breps = face.Split.Overloads[IEnumerable[rg.Curve], System.Double](cutters,0.01)
        for brep in breps:
            # plane = GetBrepFace.get_brep_plane(brep)

            srf=brep.Faces[0]#给unit添加面属性
            plane=srf.TryGetPlane()[1]
            frame=brep.DuplicateEdgeCurves()
            unit_frame = UnitFrame(plane,frame,srf)
            frames.append(unit_frame)

        return frames

class Cutter: 
    def __init__(self):
        self.u_count = 0
        self.v_count = 0
        self.u_steps=[]
        self.v_steps=[]
    
    # 求横向竖向基础分割线
    @staticmethod
    def get_face_base_cutter(face):
        edges = face.DuplicateEdgeCurves()
        for crv in edges:
            vec = crv.TangentAtStart
            if rg.Vector3d.VectorAngle(rg.Vector3d(0,0,1),vec)-0 <= tol:
                v_cutter = crv
        for crv in edges:
            if crv != v_cutter and crv.PointAtEnd == v_cutter.PointAtStart:
                u_cutter = crv
        return { "u":u_cutter,"v":v_cutter}
    
    # 用向量复制线
    @staticmethod
    def duplicate_crv_with_vec(crv,vec):
        trans = rg.Transform.Translation(vec)
        new_crv = crv.Duplicate()
        new_crv.Transform(trans)
        return new_crv

class GetCutterUniform(Cutter):
    #按数量复制分割线,均匀分
    def __init__(self,u_count,v_count):
        self.u_count=u_count
        self.v_count=v_count
        self.u_steps=[]
        self.v_steps=[]

    def get_face_cutters(self,face):
        base_cutter = self.get_face_base_cutter(face)

        u_cutter = base_cutter["u"]
        v_cutter = base_cutter["v"]
        
        u_vec = -u_cutter.TangentAtStart
        v_vec = v_cutter.TangentAtStart
        
        cutters = []
        u_step = u_cutter.GetLength()/self.u_count
        v_step = v_cutter.GetLength()/self.v_count
        
        for i in range(1,self.u_count):
            vec = i*u_step*u_vec
            cutters.append(self.duplicate_crv_with_vec(v_cutter,vec))
            
        for i in range(1,self.v_count):
            vec = i*v_step*v_vec
            cutters.append(self.duplicate_crv_with_vec(u_cutter,vec))
        return cutters

class GetCutterCustom(Cutter):
    #按距离复制分割线，自定义分
    def __init__(self,u_steps,v_steps):
        self.u_steps=u_steps
        self.v_steps=v_steps
        self.u_count=0
        self.v_count=0

    def get_face_cutters(self,face):
        
        base_cutter=self.get_face_base_cutter(face)
        u_cutter = base_cutter["u"]
        v_cutter = base_cutter["v"]
        
        u_vec = -u_cutter.TangentAtStart
        v_vec = v_cutter.TangentAtStart
        
        cutters = []
        self.u_count=len(self.u_steps)
        self.v_count=len(self.v_steps)

        for i in range(0,self.u_count+1):
            vec=sum(u_steps[:i])*u_vec
            cutters.append(self.duplicate_crv_with_vec(v_cutter,vec))

        for i in range(0,self.v_count+1):
            vec=sum(v_steps[:i])*v_vec
            cutters.append(self.duplicate_crv_with_vec(u_cutter,vec))
        return cutters

class GetCutterUUniform(Cutter):
    #按数量分U方向，按距离分V方向
    def __init__(self,u_count,v_steps):
        self.u_steps=[]
        self.v_steps=v_steps
        self.u_count=u_count
        self.v_count=0

    def get_face_cutters(self,face):
        
        base_cutter=self.get_face_base_cutter(face)
        u_cutter = base_cutter["u"]
        v_cutter = base_cutter["v"]
        
        u_vec = -u_cutter.TangentAtStart
        v_vec = v_cutter.TangentAtStart
        
        cutters = []
        u_step = u_cutter.GetLength()/self.u_count
        self.v_count=len(self.v_steps)

        for i in range(1,self.u_count):
            vec = i*u_step*u_vec
            cutters.append(self.duplicate_crv_with_vec(v_cutter,vec))

        for i in range(0,self.v_count+1):
            vec=sum(v_steps[:i])*v_vec
            cutters.append(self.duplicate_crv_with_vec(u_cutter,vec))
        return cutters

class GetCutterVUniform(Cutter):
    #按数量分V方向，按距离分U方向
    def __init__(self,u_steps,v_count):
        self.u_steps=u_steps
        self.v_steps=[]
        self.u_count=0
        self.v_count=v_count

    def get_face_cutters(self,face):
        
        base_cutter=self.get_face_base_cutter(face)
        u_cutter = base_cutter["u"]
        v_cutter = base_cutter["v"]
        
        u_vec = -u_cutter.TangentAtStart
        v_vec = v_cutter.TangentAtStart
        
        cutters = []
        self.u_count=len(self.u_steps)
        v_step = v_cutter.GetLength()/self.v_count

        for i in range(0,self.u_count+1):
            vec=sum(u_steps[:i])*u_vec
            cutters.append(self.duplicate_crv_with_vec(v_cutter,vec))

        for i in range(1,self.v_count):
            vec = i*v_step*v_vec
            cutters.append(self.duplicate_crv_with_vec(u_cutter,vec))
        return cutters

class GetCutter:
    @staticmethod
    #根据输入端的U，V count输入情况，决定cutter实例化类型
    #count端的优先级大于steps端
    def create_cutter():
        if u_count is None and v_count is not None:
            cutter=GetCutterVUniform(u_steps,v_count)
        elif u_count is None and v_count is None:
            cutter=GetCutterCustom(u_steps,v_steps)
        elif u_count is not None and v_count is not None:
            cutter=GetCutterUniform(u_count,v_count)
        elif u_count is not None and v_count is None:
            cutter=GetCutterUUniform(u_count,v_steps)
        return cutter

class Element:
    def __init__(self,geos,wAxis,hAxis,base):
        wAxis.Unitize()
        hAxis.Unitize()
        self.geos=geos
        self.wAxis=wAxis
        self.hAxis=hAxis
        self.base=base
        self.w=0
        self.h=0
        self.plane=None
        self.centerPt=None

    #赋予element宽高尺寸，平面，基面中心点等属性
    def set_attributes(self):
        bbox=GetBrepFace.get_breps_bbox(self.geos,self.plane)
        self.plane=rg.Plane(self.base,self.wAxis,self.hAxis)#得到基准面

        face=GetBrepFace.get_common_face(bbox.ToBrep(),self.plane)
        face.SetDomain(0,rg.Interval(0,1))
        face.SetDomain(1,rg.Interval(0,1))
        self.centerPt=face.PointAt(0.5,0.5)#得到基准面中点

        srf=face.ToNurbsSurface()
        #将曲面uv方向与单元wh方向进行匹配
        vecU=srf.UVNDirectionsAt(0,0)[1]#得到曲面u方向
        result=rg.Vector3d.CrossProduct(vecU,self.wAxis)
        #若U方向与单元W方向共线，则曲面U宽度设置为单元宽度
        if result.Length<0.001:
            self.w=srf.GetSurfaceSize()[1]
            self.h=srf.GetSurfaceSize()[2]
        else:
            self.w=srf.GetSurfaceSize()[2]
            self.h=srf.GetSurfaceSize()[1]

class Transform:
    def __init__(self):
        self.cells=[]

    @staticmethod
    #确定w方向，h方向缩放比例
    def get_scale_factor(unit,element):
        unit.get_unit_size()
        element.set_attributes()
        w0=unit.w
        h0=unit.h
        w1=element.w
        h1=element.h
        w_factor=w0/w1
        h_factor=h0/h1
        return w_factor,h_factor

    @staticmethod
    #根据w,h值中心缩放单元
    def scale_element(element,center,w_factor,h_factor):
        plane=rg.Plane(center,element.wAxis,element.hAxis)
        trans=rg.Transform.Scale(plane,w_factor,h_factor,1)
        gs=element.geos
        objs=[]
        for g in gs:
            g.Transform(trans)
            objs.append(g)
        return objs

    @staticmethod
    #将物体从基准点移动至目标点
    def map_to_point(base,target,geos):
        vec=rg.Vector3d(target-base)
        trans=rg.Transform.Translation(vec)
        gs=geos
        objs=[]
        for g in gs:
            g.Transform(trans)
            objs.append(g)
        return objs

    @staticmethod
    #将物体从基准面映射至目标平面
    def map_to_plane(geos,plane1,plane2):
        trans=rg.Transform.PlaneToPlane(plane1,plane2)
        gs=geos
        objs=[]
        for g in gs:
            g.Transform(trans)
            objs.append(g)
        return objs

    #将element变换至一个unit
    def transform_one_element(self,unit,element):
        #第一步对基本单元进行缩放
        center=element.centerPt
        w_factor,h_factor=self.get_scale_factor(unit,element)
        scaled=self.scale_element(element,center,w_factor,h_factor)

        #第二步将缩放后的单元映射至unit平面
        plane0=element.plane
        plane=unit.plane
        planeMapped=self.map_to_plane(scaled,plane0,plane)

        pt=[]
        pt.append(center)
        ptMapped=self.map_to_plane(pt,plane0,plane)[0]#将element中点也进行映射，便于定位

        #第三步将平面映射后的单元物件移动至unit中心
        target=unit.get_centerPt()
        pointMapped=self.map_to_point(ptMapped,target,planeMapped)
        return pointMapped


    #将element变换至多个unit
    def transform_multi_element(self,units,element):

        for i in range(len(units)):
            geos=self.transform_one_element(units[i],copy.deepcopy(element))
            self.cells.extend(geos)

    #根据element中元素数量，筛选出完整一套element几何体
    def partition(self,element):
        cell=defaultdict(list)
        n=len(element.geos)
        lsts=[self.cells[i:i+n] for i in range(0,len(self.cells),n)]
        for i in range(len(lsts)):
            cell[i]=lsts[i]
        return cell

if __name__ == "__main__":
    cutter=GetCutter.create_cutter()
    face=GetBrepFace().get_vertical_faces(x)[0]#从四个竖直面中取一个
    units=SetFrames.generate_frames(face,cutter)#得到立面上的unit单元

    frames=[]
    for unit in units:
        frames.extend(unit.frame)#显示幕墙划分

    if bool==True:
        ele=Element(cell,cell_W,cell_H,cell_base)
        ele.set_attributes()#实例化单元件

        transformer=Transform()

        # a=transformer.cells
        # cell=transformer.partition(ele)[i]#显示结果

        # a=transformer.transform_one_element(units[0],ele)
        transformer.transform_multi_element(units,ele)
        a=transformer.cells


        

















