Shader "Custom/VertexAnimationShader"
{
    Properties
    {
        _MainTex("Texture",2D)="white"{}
        _PosTex("Position Texture",2D)="black"{}
        _NmlTex("Normal Texture",2D)="white"{}
        _Amount("Clip amount",float)=1
        _Length("Clip length",float)=1
        _Offset("Offset time",float)=0
    }
    SubShader
    {
        Tags{"RenderType"="Opaque"}
        LOD 100
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            //开启gpu instancing
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "Lighting.cginc" // 包含Unity的光照函数
            #define TS _PosTex_TexelSize

            //输入网格数据
            struct appdata
            {
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID // 添加实例ID输入
            };

            //顶点到片元结构体
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex, _PosTex, _NmlTex;
            float4 _PosTex_TexelSize;
            int _Index;

            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float, _Amount)
            UNITY_DEFINE_INSTANCED_PROP(float, _Offset)
            UNITY_DEFINE_INSTANCED_PROP(float, _Length)
            UNITY_INSTANCING_BUFFER_END(Props)


            //几何阶段（构建顶点到片元结构体）
            v2f vert(appdata v, uint vid : SV_VertexID)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v); // 设置实例ID
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                float amount = UNITY_ACCESS_INSTANCED_PROP(Props, _Amount);
                float myoffset = UNITY_ACCESS_INSTANCED_PROP(Props, _Offset);
                float length = UNITY_ACCESS_INSTANCED_PROP(Props, _Length);

                //float t = (_Time.y % 1) * length + myoffset;
                //float t = ((_Time.y) % length) / length * amount  + myoffset;
                float t = fmod(_Time.y, length) / length * amount  + myoffset;
                float x = (vid + 0.5) / TS.z;
                
                float3 pos = tex2Dlod(_PosTex, float4(x, t, 0, 0));
                float3 normal = tex2Dlod(_NmlTex, float4(x, t, 0, 0));

                o.vertex = UnityObjectToClipPos(pos);
                o.normal = UnityObjectToWorldNormal(normal);
                o.uv = v.uv;
                return o;
            }

            //光栅化阶段
            fixed4  frag(v2f i):SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
        
    }
    FallBack "Diffuse"
}