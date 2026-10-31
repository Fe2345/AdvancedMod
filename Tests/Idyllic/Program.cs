// 2026-10-30：独立验证局部圆心角、弦长、曲率、径向厚度和边界输入。
using System;
using AdvancedMod.Contents.Vfx;
using Microsoft.Xna.Framework;
int checks=0;
void Check(bool yes,string name){if(!yes)throw new Exception(name);checks++;}
float previous=10000;
foreach(int angle in new[]{120,90,60,45,30,15})
{
 var g=new IdyllicArcGeometry(angle,640);
 Check(MathF.Abs(Vector2.Distance(g.Point(0),g.Point(1))-640)<.001f,"固定弦长");
 Check(g.Sagitta<previous,"逐渐变平");previous=g.Sagitta;
 var center=new Vector2(0,g.Radius);
 float actual=MathF.Acos(Math.Clamp(Vector2.Dot(Vector2.Normalize(g.Point(0)-center),Vector2.Normalize(g.Point(1)-center)),-1,1))*180/MathF.PI;
 Check(MathF.Abs(actual-angle)<.001f,"实际圆心角");
 for(int i=0;i<=10;i++)Check(MathF.Abs(Vector2.Distance(g.Point(i/10f),g.Point(i/10f,12))-12)<.001f,"径向厚度独立");
 Check(new IdyllicArcGeometry(angle,640,true).Radius==640,"固定半径");
 Check(MathF.Abs(g.TextureDistance(1)-g.TextureDistance(0)-g.ArcLength)<.001f,"实际弧长");
}
Check(new IdyllicArcGeometry().ArcAngleDegrees==60,"默认角度");
Check(new IdyllicArcGeometry(0,1).ArcAngleDegrees==15,"角度下限");
Check(new IdyllicArcGeometry(999,9000).ChordLength<1024.001f,"尺寸上限");
try{_=new IdyllicArcGeometry(float.NaN,100);throw new Exception("NaN 未拒绝");}catch(ArgumentException){checks++;}
Console.WriteLine($"PASS {checks} 几何断言");
