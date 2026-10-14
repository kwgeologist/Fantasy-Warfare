using Godot;
using System;
using System.Linq;
using FantasyWarfare.Simulation;

public partial class BattleView : Node3D
{
    private readonly Battle battle = new(42);
    private Camera3D camera = null!;
    private Node3D formation = null!;
    private MeshInstance3D selection = null!, targetMarker = null!;
    private Label status = null!;
    private bool selected;
    private int mode;
    private float yaw = 0.65f, pitch = 0.85f, distance = 72;
    private Vector3 focus = Vector3.Zero;
    private bool smoke;
    private int smokeStage, smokeFrames;
    private Position pausePosition;
    private long pauseTick;
    private double elapsedRemainder;
    public override void _Ready()
    {
        smoke = OS.GetCmdlineUserArgs().Contains("--smoke");
        var world = new WorldEnvironment { Environment = new Godot.Environment {
            BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color("819aad"),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = Colors.White, AmbientLightEnergy = 0.65f } };
        AddChild(world);
        AddChild(new DirectionalLight3D { RotationDegrees = new(-55,-30,0), LightEnergy=1.1f, ShadowEnabled=true });
        BuildTerrain();
        for(int i=0;i<32;i++)
        {
            float x=-34+(i%8)*2.3f, z=-28+(i/8)*3.8f;
            float y=Terrain.Height(x,z);
            AddMesh(this,new CylinderMesh { TopRadius=0.22f, BottomRadius=0.3f, Height=3 },new(x,y+1.5f,z),new Color("705438"));
            AddMesh(this,new CylinderMesh { TopRadius=0, BottomRadius=1.35f, Height=4 },new(x,y+4,z),new Color("254c32"));
        }
        AddTerrainLabel("PLAIN",new(-27,0.2f,17));
        AddTerrainLabel("RIDGE",new(12,Terrain.Height(12,0)+0.4f,0));
        AddTerrainLabel("WOODS",new(-26,7,-22));
        AddTerrainLabel("VALLEY",new(-10,Terrain.Height(-10,22)+0.5f,22));
        formation = new Node3D(); AddChild(formation);
        for(int i=0;i<battle.Formation.Strength;i++) AddMesh(formation,new BoxMesh { Size=new(0.65f,1.4f,0.65f) },new((i%6-2.5f)*0.95f,0.7f,(i/6-1.5f)*0.95f),new Color("477fe0"));
        selection=AddMesh(formation,new CylinderMesh { TopRadius=3.9f,BottomRadius=3.9f,Height=0.08f },new(0,0.05f,0),new Color(0.95f,0.8f,0.2f,0.5f));
        selection.Visible=false;
        var body=new StaticBody3D { CollisionLayer=2,CollisionMask=0 }; formation.AddChild(body);
        body.AddChild(new CollisionShape3D { Shape=new BoxShape3D { Size=new(6.5f,2,4.5f) },Position=new(0,1,0) });
        targetMarker=AddMesh(this,new CylinderMesh { TopRadius=0.7f,BottomRadius=0.7f,Height=0.15f },Vector3.Zero,Colors.Orange);
        targetMarker.Visible=false;
        camera=new Camera3D { Far=400,Current=true }; AddChild(camera); UpdateCamera();
        var ui=new CanvasLayer(); AddChild(ui);
        var panel=new PanelContainer { Position=new(12,12) }; ui.AddChild(panel);
        var box=new VBoxContainer(); panel.AddChild(box);
        box.AddChild(new Label { Text=@"FANTASY WARFARE | Gate 0
Left click: select / deselect formation   Right click: move
Shift + right click: append waypoint   Space: pause / resume
1: RTS   2: Free orbit   3: Overhead
WASD: pan   Wheel: zoom   Middle drag: orbit (Free)" });
        status=new Label(); box.AddChild(status);
        SyncPresentation();
        GD.Print("Gate 0 scene ready: plain, ridge, woods, valley; 24 visual troops; three camera modes.");
    }
    private MeshInstance3D AddMesh(Node parent,Mesh mesh,Vector3 position,Color color)
    {
        var material=new StandardMaterial3D { AlbedoColor=color };
        if(color.A<1) material.Transparency=BaseMaterial3D.TransparencyEnum.Alpha;
        var node=new MeshInstance3D { Mesh=mesh,Position=position,MaterialOverride=material }; parent.AddChild(node); return node;
    }
    private void AddTerrainLabel(string text,Vector3 position) => AddChild(new Label3D { Text=text, Position=position,FontSize=64,PixelSize=0.035f,Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,NoDepthTest=false });
    private void BuildTerrain()
    {
        var surface=new SurfaceTool(); surface.Begin(Mesh.PrimitiveType.Triangles);
        Vector3 Point(int x,int z)=>new(x,Terrain.Height(x,z),z);
        void Vertex(Vector3 p) { surface.SetColor(p.Y>2?new Color("8b8760"):p.Y<-1?new Color("4d704e"):new Color("77935e")); surface.AddVertex(p); }
        for(int x=-50;x<50;x+=2) for(int z=-50;z<50;z+=2)
        {
            Vertex(Point(x,z)); Vertex(Point(x+2,z)); Vertex(Point(x,z+2));
            Vertex(Point(x+2,z)); Vertex(Point(x+2,z+2)); Vertex(Point(x,z+2));
        }
        surface.GenerateNormals();
        var mesh=surface.Commit();
        var material=new StandardMaterial3D { VertexColorUseAsAlbedo=true };
        var node=new MeshInstance3D { Mesh=mesh,MaterialOverride=material }; AddChild(node); node.CreateTrimeshCollision();
    }
    private void UpdateCamera()
    {
        camera.Projection=mode==2?Camera3D.ProjectionType.Orthogonal:Camera3D.ProjectionType.Perspective;
        camera.Size=distance;
        float p=mode==0?0.85f:mode==2?1.55f:pitch;
        float y=mode==0?0.65f:mode==2?0:yaw;
        camera.Position=focus+new Vector3(MathF.Sin(y)*MathF.Cos(p),MathF.Sin(p),MathF.Cos(y)*MathF.Cos(p))*distance;
        camera.LookAt(focus,Vector3.Up);
    }
    public override void _UnhandledInput(InputEvent e)
    {
        if(e is InputEventKey key && key.Pressed && !key.Echo)
        {
            if(key.Keycode==Key.Space) battle.SetPaused(!battle.Paused);
            if(key.Keycode>=Key.Key1 && key.Keycode<=Key.Key3) { mode=(int)key.Keycode-(int)Key.Key1; UpdateCamera(); }
        }
        if(e is InputEventMouseMotion motion && (motion.ButtonMask & MouseButtonMask.Middle)!=0 && mode==1)
        { yaw-=motion.Relative.X*0.006f; pitch=Math.Clamp(pitch+motion.Relative.Y*0.006f,0.15f,1.5f); UpdateCamera(); }
        if(e is not InputEventMouseButton mouse || !mouse.Pressed) return;
        if(mouse.ButtonIndex==MouseButton.WheelUp || mouse.ButtonIndex==MouseButton.WheelDown)
        { distance=Math.Clamp(distance+(mouse.ButtonIndex==MouseButton.WheelUp?-4:4),15,130); UpdateCamera(); return; }
        var from=camera.ProjectRayOrigin(mouse.Position);
        var query=PhysicsRayQueryParameters3D.Create(from,from+camera.ProjectRayNormal(mouse.Position)*400,mouse.ButtonIndex==MouseButton.Left?2u:1u);
        var hit=GetWorld3D().DirectSpaceState.IntersectRay(query);
        if(mouse.ButtonIndex==MouseButton.Left) selected=hit.Count>0;
        if(mouse.ButtonIndex==MouseButton.Right && selected && hit.Count>0)
        {
            Vector3 point=hit["position"].AsVector3();
            if(MathF.Abs(point.X)<=48 && MathF.Abs(point.Z)<=48)
                battle.IssueMove(new((int)MathF.Round(point.X*1000),(int)MathF.Round(point.Z*1000)),mouse.ShiftPressed);
        }
    }
    public override void _Process(double delta)
    {
        // Retain conversion remainder so frame partitioning does not discard fractional clock ticks.
        if(!battle.Paused)
        {
            elapsedRemainder+=delta*TimeSpan.TicksPerSecond;
            long ticks=(long)elapsedRemainder; elapsedRemainder-=ticks;
            battle.Advance(TimeSpan.FromTicks(ticks));
        }
        float x=0,z=0;
        if(Input.IsPhysicalKeyPressed(Key.A)) x-=1; if(Input.IsPhysicalKeyPressed(Key.D)) x+=1;
        if(Input.IsPhysicalKeyPressed(Key.W)) z-=1; if(Input.IsPhysicalKeyPressed(Key.S)) z+=1;
        focus+=new Vector3(x,0,z)*(float)delta*25; focus.X=Math.Clamp(focus.X,-48,48); focus.Z=Math.Clamp(focus.Z,-48,48);
        UpdateCamera(); SyncPresentation();
        if(smoke) RunSmoke();
    }
    private void SyncPresentation()
    {
        var p=battle.Formation.Position; formation.Position=new(p.X/1000f,Terrain.Height(p.X/1000f,p.Z/1000f),p.Z/1000f);
        selection.Visible=selected;
        Position? intent=battle.Pending.Count>0?battle.Pending[^1].Target:battle.Formation.Destination;
        targetMarker.Visible=intent.HasValue;
        if(intent is Position t) targetMarker.Position=new(t.X/1000f,Terrain.Height(t.X/1000f,t.Z/1000f)+0.2f,t.Z/1000f);
        status.Text=$@"{(battle.Paused?"PAUSED — orders pending until resume":"RUNNING")} | Tick {battle.Tick} | {new[]{"RTS","Free","Overhead"}[mode]}
Formation 1: {battle.Formation.Strength} troops | {(selected?"SELECTED":"click blue formation to select")}
Position {p.X/1000f:F1}, {p.Z/1000f:F1} m | Pending {battle.Pending.Count}, waypoints {battle.Waypoints.Length}";
    }
    // Exercises the same input adapter and actual terrain/formation raycasts in the engine.
    private void RunSmoke()
    {
        if(!smoke) return;
        try
        {
            if(++smokeFrames>120) throw new Exception("smoke timeout");
            void Assert(bool condition,string name) { if(!condition) throw new Exception(name); }
            void KeyPress(Key key)=>_UnhandledInput(new InputEventKey { Keycode=key,Pressed=true });
            void Click(MouseButton button,Vector3 point,bool shift=false)=>_UnhandledInput(new InputEventMouseButton { ButtonIndex=button,Pressed=true,Position=camera.UnprojectPosition(point),ShiftPressed=shift });
            switch(smokeStage++)
            {
                case 0: return; // Physics registration needs a frame.
                case 1:
                    Click(MouseButton.Left,formation.Position+Vector3.Up); Assert(selected,"selection raycast");
                    Click(MouseButton.Right,new Vector3(-15,Terrain.Height(-15,0),0)); Assert(battle.Pending.Count==1,"terrain move raycast"); break;
                case 2:
                    Assert(battle.Formation.Position.X>-20000,"live movement");
                    Click(MouseButton.Right,new Vector3(-25,Terrain.Height(-25,0),0)); Assert(battle.Pending.Count==1,"live reissue");
                    KeyPress(Key.Space); pausePosition=battle.Formation.Position; pauseTick=battle.Tick;
                    Click(MouseButton.Right,new Vector3(-18,Terrain.Height(-18,2),2));
                    Click(MouseButton.Right,new Vector3(-16,Terrain.Height(-16,4),4),true); break;
                case 3:
                    Assert(battle.Tick==pauseTick && battle.Formation.Position==pausePosition,"pause freeze");
                    Assert(battle.Pending.Count==3,"paused order queue");
                    KeyPress(Key.Key2); Assert(mode==1 && camera.Projection==Camera3D.ProjectionType.Perspective,"free camera");
                    var oldCamera=camera.Position;
                    _UnhandledInput(new InputEventMouseMotion { Relative=new(20,10),ButtonMask=MouseButtonMask.Middle });
                    Assert(camera.Position!=oldCamera,"free orbit");
                    float oldDistance=distance;
                    _UnhandledInput(new InputEventMouseButton { ButtonIndex=MouseButton.WheelUp,Pressed=true });
                    Assert(distance<oldDistance,"camera zoom");
                    KeyPress(Key.Key3); Assert(mode==2 && camera.Projection==Camera3D.ProjectionType.Orthogonal,"overhead camera");
                    KeyPress(Key.Key1); Assert(mode==0,"RTS camera"); KeyPress(Key.Space); break;
                case 4:
                    if(battle.Tick<=pauseTick) { smokeStage--; return; }
                    Assert(battle.Tick>pauseTick && battle.Pending.Count==0,"resume delivery");
                    GD.Print("PASS Godot integration: selection, terrain raycast move, live reissue, pause queue/resume, three cameras, orbit and zoom"); GetTree().Quit(0); break;
            }
        }
        catch(Exception e) { GD.PrintErr($"FAIL Godot smoke: {e}"); GetTree().Quit(1); }
    }
}
