import Rhino.Geometry as rg





class ConvexHull():
    def __init__(self,pts):
        self.pts=pts
        self.convexPts=[]


    @staticmethod
    def get_right_point(pts):
        #得到最右侧的点，如果最右侧边界有多个点，就取最上方的点
        y_vals=[]
        x_vals = sorted([pt.X for pt in pts])
        x= x_vals[-1]
        for pt in pts:
            if pt[0] == x:
                y_vals.append(pt.Y)
        y = max(y_vals)
        return rg.Point3d(x, y, 0)

    @staticmethod
    def get_left_point(pts):
        #得到最左侧的点，如果最左侧边界有多个点，就取最上方的点
        y_vals=[]
        x_vals = sorted([pt.X for pt in pts])
        x= x_vals[0]
        for pt in pts:
            if pt[0] == x:
                y_vals.append(pt.Y)
        y = max(y_vals)
        return rg.Point3d(x, y, 0)

    @staticmethod
    def get_top_point(pts):
        #得到最上方的点，如果最上边界有多个点，就取最左侧的点
        x_vals=[]
        y_vals = sorted([pt.Y for pt in pts])
        y= y_vals[-1]
        for pt in pts:
            if pt[1] == y:
                x_vals.append(pt.X)
        x = min(x_vals)
        return rg.Point3d(x, y, 0)

    @staticmethod
    def get_bottom_point(pts):
        #得到最下方的点，如果最下边界有多个点，就取最左侧的点
        x_vals=[]
        y_vals = sorted([pt.Y for pt in pts])
        y= y_vals[0]
        for pt in pts:
            if pt[1] == y:
                x_vals.append(pt.X)
        x = min(x_vals)
        return rg.Point3d(x, y, 0)

    def get_upper_pts(self,pts):
        #得到最左侧点、最右侧点、二者连线上方的点
        upperPts=[]
        leftPt=self.get_left_point(pts)
        rightPt=self.get_right_point(pts)


        vec0=rightPt-leftPt
        for pt in pts:
            vec1=pt-leftPt
            vec2=rg.Vector3d.CrossProduct(vec0,vec1)
            if vec2.Z>=0:
                upperPts.append(pt)

        return upperPts

    def get_upper_max_triangle(self,pts):
        #在底边确定的前提下，得到上方面积最大的三角形
        leftPt=self.get_left_point(pts)
        rightPt=self.get_right_point(pts)
        pts.remove(leftPt)
        pts.remove(rightPt)
        topPt=self.get_top_point(pts)
        pts=[leftPt,rightPt,topPt,leftPt]
        polyline=rg.Polyline(pts)
        triangle=polyline.ToNurbsCurve()

        return triangle

    def div_pts(self,tri,pts):
        """
        根据点和三角形的位置关系，将点分为左半部分和右半部分
        其中，左半部分包括三角形的左斜边两顶点
        右半部分包括三角形右斜边两顶点
        return 左右两部分的点
        """
        #得到三角形左侧斜边的向量
        poly=tri.ToPolyline(0.001,0.001,0.001,9999)
        vec=-poly.TangentAtEnd

        leftPts=[]
        rightPts=[]
        pStart=poly.PointAtStart

        #根据向量叉乘结果的Z值正负筛选点，若为正，则位于左侧，反之位于右侧，分别添加至列表
        for pt in pts:
            vec1=pt-pStart
            vec2=rg.Vector3d.CrossProduct(vec,vec1)
            if vec2.Z<0:
                rightPts.append(pt)
            else:
                leftPts.append(pt)

        #将三角形最高点添加至左侧列表，以此确保左右两半部分分别包含三角形左右两边的端点
        topPt=self.get_top_point(pts)
        leftPts.append(topPt)
        return leftPts,rightPts


    def add_above_convexPt(self,pts):
        """
        得到位于最左侧点和最右侧点连线上方的所有凸包上的点
        若连线上方没有点，则返回空
        ============计算思路===========
        如果左端点和右端点上方有点，就取连线上方的最高点
        并将其与两端点连线组成三角形，位于三角形内部和边线上的点必定不是凸包上的点
        如果三角形外没有点，则只将最左侧点、最右侧点和最高点添加至凸包点列表
        若三角形外有点，取连线上方位于三角形外的点
        根据上方三角形外部的点与三角形的位置关系分为左半部分和右半部分
        
        递归
        """
        upperPts=self.get_upper_pts(pts)
        if len(upperPts)>2:
            #如果左右端点连线上方有点，将最高点添加至凸包点列表
            topPt=self.get_top_point(upperPts)
            leftPt=self.get_left_point(upperPts)
            rightPt=self.get_right_point(upperPts)
            self.convexPts.append(topPt)

            #利用三角形筛选位于外部的点和三角形的三个端点
            triangle=self.get_upper_max_triangle(pts)
            outsidePts=[]
            for pt in upperPts:
                notInCrv = triangle.Contains(pt,rg.Plane.WorldXY,0.001)
                if notInCrv== rg.PointContainment.Outside or notInCrv== rg.PointContainment.Coincident:
                    outsidePts.append(pt)
            
            #如果外部有点，根据与三角形的位置关系分为左半部分与右半部分
            if len(outsidePts)>3:
                leftPts=self.div_pts(triangle,outsidePts)[0]
                rightPts=self.div_pts(triangle,outsidePts)[1]
                # self.add_above_convexPt(leftPts)
                # self.add_above_convexPt(rightPts)



do=ConvexHull(pts)
c=do.add_above_convexPt(pts)
b=do.convexPts

