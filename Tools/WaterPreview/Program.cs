// 2026-10-27：独立 FNA 预览器、确定性回放与运行证据导出。
using System.Diagnostics;
using System.Text.Json;
using AdvancedMod.Basic.Graphics.Water;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

internal static class Program
{
    [STAThread] static void Main(string[] args)
    {
        // 双击编译产物时定位源码工作区，确保资源与热重载编译器使用同一路径。
        if(!File.Exists("Assets/Shaders/WaterVortex.fx"))
        {
            var directory=new DirectoryInfo(AppContext.BaseDirectory);
            while(directory!=null&&!File.Exists(Path.Combine(directory.FullName,"Assets/Shaders/WaterVortex.fx"))) directory=directory.Parent;
            if(directory==null)throw new DirectoryNotFoundException("找不到水体实验工作区 Assets/Shaders/WaterVortex.fx");
            Directory.SetCurrentDirectory(directory.FullName);
        }
        if(args.Contains("--test")) {WaterTests.Run();return;}
        Environment.SetEnvironmentVariable("FNA3D_FORCE_DRIVER","D3D11");
        using var game=new Preview(args); game.Run();
    }
}
internal sealed class Preview : Game
{
    readonly GraphicsDeviceManager manager;
    readonly string root=Directory.GetCurrentDirectory();
    readonly string[] args;
    SpriteBatch batch=null!; Effect effect=null!; Texture2D background=null!;
    RenderTarget2D frame=null!;
    WaterRenderer[] instances=Array.Empty<WaterRenderer>();
    WaterSettings settings=new();
    KeyboardState old;
    double time; int frames,mode=2,resolution=256,count=1,bg,selected; bool paused,slow,overlap;
    readonly Stopwatch watch=new();
    readonly List<double> timings=new();
    string status="ready";
    readonly ShaderReload reload;
    int replaced;
    bool sawFailure;
    bool sawInterfaceFailure;
    string loadError="";
    double cpuAverage;
    static readonly string[] ParameterNames={"Rotation","Radial","Width","Branch","Threshold","Depth","Thickness","Specular","Refraction","Reflection"};
    bool screenshot,life;
    int limit=360;
    string? imagePath;
    string captureDirectory="docs/assets/water";
    Color[]? pausedPixels;
    bool pauseStable;
    public Preview(string[] arguments)
    {
        args=arguments; manager=new(this){PreferredBackBufferWidth=1000,PreferredBackBufferHeight=760,SynchronizeWithVerticalRetrace=false};
        IsFixedTimeStep=false; IsMouseVisible=true; Window.AllowUserResizing=true;
        if(args.Contains("--vortex")) settings.Fusion=false;
        if(args.Contains("--shaded")) mode=0;
        string? Arg(string name) {int i=Array.IndexOf(args,name);return i>=0?args[i+1]:null;}
        count=int.Parse(Arg("--instances")??"1"); resolution=int.Parse(Arg("--resolution")??"256");
        overlap=args.Contains("--overlap"); limit=int.Parse(Arg("--frames")??"360");
        imagePath=Arg("--background");
        captureDirectory=Arg("--capture-dir")??captureDirectory;
        bg=int.Parse(Arg("--bg")??"0");
        mode=int.Parse(Arg("--view")??mode.ToString());
        if(count!=1&&count!=5&&count!=10)throw new ArgumentException("实例数必须是 1/5/10");
        if(limit<40)throw new ArgumentException("测量至少需要 40 帧");
        if(args.Contains("--reload-test")) {Directory.CreateDirectory("obj/water-preview");File.Copy("Assets/Shaders/WaterVortex.fx","obj/water-preview/watch.fx",true);reload=new("obj/water-preview/watch.fx");}
        else reload=new();
        if(Arg("--preset") is string preset) settings=JsonSerializer.Deserialize<WaterSettings>(File.ReadAllText(preset))!;
    }
    protected override void LoadContent()
    {
        batch=new(GraphicsDevice); effect=LoadEffect(); Recreate();
        Console.WriteLine($"FNA={typeof(Game).Assembly.FullName}; adapter={GraphicsAdapter.DefaultAdapter.Description}; window=1000x760; VSync=false");
    }
    Effect LoadEffect(string path="Assets/Shaders/WaterVortex.xnb")
    {
        byte[] xnb=File.ReadAllBytes(Path.Combine(root,path));
        using var stream=new MemoryStream(xnb); using var reader=new BinaryReader(stream);
        stream.Position=10; reader.Read7BitEncodedInt(); reader.ReadString(); reader.ReadInt32(); reader.Read7BitEncodedInt(); reader.Read7BitEncodedInt();
        var loaded=new Effect(GraphicsDevice,reader.ReadBytes(reader.ReadInt32()));
        try {WaterRenderer.ValidateEffect(loaded);return loaded;}
        catch {loaded.Dispose();throw;}
    }
    void Recreate()
    {
        foreach(var item in instances) item.Dispose();
        instances=Enumerable.Range(0,count).Select(_=>new WaterRenderer(GraphicsDevice,resolution)).ToArray();
        background?.Dispose(); frame?.Dispose();
        int w=GraphicsDevice.PresentationParameters.BackBufferWidth,h=GraphicsDevice.PresentationParameters.BackBufferHeight;
        frame=new(GraphicsDevice,w,h,false,SurfaceFormat.Color,DepthFormat.None);
        background=new(GraphicsDevice,w,h); var pixels=new Color[w*h];
        for(int y=0;y<h;y++) for(int x=0;x<w;x++) pixels[y*w+x]=bg==0?(((x/32+y/32)%2==0)?new Color(174,160,133):new Color(57,71,80)):bg==1?new Color(17,22,29):new Color(225,227,224);
        background.SetData(pixels);
        if(imagePath!=null) {background.Dispose();using var input=File.OpenRead(imagePath);background=Texture2D.FromStream(GraphicsDevice,input);}
        if(args.Contains("--capture")) {Directory.CreateDirectory("docs/assets/water"); using var output=File.Create("docs/assets/water/background.png"); background.SaveAsPng(output,w,h);}
    }
    bool Press(Keys k,KeyboardState now)=>now.IsKeyDown(k)&&old.IsKeyUp(k);
    protected override void Update(GameTime gt)
    {
        var k=Keyboard.GetState(); if(Press(Keys.Escape,k)) Exit();
        if(Press(Keys.Space,k)) paused=!paused;
        if(Press(Keys.OemPeriod,k)) time+=1.0/60;
        if(Press(Keys.S,k)) slow=!slow;
        if(Press(Keys.R,k)) time=0;
        if(Press(Keys.Tab,k)) { settings.Fusion=!settings.Fusion; time=0; }
        if(Press(Keys.D,k)) mode=(mode+1)%6;
        if(Press(Keys.B,k)) {bg=(bg+1)%3; Recreate();}
        if(Press(Keys.I,k)) {count=count==1?5:count==5?10:1; Recreate();}
        if(Press(Keys.F,k)) {resolution=resolution==128?256:resolution==256?512:128; Recreate();}
        if(Press(Keys.O,k)) overlap=!overlap;
        if(Press(Keys.L,k)) life=!life;
        if(Press(Keys.F12,k)) screenshot=true;
        if(Press(Keys.F5,k)) File.WriteAllText("Tools/WaterPreview/preset.json",JsonSerializer.Serialize(settings,new JsonSerializerOptions{WriteIndented=true}));
        if(Press(Keys.F9,k)) settings=JsonSerializer.Deserialize<WaterSettings>(File.ReadAllText("Tools/WaterPreview/preset.json"))!;
        if(Press(Keys.Up,k)) selected=(selected+1)%10;
        if(Press(Keys.Down,k)) selected=(selected+9)%10;
        if(k.IsKeyDown(Keys.Left)||k.IsKeyDown(Keys.Right)) Adjust((k.IsKeyDown(Keys.Right)?1:-1)*(float)gt.ElapsedGameTime.TotalSeconds*0.15f);
        if(!paused) time+=args.Contains("--capture")?1.0/30:Math.Min(gt.ElapsedGameTime.TotalSeconds,0.1)*(slow?0.2:1);
        old=k;
        Window.Title=$"Water | CPU {cpuAverage:F1}ms GPU n/a | t={time:F2} {status} | {(settings.Fusion?"Fusion":"Vortex")} view={mode} {count}x{resolution} | {ParameterNames[selected]}: {Value():F3} | arrows edit / Space pause / Tab preset / D view";
        base.Update(gt);
    }
    float Value()=>selected switch {0=>settings.Rotation,1=>settings.Radial,2=>settings.Width,3=>settings.Branch,4=>settings.Threshold,5=>settings.Depth,6=>settings.Thickness,7=>settings.Specular,8=>settings.Refraction,_=>settings.Reflection};
    void Adjust(float delta)
    {
        float v=Value()+delta;
        switch(selected) {case 0:settings.Rotation=v;break;case 1:settings.Radial=v;break;case 2:settings.Width=Math.Clamp(v,0.02f,0.25f);break;case 3:settings.Branch=v;break;case 4:settings.Threshold=Math.Clamp(v,0.1f,1.5f);break;case 5:settings.Depth=v;break;case 6:settings.Thickness=Math.Max(0,v);break;case 7:settings.Specular=Math.Max(0,v);break;case 8:settings.Refraction=v;break;default:settings.Reflection=Math.Clamp(v,0,1);break;}
    }
    protected override void Draw(GameTime gt)
    {
        if(args.Contains("--exercise"))
        {
            if(frames==20){paused=true;time=4;}
            if(frames==50)paused=false;
            if(frames==60){manager.PreferredBackBufferWidth=840;manager.PreferredBackBufferHeight=620;manager.ApplyChanges();}
            if(frames==100)settings.Rotation=-settings.Rotation;
            if(frames==120){bg=1;Recreate();}
            if(frames==150){bg=2;Recreate();}
            if(frames==180){resolution=128;count=10;Recreate();}
            if(frames==240){resolution=512;count=1;Recreate();}
            if(frames==280){resolution=256;count=5;Recreate();}
        }
        if(reload.Poll())
        {
            try {var replacement=LoadEffect("obj/water-preview/reload.xnb");var previous=effect;effect=replacement;previous.Dispose();status="shader reloaded";loadError="";replaced++;}
            catch(Exception error) {status=loadError="LOAD FAILED - OLD EFFECT";sawInterfaceFailure=true;Console.Error.WriteLine(error);}
        }
        else status=loadError.Length>0?loadError:reload.Status;
        sawFailure |= status.Contains("COMPILE FAILED");
        if(args.Contains("--reload-test"))
        {
            if(frames==30) File.WriteAllText("obj/water-preview/watch.fx","intentional invalid shader");
            if(frames==250) File.Copy("Assets/Shaders/WaterVortex.fx","obj/water-preview/watch.fx",true);
            if(frames==450) File.WriteAllText("obj/water-preview/watch.fx",File.ReadAllText("Assets/Shaders/WaterVortex.fx").Replace("LightDirection","IncompatibleLight"));
            if(frames==700) File.Copy("Assets/Shaders/WaterVortex.fx","obj/water-preview/watch.fx",true);
        }
        watch.Restart();
        if(frame.Width!=GraphicsDevice.PresentationParameters.BackBufferWidth||frame.Height!=GraphicsDevice.PresentationParameters.BackBufferHeight) Recreate();
        GraphicsDevice.SetRenderTarget(frame); GraphicsDevice.Clear(Color.Black);
        batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.PointClamp,DepthStencilState.None,RasterizerState.CullNone); batch.Draw(background,frame.Bounds,Color.White); batch.End();
        for(int i=0;i<count;i++)
        {
            instances[i].Update(time+i*0.43,settings);
            int size=count==1?640:overlap?400:180+(i%3)*35;
            int x=count==1?(frame.Width-size)/2:overlap?200+i*20:(i%5)*190;
            int y=count==1?(frame.Height-size)/2:overlap?100+i*12:100+(i/5)*300;
            float phase=(float)(time%12);float opacity=life?WaterField.Smooth(0,2,phase)*(1-WaterField.Smooth(9,12,phase)):1;
            instances[i].Draw(batch,effect,background,new(x,y,size,size),Matrix.Identity,settings,(float)time,mode,opacity);
        }
        GraphicsDevice.SetRenderTarget(null); batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.PointClamp,DepthStencilState.None,RasterizerState.CullNone); batch.Draw(frame,GraphicsDevice.Viewport.Bounds,Color.White); batch.End();
        watch.Stop(); if(frames>30 && (args.Contains("--bench")||args.Contains("--capture"))) timings.Add(watch.Elapsed.TotalMilliseconds);
        cpuAverage=cpuAverage*0.95+watch.Elapsed.TotalMilliseconds*0.05;
        if(WaterRenderer.LiveFieldTextures!=count*2) throw new Exception("水体 GPU 资源数量异常");
        if(args.Contains("--exercise") && (frames==40||frames==41))
        {
            var pixels=new Color[frame.Width*frame.Height];frame.GetData(pixels);
            if(frames==40)pausedPixels=pixels;else {pauseStable=pausedPixels!.SequenceEqual(pixels);if(!pauseStable)throw new Exception("暂停画面不稳定");}
        }
        if(screenshot || (args.Contains("--capture") && frames%10==0))
        {
            Directory.CreateDirectory(captureDirectory); using var file=File.Create($"{captureDirectory}/{(settings.Fusion?"fusion":"vortex")}-{mode}-{frames:D4}.png"); frame.SaveAsPng(file,frame.Width,frame.Height);
            screenshot=false;
        }
        frames++;
        if((args.Contains("--capture")||args.Contains("--bench"))&&frames>=limit)
        {
            var report=new {resolution,count,overlap,window="1000x760",vsync=false,frames,cpuMeanMs=timings.Average(),cpuP95Ms=timings.Order().ElementAt((int)(timings.Count*0.95)),gpuMs=(double?)null,managedBytes=GC.GetTotalMemory(false),textures=count*2+2};
            string json=JsonSerializer.Serialize(report);Console.WriteLine(json);Directory.CreateDirectory("docs/testing/water");
            string category=args.Contains("--reload-test")?"reload-run":args.Contains("--exercise")?"exercise-run":args.Contains("--capture")?"capture-run":"perf";
            File.WriteAllText($"docs/testing/water/{category}-{resolution}-{count}-{overlap}.json",json);Exit();
            if(args.Contains("--reload-test")) {File.WriteAllText("docs/testing/water/reload.json",JsonSerializer.Serialize(new{sawFailure,sawInterfaceFailure,replaced}));if(!sawFailure||!sawInterfaceFailure||replaced<2)throw new Exception("热重载实机失败");}
        }
        base.Draw(gt);
    }
    protected override void UnloadContent()
    {
        foreach(var i in instances)i.Dispose(); effect.Dispose();background.Dispose();frame.Dispose();batch.Dispose();
        if(WaterRenderer.LiveFieldTextures!=0)throw new Exception("GPU 场资源未释放");
        if(args.Contains("--exercise"))File.WriteAllText("docs/testing/water/lifecycle.json",JsonSerializer.Serialize(new{pauseStable,liveFieldTextures=WaterRenderer.LiveFieldTextures,resize=true,resolutionChanges=true,instanceChanges=true}));
    }
}

