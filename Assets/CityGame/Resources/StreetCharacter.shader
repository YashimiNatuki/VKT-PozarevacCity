Shader "VKT/StreetCharacter"
{
 Properties
 {
  _BaseMap("Character atlas",2D)="white"{}
  _BaseColor("Base",Color)=(1,1,1,1)
  _Shirt("Shirt",Color)=(.07,.28,.31,1)
  _Pants("Pants",Color)=(.12,.14,.17,1)
  _Shoes("Shoes",Color)=(.85,.88,.9,1)
  _Accent("Print",Color)=(.7,.85,.8,1)
  _Cull("Cull",Float)=2
  _Cutoff("Cutoff",Float)=.5
  _PreviewFill("Studio fill",Range(0,1))=0
  _Face("Blink, effort, smile, gaze",Vector)=(0,0,0,0)
 }
 SubShader
 {
  Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  Pass
  {
   Name "Forward"
   Tags{"LightMode"="UniversalForward"}
   Cull [_Cull]
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST, _BaseColor, _Shirt, _Pants, _Shoes, _Accent, _Face;
    float _PreviewFill;
   CBUFFER_END
   struct Input{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
   struct Output{float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float2 uv:TEXCOORD1;float3 positionWS:TEXCOORD2;float fog:TEXCOORD3;};
   Output Vert(Input v)
   {
    Output o;VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);
    o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.normalWS=TransformObjectToWorldNormal(v.normalOS);
    o.uv=TRANSFORM_TEX(v.uv,_BaseMap);o.fog=ComputeFogFactor(p.positionCS.z);return o;
   }
   half4 Frag(Output i):SV_Target
   {
    float2 faceUv=i.uv;
    // Restrict all motion to the reviewed face islands; clothing and ears retain their UVs.
    float eyeX=faceUv.x<.315?.274:.354;
    float2 eye=float2((faceUv.x-eyeX)/.017,(faceUv.y-.785)/.012);
    float eyeMask=1-smoothstep(.65,1.15,length(eye));
    float browMask=(1-smoothstep(.009,.018,abs(faceUv.x-eyeX)))*(1-smoothstep(.004,.010,abs(faceUv.y-.806)));
    faceUv.x-=_Face.w*.003*eyeMask;
    faceUv.y+=_Face.y*.003*browMask;
    float mouthMask=(1-smoothstep(.016,.030,abs(faceUv.x-.314)))*(1-smoothstep(.006,.016,abs(faceUv.y-.733)));
    faceUv.y-=mouthMask*_Face.z*.004*abs((faceUv.x-.314)/.025);
    half3 source=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,faceUv).rgb;
    half3 lid=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,float2(eyeX,.815)).rgb;
    float closure=saturate(_Face.x)*eyeMask;
    source=lerp(source,lid,closure);
    float lash=(1-smoothstep(.08,.22,abs(eye.y+.08)))*(1-smoothstep(.65,1,abs(eye.x)));
    source=lerp(source,lid*.45,lash*closure*.65);
    float shirt=step(i.uv.x,.59)*step(i.uv.y,.515);
    float pants=step(.59,i.uv.x)*step(i.uv.y,.255);
    float shoes=step(.63,i.uv.x)*step(i.uv.x,.81)*step(.50,i.uv.y)*step(i.uv.y,.87);
    half luminance=dot(source,half3(.299,.587,.114));
    half3 color=lerp(source,_Shirt.rgb*(.3+luminance*.9),shirt);
    color=lerp(color,_Pants.rgb*(.45+luminance),pants);
    color=lerp(color,_Shoes.rgb*(.22+luminance*.9),shoes);
    float fabric=.985+.015*sin(i.uv.x*1608)*sin(i.uv.y*1608);
    color*=lerp(1,fabric,saturate(shirt+pants));
    float stripe=step(.18,i.uv.x)*step(i.uv.x,.40)*step(.32,i.uv.y)*step(i.uv.y,.37);
    color=lerp(color,_Accent.rgb,stripe*step(.52,frac(i.uv.x*42))*shirt*.85);
    half3 n=normalize(i.normalWS);Light main=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
    half3 lighting=SampleSH(n)+main.color*saturate(dot(n,main.direction))*main.shadowAttenuation*main.distanceAttenuation;
    color*=max(lighting,half3(.12,.12,.12))+_PreviewFill;return half4(MixFog(color,i.fog),1);
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
  UsePass "Universal Render Pipeline/Lit/DepthNormals"
 }
}
