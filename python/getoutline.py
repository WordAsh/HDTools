# coding=utf-8
import Rhino.Geometry as rg
import math

class Convex:
    def __init__(self, pts):
        self.pts = pts
        self.tol = 0.0001

    def get_bottom_point(self, points):
        """
        返回points中纵坐标最小的点的索引,如果有多个纵坐标最小的点则返回其中横坐标最小的那个
        :param points: 初始点阵
        :return:
        """
        x_vals = []
        y_vals = sorted([i.Y for i in points])
        y0 = y_vals[0]
        y1 = y_vals[1]
        if y1-y0 <= self.tol:
            for it in points:
                if it[1] == y0 or it[1] == y1:
                    x_vals.append(it.X)
            x = min(x_vals)
        else:
            for it in points:
                if it[1] == y0:
                    x_vals.append(it.X)
            x = min(x_vals)
        return rg.Point3d(x, y0, 0)

    def pt_normal_val(self, pt, bottom_point):
        """
        返回每个point_与bottom_point构成的向量,每个point_与bottom_point形成矢量的模长
        :param pt: 单点
        :param bottom_point: 极小点
        :return vec: 单点与极小点构成的矢量
        :return bottom_point: 单点与极小点构成的矢量模长
        """
        vec = [pt[0] - bottom_point[0], pt[1] - bottom_point[1]]
        norm_value = math.sqrt(vec[0] * vec[0] + vec[1] * vec[1])
        return vec, norm_value

    def put_in_dict(self, dic, key, value):
        if key not in dic.keys():
            dic[key] = [value]
        elif key in dic.keys():
            dic.get(key).append(value)

    def sort_polar_angle_cos(self, points):
        """
        按照点阵与极小点的极角进行排序,使用的是余弦的方法
        若有共线的点阵则取模长最长的点,删除中间的点
        :param points: 需要排序的点
        :return sorted_pts: 经过删选和排序的点阵
        """
        cos_value = []
        rank = []
        dic = {}
        bottom_point = self.get_bottom_point(points)
        for i in range(len(points)):
            if points[i] != bottom_point:
                rank.append(i)
                # 单点与极小点构成的矢量
                point = self.pt_normal_val(points[i], bottom_point)[0]
                # 单点与极小点构成的矢量的模长
                norm_value = self.pt_normal_val(points[i], bottom_point)[1]
                if norm_value == 0:
                    # 单点与极小点构成的矢量与x轴夹角的余弦值cos_value
                    cos_value.append(1)
                else:
                    cos_value.append(round(point[0] / norm_value, 4))

        # 去重cos_value并建立cos_value为key,点为value的字典
        for j, k in zip(cos_value, rank):
            self.put_in_dict(dic, j, k)

        # 极角与cos_value值成负相关
        sorted_cos = sorted(dic.keys(), reverse=True)
        sorted_pts = []
        for i in range(len(sorted_cos)):
            pt = [points[it] for it in dic[sorted_cos[i]]]
            # 若该极角只对应单个点则直接加入sorted_pts
            if len(pt) == 1:
                tup = pt[0]
                pt_3d = rg.Point3d(tup[0], tup[1], tup[2])
                sorted_pts.append(pt_3d)
            # 若该极角对应多个点则选取模长最长的加入sorted_pts
            elif len(pt) > 1:
                vals = [self.pt_normal_val(j, bottom_point)[1] for j in pt]
                for k in pt:
                    normal_val = self.pt_normal_val(k, bottom_point)[1]
                    if normal_val == max(vals):
                        pt_3d = rg.Point3d(k[0], k[1], k[2])
                        sorted_pts.append(pt_3d)
        return sorted_pts

    def coss_multi(self, v1, v2):
        """
        计算两个向量的向量积
        :param v1:
        :param v2:
        :return: 
        """
        return v1[0] * v2[1] - v1[1] * v2[0]

    def graham_scan(self):
        # 从点阵列表中选取极小点
        bottom_point = self.get_bottom_point(self.pts)
        # 经过删选和排序的点阵
        sorted_points = self.sort_polar_angle_cos(self.pts)

        m = len(sorted_points)
        # 需要三个点以上才能构成凸包
        if m < 2:
            print("点的数量过少，无法构成凸包")
            return

        stack = []
        # 将前三个点放入列表
        stack.append(bottom_point)
        stack.append(sorted_points[0])
        stack.append(sorted_points[1])

        # 遍历点阵,根据向量积正负求解凸包
        for i in range(2, m):
            length = len(stack)
            # 上一次求得的凸包点
            top = stack[length - 1]
            # 上上次求得的凸包点
            next_top = stack[length - 2]
            # v1为以next_top为起点,点阵中当前被遍历的点为终点的向量
            v1 = [sorted_points[i][0] - next_top[0],
                  sorted_points[i][1] - next_top[1]]
            # v2为以top为起点,next_top为终点的向量
            v2 = [top[0] - next_top[0], top[1] - next_top[1]]

            # 根据右手定则,在v1,v2夹角为180°以内时,二者的向量积为负数
            # 当v1,v2的向量积为非负数时,v1,v2夹角范围在180°-360°之间,不满足凸包
            # 弹出上次计算的错误的凸包点,并从上上次求得的凸包点开始重新循环计算
            while self.coss_multi(v1, v2) >= -2 * self.tol and len(stack) > 2:
                # 不带参数的pop()方法默认弹出列表内最后一个元素
                stack.pop()
                length = len(stack)
                top = stack[length - 1]
                next_top = stack[length - 2]
                # 这里的v1,v2与上面生成方法相同,用于计算向量的点变为上上次求得的凸包点(类似递归)
                v1 = [sorted_points[i][0] - next_top[0],
                      sorted_points[i][1] - next_top[1]]
                v2 = [top[0] - next_top[0], top[1] - next_top[1]]
            # 当跳出while循环时说明sorted_points[i]满足凸包,加入凸包列表
            stack.append(sorted_points[i])
        return stack
        
        
def is_two_points_equal(pt1,pt2,tolerance):
    return rg.Line(pt1, pt2).Length <= tolerance
        
def select_first_curves(crvs):
    points = []
    for crv in crvs:
        points.append(crv.PointAtStart)
        points.append(crv.PointAtEnd)
    ipt = points[0]
    for pt in points:
        if pt.Y <= ipt.Y:
            ipt = pt
    curves = []
    for crv in crvs:
        if crv.PointAtStart == ipt or crv.PointAtEnd == ipt:
            curves.append(crv)
    return curves

def select_corner_curve(crvs):
    curves = select_first_curves(crvs)
    points = []
    for crv in curves:
        spt = crv.PointAtStart
        ept = crv.PointAtEnd
        if spt not in points:
            points.append(spt)
        if ept not in points:
            points.append(ept)
    convex = Convex(points).graham_scan()
    for crv in curves:
        spt = crv.PointAtStart
        ept = crv.PointAtEnd
        if spt in convex and ept in convex:
            if crv.PointAtStart.X < crv.PointAtEnd.X:
                crv.Reverse()
            return crv
    
def find_next_connections(brim,crvs):
    brim.Reverse()
    ipt = brim.PointAtStart
    
    connections = []
    for crv in crvs:
        if crv != brim:
            if is_two_points_equal(ipt, crv.PointAtStart, 0.001):
                connections.append(crv)
            if is_two_points_equal(ipt, crv.PointAtEnd, 0.001):
                crv.Reverse()
                connections.append(crv)
    return connections

def find_next_brim(brim, connections):
    ivec = brim.TangentAtStart
    icrv = connections[0]
    for crv in connections:
        if crv != icrv:
            iang = rg.Vector3d.VectorAngle(ivec, icrv.TangentAtStart,rg.Plane.WorldXY)
            ang = rg.Vector3d.VectorAngle(ivec, crv.TangentAtStart,rg.Plane.WorldXY)
            if ang >= iang:
                icrv = crv
    return icrv

def main(crvs):
    ipt = select_corner_curve(crvs).PointAtEnd
    brims = []
    n = 1
    while True:
        if n == 1:
            brim = select_corner_curve(crvs)
            brims.append(brim)
            n += 1
        if n == 1000:
            break
        else:
            connections = find_next_connections(brim, crvs)
            brim = find_next_brim(brim, connections)
            brims.append(brim)
            n += 1
            if brim.PointAtEnd == ipt:
                return brims
          
a = main(x)