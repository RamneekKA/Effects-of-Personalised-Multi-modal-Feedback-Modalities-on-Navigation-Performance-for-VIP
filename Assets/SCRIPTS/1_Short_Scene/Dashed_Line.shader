Shader "Custom/DashedLine"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _DashSize ("Dash Size", Float) = 5.0
        _GapSize ("Gap Size", Float) = 5.0
        _Speed ("Animation Speed", Float) = 0.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float distance : TEXCOORD1;
            };
            
            fixed4 _Color;
            float _DashSize;
            float _GapSize;
            float _Speed;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                
                // Calculate distance along the line for dashing
                o.distance = v.uv.x;
                
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate dash pattern
                float totalSize = _DashSize + _GapSize;
                float animatedDistance = i.distance * 50.0 + _Time.y * _Speed;
                float dashPhase = fmod(animatedDistance, totalSize);
                
                // Create dash pattern
                float alpha = step(dashPhase, _DashSize);
                
                fixed4 col = _Color;
                col.a *= alpha;
                
                return col;
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}