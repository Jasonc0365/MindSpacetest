Shader "Custom/TableSurfaceHighlight"
{
    Properties
    {
        _Color ("Color", Color) = (0, 1, 0, 0.3)
        _GlowIntensity ("Glow Intensity", Range(0, 2)) = 1.0
        _PulseSpeed ("Pulse Speed", Range(0.5, 5)) = 1.0
        _PulseAmplitude ("Pulse Amplitude", Range(0, 0.5)) = 0.1
        _EnablePulse ("Enable Pulse", Float) = 0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "IgnoreProjector"="True"
        }
        
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off  // Render both sides so it's visible from above and below
        
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
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float3 normal : TEXCOORD2;
            };
            
            fixed4 _Color;
            float _GlowIntensity;
            float _PulseSpeed;
            float _PulseAmplitude;
            float _EnablePulse;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Base color
                fixed4 color = _Color;
                
                // Add pulse animation if enabled
                if (_EnablePulse > 0.5)
                {
                    float pulse = sin(_Time.y * _PulseSpeed) * _PulseAmplitude;
                    color.a = saturate(color.a + pulse);
                }
                
                // Add glow effect based on distance from center
                float2 center = float2(0.5, 0.5);
                float2 uvFromCenter = i.uv - center;
                float distFromCenter = length(uvFromCenter);
                
                // Glow effect (brighter at edges, but clamp to avoid over-bright)
                float glow = 1.0 + saturate(1.0 - distFromCenter * 2.0) * _GlowIntensity * 0.3;
                color.rgb = saturate(color.rgb * glow);
                
                return color;
            }
            ENDCG
        }
    }
    
    FallBack "Transparent/Diffuse"
}

