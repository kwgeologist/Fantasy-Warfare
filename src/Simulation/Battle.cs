namespace FantasyWarfare.Simulation;

/// <summary>The same authoritative fixed-tick engine serves the regression and tactical scenarios.</summary>
public sealed partial class Battle
{
    public const int SchemaVersion=2;
    public const long TickDuration=TimeSpan.TicksPerSecond/20;
    public const int StepMillimetres=200;
    public uint Seed { get; }
    public long Tick { get; private set; }
    public bool Paused { get; private set; }
    public ScenarioDefinition Scenario { get; }
    public TerrainMap Map=>Scenario.Map;
    public Formation Formation=>formations[0];
    public IReadOnlyList<Formation> Formations=>formations.AsReadOnly();
    public IReadOnlyList<TacticalOrder> PendingOrders=>pending.AsReadOnly();
    public IReadOnlyList<MoveOrder> Pending=>pending.Where(o=>o.FormationId==Formation.Id).Select(o=>new MoveOrder(o.SchemaVersion,o.Id,o.DeliveryTick,o.Target,o.Append)).ToArray();
    public Position[] Waypoints=>Formation.QueuedDestinations.ToArray();
    public string? Outcome { get; private set; }
    public uint RandomState=>random.State;
    public IReadOnlyList<BattleEvent> Events=>events.AsReadOnly();
    public IReadOnlyList<PlanDecision> Decisions=>decisions.AsReadOnly();
    private readonly List<Formation> formations;
    private readonly List<TacticalOrder> pending=new();
    private readonly List<BattleEvent> events=new();
    private readonly List<PlanDecision> decisions=new();
    private readonly Dictionary<int,FactionPerception> perceptions=new();
    private readonly TacticalCommander commander=new();
    private readonly SeededRandom random;
    private readonly bool legacy;
    private long remainder,nextId;
    public Battle(uint seed=1):this(new ScenarioDefinition("Gate 0 regression",new TerrainMap(),
        [new FormationSpec(1,1,UnitKind.Infantry,new(-20000,0))],false,false,false,false),seed,true) { }
    public Battle(ScenarioDefinition scenario,uint seed=42):this(scenario,seed,false) { }
    private Battle(ScenarioDefinition scenario,uint seed,bool legacy)
    {
        if(scenario.Formations.Count==0 || scenario.Formations.Select(f=>f.Id).Distinct().Count()!=scenario.Formations.Count)
            throw new ArgumentException("Scenario needs unique, nonempty formation IDs.");
        if(scenario.Formations.Any(f=>f.Id<=0||f.Faction<1||f.Faction>2||f.Strength<=0||f.Strength>500||f.Morale<0||f.Morale>1000||!Enum.IsDefined(f.Kind)))
            throw new ArgumentException("Invalid formation definition.");
        Scenario=scenario;Seed=seed;this.legacy=legacy;random=new(seed);
        formations=scenario.Formations.OrderBy(s=>s.Id).Select(s=>new Formation(s)).ToList();
        if(formations.Any(f=>!Map.CanOccupy(f.Position,legacy?0:f.Definition.Radius))) throw new ArgumentException("Deployment intersects obstacle/map boundary.");
        perceptions[1]=new(1);perceptions[2]=new(2);UpdatePerceptions();
    }
    public IEnumerable<Formation> OwnFormations(int faction)=>formations.Where(f=>f.Faction==faction);
    public Formation GetFormation(int id)=>formations.Single(f=>f.Id==id);
    public FactionPerception Perception(int faction)=>perceptions[faction];
    public void SetPaused(bool value)=>Paused=value;
    public MoveOrder IssueMove(Position target,bool append=false,long? deliveryTick=null)
    {
        if(Math.Abs((long)target.X)>48000||Math.Abs((long)target.Z)>48000) throw new ArgumentOutOfRangeException(nameof(target));
        var order=IssueSingle(Formation.Faction,Formation.Id,OrderKind.Move,target,null,append,0,deliveryTick);
        return new(order.SchemaVersion,order.Id,order.DeliveryTick,order.Target,order.Append);
    }
    public string? IssueGroup(int faction,IEnumerable<int> ids,OrderKind kind,Position target,int? targetId=null,bool append=false,long? deliveryTick=null)
    {
        var selected=ids.Distinct().Order().Select(id=>formations.FirstOrDefault(f=>f.Id==id)).ToArray();
        if(selected.Length==0||selected.Any(f=>f is null||f.Faction!=faction||!f.Active)) return "Select living friendly formations.";
        var units=selected.Select(f=>f!).ToArray();
        if(kind!=OrderKind.Rally && units.Any(f=>f.Status==FormationStatus.Routing)) return "Routing formations need a safe rally order.";
        if(kind==OrderKind.Attack && (targetId is null||!perceptions[faction].IsVisible(targetId.Value))) return "Attack requires a currently observed enemy.";
        int cx=(int)units.Average(f=>f.Position.X),cz=(int)units.Average(f=>f.Position.Z);
        var destinations=units.Select(f=>kind is OrderKind.Move or OrderKind.Withdraw?
            new Position(target.X+f.Position.X-cx,target.Z+f.Position.Z-cz):kind==OrderKind.Attack?target:f.Position).ToArray();
        if(kind is OrderKind.Move or OrderKind.Withdraw && destinations.Where((p,i)=>!Map.CanOccupy(p,legacy?0:units[i].Definition.Radius)).Any())
            return "Group footprint intersects obstacle or map edge; choose another destination.";
        if(deliveryTick<=Tick) return "Delivery tick must be in the future.";
        int speed=units.Min(f=>f.Definition.Speed);
        for(int i=0;i<units.Length;i++) IssueSingle(faction,units[i].Id,kind,destinations[i],targetId,append,speed,deliveryTick);
        return null;
    }
    private TacticalOrder IssueSingle(int faction,int id,OrderKind kind,Position target,int? targetId,bool append,int groupSpeed,long? deliveryTick=null)
    {
        long delivery=deliveryTick??Tick+1;if(delivery<=Tick) throw new ArgumentOutOfRangeException(nameof(deliveryTick));
        var order=new TacticalOrder(SchemaVersion,++nextId,delivery,faction,id,kind,target,targetId,append,groupSpeed);
        pending.Add(order);return order;
    }
    public void Advance(TimeSpan elapsed)
    {
        if(elapsed.Ticks<0) throw new ArgumentOutOfRangeException(nameof(elapsed));
        if(Paused||Outcome is not null) return;
        remainder=checked(remainder+elapsed.Ticks);
        while(remainder>=TickDuration && Outcome is null) { remainder-=TickDuration;Step(); }
    }
    private void Step()
    {
        Tick++;
        foreach(var order in pending.Where(o=>o.DeliveryTick<=Tick).OrderBy(o=>o.DeliveryTick).ThenBy(o=>o.Id).ToArray()) Receive(order);
        pending.RemoveAll(o=>o.DeliveryTick<=Tick);
        UpdatePerceptions();
        if(Scenario.Commander && Tick%20==1) RunCommander();
        foreach(var f in formations) { f.MovedThisTick=false;f.EngagedThisTick=false;if(f.Active) Move(f); }
        UpdatePerceptions();
        if(Scenario.Combat) ResolveCombat();
        if(Scenario.Morale) UpdateMorale();
        UpdatePerceptions();
        if(Scenario.Victory)
        {
            bool one=formations.Any(f=>f.Faction==1&&f.Active&&f.Status!=FormationStatus.Routing);
            bool two=formations.Any(f=>f.Faction==2&&f.Active&&f.Status!=FormationStatus.Routing);
            if(!one||!two) { Outcome=one?"Blue victory":two?"Red victory":"Mutual collapse";events.Add(new(Tick,"Outcome",0,null,Outcome)); }
        }
    }
    private void Receive(TacticalOrder o)
    {
        var f=GetFormation(o.FormationId);
        if(!f.Active||f.Faction!=o.Faction||f.Status==FormationStatus.Routing&&o.Kind!=OrderKind.Rally) return;
        if(o.Kind==OrderKind.Attack && (o.TargetId is null||!perceptions[o.Faction].IsVisible(o.TargetId.Value))) { f.CommandStatus="Target lost before delivery";return; }
        if(o.Kind==OrderKind.Rally && perceptions[f.Faction].Visible.Any(e=>Position.Distance(e.Position,f.Position)<12000)) { f.CommandStatus="Cannot rally near observed enemy";return; }
        if(!o.Append || o.Kind is not OrderKind.Move) { f.QueuedDestinations.Clear();f.Route.Clear();f.Destination=null; }
        f.CurrentOrder=o;f.GroupSpeed=o.GroupSpeed;
        if(o.Kind==OrderKind.Hold) { f.Status=FormationStatus.Ready;f.CommandStatus="Holding"; }
        else if(o.Kind==OrderKind.Rally) { f.Status=FormationStatus.Rallying;f.CommandStatus="Rallying"; }
        else
        {
            f.Status=o.Kind==OrderKind.Withdraw?FormationStatus.Withdrawing:FormationStatus.Ready;
            f.QueuedDestinations.Enqueue(o.Target);
            if(f.Destination is null) BeginNextDestination(f);
        }
        events.Add(new(Tick,"OrderReceived",f.Id,o.TargetId,o.Kind.ToString()));
    }
    private void BeginNextDestination(Formation f)
    {
        if(f.QueuedDestinations.Count==0) return;
        var target=f.QueuedDestinations.Dequeue();f.Destination=target;
        f.Route=new(Navigation.FindPath(Map,f.Position,target,legacy?0:f.Definition.Radius,f.Kind));
        if(f.Route.Count==0) { f.Destination=null;f.CommandStatus="No footprint-safe route";events.Add(new(Tick,"NoPath",f.Id,null,"Destination unreachable")); }
        else f.CommandStatus=f.CurrentOrder?.Kind==OrderKind.Attack?"Advancing to observed target":"Moving";
    }
    private void Move(Formation f)
    {
        if(f.Status==FormationStatus.Rallying) return;
        if(f.CurrentOrder?.Kind==OrderKind.Attack && f.Status!=FormationStatus.Routing)
        {
            var target=perceptions[f.Faction].Visible.FirstOrDefault(e=>e.Id==f.CurrentOrder.TargetId);
            if(target is not null)
            {
                int range=f.Kind==UnitKind.Archers?f.Definition.Range-2000:f.Definition.Radius+target.Radius+1000;
                if(Position.Distance(f.Position,target.Position)<=range && Map.HasLineOfSight(f.Position,target.Position)) { f.Route.Clear();f.Destination=null;f.Facing=FacingTo(f.Position,target.Position);f.CommandStatus="Engaging";return; }
                if(f.Destination is null || Tick%20==0&&Position.Distance(f.Destination.Value,target.Position)>2000)
                { f.Route.Clear();f.QueuedDestinations.Clear();f.QueuedDestinations.Enqueue(target.Position);BeginNextDestination(f); }
            }
            else if(f.Destination is null) f.CommandStatus="Target lost — holding last known position";
        }
        if(f.Destination is null) BeginNextDestination(f);
        if(f.Route.Count==0) return;
        var waypoint=f.Route.Peek();
        int speed=f.GroupSpeed>0?Math.Min(f.GroupSpeed,f.Definition.Speed):f.Definition.Speed;
        int step=legacy?StepMillimetres:Math.Max(10,speed/20*Map.MovementPercent(f.Position,f.Kind,waypoint)/100*(1000-f.Fatigue/3)/1000);
        if(f.Status==FormationStatus.Routing) step=step*115/100;
        var next=Position.Toward(f.Position,waypoint,step);
        // Actual local contact cannot be bypassed; it reveals the encountered formation, not remote hidden units.
        var blocked=legacy?null:formations.FirstOrDefault(other=>other.Id!=f.Id&&other.Active&&Position.Distance(next,other.Position)<(f.Definition.Radius+other.Definition.Radius)*85/100);
        if(blocked is not null)
        {
            if(blocked.Faction!=f.Faction) { perceptions[f.Faction].Observe(blocked,Tick);perceptions[blocked.Faction].Observe(f,Tick); }
            f.CommandStatus="Blocked by formation";f.BlockedTicks++;
            // Local sidestep uses only own/observed contact; static obstacle clearance remains authoritative.
            if(f.BlockedTicks>20)
            {
                var side=new Position(f.Position.X+(f.Id%2==0?step:-step),f.Position.Z+(f.Id%2==0?-step:step));
                if(Map.CanOccupy(side,f.Definition.Radius)&&!formations.Any(o=>o.Id!=f.Id&&o.Active&&Position.Distance(side,o.Position)<(f.Definition.Radius+o.Definition.Radius)*85/100)) next=side;else return;
            }
            else return;
        }
        if(!Map.SegmentClear(f.Position,next,legacy?0:f.Definition.Radius)) { f.CommandStatus="Route blocked";return; }
        f.BlockedTicks=0;f.Facing=FacingTo(f.Position,waypoint);f.MovedThisTick=next!=f.Position;f.Position=next;
        if(next==waypoint)
        {
            f.Route.Dequeue();
            if(f.Route.Count==0) { f.Destination=null;f.CommandStatus="Arrived";if(f.Status==FormationStatus.Withdrawing) f.Status=FormationStatus.Ready; }
        }
    }
    public static int FacingTo(Position a,Position b)=>((int)Math.Round(Math.Atan2(b.X-a.X,-(b.Z-a.Z))*1800/Math.PI)+3600)%3600;
    public static int FacingDifference(int a,int b) { int d=Math.Abs(a-b)%3600;return Math.Min(d,3600-d); }
    public string Fingerprint()=>$"{Tick}|{random.State}|{Outcome}|"+string.Join(";",formations.Select(f=>$"{f.Id},{f.Position.X},{f.Position.Z},{f.Strength},{f.Ammunition},{f.Facing},{f.Fatigue},{f.Cohesion},{f.Morale},{f.Status},{f.CurrentOrder?.Kind},{f.Destination}"));
}
public static class Terrain
{
    public static float Height(float x,float z)=>7f*MathF.Exp(-MathF.Pow((x-12f)/8f,2))*MathF.Exp(-MathF.Pow(z/35f,2))
        -4f*MathF.Exp(-MathF.Pow((x+10f)/9f,2))*MathF.Exp(-MathF.Pow((z-22f)/12f,2));
}
