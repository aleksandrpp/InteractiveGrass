Shader "AK.InteractiveGrass/Blades"
{
    Properties
    {
        _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
        _WindDistortionMap("Wind Distortion Map", 2D) = "white" {}
        _WindFrequency("Wind Frequency", Vector) = (0.05, 0.05, 0, 0)
        _WindStrength("Wind Strength", Float) = 0.5
    }
    SubShader
    {
        Tags { "LightMode" = "ForwardBase" }
        LOD 100
        Cull Off
        ZWrite On
        ZTest LEqual

        Pass
        {
            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #define SHADOWS_SCREEN

            #include "UnityCG.cginc"
            #include "AutoLight.cginc" 

            #include "/AngleAxis3x3.cginc"
            #include "/Noise1.cginc"

            struct Blade
            {
                float3 position;
                float2 size;
                float bend;
                float bendVelocity;
                float3 direction;
                float color;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0; 
                UNITY_SHADOW_COORDS(2)
                UNITY_FOG_COORDS(1)
                float4 pos : POSITION;
                fixed4 color : COL;
            }; 

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed _Cutoff;

            sampler2D _WindDistortionMap;
            float4 _WindDistortionMap_ST;
            float2 _WindFrequency; 
            float _WindStrength;

            uniform RWStructuredBuffer<Blade> _BladeBuffer : register(u1);
            int _VertexCount;
            sampler2D _ShadowTexture;

            float _SpringForce;
            float _SpringDamping;
            float _BendForce;

            float3 _InputPosition;
            float3 _InputDirection;

            v2f vert (uint id : SV_VertexID)
            {
                uint objectId = id / _VertexCount;
                uint vertexId = id % _VertexCount;

                // quad shape
                float3 vertex = float3((vertexId >= 2 && vertexId <= 4) ? 1 : -1, (vertexId >= 1 && vertexId <= 3) ? 1 : 0, 0);

                v2f o;
                o.uv = vertex.xy;

                Blade blade = _BladeBuffer[objectId];

                vertex.x *= blade.size.x;
                vertex.y *= blade.size.y;

                // random rotate based on blade position
                vertex = mul(AngleAxis3x3(noise1(blade.position) * UNITY_TWO_PI, float3(0, 1, 0)), vertex);

                // bend
                float pointerDistanceMask = smoothstep(1.2, 0, distance(blade.position, _InputPosition));
                float pointerVelocityMask = smoothstep(0, .5, length(_InputDirection.xz));

                blade.bendVelocity -= (_SpringForce * blade.bend + _SpringDamping * blade.bendVelocity) * unity_DeltaTime;
                blade.bend += blade.bendVelocity * unity_DeltaTime;
                blade.bend += _BendForce * pointerDistanceMask * pointerVelocityMask * unity_DeltaTime;
                blade.bend = clamp(blade.bend, -1, 1);
                blade.direction = lerp(
                    blade.direction, 
                    lerp(blade.direction, normalize(_InputDirection), pointerDistanceMask), 
                    (1 - clamp(blade.bend, 0, 1)) * pointerVelocityMask * unity_DeltaTime * 50);

                vertex = mul(AngleAxis3x3(blade.bend * UNITY_HALF_PI, cross(float3(0, 1, 0), blade.direction)), vertex);

                // wind
                float2 windUV = blade.position.xz * _WindDistortionMap_ST.xy + _WindDistortionMap_ST.zw + _WindFrequency * _Time.y;
                float2 windSample = (tex2Dlod(_WindDistortionMap, float4(windUV, 0, 0)).xy * 2 - 1) * _WindStrength;
                float3 windDirection = -normalize(float3(windSample.x, 0, windSample.y));
                vertex = mul(AngleAxis3x3(UNITY_PI * windSample, cross(float3(0,1,0),windDirection)), vertex);

                // position
                vertex += blade.position;

                // color
                o.color = fixed4(blade.color, blade.color, blade.color, 1);

                o.pos = UnityApplyLinearShadowBias(
                    UnityObjectToClipPos(float4(vertex,1)));

                UNITY_TRANSFER_SHADOW(o, o.uv)
                UNITY_TRANSFER_FOG(o, o.pos);

                _BladeBuffer[objectId] = blade;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            { 
                // sample shadowmap
                float3 shadowCoord = i._ShadowCoord.xyz / i._ShadowCoord.w;
                float shadowmap = tex2D(_ShadowTexture, shadowCoord.xy).b;

                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                clip(col.a - _Cutoff);

                col *= i.color * shadowmap;

                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
