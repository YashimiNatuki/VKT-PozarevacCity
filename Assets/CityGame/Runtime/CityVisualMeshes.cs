using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pozarevac.CityGame
{
    public static class CityVisualMeshes
    {
        public static Mesh RoundedBox(float radius=.12f,int subdivisions=6)
        {
            if(float.IsNaN(radius)||radius<=0||radius>=.5f||subdivisions<2||subdivisions>16)throw new ArgumentOutOfRangeException();
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
            Vector3[] directions={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
            foreach(Vector3 face in directions)
            {
                Vector3 u=Mathf.Abs(face.y)>.5f?Vector3.right:Vector3.Cross(Vector3.up,face),v=Vector3.Cross(face,u);
                int start=vertices.Count;
                for(int y=0;y<=subdivisions;y++)for(int x=0;x<=subdivisions;x++)
                {
                    Vector3 p=face*.5f+u*((float)x/subdivisions-.5f)+v*((float)y/subdivisions-.5f);
                    float inner=.5f-radius;Vector3 core=new Vector3(Mathf.Clamp(p.x,-inner,inner),Mathf.Clamp(p.y,-inner,inner),Mathf.Clamp(p.z,-inner,inner));
                    Vector3 normal=(p-core).normalized;vertices.Add(core+normal*radius);normals.Add(normal);uv.Add(new Vector2((float)x/subdivisions,(float)y/subdivisions));
                }
                for(int y=0;y<subdivisions;y++)for(int x=0;x<subdivisions;x++)
                {int a=start+y*(subdivisions+1)+x,b=a+subdivisions+1;indices.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
            }
            Mesh mesh=new Mesh{name="VKT shared rounded box"};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();return mesh;
        }
        public static Mesh Lathe(Vector2[] profile,int segments=32)
        {
            if(profile==null||profile.Length<2||segments<8||segments>64)throw new ArgumentOutOfRangeException();
            foreach(Vector2 point in profile)if(float.IsNaN(point.x)||float.IsInfinity(point.x)||float.IsNaN(point.y)||float.IsInfinity(point.y)||point.x<0)throw new ArgumentOutOfRangeException(nameof(profile));
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
            for(int row=0;row<profile.Length;row++)for(int ring=0;ring<=segments;ring++)
            {
                float angle=ring*Mathf.PI*2/segments,c=Mathf.Cos(angle),s=Mathf.Sin(angle);
                Vector2 tangent=profile[Mathf.Min(row+1,profile.Length-1)]-profile[Mathf.Max(0,row-1)];
                vertices.Add(new Vector3(c*profile[row].x,profile[row].y,s*profile[row].x));
                normals.Add(new Vector3(c*tangent.y,-tangent.x,s*tangent.y).normalized);uv.Add(new Vector2((float)ring/segments,(float)row/(profile.Length-1)));
                if(row<profile.Length-1&&ring<segments){int a=row*(segments+1)+ring,b=a+segments+1;indices.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
            }
            Mesh mesh=new Mesh{name="VKT revolved ceramic planter"};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();return mesh;
        }
        public static Texture2D Surface(int kind)
        {
            const int size=256;var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                uint hash=unchecked((uint)(x*73856093)^(uint)(y*19349663));hash^=hash>>13;hash*=1274126177u;
                float grain=(hash&255)/255f,cloud=Mathf.PerlinNoise(x*.045f,y*.045f);
                Color color;
                if(kind==0){float value=.115f+grain*.055f+cloud*.035f;color=new Color(value*.94f,value,value*1.04f);}
                else if(kind==1){bool seam=(x%64<2||y%32<2);float value=seam?.23f:.46f+grain*.08f+cloud*.09f;color=new Color(value,value*.97f,value*.89f);}
                else{float value=.16f+cloud*.13f+grain*.055f;color=new Color(value*.67f,value,value*.42f);}
                pixels[y*size+x]=color;
            }
            Texture2D texture=new Texture2D(size,size,TextureFormat.RGBA32,true){name="VKT authored procedural surface "+kind,wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};texture.SetPixels32(pixels);texture.Apply(true,true);return texture;
        }
    }
}
