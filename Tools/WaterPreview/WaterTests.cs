// 2026-10-27：验证拓扑事件、确定性、有限值与热重载编译失败路径。
using AdvancedMod.Basic.Graphics.Water;
using System.Text.Json;
internal static class WaterTests
{
    public static void Run()
    {
        var p=new WaterSettings();var field=new WaterField(256);var results=new List<object>();
        void Check(string name,bool ok) {results.Add(new{name,ok});if(!ok)throw new Exception(name);}
        field.Generate(0,p);var first=(Microsoft.Xna.Framework.Vector4[])field.Layers[0].Clone();
        field.Generate(0,p);Check("相同时间种子逐浮点一致",first.SequenceEqual(field.Layers[0]));
        int Components()
        {
            var seen=new bool[256*256];int components=0;var queue=new Queue<int>();
            for(int i=0;i<seen.Length;i++)
            {
                if(seen[i]||field.Layers[0][i].W<0.5f)continue;
                components++;seen[i]=true;queue.Enqueue(i);
                while(queue.Count>0)
                {
                    int j=queue.Dequeue();int x=j%256,y=j/256;
                    foreach(var n in new[]{x>0?j-1:-1,x<255?j+1:-1,y>0?j-256:-1,y<255?j+256:-1})
                        if(n>=0&&!seen[n]&&field.Layers[0][n].W>=0.5f) {seen[n]=true;queue.Enqueue(n);}
                }
            }
            return components;
        }
        Check("分离时两个连通分量",Components()==2);
        field.Generate(Math.PI/0.65,p);Check("汇合时一个实际遮罩分量",Components()==1);
        field.Generate(Math.PI*2/0.65,p);Check("分离后恢复两个分量",Components()==2);
        bool finite=true;float largestChange=0;
        var previous=new float[256*256];
        for(int frame=0;frame<180;frame++)
        {
            field.Generate(frame/30.0,p);
            for(int i=0;i<previous.Length;i++) {var v=field.Layers[0][i];finite &= float.IsFinite(v.X)&&float.IsFinite(v.Y)&&float.IsFinite(v.Z)&&v.W>=0&&v.W<=1;if(frame>0)largestChange=Math.Max(largestChange,Math.Abs(v.X-previous[i]));previous[i]=v.X;}
        }
        Check("动画全程有限且遮罩有界",finite);Check("30Hz 原始场连续变化上界",largestChange<0.15f);
        field.Generate(2,p);long start=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<30;i++)field.Generate(i/60.0,p);
        Check("稳态 CPU 场生成零托管分配",GC.GetAllocatedBytesForCurrentThread()==start);
        var other=new WaterField(128);other.Generate(0,p);var saved=other.Layers[0][100];field.Generate(5,p);Check("实例数据隔离",other.Layers[0][100]==saved);
        Directory.CreateDirectory("obj/water-preview");File.WriteAllText("obj/water-preview/broken.fx","intentional syntax error");
        Check("错误 shader 编译失败",!ShaderReload.Compile("obj/water-preview/broken.fx").ok);
        Check("有效 shader 编译成功",ShaderReload.Compile("Assets/Shaders/WaterVortex.fx").ok);
        Directory.CreateDirectory("docs/testing/water");File.WriteAllText("docs/testing/water/offline.json",JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine(JsonSerializer.Serialize(results));
    }
}
