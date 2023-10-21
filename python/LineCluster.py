
import Rhino.Geometry as rg
import Rhino.RhinoDoc as doc


class DetermineClusters():
    def __init__(self,crvs):
        self.crvs=crvs
        self.clusterList=[]

    @staticmethod
    def is_two_lines_intersect(line1,line2):
        #检查两条线是否相交
        tol = doc.ActiveDoc.ModelAbsoluteTolerance
        events=rg.Intersect.Intersection.CurveCurve(line1,line2,tol,0.0)
        if events:
            return True

    @staticmethod
    def del_dup_lines(cluster,lineList):
        #在线段列表中删除已经加入至簇的线
        for line in cluster:
            if line in lineList:
                lineList.remove(line)
        return lineList

    def first_sort_cluster(self,lines):
        #对线列表进行初始筛选，将与其中一条线相交的线同这条线一起放入一个簇
        cluster=[]
        cluster.append(lines[0])
        lines.pop(0)
        for i in range(len(lines)):
            if self.is_two_lines_intersect(cluster[0],lines[i]):
                cluster.append(lines[i])
        return cluster

    def sort_one_cluster(self,lines):
        #选出一个线段簇
        cluster=self.first_sort_cluster(lines)
        left_lines=self.del_dup_lines(cluster,lines)
        while True:
            temp_cluster=[]
            for left_line in left_lines:
                for line in cluster:
                    if self.is_two_lines_intersect(line,left_line):
                        temp_cluster.append(left_line)
                        break
            if len(temp_cluster)!=0:
                cluster.extend(temp_cluster)
                left_lines=self.del_dup_lines(cluster,left_lines)
            else:
                self.clusterList.append(cluster)
                break
        return left_lines
        
    def sort_all_cluster(self):
        #筛选出所有线段簇
        left_lines=self.crvs
        while True:
            left_lines=self.sort_one_cluster(left_lines)
            if len(left_lines)==0:
                break


class DelOverlapLines():
    def __init__(self,crvs):
        self.crvs=crvs
        self.exploded_lines=[]
        self.checked_lines=[]

    @staticmethod
    def is_two_lines_overlap(crv1,crv2):
        #检查两条线是否重叠
        tol = doc.ActiveDoc.ModelAbsoluteTolerance
        crv11=crv1.ToNurbsCurve()
        crv22=crv2.ToNurbsCurve()
        event=rg.Intersect.Intersection.CurveCurve(crv1,crv22,tol,tol)
        if event:
            return event[0].IsOverlap

    def merge_curves(self,short_line,long_line):
        #去除重合线，并组合曲线
        tol = doc.ActiveDoc.ModelAbsoluteTolerance
        sPt=short_line.PointAtStart
        ePt=short_line.PointAtEnd
        t1=long_line.ClosestPoint(sPt)[1]
        crvPt1=long_line.PointAt(t1)
        t2=long_line.ClosestPoint(ePt)[1]
        crvPt2=long_line.PointAt(t2)
        if crvPt1.DistanceTo(sPt) <tol and crvPt2.DistanceTo(ePt)<tol:
            return long_line
        else:
            lines=[]
            if crvPt1.DistanceTo(sPt) <tol:
                subLine=long_line.Split(t1)[0]
                if not self.is_two_lines_overlap(subLine,short_line):
                    lines.append(subLine)
                    lines.append(short_line)
                    return rg.Curve.JoinCurves(lines,tol)[0]
                else:
                    lines.append(long_line.Split(t1)[1])
                    lines.append(short_line)
                    return rg.Curve.JoinCurves(lines,tol)[0]
            else:
                subLine=long_line.Split(t2)[0]
                if not self.is_two_lines_overlap(subLine,short_line):
                    lines.append(subLine)
                    lines.append(short_line)
                    return rg.Curve.JoinCurves(lines,tol)[0]
                else:
                    lines.append(long_line.Split(t2)[1])
                    lines.append(short_line)
                    return rg.Curve.JoinCurves(lines,tol)[0]

    def merge_two_overlap_curve(self,line1,line2):
        #将两条重合线合并为一条线，并返回这条线
        tol = doc.ActiveDoc.ModelAbsoluteTolerance
        if line1.GetLength() > line2.GetLength():
            return self.merge_curves(line2,line1)
        elif line1.GetLength() == line2.GetLength():
            return self.merge_curves(line1,line2)
        else :
            return self.merge_curves(line1,line2)


    @staticmethod
    def explode_polyCrv(line):
        #接受一个值，返回一个列表
        sPt=line.PointAtStart
        ePt=line.PointAtEnd
        t_start=line.ClosestPoint(sPt)[1]
        t_end=line.ClosestPoint(ePt)[1]
        if line.GetNextDiscontinuity(rg.Continuity.C1_continuous,t_start,t_end)[0]:
        #检查是多段线还是一条线
            lines=[]
            while line.GetNextDiscontinuity(rg.Continuity.C1_continuous,t_start,t_end)[0]:
                t0=line.GetNextDiscontinuity(rg.Continuity.C1_continuous,t_start,t_end)[1]
                new_line=line.Split(t0)[0]
                line=line.Split(t0)[1]
                lines.append(new_line)
                t_start=t0
            lines.append(line)
            return lines
        else:
            lines=[]
            lines.append(line)
            return lines
        
    def explode_crvs(self,lines):
        #接收线列表，炸开一组线，返回一个列表
        if len(lines)>1:
            for line in lines:
                newLine_list=self.explode_polyCrv(line)
                for i in newLine_list:
                    self.exploded_lines.append(i)
        else:
            newLine_list=self.explode_polyCrv(lines[0])
            for i in newLine_list:
                self.exploded_lines.append(i)

        
    def clean_crvs(self,crvs):
        if len(crvs)==1:
            self.checked_lines.append(crvs[0])
        else:
            newCrvs=crvs
            try:
                for x in range(len(newCrvs)):
                    crv=newCrvs[x]
                    newCrvs.remove(crv)
                    for y in newCrvs:
                        if self.is_two_lines_overlap(crv,y):
                            newCrv=self.merge_two_overlap_curve(crv,y)
                            newCrvs.remove(y)
                            newCrvs.append(newCrv)
                            return self.clean_crvs(newCrvs)
                    self.checked_lines.append(crv)
            except IndexError:
                #二分法
                self.clean_crvs(newCrvs)
                
if __name__ =="__main__":
    dol=DelOverlapLines(crvs)
    dol.explode_crvs(crvs)
    exploded=dol.exploded_lines
    dol.clean_crvs(exploded)
    a=dol.checked_lines
    dc=DetermineClusters(a)
    dc.sort_all_cluster()
    count=dc.clusterList
    try:
        cluster=dc.clusterList[i]
    except IndexError as e:
        print(e)



