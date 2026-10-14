using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using FantasyWarfare.Simulation;

public partial class BattleView : Node3D
{
    private Battle battle = null!;
    private readonly FormationSelection selected = new();
    private readonly Dictionary<int,FormationVisual> visuals = new();
    private Camera3D camera = null!;
    private CompactHud hud = null!;
    private readonly Dictionary<Position,TerrainKind> renderedTerrain = new();
    private SelectionOverlay selectionOverlay = null!;
    private int mode, eventCursor;
    private float yaw=0.65f, pitch=0.85f, distance=90, speed=1;
    private Vector3 focus=Vector3.Zero;
    private bool legacySmoke, tacticalSmoke, correctionsSmoke, drag, withdrawMode;
    private int trialIndex=-1;
    private long trialStartTick;
    private bool trialStarted,trialDemo,trialFinished;
    private ArrayMesh terrainMesh=null!;
        private Vector2 dragStart, dragEnd;
    private string message="Select a friendly formation, or Tab for all.";
    private int smokeStage, smokeFrames;
    private long pauseTick;
    private Position[] pausedPositions=Array.Empty<Position>();
    private double elapsedRemainder;
    private sealed class FormationVisual
    {
        public Node3D Root=null!;
        public MeshInstance3D Disk=null!, Intent=null!, RouteLine=null!;
        public StaticBody3D Body=null!;
        public Label3D Label=null!;
        public List<MeshInstance3D> Troops=new();
    }
    public override void _Ready()
    {
        legacySmoke=OS.GetCmdlineUserArgs().Contains("--smoke");
        tacticalSmoke=OS.GetCmdlineUserArgs().Contains("--gate1-smoke");
        correctionsSmoke=OS.GetCmdlineUserArgs().Contains("--corrections-smoke");
        trialDemo=OS.GetCmdlineUserArgs().Any(a=>a.StartsWith("--terrain-demo="));
        var trialArg=OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--terrain-trial=")||a.StartsWith("--terrain-demo="));
        if(trialArg is not null)trialIndex=int.Parse(trialArg.Split('=')[1]);
        battle=trialIndex>=0?TerrainTrials.Create(TerrainTrials.Cases[trialIndex]):legacySmoke?new Battle(42):new Battle(Scenarios.Skirmish(),42);
        if(legacySmoke||tacticalSmoke||correctionsSmoke||trialIndex>=0)
        {
            GetWindow().Size=new Vector2I(1280,720);
            GetWindow().ContentScaleSize=new Vector2I(1280,720);
            GetWindow().ContentScaleMode=Window.ContentScaleModeEnum.CanvasItems;
        }
        AddChild(new WorldEnvironment { Environment=new Godot.Environment {
            BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new Color("819aad"),
            AmbientLightSource=Godot.Environment.AmbientSource.Color,AmbientLightColor=Colors.White,AmbientLightEnergy=0.45f } });
        AddChild(new DirectionalLight3D { RotationDegrees=new(-55,-30,0),LightEnergy=0.8f,ShadowEnabled=true });
        BuildTerrain();
        foreach(var f in battle.Formations) BuildFormation(f);
        camera=new Camera3D { Far=400,Current=true };AddChild(camera);UpdateCamera();
        var ui=new CanvasLayer();AddChild(ui);
        hud=new CompactHud();ui.AddChild(hud);hud.Build(KeyPress);
        selectionOverlay=new SelectionOverlay { MouseFilter=Control.MouseFilterEnum.Ignore };ui.AddChild(selectionOverlay);
        SyncPresentation();
        GD.Print($"Gate 1 scene ready: {battle.OwnFormations(1).Count()} friendly formations; shared simulation, terrain and faction visibility.");
    }
    private float Height(float x,float z)=>battle.Map.Height(new((int)MathF.Round(x*1000),(int)MathF.Round(z*1000)))/1000f;
    private Vector3 World(Position p,float lift=0)=>new(p.X/1000f,Height(p.X/1000f,p.Z/1000f)+lift,p.Z/1000f);
    private MeshInstance3D AddMesh(Node parent,Mesh mesh,Vector3 position,Color color)
    {
        var material=new StandardMaterial3D { AlbedoColor=color };
        if(color.A<1) material.Transparency=BaseMaterial3D.TransparencyEnum.Alpha;
        var node=new MeshInstance3D { Mesh=mesh,Position=position,MaterialOverride=material };parent.AddChild(node);return node;
    }
    private static Color TerrainColor(TerrainCell cell)=>cell.Kind switch { TerrainKind.Forest=>new Color("477348"),TerrainKind.Road=>new Color("b8a073"),TerrainKind.Rock=>new Color("696974"),_=>cell.Height>2000?new Color("8b8760"):cell.Height<-1000?new Color("56764d"):new Color("77935e") };
    private void BuildTerrain()
    {
        var surface=new SurfaceTool();surface.Begin(Mesh.PrimitiveType.Triangles);
        void Vertex(int x,int z,TerrainCell cell)
        {
            var color=TerrainColor(cell);
            surface.SetColor(color);surface.AddVertex(new(x,Height(x,z),z));
        }
        for(int x=-50;x<50;x+=2)for(int z=-50;z<50;z+=2)
        {
            var center=new Position(x*1000+1000,z*1000+1000);var cell=battle.Map.Cell(center);renderedTerrain[center]=cell.Kind;
            Vertex(x,z,cell);Vertex(x+2,z,cell);Vertex(x,z+2,cell);Vertex(x+2,z,cell);Vertex(x+2,z+2,cell);Vertex(x,z+2,cell);
        }
        surface.GenerateNormals();terrainMesh=surface.Commit();var node=new MeshInstance3D { Mesh=terrainMesh,MaterialOverride=new StandardMaterial3D { VertexColorUseAsAlbedo=true } };AddChild(node);node.CreateTrimeshCollision();
        for(int x=0;x<50;x++)for(int z=0;z<50;z++)
        {
            var p=TerrainMap.Center(x,z);var cell=battle.Map.Cell(p);
            if(cell.Kind==TerrainKind.Forest && x%2==0 && z%2==0)
            {
                AddMesh(this,new CylinderMesh { TopRadius=0.2f,BottomRadius=0.3f,Height=2 },World(p,1),new Color("705438"));
                AddMesh(this,new CylinderMesh { TopRadius=0,BottomRadius=1.2f,Height=3 },World(p,3),new Color("254c32"));
            }
            if(cell.Kind==TerrainKind.Rock) AddMesh(this,new BoxMesh { Size=new(2,2.5f,2) },World(p,1.25f),new Color("666671"));
        }
        foreach(var (text,p) in new[]{("PLAIN",new Position(-29000,15000)),("RIDGE",new Position(12000,0)),("WOODS",new Position(-26000,-22000)),("VALLEY",new Position(-10000,26000)),("ROAD",new Position(0,-6000)),("ROCK: IMPASSABLE",new Position(2000,21000))})
            AddChild(new Label3D { Text=text,Position=World(p,2),FontSize=48,PixelSize=0.035f,Billboard=BaseMaterial3D.BillboardModeEnum.Enabled });
    }
    private void BuildFormation(Formation f)
    {
        var v=new FormationVisual { Root=new Node3D() };AddChild(v.Root);
        var color=f.Faction==1?new Color("477fe0"):new Color("ca5145");
        for(int i=0;i<Math.Min(24,f.InitialStrength);i++)
        {
            Mesh mesh=f.Kind switch { UnitKind.Cavalry=>new BoxMesh { Size=new(0.7f,1.3f,1.1f) },UnitKind.Archers=>new CylinderMesh { TopRadius=0.3f,BottomRadius=0.3f,Height=1.3f },UnitKind.Spearmen=>new BoxMesh { Size=new(0.35f,1.8f,0.35f) },_=>new BoxMesh { Size=new(0.65f,1.4f,0.65f) } };
            v.Troops.Add(AddMesh(v.Root,mesh,new((i%6-2.5f)*0.8f,0.7f,(i/6-1.5f)*0.8f),color));
        }
        AddMesh(v.Root,new BoxMesh { Size=new(0.25f,0.15f,1.8f) },new(0,0.25f,-3),Colors.White);
        v.Disk=AddMesh(v.Root,new CylinderMesh { TopRadius=3.3f,BottomRadius=3.3f,Height=0.08f },new(0,0.05f,0),new Color(1,0.8f,0.1f,0.5f));
        v.Body=new StaticBody3D { CollisionLayer=2,CollisionMask=0 };v.Body.SetMeta("formation_id",f.Id);v.Root.AddChild(v.Body);
        v.Body.AddChild(new CollisionShape3D { Shape=new BoxShape3D { Size=new(6,2.5f,4.5f) },Position=new(0,1,0) });
        v.Label=new Label3D { Position=new(0,3.5f,0),FontSize=40,PixelSize=0.025f,Billboard=BaseMaterial3D.BillboardModeEnum.Enabled };v.Root.AddChild(v.Label);
        v.Intent=AddMesh(this,new CylinderMesh { TopRadius=0.65f,BottomRadius=0.65f,Height=0.15f },Vector3.Zero,Colors.Orange);
        v.RouteLine=new MeshInstance3D { MaterialOverride=new StandardMaterial3D { AlbedoColor=Colors.Orange,ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded } };AddChild(v.RouteLine);
        visuals[f.Id]=v;
    }
    private void UpdateCamera()
    {
        camera.Projection=mode==2?Camera3D.ProjectionType.Orthogonal:Camera3D.ProjectionType.Perspective;camera.Size=distance;
        float p=mode==0?0.85f:mode==2?1.55f:pitch,y=mode==0?0.65f:mode==2?0:yaw;
        camera.Position=focus+new Vector3(MathF.Sin(y)*MathF.Cos(p),MathF.Sin(p),MathF.Cos(y)*MathF.Cos(p))*distance;camera.LookAt(focus,Vector3.Up);
    }
    private Godot.Collections.Dictionary Hit(Vector2 screen,uint mask)
    {
        var from=camera.ProjectRayOrigin(screen);
        var result=GetWorld3D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(from,from+camera.ProjectRayNormal(screen)*400,mask));
        // Trimesh vertex/edge precision can reject an exact pixel ray. Retry within a quarter pixel for terrain only.
        if(result.Count==0&&mask==1)
            foreach(var offset in new[]{new Vector2(0.2f,0),new Vector2(-0.2f,0),new Vector2(0,0.2f),new Vector2(0,-0.2f)})
            {
                var point=screen+offset;from=camera.ProjectRayOrigin(point);
                result=GetWorld3D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(from,from+camera.ProjectRayNormal(point)*400,mask));
                if(result.Count>0)break;
            }
        return result;
    }
    public override void _UnhandledInput(InputEvent e)
    {
        if(e is InputEventKey key && key.Pressed && !key.Echo)
        {
            if(key.Keycode==Key.Space) battle.SetPaused(!battle.Paused);
            if(key.Keycode>=Key.Key1 && key.Keycode<=Key.Key3) { mode=(int)key.Keycode-(int)Key.Key1;UpdateCamera(); }
            if(key.Keycode==Key.Tab) selected.SelectMany(battle.OwnFormations(1).Select(f=>f.Id),battle.OwnFormations(1));
            if(key.Keycode==Key.H) Issue(OrderKind.Hold,default);
            if(key.Keycode==Key.R) Issue(OrderKind.Rally,default);
            if(key.Keycode==Key.X) { withdrawMode=true;message="Withdraw: right-click a safe terrain destination."; }
            if(key.Keycode==Key.F1) hud.Visible=!hud.Visible;
            if(key.Keycode==Key.F2) hud.ToggleHelp();
            if(key.Keycode==Key.F3) hud.ToggleDetails();
            if(key.Keycode==Key.F9) GetTree().ReloadCurrentScene();
            if(key.Keycode==Key.Equal) speed=Math.Min(4,speed*2);
            if(key.Keycode==Key.Minus) speed=Math.Max(0.5f,speed/2);
        }
        if(e is InputEventMouseMotion motion)
        {
            if(drag) { dragEnd=motion.Position;selectionOverlay.Box=new Rect2(dragStart,dragEnd-dragStart).Abs();selectionOverlay.QueueRedraw(); }
            if((motion.ButtonMask & MouseButtonMask.Middle)!=0 && mode==1) { yaw-=motion.Relative.X*0.006f;pitch=Math.Clamp(pitch+motion.Relative.Y*0.006f,0.15f,1.5f);UpdateCamera(); }
        }
        if(e is not InputEventMouseButton mouse) return;
        if(mouse.ButtonIndex==MouseButton.Left)
        {
            if(mouse.Pressed)
            {
                drag=true;dragStart=dragEnd=mouse.Position;var hit=Hit(mouse.Position,2);
                if(hit.Count>0&&hit["collider"].AsGodotObject() is Node collider)
                    selected.Select(collider.GetMeta("formation_id").AsInt32(),battle.OwnFormations(1),mouse.ShiftPressed);
                else if(!mouse.ShiftPressed) selected.Clear();
            }
            else if(drag)
            {
                drag=false;selectionOverlay.Box=default;selectionOverlay.QueueRedraw();
                if(dragStart.DistanceTo(mouse.Position)>8)
                {
                    var rect=new Rect2(dragStart,mouse.Position-dragStart).Abs();
                    selected.SelectMany(battle.OwnFormations(1).Where(f=>!camera.IsPositionBehind(World(f.Position))&&rect.HasPoint(camera.UnprojectPosition(World(f.Position,1)))).Select(f=>f.Id),battle.OwnFormations(1),mouse.ShiftPressed);
                }
            }
            return;
        }
        if(!mouse.Pressed) return;
        if(mouse.ButtonIndex==MouseButton.WheelUp||mouse.ButtonIndex==MouseButton.WheelDown)
        { distance=Math.Clamp(distance+(mouse.ButtonIndex==MouseButton.WheelUp?-4:4),15,140);UpdateCamera();return; }
        if(mouse.ButtonIndex!=MouseButton.Right||selected.Ids.Length==0) return;
        var formationHit=Hit(mouse.Position,2);
        if(!withdrawMode&&formationHit.Count>0&&formationHit["collider"].AsGodotObject() is Node unit)
        {
            int id=unit.GetMeta("formation_id").AsInt32();var enemy=battle.Perception(1).Visible.FirstOrDefault(f=>f.Id==id);
            if(enemy is not null) { Issue(OrderKind.Attack,enemy.Position,id);return; }
        }
        var ground=Hit(mouse.Position,1);
        if(ground.Count==0) { message="Click navigable terrain.";return; }
        var point=ground["position"].AsVector3();var target=new Position((int)MathF.Round(point.X*1000),(int)MathF.Round(point.Z*1000));
        Issue(withdrawMode?OrderKind.Withdraw:OrderKind.Move,target,null,mouse.ShiftPressed);withdrawMode=false;
    }
    private void Issue(OrderKind kind,Position target,int? enemy=null,bool append=false)
    {
        var error=battle.IssueGroup(1,selected.Ids,kind,target,enemy,append);
        message=error??$"{kind} ordered for {selected.Ids.Length} formation(s).";
    }
    public override void _Process(double delta)
    {
        if(!battle.Paused)
        {
            elapsedRemainder+=delta*speed*TimeSpan.TicksPerSecond;long ticks=(long)elapsedRemainder;elapsedRemainder-=ticks;battle.Advance(TimeSpan.FromTicks(ticks));
        }
        float x=0,z=0;if(Input.IsPhysicalKeyPressed(Key.A))x--;if(Input.IsPhysicalKeyPressed(Key.D))x++;if(Input.IsPhysicalKeyPressed(Key.W))z--;if(Input.IsPhysicalKeyPressed(Key.S))z++;
        focus+=new Vector3(x,0,z)*(float)delta*25;focus.X=Math.Clamp(focus.X,-48,48);focus.Z=Math.Clamp(focus.Z,-48,48);
        UpdateCamera();SyncPresentation();if(legacySmoke) RunLegacySmoke();if(tacticalSmoke) RunTacticalSmoke();if(correctionsSmoke) RunCorrectionsSmoke();if(trialIndex>=0) RunTerrainTrial();
    }
    private void SyncPresentation()
    {
        selected.Prune(battle.OwnFormations(1));
        foreach(var f in battle.Formations)
        {
            var v=visuals[f.Id];bool show=f.Active&&(f.Faction==1||battle.Perception(1).IsVisible(f.Id));
            v.Root.Visible=show;v.Body.CollisionLayer=show?2u:0u;v.Root.Position=World(f.Position);v.Root.Rotation=new(0,-f.Facing*MathF.PI/1800,0);
            v.Disk.Visible=selected.Ids.Contains(f.Id);v.Label.Text=$"{f.Id} {f.Kind} | {f.Strength}\n{f.Status}";
            for(int i=0;i<v.Troops.Count;i++)v.Troops[i].Visible=i<Math.Ceiling((double)f.Strength/f.InitialStrength*v.Troops.Count);
            Position? intent=f.Faction==1?battle.PendingOrders.LastOrDefault(o=>o.FormationId==f.Id&&o.Kind is OrderKind.Move or OrderKind.Withdraw or OrderKind.Attack)?.Target??f.Destination:null;
            if(intent is null&&f.Faction==1&&f.CurrentOrder?.Kind==OrderKind.Attack) intent=battle.Perception(1).Visible.FirstOrDefault(e=>e.Id==f.CurrentOrder.TargetId)?.Position;
            v.Intent.Visible=show&&selected.Ids.Contains(f.Id)&&intent.HasValue;v.RouteLine.Visible=v.Intent.Visible;
            if(intent is Position target)
            {
                v.Intent.Position=World(target,0.25f);var line=new ImmediateMesh();line.SurfaceBegin(Mesh.PrimitiveType.Lines);
                var last=World(f.Position,0.5f);var route=f.PlannedRoute.Count>0?f.PlannedRoute:new[]{target};
                foreach(var p in route) { var next=World(p,0.5f);line.SurfaceAddVertex(last);line.SurfaceAddVertex(next);last=next; }
                line.SurfaceEnd();v.RouteLine.Mesh=line;
            }
        }
        while(eventCursor<battle.Events.Count)
        {
            var e=battle.Events[eventCursor++];
            if(e.Kind=="Volley"&&e.OtherId is int target && visuals[e.FormationId].Root.Visible&&visuals[target].Root.Visible)
            {
                var mesh=new ImmediateMesh();mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);mesh.SurfaceAddVertex(World(battle.GetFormation(e.FormationId).Position,2));mesh.SurfaceAddVertex(World(battle.GetFormation(target).Position,2));mesh.SurfaceEnd();
                var tracer=new MeshInstance3D { Mesh=mesh,MaterialOverride=new StandardMaterial3D { AlbedoColor=Colors.Yellow,ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded } };AddChild(tracer);
                GetTree().CreateTimer(0.3).Timeout+=()=> { if(GodotObject.IsInstanceValid(tracer))tracer.QueueFree(); };
            }
        }
        hud.Update(battle,selected.Ids,speed,mode,message,GetViewport().GetVisibleRect().Size);
    }

    private void Assert(bool condition,string name) { if(!condition)throw new Exception(name); }
    private void KeyPress(Key key)=>_UnhandledInput(new InputEventKey { Keycode=key,Pressed=true });
    private void Click(MouseButton button,Vector3 point,bool shift=false)
    {
        var screen=camera.UnprojectPosition(point);
        _UnhandledInput(new InputEventMouseButton { ButtonIndex=button,Pressed=true,Position=screen,ShiftPressed=shift });
        if(button==MouseButton.Left) _UnhandledInput(new InputEventMouseButton { ButtonIndex=button,Pressed=false,Position=screen,ShiftPressed=shift });
    }
    private void SmokeFail(Exception e) { GD.PrintErr($"FAIL Godot smoke: {e}");GetTree().Quit(1); }
    private void RunLegacySmoke()
    {
        try
        {
            if(++smokeFrames>120)throw new Exception("legacy smoke timeout");var f=battle.Formation;
            switch(smokeStage++)
            {
                case 0:return;
                case 1:Click(MouseButton.Left,World(f.Position,1));Assert(selected.Ids.SequenceEqual(new[]{1}),"selection raycast");Click(MouseButton.Right,World(new(-15000,0)));Assert(battle.Pending.Count==1,"terrain move raycast");break;
                case 2:Assert(f.Position.X>-20000,"live movement");Click(MouseButton.Right,World(new(-25000,0)));Assert(battle.Pending.Count==1,"live reissue");KeyPress(Key.Space);pauseTick=battle.Tick;pausedPositions=new[]{f.Position};Click(MouseButton.Right,World(new(-18000,2000)));Click(MouseButton.Right,World(new(-16000,4000)),true);break;
                case 3:Assert(battle.Tick==pauseTick&&f.Position==pausedPositions[0],"pause freeze");Assert(battle.Pending.Count==3,$"paused order queue count={battle.Pending.Count}, message={message}, paused={battle.Paused}, selected={string.Join(",",selected.Ids)}");CheckCameras();KeyPress(Key.Space);break;
                case 4:if(battle.Tick<=pauseTick){smokeStage--;return;}Assert(battle.Pending.Count==0,"resume delivery");GD.Print("PASS Godot integration: selection, terrain raycast move, live reissue, pause queue/resume, three cameras, orbit and zoom");GetTree().Quit(0);break;
            }
        }catch(Exception e){SmokeFail(e);}
    }
    private void CheckCameras()
    {
        KeyPress(Key.Key2);Assert(mode==1&&camera.Projection==Camera3D.ProjectionType.Perspective,"free camera");var old=camera.Position;
        _UnhandledInput(new InputEventMouseMotion { Relative=new(20,10),ButtonMask=MouseButtonMask.Middle });Assert(camera.Position!=old,"free orbit");float before=distance;
        _UnhandledInput(new InputEventMouseButton { ButtonIndex=MouseButton.WheelUp,Pressed=true });Assert(distance<before,"zoom");KeyPress(Key.Key3);Assert(mode==2&&camera.Projection==Camera3D.ProjectionType.Orthogonal,"overhead");KeyPress(Key.Key1);Assert(mode==0,"RTS");
    }
    private void RunTacticalSmoke()
    {
        try
        {
            if(++smokeFrames>180)throw new Exception("tactical smoke timeout");
            switch(smokeStage++)
            {
                case 0:return;
                case 1:
                    Assert(battle.Formations.Count==12,"multiple formations");Click(MouseButton.Left,World(battle.GetFormation(3).Position,1));Click(MouseButton.Left,World(battle.GetFormation(4).Position,1),true);
                    Assert(selected.Ids.SequenceEqual(new[]{3,4}),"multi-selection");
                    var a=camera.UnprojectPosition(World(battle.GetFormation(3).Position,1));var b=camera.UnprojectPosition(World(battle.GetFormation(4).Position,1));
                    var start=new Vector2(Math.Min(a.X,b.X)-4,Math.Min(a.Y,b.Y)-4);var end=new Vector2(Math.Max(a.X,b.X)+4,Math.Max(a.Y,b.Y)+4);
                    _UnhandledInput(new InputEventMouseButton { ButtonIndex=MouseButton.Left,Pressed=true,Position=start });
                    _UnhandledInput(new InputEventMouseMotion { Position=end,ButtonMask=MouseButtonMask.Left });
                    _UnhandledInput(new InputEventMouseButton { ButtonIndex=MouseButton.Left,Pressed=false,Position=end });
                    Assert(selected.Ids.SequenceEqual(new[]{3,4}),$"box-selection adapter ids={string.Join(",",selected.Ids)} start={start} end={end} a={a} b={b}");
                    Assert(battle.OwnFormations(2).All(f=>!visuals[f.Id].Root.Visible&&visuals[f.Id].Body.CollisionLayer==0),"concealed enemy render/picking leak");
                    Click(MouseButton.Right,World(new(-32000,-2500)));Assert(battle.PendingOrders.Count(o=>o.Faction==1)==2,"group terrain move");break;
                case 2:
                    Assert(selected.Ids.All(id=>battle.GetFormation(id).Position.X>-42000),"group movement");Click(MouseButton.Right,World(new(-40000,-2500)));KeyPress(Key.Space);
                    pauseTick=battle.Tick;pausedPositions=selected.Ids.Select(id=>battle.GetFormation(id).Position).ToArray();
                    Click(MouseButton.Right,World(new(-36000,-2500)));Click(MouseButton.Right,World(new(-34000,-2500)),true);break;
                case 3:
                    Assert(battle.Tick==pauseTick&&selected.Ids.Select(id=>battle.GetFormation(id).Position).SequenceEqual(pausedPositions),"tactical pause freeze");Assert(battle.PendingOrders.Count(o=>o.Faction==1)==6,"paused group queue");CheckCameras();KeyPress(Key.Space);break;
                case 4:
                    if(battle.Tick<=pauseTick){smokeStage--;return;}Assert(battle.PendingOrders.Count(o=>o.Faction==1)==0,"group resume delivery");KeyPress(Key.H);break;
                case 5:
                    if(battle.PendingOrders.Any(o=>o.Faction==1)){smokeStage--;return;}
                    Assert(selected.Ids.All(id=>battle.GetFormation(id).CurrentOrder?.Kind==OrderKind.Hold),"hold input");
                    KeyPress(Key.X);Click(MouseButton.Right,World(new(-41000,-2500)));Assert(battle.PendingOrders.Any(o=>o.Kind==OrderKind.Withdraw&&o.Faction==1),"withdraw input");
                    KeyPress(Key.R);Assert(battle.PendingOrders.Any(o=>o.Kind==OrderKind.Rally&&o.Faction==1),"rally input");
                    KeyPress(Key.Tab);Assert(selected.Ids.Length==6,"select all");
                    GD.Print("PASS Gate 1 Godot integration: 12 formations, click/shift/box-selection, group move/reissue, pause queue/resume, hold/withdraw/rally, hidden enemy render/pick boundary, cameras/orbit/zoom");GetTree().Quit(0);break;
            }
        }catch(Exception e){SmokeFail(e);}
    }
    private void RunCorrectionsSmoke()
    {
        try
        {
            if(++smokeFrames>180)throw new Exception("corrections smoke timeout");
            switch(smokeStage++)
            {
                case 0: return;
                case 1:
                    var colors=terrainMesh.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Color].AsColorArray();
                    int cellIndex=0;
                    for(int x=0;x<50;x++)for(int z=0;z<50;z++)
                    {
                        var expected=TerrainColor(battle.Map.Cell(TerrainMap.Center(x,z)));
                        for(int vertex=0;vertex<6;vertex++)Assert(colors[cellIndex*6+vertex].IsEqualApprox(expected),"rendered per-cell mesh colour mismatch");
                        cellIndex++;
                    }
                    Assert(renderedTerrain.Count==2500&&renderedTerrain.All(kv=>battle.Map.Cell(kv.Key).Kind==kv.Value),"rendered terrain classifications");
                    foreach(var trial in TerrainTrials.Cases)
                    {
                        foreach(var point in new[]{trial.Start,trial.Target})
                        {
                            var world=World(point);var from=world+Vector3.Up*20;
                            var hit=GetWorld3D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(from,world-Vector3.Up*20,1));
                            Assert(hit.Count>0&&Math.Abs(hit["position"].AsVector3().Y-world.Y)<0.002f,"terrain collision/simulation height agreement");
                        }
                    }
                    Assert(!hud.Details.Visible&&!hud.Help.Visible,"secondary panels default hidden");
                    Assert(hud.Bar.Size.Y<=80&&hud.Bar.Position.Y>=620,"compact bottom HUD");
                    Assert(!hud.Bar.GetGlobalRect().HasPoint(new(640,360)),"center covered by persistent HUD");
                    Click(MouseButton.Left,World(battle.GetFormation(3).Position,1));SyncPresentation();Assert(hud.Details.Visible,"selected details missing");
                    KeyPress(Key.F3);SyncPresentation();Assert(!hud.Details.Visible,"details collapse");KeyPress(Key.F3);
                    KeyPress(Key.F2);Assert(hud.Help.Visible,"help toggle");KeyPress(Key.F2);
                    KeyPress(Key.F1);Assert(!hud.Visible,"HUD hide");KeyPress(Key.F1);
                    Assert(hud.PauseButton.FocusMode==Control.FocusModeEnum.None&&hud.HoldButton.FocusMode==Control.FocusModeEnum.None,"buttons steal camera hotkeys");
                    hud.PauseButton.EmitSignal(Button.SignalName.Pressed);Assert(battle.Paused,"HUD pause command");pauseTick=battle.Tick;
                    Click(MouseButton.Right,World(new(-32000,-9000)));Assert(battle.PendingOrders.Any(o=>o.Faction==1),"selected terrain order with compact HUD");CheckCameras();break;
                case 2:
                    Assert(battle.Tick==pauseTick,"HUD tactical pause freeze");hud.PauseButton.EmitSignal(Button.SignalName.Pressed);break;
                case 3:
                    if(battle.Tick<=pauseTick){smokeStage--;return;}
                    Assert(battle.PendingOrders.All(o=>o.Faction!=1),"HUD resume order delivery");hud.HoldButton.EmitSignal(Button.SignalName.Pressed);break;
                case 4:
                    if(battle.PendingOrders.Any(o=>o.Faction==1)){smokeStage--;return;}
                    Assert(battle.GetFormation(3).CurrentOrder?.Kind==OrderKind.Hold,"HUD hold button");
                    GD.Print("PASS Corrections Godot integration: compact HUD, selected details/collapse/help, selection/order/cameras/pause/buttons, 2500 rendered terrain classes, collision heights");GetTree().Quit(0);break;
            }
        }catch(Exception e){SmokeFail(e);}
    }
    private void RunTerrainTrial()
    {
        try
        {
            if(trialFinished)return;
            var trial=TerrainTrials.Cases[trialIndex];
            if(!trialStarted)
            {
                // Run through the playable input adapter, rendered map, ordinary _Process and authoritative ticks.
                if(++smokeFrames<3)return;
                KeyPress(Key.Space);selected.Select(1,battle.OwnFormations(1));
                focus=World(trial.Start);KeyPress(Key.Key3);SyncPresentation();
                Click(MouseButton.Right,World(trial.Target));
                Assert(battle.PendingOrders.Count==1,"terrain trial right-click order");
                trialStartTick=battle.Tick;trialStarted=true;message=trialDemo?$"{trial.Name}: Space starts the measured run.":trial.Name;
                if(!trialDemo)KeyPress(Key.Space);return;
            }
            if(battle.Tick>2100)throw new Exception("playable terrain trial timeout");
            if(battle.Formation.Destination is not null||battle.PendingOrders.Count>0)return;
            Assert(FantasyWarfare.Simulation.Position.Distance(battle.Formation.Position,trial.Target)<=10,"terrain click target mismatch");
            var result=new TerrainTrials.Result(trial.Name,trial.Kind,battle.Formation.LastArrivalTick-trialStartTick,(int)battle.Formation.DistanceTravelled,battle.Map.Height(battle.Formation.Position)-battle.Map.Height(trial.Start),battle.Formation.LastArrivalFatigue);
            // Exclude startup ticks before the player order.
            var reference=TerrainTrials.Run(trial);Assert(result==reference,"playable/frame movement differs from simulation trial");
            GD.Print($"MEASURE playable {result}");GD.Print("PASS Playable terrain trial: "+trial.Name);
            if(trialDemo){trialFinished=true;battle.SetPaused(true);message=result.ToString()+" | F9 repeats";}else GetTree().Quit(0);
        }catch(Exception e){SmokeFail(e);}
    }

}
