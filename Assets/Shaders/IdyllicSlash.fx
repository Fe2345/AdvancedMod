/*
	Copyright 2025 Ali
	
	Permission is hereby granted, free of charge, to any person obtaining a copy of 
	this software and associated documentation files (the “Software”), to deal in 
	the Software without restriction, including without limitation the rights to use, 
	copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the 
	Software, and to permit persons to whom the Software is furnished to do so, 
	subject to the following conditions:
	
	The above copyright notice and this permission notice shall be included in all 
	copies or substantial portions of the Software.
	
	THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
	WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN 
	CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

// 2026-10-30：Alkaliii CosmicSlash 五层预设的 HLSL 移植模板。许可见 Assets/Idyllic/LICENSE.txt。
float4x4 uWorldViewProjection;
float Progress, Thickness, Feather, TextureScale, Original, Brightness;
float WindOpacity;
float3 WindTint;
texture MaskTexture, DistortTexture, GradientTexture, WideTexture;
sampler Mask = sampler_state { Texture=<MaskTexture>; MinFilter=Linear; MagFilter=Linear; AddressU=Wrap; AddressV=Wrap; };
sampler Distort = sampler_state { Texture=<DistortTexture>; MinFilter=Linear; MagFilter=Linear; AddressU=Wrap; AddressV=Wrap; };
sampler Gradient = sampler_state { Texture=<GradientTexture>; MinFilter=Linear; MagFilter=Linear; AddressU=Clamp; AddressV=Clamp; };
sampler Wide = sampler_state { Texture=<WideTexture>; MinFilter=Linear; MagFilter=Linear; AddressU=Clamp; AddressV=Clamp; };
struct VI {float4 Position:POSITION0;float4 Color:COLOR0;float2 UV:TEXCOORD0;};
struct VO {float4 Position:POSITION0;float2 UV:TEXCOORD0;};
VO VS(VI v){VO o;o.Position=mul(v.Position,uWorldViewProjection);o.UV=v.UV;return o;}
float chrono(float n) {
 float p=Progress;
 if(n==0)return 0;if(n==1)return p;if(n==2)return p;
 if(n==3)return p*p*p;if(n==4)return 1-pow(1-p,3);
 if(n==5)return p<.5?4*p*p*p:1-pow(-2*p+2,3)/2;
 return 4*pow(p-.5,3)+.5;
}
float2 polar(float2 uv){float2 d=uv-.5;return frac(float2(length(d)*1.2,atan2(d.y,d.x)/6.2832));}
float2 project(float2 uv){
 float sy=sin(radians(11)),cy=cos(radians(11)),sx=sin(radians(70)),cx=cos(radians(70));
 float3 w=float3(cy*(uv.x-.5)+sy*sx*(uv.y-.5)+sy*cx*.5,cx*(uv.y-.5)-sx*.5,-sy*(uv.x-.5)+cy*sx*(uv.y-.5)+cy*cx*.5);
 return w.xy*(cy*cx)/w.z-float2(sy*cx,-sx)+.5;
}
float edgeDistance(float2 q,float2 a,float2 b){float2 e=b-a;float2 w=q-a;return dot(w-e*saturate(dot(w,e)/dot(e,e)),w-e*saturate(dot(w,e)/dot(e,e)));}
float polygonDistance(float2 q,float preset){
 float2 v[4];int count;
 if(preset==4){v[0]=float2(-.025,-.5);v[1]=float2(0,.3);v[2]=float2(-.025,.35);v[3]=float2(-.05,.3);count=4;}
 else{v[0]=float2(-.05,-.5);v[1]=float2(-.05,.4);v[2]=float2(-.15,.4);v[3]=v[2];count=3;}
 float d=100,s=1;int j=count-1;
 [unroll]for(int i=0;i<4;i++){if(i<count){float2 a=v[i],b=v[j],e=b-a,w=q-a;d=min(d,edgeDistance(q,a,b));bool3 c=bool3(q.y>=a.y,q.y<b.y,e.x*w.y>e.y*w.x);if(all(c)||all(!c))s=-s;j=i;}}
 return s*sqrt(d);
}
float falloff(float d,float feather,float kind,float3 control){
 float x=saturate(d/max(feather,0.0001));
 if(kind==1){float a=control.x,b=control.y;return x<=a?b-b*pow(1-x/a,3):b+(1-b)*pow((x-a)/(1-a),3);}
 if(kind==2)return pow(x,control.x<.5?2*control.x:1/(2-2*control.x));
 return x*x*(3-2*x);
}
float maskgrad(float2 uv,float type,float2 edge){uv=smoothstep(edge.x,edge.y,uv);return type==5?(uv.x+uv.y)/2:uv.y;}
float baseImage_0(float2 uv){
 float2 baseUV=uv-lerp((float4(-0.05,0.0,0.0,0.0)).xy,(float4(-0.05,0.0,0.0,0.0)).zw,chrono((0.0)));
 float2 q=(baseUV-.5)/lerp((float4(0.075,1.0,1.0,1.0)).xy,(float4(0.075,1.0,1.0,1.0)).zw,chrono((0.0)))+.5;
 float cp=chrono((6.0)),from=step(.5,cp);float wp=(4.0)==5?min(cp,.5):cp;float wave=max(1.0/4096,(-cos(wp*6.2831853)*.5+.5));
 q.y=(q.y-from)/wave+from;
 float d=polygonDistance(q-.5,(4.0));
 float aa=max(abs(ddx(d))+abs(ddy(d)),.0001);
 float shape=1-falloff(d,max((2.0)*Feather,aa),(0.0),(float3(0.1,0.2,32.0)));
 float2 bmuv=baseUV;
 // 形状使用归一化角坐标，纹理独立使用实际弧长比例。
 bmuv+=lerp((float4(0.4,0.0,0.0,0.0)).xy,(float4(0.4,0.0,0.0,0.0)).zw,chrono((2.0)))*Progress;
 bmuv=(bmuv-.5)/lerp((float4(1.0,1.0,1.0,1.0)).xy,(float4(1.0,1.0,1.0,1.0)).zw,chrono((0.0)))+.5;
 float2 sampleUV=bmuv;
 if(Original<.5)sampleUV.y=(sampleUV.y-.5)*TextureScale+.5;
 float value=tex2D(Mask,sampleUV).r;
 value*=maskgrad(saturate(bmuv),(2.0),lerp((float4(-0.5,1.0,0.0,1.0)).xy,(float4(-0.5,1.0,0.0,1.0)).zw,chrono((0.0))));
 float2 edge=lerp((float4(0.0,0.01,0.0,1.0)).xy,(float4(0.0,0.01,0.0,1.0)).zw,chrono((6.0)));
 value=smoothstep(edge.x,edge.y,pow(max(value,0),lerp((float2(0.1,1.0)).x,(float2(0.1,1.0)).y,chrono((0.0)))));
 return shape*pow(value,(1.0));
}
float4 PS_0(VO input):COLOR0{
 float2 uv=input.UV,a;
 if(Original>.5){uv=project(uv);clip(uv);clip(1-uv);a=polar(float2(1-uv.y,uv.x));}
 else a=uv;
 float2 duv=Original>.5?polar(uv):float2(a.x,(a.y-.5)*TextureScale+.5);
 duv=(duv-.5)/(float4(0.3,1.0,1.0,1.0)).xy+.5;
 float2 scroll=lerp((float4(0.2,-0.5,0.0,-0.5)).xy,(float4(0.2,-0.5,0.0,-0.5)).zw,chrono((2.0)))*Progress*.5;
 float2 distortion=float2(tex2D(Distort,duv+scroll).r,tex2D(Distort,1-duv.yx+scroll).g)*2-1;
 a+=distortion*lerp((0.10000004619372004),(0.6000000536443197),chrono((5.0)))*.1;
 float4 poly=baseImage_0(a);float shade=poly.r;
 if((0.0)==1){
  float4 sum=0,weight=0;
  [loop]for(int i=0;i<=10;i++){float t=i/10.0;float value=baseImage_0(a+(t-.5)*(float2(0.035,0.2)));float4 w=pow(float4(t,1-abs(2*t-1),1-t,.5),4);sum+=value*value*w;weight+=w;}
  poly=sqrt(sum/weight);shade=dot(poly.rgb,float3(.333333,.333333,.333333));
 }
 float energy=lerp((float2(0.6,1.0)).x,(float2(0.6,1.0)).y,chrono((0.0)));poly*=energy;
 float2 baseUV=a-lerp((float4(-0.05,0.0,0.0,0.0)).xy,(float4(-0.05,0.0,0.0,0.0)).zw,chrono((0.0)));
 float shift=maskgrad(baseUV,(2.0),lerp((float4(-1.0,1.0,0.5,1.0)).xy,(float4(-1.0,1.0,0.5,1.0)).zw,chrono((4.0))))+lerp((float2(0.0,0.0)).x,(float2(0.0,0.0)).y,chrono((0.0)));
 float sample=pow(smoothstep((float3(0.0,3.0,1.0)).x,(float3(0.0,3.0,1.0)).y,shade*energy),(float3(0.0,3.0,1.0)).z)+lerp((float2(0.5,-0.25)).x,(float2(0.5,-0.25)).y,chrono((5.0)))+shift;
 float4 color=tex2D(Gradient,float2(sample,0));
 float wide=tex2D(Wide,float2(.5,Original>.5?polar(float2(1-uv.y,uv.x)).y:input.UV.y)).r;
 float alpha=smoothstep((0.0),1,saturate(dot(poly.rgb,float3(.333333,.333333,.333333)))*color.a*wide);
 // 原 blend_mix 为直通 RGB/Alpha；FNA AlphaBlend 要求输出预乘。无全屏泛白矩形。
  float3 rgb=saturate(poly.rgb*color.rgb*(2.0000000298024)*wide*Brightness);
 // 2026-10-30：风刃改为微冷浅白；保留明暗纹理，透明度同时作用于预乘 RGB 与 Alpha。
 float light=max(rgb.r,max(rgb.g,rgb.b));rgb=light*WindTint;alpha*=WindOpacity;
 return float4(rgb*alpha,alpha);
}
technique Layer_0 {pass Pass0 {VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 PS_0();}}
float baseImage_1(float2 uv){
 float2 baseUV=uv-lerp((float4(0.005,-0.1,0.005,0.0)).xy,(float4(0.005,-0.1,0.005,0.0)).zw,chrono((4.0)));
 float2 q=(baseUV-.5)/lerp((float4(0.125,1.0,1.0,1.0)).xy,(float4(0.125,1.0,1.0,1.0)).zw,chrono((0.0)))+.5;
 float cp=chrono((4.0)),from=step(.5,cp);float wp=(5.0)==5?min(cp,.5):cp;float wave=max(1.0/4096,(-cos(wp*6.2831853)*.5+.5));
 q.y=(q.y-from)/wave+from;
 float d=polygonDistance(q-.5,(4.0));
 float aa=max(abs(ddx(d))+abs(ddy(d)),.0001);
 float shape=1-falloff(d,max((0.4)*Feather,aa),(0.0),(float3(0.1,0.5,0.5)));
 float2 bmuv=baseUV;
 // 形状使用归一化角坐标，纹理独立使用实际弧长比例。
 bmuv+=lerp((float4(-0.1,0.0,0.0,0.0)).xy,(float4(-0.1,0.0,0.0,0.0)).zw,chrono((0.0)))*Progress;
 bmuv=(bmuv-.5)/lerp((float4(0.1,0.1,1.0,1.0)).xy,(float4(0.1,0.1,1.0,1.0)).zw,chrono((0.0)))+.5;
 float2 sampleUV=bmuv;
 if(Original<.5)sampleUV.y=(sampleUV.y-.5)*TextureScale+.5;
 float value=tex2D(Mask,sampleUV).r;
 value*=maskgrad(saturate(bmuv),(2.0),lerp((float4(-0.5,1.0,0.0,1.0)).xy,(float4(-0.5,1.0,0.0,1.0)).zw,chrono((0.0))));
 float2 edge=lerp((float4(0.0,0.4,0.0,0.6)).xy,(float4(0.0,0.4,0.0,0.6)).zw,chrono((2.0)));
 value=smoothstep(edge.x,edge.y,pow(max(value,0),lerp((float2(1.0,1.0)).x,(float2(1.0,1.0)).y,chrono((0.0)))));
 return shape*pow(value,(1.0));
}
float4 PS_1(VO input):COLOR0{
 float2 uv=input.UV,a;
 if(Original>.5){uv=project(uv);clip(uv);clip(1-uv);a=polar(float2(1-uv.y,uv.x));}
 else a=uv;
 float2 duv=Original>.5?polar(uv):float2(a.x,(a.y-.5)*TextureScale+.5);
 duv=(duv-.5)/(float4(0.02,1.0,1.0,1.0)).xy+.5;
 float2 scroll=lerp((float4(0.2,0.1,0.0,0.1)).xy,(float4(0.2,0.1,0.0,0.1)).zw,chrono((0.0)))*Progress*.5;
 float2 distortion=float2(tex2D(Distort,duv+scroll).r,tex2D(Distort,1-duv.yx+scroll).g)*2-1;
 a+=distortion*lerp((0.10000004619372004),(0.20000004768384017),chrono((5.0)))*.1;
 float4 poly=baseImage_1(a);float shade=poly.r;
 if((0.0)==1){
  float4 sum=0,weight=0;
  [loop]for(int i=0;i<=10;i++){float t=i/10.0;float value=baseImage_1(a+(t-.5)*(float2(0.035,0.2)));float4 w=pow(float4(t,1-abs(2*t-1),1-t,.5),4);sum+=value*value*w;weight+=w;}
  poly=sqrt(sum/weight);shade=dot(poly.rgb,float3(.333333,.333333,.333333));
 }
 float energy=lerp((float2(1.0,0.0)).x,(float2(1.0,0.0)).y,chrono((3.0)));poly*=energy;
 float2 baseUV=a-lerp((float4(0.005,-0.1,0.005,0.0)).xy,(float4(0.005,-0.1,0.005,0.0)).zw,chrono((4.0)));
 float shift=maskgrad(baseUV,(2.0),lerp((float4(-1.0,1.0,0.5,1.0)).xy,(float4(-1.0,1.0,0.5,1.0)).zw,chrono((6.0))))+lerp((float2(0.0,0.0)).x,(float2(0.0,0.0)).y,chrono((0.0)));
 float sample=pow(smoothstep((float3(0.0,3.0,1.0)).x,(float3(0.0,3.0,1.0)).y,shade*energy),(float3(0.0,3.0,1.0)).z)+lerp((float2(0.5,-0.25)).x,(float2(0.5,-0.25)).y,chrono((5.0)))+shift;
 float4 color=tex2D(Gradient,float2(sample,0));
 float wide=tex2D(Wide,float2(.5,Original>.5?polar(float2(1-uv.y,uv.x)).y:input.UV.y)).r;
 float alpha=smoothstep((0.0),1,saturate(dot(poly.rgb,float3(.333333,.333333,.333333)))*color.a*wide);
 // 原 blend_mix 为直通 RGB/Alpha；FNA AlphaBlend 要求输出预乘。无全屏泛白矩形。
  float3 rgb=saturate(poly.rgb*color.rgb*(16.0)*wide*Brightness);
 // 2026-10-30：风刃改为微冷浅白；保留明暗纹理，透明度同时作用于预乘 RGB 与 Alpha。
 float light=max(rgb.r,max(rgb.g,rgb.b));rgb=light*WindTint;alpha*=WindOpacity;
 return float4(rgb*alpha,alpha);
}
technique Layer_1 {pass Pass0 {VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 PS_1();}}
float baseImage_2(float2 uv){
 float2 baseUV=uv-lerp((float4(0.005,0.0,0.005,-0.1)).xy,(float4(0.005,0.0,0.005,-0.1)).zw,chrono((4.0)));
 float2 q=(baseUV-.5)/lerp((float4(0.125,1.0,1.0,1.0)).xy,(float4(0.125,1.0,1.0,1.0)).zw,chrono((0.0)))+.5;
 float cp=chrono((4.0)),from=step(.5,cp);float wp=(5.0)==5?min(cp,.5):cp;float wave=max(1.0/4096,(-cos(wp*6.2831853)*.5+.5));
 q.y=(q.y-from)/wave+from;
 float d=polygonDistance(q-.5,(4.0));
 float aa=max(abs(ddx(d))+abs(ddy(d)),.0001);
 float shape=1-falloff(d,max((0.4)*Feather,aa),(0.0),(float3(0.1,0.5,0.5)));
 float2 bmuv=baseUV;
 // 形状使用归一化角坐标，纹理独立使用实际弧长比例。
 bmuv+=lerp((float4(0.1,0.0,0.0,0.0)).xy,(float4(0.1,0.0,0.0,0.0)).zw,chrono((0.0)))*Progress;
 bmuv=(bmuv-.5)/lerp((float4(0.1,0.1,1.0,1.0)).xy,(float4(0.1,0.1,1.0,1.0)).zw,chrono((0.0)))+.5;
 float2 sampleUV=bmuv;
 if(Original<.5)sampleUV.y=(sampleUV.y-.5)*TextureScale+.5;
 float value=tex2D(Mask,sampleUV).r;
 value*=maskgrad(saturate(bmuv),(2.0),lerp((float4(-0.5,1.0,0.0,1.0)).xy,(float4(-0.5,1.0,0.0,1.0)).zw,chrono((0.0))));
 float2 edge=lerp((float4(0.0,0.3,0.0,1.0)).xy,(float4(0.0,0.3,0.0,1.0)).zw,chrono((0.0)));
 value=smoothstep(edge.x,edge.y,pow(max(value,0),lerp((float2(1.5,1.0)).x,(float2(1.5,1.0)).y,chrono((0.0)))));
 return shape*pow(value,(1.0));
}
float4 PS_2(VO input):COLOR0{
 float2 uv=input.UV,a;
 if(Original>.5){uv=project(uv);clip(uv);clip(1-uv);a=polar(float2(1-uv.y,uv.x));}
 else a=uv;
 float2 duv=Original>.5?polar(uv):float2(a.x,(a.y-.5)*TextureScale+.5);
 duv=(duv-.5)/(float4(0.02,1.0,1.0,1.0)).xy+.5;
 float2 scroll=lerp((float4(-0.2,-0.1,0.0,-0.1)).xy,(float4(-0.2,-0.1,0.0,-0.1)).zw,chrono((0.0)))*Progress*.5;
 float2 distortion=float2(tex2D(Distort,duv+scroll).r,tex2D(Distort,1-duv.yx+scroll).g)*2-1;
 a+=distortion*lerp((0.10000004619372004),(0.20000004768384017),chrono((5.0)))*.1;
 float4 poly=baseImage_2(a);float shade=poly.r;
 if((0.0)==1){
  float4 sum=0,weight=0;
  [loop]for(int i=0;i<=10;i++){float t=i/10.0;float value=baseImage_2(a+(t-.5)*(float2(0.035,0.2)));float4 w=pow(float4(t,1-abs(2*t-1),1-t,.5),4);sum+=value*value*w;weight+=w;}
  poly=sqrt(sum/weight);shade=dot(poly.rgb,float3(.333333,.333333,.333333));
 }
 float energy=lerp((float2(1.0,0.0)).x,(float2(1.0,0.0)).y,chrono((3.0)));poly*=energy;
 float2 baseUV=a-lerp((float4(0.005,0.0,0.005,-0.1)).xy,(float4(0.005,0.0,0.005,-0.1)).zw,chrono((4.0)));
 float shift=maskgrad(baseUV,(2.0),lerp((float4(-1.0,1.0,0.5,1.0)).xy,(float4(-1.0,1.0,0.5,1.0)).zw,chrono((0.0))))+lerp((float2(0.0,0.0)).x,(float2(0.0,0.0)).y,chrono((0.0)));
 float sample=pow(smoothstep((float3(0.0,3.0,1.0)).x,(float3(0.0,3.0,1.0)).y,shade*energy),(float3(0.0,3.0,1.0)).z)+lerp((float2(0.5,-0.25)).x,(float2(0.5,-0.25)).y,chrono((5.0)))+shift;
 float4 color=tex2D(Gradient,float2(sample,0));
 float wide=tex2D(Wide,float2(.5,Original>.5?polar(float2(1-uv.y,uv.x)).y:input.UV.y)).r;
 float alpha=smoothstep((0.0),1,saturate(dot(poly.rgb,float3(.333333,.333333,.333333)))*color.a*wide);
 // 原 blend_mix 为直通 RGB/Alpha；FNA AlphaBlend 要求输出预乘。无全屏泛白矩形。
  float3 rgb=saturate(poly.rgb*color.rgb*(16.0)*wide*Brightness);
 // 2026-10-30：风刃改为微冷浅白；保留明暗纹理，透明度同时作用于预乘 RGB 与 Alpha。
 float light=max(rgb.r,max(rgb.g,rgb.b));rgb=light*WindTint;alpha*=WindOpacity;
 return float4(rgb*alpha,alpha);
}
technique Layer_2 {pass Pass0 {VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 PS_2();}}
float baseImage_3(float2 uv){
 float2 baseUV=uv-lerp((float4(0.0,0.0,0.0,0.0)).xy,(float4(0.0,0.0,0.0,0.0)).zw,chrono((0.0)));
 float2 q=(baseUV-.5)/lerp((float4(0.5,1.0,1.0,1.0)).xy,(float4(0.5,1.0,1.0,1.0)).zw,chrono((0.0)))+.5;
 float cp=chrono((6.0)),from=step(.5,cp);float wp=(4.0)==5?min(cp,.5):cp;float wave=max(1.0/4096,(-cos(wp*6.2831853)*.5+.5));
 q.y=(q.y-from)/wave+from;
 float d=polygonDistance(q-.5,(6.0));
 float aa=max(abs(ddx(d))+abs(ddy(d)),.0001);
 float shape=1-falloff(d,max((0.109)*Feather,aa),(1.0),(float3(0.1,0.5,0.5)));
 float2 bmuv=baseUV;
 // 形状使用归一化角坐标，纹理独立使用实际弧长比例。
 bmuv+=lerp((float4(0.4,0.0,0.0,0.0)).xy,(float4(0.4,0.0,0.0,0.0)).zw,chrono((2.0)))*Progress;
 bmuv=(bmuv-.5)/lerp((float4(1.0,1.0,1.0,1.0)).xy,(float4(1.0,1.0,1.0,1.0)).zw,chrono((0.0)))+.5;
 float2 sampleUV=bmuv;
 if(Original<.5)sampleUV.y=(sampleUV.y-.5)*TextureScale+.5;
 float value=tex2D(Mask,sampleUV).r;
 value*=maskgrad(saturate(bmuv),(2.0),lerp((float4(-0.5,1.0,0.0,1.0)).xy,(float4(-0.5,1.0,0.0,1.0)).zw,chrono((0.0))));
 float2 edge=lerp((float4(0.0,0.01,0.0,1.0)).xy,(float4(0.0,0.01,0.0,1.0)).zw,chrono((6.0)));
 value=smoothstep(edge.x,edge.y,pow(max(value,0),lerp((float2(1.0,1.0)).x,(float2(1.0,1.0)).y,chrono((0.0)))));
 return shape*pow(value,(1.0));
}
float4 PS_3(VO input):COLOR0{
 float2 uv=input.UV,a;
 if(Original>.5){uv=project(uv);clip(uv);clip(1-uv);a=polar(float2(1-uv.y,uv.x));}
 else a=uv;
 float2 duv=Original>.5?polar(uv):float2(a.x,(a.y-.5)*TextureScale+.5);
 duv=(duv-.5)/(float4(0.3,1.0,1.0,1.0)).xy+.5;
 float2 scroll=lerp((float4(0.2,-0.5,0.0,-0.5)).xy,(float4(0.2,-0.5,0.0,-0.5)).zw,chrono((2.0)))*Progress*.5;
 float2 distortion=float2(tex2D(Distort,duv+scroll).r,tex2D(Distort,1-duv.yx+scroll).g)*2-1;
 a+=distortion*lerp((0.10000004619372004),(0.6000000536443197),chrono((5.0)))*.1;
 float4 poly=baseImage_3(a);float shade=poly.r;
 if((1.0)==1){
  float4 sum=0,weight=0;
  [loop]for(int i=0;i<=10;i++){float t=i/10.0;float value=baseImage_3(a+(t-.5)*(float2(0.035,-0.2)));float4 w=pow(float4(t,1-abs(2*t-1),1-t,.5),4);sum+=value*value*w;weight+=w;}
  poly=sqrt(sum/weight);shade=dot(poly.rgb,float3(.333333,.333333,.333333));
 }
 float energy=lerp((float2(1.0,1.0)).x,(float2(1.0,1.0)).y,chrono((0.0)));poly*=energy;
 float2 baseUV=a-lerp((float4(0.0,0.0,0.0,0.0)).xy,(float4(0.0,0.0,0.0,0.0)).zw,chrono((0.0)));
 float shift=maskgrad(baseUV,(5.0),lerp((float4(0.0,1.0,0.5,1.0)).xy,(float4(0.0,1.0,0.5,1.0)).zw,chrono((4.0))))+lerp((float2(0.0,-0.25)).x,(float2(0.0,-0.25)).y,chrono((0.0)));
 float sample=pow(smoothstep((float3(0.0,3.0,1.0)).x,(float3(0.0,3.0,1.0)).y,shade*energy),(float3(0.0,3.0,1.0)).z)+lerp((float2(0.5,-0.25)).x,(float2(0.5,-0.25)).y,chrono((5.0)))+shift;
 float4 color=tex2D(Gradient,float2(sample,0));
 float wide=tex2D(Wide,float2(.5,Original>.5?polar(float2(1-uv.y,uv.x)).y:input.UV.y)).r;
 float alpha=smoothstep((0.0),1,saturate(dot(poly.rgb,float3(.333333,.333333,.333333)))*color.a*wide);
 // 原 blend_mix 为直通 RGB/Alpha；FNA AlphaBlend 要求输出预乘。无全屏泛白矩形。
  float3 rgb=saturate(poly.rgb*color.rgb*(3.0000000447036004)*wide*Brightness);
 // 2026-10-30：风刃改为微冷浅白；保留明暗纹理，透明度同时作用于预乘 RGB 与 Alpha。
 float light=max(rgb.r,max(rgb.g,rgb.b));rgb=light*WindTint;alpha*=WindOpacity;
 return float4(rgb*alpha,alpha);
}
technique Layer_3 {pass Pass0 {VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 PS_3();}}
float baseImage_4(float2 uv){
 float2 baseUV=uv-lerp((float4(0.005,0.0,0.0,0.0)).xy,(float4(0.005,0.0,0.0,0.0)).zw,chrono((0.0)));
 float2 q=(baseUV-.5)/lerp((float4(0.125,1.0,1.0,1.0)).xy,(float4(0.125,1.0,1.0,1.0)).zw,chrono((0.0)))+.5;
 float cp=chrono((6.0)),from=step(.5,cp);float wp=(4.0)==5?min(cp,.5):cp;float wave=max(1.0/4096,(-cos(wp*6.2831853)*.5+.5));
 q.y=(q.y-from)/wave+from;
 float d=polygonDistance(q-.5,(6.0));
 float aa=max(abs(ddx(d))+abs(ddy(d)),.0001);
 float shape=1-falloff(d,max((0.2)*Feather,aa),(2.0),(float3(0.1,0.5,0.5)));
 float2 bmuv=baseUV;
 // 形状使用归一化角坐标，纹理独立使用实际弧长比例。
 bmuv+=lerp((float4(0.4,0.0,0.0,0.0)).xy,(float4(0.4,0.0,0.0,0.0)).zw,chrono((2.0)))*Progress;
 bmuv=(bmuv-.5)/lerp((float4(1.0,1.0,1.0,1.0)).xy,(float4(1.0,1.0,1.0,1.0)).zw,chrono((0.0)))+.5;
 float2 sampleUV=bmuv;
 if(Original<.5)sampleUV.y=(sampleUV.y-.5)*TextureScale+.5;
 float value=tex2D(Mask,sampleUV).r;
 value*=maskgrad(saturate(bmuv),(2.0),lerp((float4(-0.5,1.0,0.0,1.0)).xy,(float4(-0.5,1.0,0.0,1.0)).zw,chrono((0.0))));
 float2 edge=lerp((float4(0.0,0.01,0.0,1.0)).xy,(float4(0.0,0.01,0.0,1.0)).zw,chrono((6.0)));
 value=smoothstep(edge.x,edge.y,pow(max(value,0),lerp((float2(0.1,1.0)).x,(float2(0.1,1.0)).y,chrono((0.0)))));
 return shape*pow(value,(1.0));
}
float4 PS_4(VO input):COLOR0{
 float2 uv=input.UV,a;
 if(Original>.5){uv=project(uv);clip(uv);clip(1-uv);a=polar(float2(1-uv.y,uv.x));}
 else a=uv;
 float2 duv=Original>.5?polar(uv):float2(a.x,(a.y-.5)*TextureScale+.5);
 duv=(duv-.5)/(float4(0.3,1.0,1.0,1.0)).xy+.5;
 float2 scroll=lerp((float4(0.2,-0.5,0.0,-0.5)).xy,(float4(0.2,-0.5,0.0,-0.5)).zw,chrono((2.0)))*Progress*.5;
 float2 distortion=float2(tex2D(Distort,duv+scroll).r,tex2D(Distort,1-duv.yx+scroll).g)*2-1;
 a+=distortion*lerp((0.10000004619372004),(0.20000004768384017),chrono((5.0)))*.1;
 float4 poly=baseImage_4(a);float shade=poly.r;
 if((0.0)==1){
  float4 sum=0,weight=0;
  [loop]for(int i=0;i<=10;i++){float t=i/10.0;float value=baseImage_4(a+(t-.5)*(float2(0.035,0.2)));float4 w=pow(float4(t,1-abs(2*t-1),1-t,.5),4);sum+=value*value*w;weight+=w;}
  poly=sqrt(sum/weight);shade=dot(poly.rgb,float3(.333333,.333333,.333333));
 }
 float energy=lerp((float2(1.0,0.0)).x,(float2(1.0,0.0)).y,chrono((4.0)));poly*=energy;
 float2 baseUV=a-lerp((float4(0.005,0.0,0.0,0.0)).xy,(float4(0.005,0.0,0.0,0.0)).zw,chrono((0.0)));
 float shift=maskgrad(baseUV,(2.0),lerp((float4(-1.0,1.0,0.5,1.0)).xy,(float4(-1.0,1.0,0.5,1.0)).zw,chrono((4.0))))+lerp((float2(0.0,0.0)).x,(float2(0.0,0.0)).y,chrono((0.0)));
 float sample=pow(smoothstep((float3(0.0,3.0,1.0)).x,(float3(0.0,3.0,1.0)).y,shade*energy),(float3(0.0,3.0,1.0)).z)+lerp((float2(0.5,-0.25)).x,(float2(0.5,-0.25)).y,chrono((5.0)))+shift;
 float4 color=tex2D(Gradient,float2(sample,0));
 float wide=tex2D(Wide,float2(.5,Original>.5?polar(float2(1-uv.y,uv.x)).y:input.UV.y)).r;
 float alpha=smoothstep((0.0),1,saturate(dot(poly.rgb,float3(.333333,.333333,.333333)))*color.a*wide);
 // 原 blend_mix 为直通 RGB/Alpha；FNA AlphaBlend 要求输出预乘。无全屏泛白矩形。
  float3 rgb=saturate(poly.rgb*color.rgb*(16.0)*wide*Brightness);
 // 2026-10-30：风刃改为微冷浅白；保留明暗纹理，透明度同时作用于预乘 RGB 与 Alpha。
 float light=max(rgb.r,max(rgb.g,rgb.b));rgb=light*WindTint;alpha*=WindOpacity;
 return float4(rgb*alpha,alpha);
}
technique Layer_4 {pass Pass0 {VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 PS_4();}}
