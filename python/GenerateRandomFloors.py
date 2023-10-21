
import System
import Rhino.Geometry as rg
import random as r

class GenerateRandomFloor():
    
    def __init__(self,crv,num,min,max,d):
        self.crv=crv
        self.num=num
        self.d=d
        self.polys=[]
        self.borders=[]
    
    @staticmethod
    def is_point_in_curve(pt, crv):
        #检查点是否在边界内
        tol = doc.ActiveDoc.ModelAbsoluteTolerance
        isInCrv = crv.Contains(pt,rg.Plane.WorldXY,tol)
        if isInCrv == rg.PointContainment.Inside or isInCrv == rg.PointContainment.Coincident:
            return pt
    
    @staticmethod
    def is_two_curves_intersect(crv0, crv1):
        tol = doc.ActiveDoc.ModelAbsoluteTolerance
        events=rg.Intersect.Intersection.CurveCurve(crv0,crv1,tol,0.0)
        if events:
            return True

    def random_point_in_curve(self):
        #在边界内生成一个随机点
        bbox = self.crv.GetBoundingBox(True)
        while True:
            X=r.uniform(bbox.Min.X,bbox.Max.X)
            Y=r.uniform(bbox.Min.Y,bbox.Max.Y)
            pt = rg.Point3d(X,Y,0)
            if self.is_point_in_curve(pt, self.crv):
                return pt

    def generate_rectangle(self,pt):
        #生成矩形
        self.w=r.uniform(min,max)
        self.h=r.uniform(min,max)
        rec=rg.Rectangle3d(rg.Plane(pt,rg.Vector3d(0,0,1)),self.w,self.h).ToNurbsCurve()
        return rec
        
    def generate_back_rectangle(self,crv):
        #生成退距线框
        tol = doc.ActiveDoc.ModelAbsoluteTolerance
        offset_rec=crv.Offset(rg.Plane.WorldXY,d,tol,rg.CurveOffsetCornerStyle(1))[0]
        return offset_rec
        
    def is_curve_valid(self, crv0, crv1):
        bbox = crv1.GetBoundingBox(True)
        pt=bbox.Center
        if not self.is_point_in_curve(pt, crv0) and not self.is_two_curves_intersect(crv0, crv1):
            return crv1


    def generate(self):
        while len(self.polys)<self.num:
            pt=self.random_point_in_curve()
            rec=self.generate_rectangle(pt)
            border=self.generate_back_rectangle(rec)
            
            if not self.is_two_curves_intersect(self.crv,rec):
                if len(self.polys) ==0:
                    #如果内部没有退距线框，直接将矩形添加至polys列表
                    self.polys.append(rec)
                    self.borders.append(border)
                    
                else:
                    #若已有退距线框，判断新生成矩形是否与原有矩形相交
                    i=0
                    while i<len(self.borders):
                        if self.is_curve_valid(self.borders[i],rec) and self.is_curve_valid(self.polys[i],rec):
                            i+=1
                            if i==len(self.borders):
                                if border not in self.borders:
                                    self.polys.append(rec)
                                    self.borders.append(border)
                                else:
                                    break
                        else:
                            break


if __name__ == "__main__":
    a=GenerateRandomFloor(crv,num,min,max,d)
    a.generate()
    floors=a.polys


