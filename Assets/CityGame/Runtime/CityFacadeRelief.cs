using System.Collections.Generic;
using UnityEngine;

namespace Pozarevac.CityGame
{
    // Original decorative trim, not photographed/surveyed landmark architecture.
    public static class CityFacadeRelief
    {
        public static Mesh Create(CityOsmStreets.Building building,int entrance)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();float height=Mathf.Clamp(building.height,3,40);
            for(int i=1;i<building.points.Length;i++)
            {
                Vector3 a=new Vector3(building.points[i-1].x,height-.18f,building.points[i-1].z);
                Vector3 b=new Vector3(building.points[i].x,height-.18f,building.points[i].z);
                if((b-a).sqrMagnitude<.01f)continue;
                Tube(a,b,.16f,vertices,triangles);
                if(i!=entrance)continue;
                Vector3 tangent=(b-a).normalized,normal=Vector3.Cross(tangent,Vector3.up)*.08f;
                Vector3 center=(a+b)*.5f;center.y=2.5f;
                for(int arc=0;arc<16;arc++)
                {
                    float from=arc*Mathf.PI/16,to=(arc+1)*Mathf.PI/16;
                    Vector3 start=center+tangent*(Mathf.Cos(from)*1.28f)+Vector3.up*(Mathf.Sin(from)*.58f)+normal;
                    Vector3 end=center+tangent*(Mathf.Cos(to)*1.28f)+Vector3.up*(Mathf.Sin(to)*.58f)+normal;
                    Tube(start,end,.065f,vertices,triangles);
                }
                for(int side=-1;side<=1;side+=2)
                    Tube(center+tangent*(side*1.28f)+normal,center+tangent*(side*1.28f)+normal-Vector3.up*2.4f,.065f,vertices,triangles);
            }
            var mesh=new Mesh{name="VKT_OriginalCorniceAndArchedDoorTrim"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private static void Tube(Vector3 from,Vector3 to,float radius,List<Vector3> vertices,List<int> triangles)
        {
            Vector3 axis=(to-from).normalized;
            Vector3 side=Vector3.Cross(axis,Mathf.Abs(axis.y)<.9f?Vector3.up:Vector3.right).normalized,up=Vector3.Cross(side,axis);
            int first=vertices.Count;
            for(int end=0;end<2;end++)for(int ring=0;ring<8;ring++)
            {
                float angle=ring*Mathf.PI*.25f;vertices.Add((end==0?from:to)+radius*(side*Mathf.Cos(angle)+up*Mathf.Sin(angle)));
            }
            for(int ring=0;ring<8;ring++)
            {
                int a=first+ring,b=first+(ring+1)%8,c=a+8,d=b+8;
                triangles.Add(a);triangles.Add(c);triangles.Add(b);triangles.Add(b);triangles.Add(c);triangles.Add(d);
            }
        }
    }
}
