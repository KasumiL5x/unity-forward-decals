// Blend mode algorithms taken from:
// https://www.shadertoy.com/view/XdS3RW

Shader "Forward Decal/Forward Decal" {
  Properties {
    _MainTex ("Texture", 2D) = "white" {}
    [DecalBlendMode] _Mode("Blend Mode", float) = 0.0
  }

  SubShader {
    Pass {
      Fog { Mode Off }
      ZWrite Off
      Blend SrcAlpha OneMinusSrcAlpha

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma target 3.0
      // All variations of the blending modes used (including none).
      #pragma shader_feature _ FD_ADD FD_DARKEN FD_MULTIPLY FD_COLORBURN FD_LINEARBURN FD_DARKERCOLOR FD_LIGHTEN FD_SCREEN FD_COLORDODGE FD_LINEARDODGE FD_LIGHTERCOLOR FD_OVERLAY FD_SOFTLIGHT FD_HARDLIGHT FD_VIVIDLIGHT FD_LINEARLIGHT FD_PINLIGHT FD_HARDMIX FD_DIFFERENCE FD_EXCLUSION FD_SUBTRACT FD_DIVIDE FD_HUE FD_COLOR FD_SATURATION FD_LUMINOSITY

      #include "UnityCG.cginc"

      // BEGIN BLENDING FUNCTIONS
      fixed3 Darken(fixed3 s, fixed3 d) {
        return min(s, d);
      }
      fixed3 Multiply(fixed3 s, fixed3 d) {
        return s * d;
      }
      fixed3 ColorBurn(fixed3 s, fixed3 d) {
        return 1.0 - (1.0 - d) / s;
      }
      fixed3 LinearBurn(fixed3 s, fixed3 d) {
        return s + d - 1.0;
      }
      fixed3 DarkerColor(fixed3 s, fixed3 d) {
        return (s.x + s.y + s.z < d.x + d.y + d.z) ? s : d;
      }
      //
      fixed3 Lighten(fixed3 s, fixed3 d) {
        return max(s, d);
      }
      fixed3 Screen(fixed3 s, fixed3 d) {
        return s + d - s * d;
      }
      fixed3 ColorDodge(fixed3 s, fixed3 d) {
        return d / (1.0 - s);
      }
      fixed3 LinearDodge(fixed3 s, fixed3 d) {
        return s + d;
      }
      fixed3 LighterColor(fixed3 s, fixed3 d) {
        return (s.x + s.y + s.z > d.x + d.y + d.z) ? s : d;
      }
      //
      float _overlay(float s, float d) {
        return (d < 0.5) ? 2.0 * s * d : 1.0 - 2.0 * (1.0 - s) * (1.0 - d);
      }
      fixed3 Overlay(fixed3 s, fixed3 d) {
        fixed3 c;
        c.x = _overlay(s.x, d.x);
        c.y = _overlay(s.y, d.y);
        c.z = _overlay(s.z, d.z);
        return c;
      }
      float _softLight(float s, float d) {
        return (s < 0.5) ? d - (1.0 - 2.0 * s) * d * (1.0 - d) 
          : (d < 0.25) ? d + (2.0 * s - 1.0) * d * ((16.0 * d - 12.0) * d + 3.0) 
            : d + (2.0 * s - 1.0) * (sqrt(d) - d);
      }
      fixed3 SoftLight(fixed3 s, fixed3 d) {
        fixed3 c;
        c.x = _softLight(s.x, d.x);
        c.y = _softLight(s.y, d.y);
        c.z = _softLight(s.z, d.z);
        return c;
      }
      float _hardLight(float s, float d) {
        return (s < 0.5) ? 2.0 * s * d : 1.0 - 2.0 * (1.0 - s) * (1.0 - d);
      }
      fixed3 HardLight(fixed3 s, fixed3 d) {
        fixed3 c;
        c.x = _hardLight(s.x, d.x);
        c.y = _hardLight(s.y, d.y);
        c.z = _hardLight(s.z, d.z);
        return c;
      }
      float _vividLight(float s, float d) {
        return (s < 0.5) ? 1.0 - (1.0 - d) / (2.0 * s) : d / (2.0 * (1.0 - s));
      }
      fixed3 VividLight(fixed3 s, fixed3 d) {
        fixed3 c;
        c.x = _vividLight(s.x, d.x);
        c.y = _vividLight(s.y, d.y);
        c.z = _vividLight(s.z, d.z);
        return c;
      }
      fixed3 LinearLight(fixed3 s, fixed3 d) {
        return 2.0 * s + d - 1.0;
      }
      float _pinLight(float s, float d) {
        return (2.0 * s - 1.0 > d) ? 2.0 * s - 1.0 : (s < 0.5 * d) ? 2.0 * s : d;
      }
      fixed3 PinLight(fixed3 s, fixed3 d) {
        fixed3 c;
        c.x = _pinLight(s.x, d.x);
        c.y = _pinLight(s.y, d.y);
        c.z = _pinLight(s.z, d.z);
        return c;
      }
      fixed3 HardMix(fixed3 s, fixed3 d) {
        return floor(s + d);
      }
      //
      fixed3 Difference(fixed3 s, fixed3 d) {
        return abs(d - s);
      }
      fixed3 Exclusion(fixed3 s, fixed3 d) {
        return s + d - 2.0 * s * d;
      }
      fixed3 Subtract(fixed3 s, fixed3 d) {
        return s - d;
      }
      fixed3 Divide(fixed3 s, fixed3 d) {
        return s / d;
      }
      //
      fixed3 _rgb2hsv(fixed3 c) {
        fixed4 K = fixed4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
        fixed4 p = lerp(fixed4(c.bg, K.wz), fixed4(c.gb, K.xy), step(c.b, c.g));
        fixed4 q = lerp(fixed4(p.xyw, c.r), fixed4(c.r, p.yzx), step(p.x, c.r));

        float d = q.x - min(q.w, q.y);
        float e = 1.0e-10;
        return fixed3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
      }
      fixed3 _hsv2rgb(fixed3 c) {
        fixed4 K = fixed4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
        fixed3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
        return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
      }
      fixed3 Hue(fixed3 s, fixed3 d) {
        d = _rgb2hsv(d);
        d.x = _rgb2hsv(s).x;
        return _hsv2rgb(d);
      }
      fixed3 Color(fixed3 s, fixed3 d) {
        s = _rgb2hsv(s);
        s.z = _rgb2hsv(d).z;
        return _hsv2rgb(s);
      }
      fixed3 Saturation(fixed3 s, fixed3 d) {
        d = _rgb2hsv(d);
        d.y = _rgb2hsv(s).y;
        return _hsv2rgb(d);
      }
      fixed3 Luminosity(fixed3 s, fixed3 d) {
        fixed3 l = fixed3(0.3, 0.59, 0.11);
        float dLum = dot(d, l);
        float sLum = dot(s, l);
        float lum = sLum - dLum;
        fixed3 c = d + lum;
        float minC = min(min(c.x, c.y), c.z);
        float maxC = max(max(c.x, c.y), c.z);
        if(minC < 0.0) {
          return sLum + ((c - sLum) * sLum) / (sLum - minC);
        } else if(maxC > 1.0) {
          return sLum + ((c - sLum) * (1.0 - sLum)) / (maxC - sLum);
        } else {
          return c;
        }
      }
      // END BLENDING FUNCTIONS

      struct v2f {
        float4 pos : SV_POSITION;
        float4 screenPos : TEXCOORD1;
        float3 ray : TEXCOORD2;
      };

      v2f vert(float3 v : POSITION) {
        v2f o;
        o.pos = UnityObjectToClipPos(v);
        o.screenPos = ComputeScreenPos(o.pos);
        o.ray = UnityObjectToViewPos(v).xyz * float3(-1.0, -1.0, 1.0);
        return o;
      }

      sampler2D _MainTex;
      sampler2D _FDS_ScreenTex; // Framebufer before this object; set by a command buffer.
      sampler2D_float _CameraDepthTexture;

      fixed4 frag(v2f i) : SV_Target {
        /// Reconstruct world position.
        float2 uv = i.screenPos.xy / i.screenPos.w;
        // Linearized depth sample.
        float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
        depth = Linear01Depth(depth);
        // Scale ray to reach far plane.
        i.ray = i.ray * (_ProjectionParams.z / i.ray.z);
        // Reconstruct view, world, and object position.
        float3 viewPos = i.ray * depth;
        float3 worldPos = mul(unity_CameraToWorld, float4(viewPos, 1.0)).xyz;
        float3 objPos = mul(unity_WorldToObject, float4(worldPos, 1.0)).xyz;

        // Object bounds clip.
        clip(float3(0.5, 0.5 ,0.5) - abs(objPos.xyz));

        // Decal sample.
        uv = objPos.xz + 0.5;
        fixed4 decalTex = tex2D(_MainTex, uv);

        // Screen tex sample.
        float2 bgUV = i.screenPos.xy / i.screenPos.w;
        bgUV.y = 1.0 - bgUV.y;
        fixed4 screenTex = tex2D(_FDS_ScreenTex, bgUV);

        // BEGIN BLENDING MODES
        #if defined(FD_DARKEN)
        return fixed4(Darken(screenTex, decalTex), decalTex.a);
        #endif

        #if defined(FD_MULTIPLY)
        return fixed4(Multiply(screenTex, decalTex), decalTex.a);
        #endif

        #if defined(FD_COLORBURN)
        return fixed4(ColorBurn(screenTex, decalTex), decalTex.a);
        #endif

        #if defined(FD_LINEARBURN)
        return fixed4(LinearBurn(screenTex, decalTex), decalTex.a);
        #endif

        #if defined(FD_DARKERCOLOR)
        return fixed4(DarkerColor(screenTex, decalTex), decalTex.a);
        #endif

        #if defined(FD_LINEARDODGE)
        return fixed4(LinearDodge(screenTex, decalTex), decalTex.a);
        #endif

        //

        #if defined(FD_LIGHTEN)
        return fixed4(Lighten(screenTex, decalTex), decalTex.a);
        #endif

        #if defined(FD_SCREEN)
        return fixed4(Screen(screenTex, decalTex), decalTex.a);
        #endif

        #if defined(FD_COLORDODGE)
        return fixed4(ColorDodge(screenTex, decalTex), decalTex.a);
        #endif

        #if defined(FD_LIGHTERCOLOR)
        return fixed4(LighterColor(screenTex, decalTex), decalTex.a);
        #endif

        //

        #if defined(FD_OVERLAY)
        return fixed4(Overlay(screenTex, decalTex), decalTex.a);
        #endif

        #if defined(FD_SOFTLIGHT)
        return fixed4(SoftLight(screenTex, decalTex), decalTex.a);
        #endif

        #if defined(FD_HARDLIGHT)
        return fixed4(HardLight(screenTex, decalTex), decalTex.a);
        #endif

        #if defined(FD_VIVIDLIGHT)
        return fixed4(VividLight(screenTex, decalTex), decalTex.a);
        #endif

        #if defined(FD_LINEARLIGHT)
        return fixed4(LinearLight(screenTex, decalTex), decalTex.a);
        #endif

        #if defined(FD_PINLIGHT)
        return fixed4(PinLight(screenTex, decalTex), decalTex.a);
        #endif

        #if defined(FD_HARDMIX)
        return fixed4(HardMix(screenTex, decalTex), decalTex.a);
        #endif

        //

        #if defined(FD_DIFFERENCE)
        return fixed4(Difference(screenTex, decalTex), decalTex.a);
        #endif

        #if defined(FD_EXCLUSION)
        return fixed4(Exclusion(screenTex, decalTex), decalTex.a);
        #endif

        #if defined(FD_SUBTRACT)
        return fixed4(Subtract(screenTex, decalTex), decalTex.a);
        #endif

        #if defined(FD_DIVIDE)
        return fixed4(Divide(screenTex, decalTex), decalTex.a);
        #endif

        //

        #if defined(FD_HUE)
        return fixed4(Hue(screenTex, decalTex), decalTex.a);
        #endif

        #if defined(FD_COLOR)
        return fixed4(Color(screenTex, decalTex), decalTex.a);
        #endif

        #if defined(FD_SATURATION)
        return fixed4(Saturation(screenTex, decalTex), decalTex.a);
        #endif

        #if defined(FD_LUMINOSITY)
        return fixed4(Luminosity(screenTex, decalTex), decalTex.a);
        #endif
        // END BLENDING MODES

        // Fallback to a multiply.
        return fixed4(Multiply(screenTex, decalTex), decalTex.a);
      }
      ENDCG
    }      
  }

  Fallback Off
}
