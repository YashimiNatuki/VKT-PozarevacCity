using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pozarevac.CityGame
{
    // Local metres, bounded miters and continuous ribbon edges; no invented survey elevations.
    public static class CityRoadGeometry
    {
        public static float Width(string kind)=>kind=="footway"?2:kind=="pedestrian"?5:kind=="primary"||kind=="secondary"?9:6;
        public static bool Valid(CityOsmStreets.Point point,float radius)=>point!=null&&
            !float.IsNaN(point.x)&&!float.IsInfinity(point.x)&&!float.IsNaN(point.z)&&!float.IsInfinity(point.z)&&Mathf.Abs(point.x)<=radius&&Mathf.Abs(point.z)<=radius;
        public static Vector2Int Key(CityOsmStreets.Point p)=>new Vector2Int(Mathf.RoundToInt(p.x*100),Mathf.RoundToInt(p.z*100));
        private static Vector3 At(CityOsmStreets.Point p)=>new Vector3(p.x,.035f,p.z);
        public static Vector3 NodeOffset(Vector3 neighborA,Vector3 neighborB,Vector3 reference)
        {
            Vector3 sum=Vector3.Cross(-neighborA.normalized,Vector3.up)+Vector3.Cross(neighborB.normalized,Vector3.up);
            if(sum.sqrMagnitude<.0001f)return reference;
            Vector3 miter=sum.normalized;float dot=Vector3.Dot(miter,reference);
            if(Mathf.Abs(dot)<.2f)return reference;
            return miter*Mathf.Sign(dot)*Mathf.Min(1.25f,1/Mathf.Abs(dot));
        }
        public static Vector3 Offset(CityOsmStreets.Point[] points,int index,float radius)
        {
            Vector3 before=Vector3.zero,after=Vector3.zero;
            if(index>0&&Valid(points[index-1],radius))before=At(points[index])-At(points[index-1]);
            if(index+1<points.Length&&Valid(points[index+1],radius))after=At(points[index+1])-At(points[index]);
            if(before.sqrMagnitude<.0001f)before=after;if(after.sqrMagnitude<.0001f)after=before;
            before.Normalize();after.Normalize();
            Vector3 n1=Vector3.Cross(before,Vector3.up),n2=Vector3.Cross(after,Vector3.up),miter=(n1+n2).normalized;
            float denominator=Vector3.Dot(miter,n2);
            if(denominator<.2f)return n2;
            return miter*Mathf.Min(1.25f,1/denominator);
        }
        public static void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,List<Vector3> vertices,List<int> triangles)
        {
            int at=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);
            Up(at,at+1,at+2,vertices,triangles);Up(at+1,at+3,at+2,vertices,triangles);
        }
        private static void Up(int a,int b,int c,List<Vector3> vertices,List<int> triangles)
        {
            float area=Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]).y;
            if(Mathf.Abs(area)<.00001f)return;
            triangles.Add(a);triangles.Add(area>0?b:c);triangles.Add(area>0?c:b);
        }
        public static void Junction(Vector3 center,float radius,List<Vector3> vertices,List<int> triangles)
        {
            if(!float.IsFinite(radius)||radius<=0||radius>8)throw new ArgumentOutOfRangeException(nameof(radius));
            int first=vertices.Count;center.y=.038f;vertices.Add(center);
            const int segments=24;
            for(int i=0;i<segments;i++){float t=i*2*Mathf.PI/segments;vertices.Add(center+new Vector3(Mathf.Cos(t)*radius,0,Mathf.Sin(t)*radius));}
            for(int i=0;i<segments;i++)Up(first,first+1+i,first+1+(i+1)%segments,vertices,triangles);
        }
    }
}
