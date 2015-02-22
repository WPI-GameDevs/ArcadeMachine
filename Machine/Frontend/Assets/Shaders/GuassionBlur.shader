Shader "Custom/Guassion"
{
    Properties
    {
        _MainTex ("Diffuse Texture", 2D) = "white" {}
		_BlurTex ("Blur Texture", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            Tags
			{ 
				"Queue"="Transparent" 
				"IgnoreProjector"="True" 
				"RenderType"="Transparent" 
				"PreviewType"="Plane"
				"CanUseSpriteAtlas"="True"
			}
 
			Cull Off
			Lighting Off
			ZWrite Off
			Fog { Mode Off }
			Blend One OneMinusSrcAlpha
 
            CGPROGRAM
 
            #pragma vertex vert
            #pragma fragment frag
 
            #include "UnityCG.cginc"
 
            // User-specified properties
            uniform sampler2D _MainTex;
			uniform sampler2D _BlurTex;
 
            struct VertexInput
            {
                float4 vertex : POSITION;
                float4 uv : TEXCOORD0;
				float4 color : COLOR;
            };
 
            struct VertexOutput
            {
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
				float4 color : COLOR;
            };
 
            VertexOutput vert(VertexInput input) 
            {
                VertexOutput output;
                output.pos = mul(UNITY_MATRIX_MVP, input.vertex);
                output.uv = input.uv;
				output.color = input.color;
                return output;
            }
 
            float4 frag(VertexOutput input) : COLOR
            {
                float4 diffuseColor = tex2D(_MainTex, input.uv);
				float4 blurVector = tex2D(_BlurTex, input.uv);

				float blurAmount = abs(1 - dot(blurVector, input.color));
				float offset = blurAmount * 0.0125;

				float4 left = tex2D(_MainTex, input.uv + float2(-offset, 0));
				float4 right = tex2D(_MainTex, input.uv + float2(offset, 0));
				float4 top = tex2D(_MainTex, input.uv + float2(0, -offset));
				float4 bottom = tex2D(_MainTex, input.uv + float2(0, offset));

				diffuseColor = (left + right + top + bottom + diffuseColor) * 0.2;

                return diffuseColor;
            }
 
            ENDCG
        }
    }
}