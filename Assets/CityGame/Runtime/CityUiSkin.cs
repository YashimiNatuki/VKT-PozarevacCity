using UnityEngine;
using UnityEngine.UI;

namespace Pozarevac.CityGame
{
    // Each UI root owns a single nine-slice sprite; no static asset lifetime or per-frame baking.
    public sealed class CityUiSkin : MonoBehaviour
    {
        private Texture2D texture;private Sprite sprite;
        public Sprite Rounded
        {
            get
            {
                if(sprite!=null)return sprite;
                const int size=64;var pixels=new Color32[size*size];
                for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                {
                    Vector2 p=new Vector2(x-31.5f,y-31.5f),q=new Vector2(Mathf.Abs(p.x)-19.5f,Mathf.Abs(p.y)-19.5f);
                    float distance=new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-12;
                    pixels[y*size+x]=new Color32(255,255,255,(byte)(Mathf.Clamp01(.5f-distance)*255));
                }
                texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="VKT owned rounded UI",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};texture.SetPixels32(pixels);texture.Apply(false,true);
                sprite=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(14,14,14,14));return sprite;
            }
        }
        public void ApplyTree()
        {
            foreach(Image image in GetComponentsInChildren<Image>(true))
            {
                if(image.color.a<.01f||image.name.Contains("Backdrop"))continue;
                image.sprite=Rounded;image.type=Image.Type.Sliced;
                Button button=image.GetComponent<Button>();
                if(button!=null){ColorBlock colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.7f,.92f,.93f);colors.pressedColor=new Color(.4f,.75f,.78f);colors.fadeDuration=.12f;button.colors=colors;}
            }
        }
        private void OnDestroy(){Release(sprite);Release(texture);}
        private static void Release(Object value){if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
    }
}
