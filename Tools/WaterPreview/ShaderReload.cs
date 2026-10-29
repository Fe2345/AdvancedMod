// 2026-10-27：时间戳防抖编译；只把结果交给绘制线程，不在工作线程操作 GPU。
using System.Diagnostics;
internal sealed class ShaderReload
{
    readonly string source;
    DateTime observed,changed;
    Task<(bool ok,string log)>? pending;
    public string Status {get;private set;}="shader current";
    public ShaderReload(string path="Assets/Shaders/WaterVortex.fx") {source=path;observed=File.GetLastWriteTimeUtc(source);}
    public bool Poll()
    {
        var stamp=File.GetLastWriteTimeUtc(source);
        if(stamp!=observed) { observed=stamp;changed=DateTime.UtcNow; }
        if(pending is {IsCompleted:true})
        {
            var result=pending.Result; pending=null;
            Console.WriteLine(result.log); Directory.CreateDirectory("obj/water-preview");File.WriteAllText("obj/water-preview/shader.log",result.log);
            Status=result.ok?"shader current":"COMPILE FAILED - OLD EFFECT";
            return result.ok;
        }
        if(changed!=default && (DateTime.UtcNow-changed).TotalMilliseconds>500 && pending==null)
        {
            changed=default;Status="compiling - OLD EFFECT";
            pending=Task.Run(()=>Compile(source));
        }
        return false;
    }
    public static (bool ok,string log) Compile(string source)
    {
        try
        {
            var info=new ProcessStartInfo("python"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
            info.Environment["PYTHONUTF8"]="1";info.Environment["PYTHONIOENCODING"]="utf-8";
            info.ArgumentList.Add("Tools/ShaderBytecode.py"); info.ArgumentList.Add("compile");info.ArgumentList.Add(source);info.ArgumentList.Add("obj/water-preview/reload.xnb");
            using var process=Process.Start(info)!;
            var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();process.WaitForExit();
            return(process.ExitCode==0,stdout.Result+stderr.Result);
        }
        catch(Exception e) {return(false,e.ToString());}
    }
}
