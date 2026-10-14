using FantasyWarfare.Simulation;
using System.Text.Json;

public static class Gate1Tests
{
    public static void Run(Action<string,Action> Test,Action<bool,string> Check)
    {
        TimeSpan T(int ticks)=>TimeSpan.FromTicks(ticks*Battle.TickDuration);
        Battle Make(FormationSpec[] units,TerrainMap? map=null,bool combat=false,bool morale=false,bool ai=false,bool victory=false)=>
            new(new ScenarioDefinition("Test scenario",map??new(),units,combat,ai,morale,victory),123);
        FormationSpec Unit(int id,int faction,int x,int z,UnitKind kind=UnitKind.Infantry,int strength=24,int facing=900,int morale=900)=>new(id,faction,kind,new(x,z),strength,facing,morale);
        Test("Gate 1 twelve identified formations and four unit classes",()=>
        {
            var b=new Battle(Scenarios.Skirmish());Check(b.Formations.Count==12,"formation count");
            Check(b.OwnFormations(1).Count()==6&&b.OwnFormations(2).Count()==6,"army counts");
            Check(b.Formations.Select(f=>f.Kind).Distinct().Count()==4,"types");
            Check(b.Formations.Select(f=>f.Id).Distinct().Count()==12,"identity");
            Check(UnitDefinition.For(UnitKind.Cavalry).Speed>UnitDefinition.For(UnitKind.Infantry).Speed,"cavalry speed");
        });
        Test("selection toggle rectangle and enemy boundary",()=>
        {
            var b=Make([Unit(1,1,-30000,-10000),Unit(2,1,-30000,10000),Unit(3,2,30000,0)]);var s=new FormationSelection();
            s.Select(3,b.OwnFormations(1));Check(s.Ids.Length==0,"enemy selected");s.Select(1,b.OwnFormations(1));s.Select(2,b.OwnFormations(1),true);
            Check(s.Ids.SequenceEqual(new[]{1,2}),"multi selection");s.Select(1,b.OwnFormations(1),true);Check(s.Ids.SequenceEqual(new[]{2}),"toggle");
            s.SelectMany([1,2,3],b.OwnFormations(1));Check(s.Ids.SequenceEqual(new[]{1,2}),"rectangle leaks enemy");
        });
        Test("group moves preserve offsets and use common pace",()=>
        {
            var b=Make([Unit(1,1,-30000,-10000),Unit(2,1,-30000,10000,UnitKind.Cavalry)]);
            Check(b.IssueGroup(1,[1,2],OrderKind.Move,new(-10000,0)) is null,"group rejected");b.Advance(T(30));
            Check(b.GetFormation(1).Destination==new Position(-10000,-10000)&&b.GetFormation(2).Destination==new Position(-10000,10000),"offset destinations");
            Check(b.GetFormation(1).Position.X==b.GetFormation(2).Position.X,"common speed cap");
        });
        Test("paused group replacement and waypoint receipt",()=>
        {
            var b=Make([Unit(1,1,-30000,-10000),Unit(2,1,-30000,10000)]);b.SetPaused(true);
            b.IssueGroup(1,[1,2],OrderKind.Move,new(-25000,0));b.IssueGroup(1,[1,2],OrderKind.Move,new(-20000,0));b.IssueGroup(1,[1,2],OrderKind.Move,new(-10000,0),append:true);
            b.Advance(T(80));Check(b.Tick==0&&b.PendingOrders.Count==6,"paused queue");b.SetPaused(false);b.Advance(T(1));
            Check(b.GetFormation(1).Destination==new Position(-20000,-10000)&&b.GetFormation(2).Destination==new Position(-20000,10000)&&b.PendingOrders.Count==0,"delivery order");
        });
        Test("invalid group destination is atomic",()=>
        {
            var b=Make([Unit(1,1,-30000,-10000),Unit(2,1,-30000,10000)]);
            Check(b.IssueGroup(1,[1,2],OrderKind.Move,new(0,45000)) is not null&&b.PendingOrders.Count==0,"partial enqueue");
            Check(b.IssueGroup(2,[1],OrderKind.Move,new(0,0)) is not null,"ownership");
        });
        Test("forest roads open terrain and cavalry speed consequences",()=>
        {
            int Run(TerrainKind terrain,UnitKind kind) { var b=Make([Unit(1,1,-30000,0,kind)],new(p=>new(terrain,0)),morale:true);b.IssueMove(new(20000,0));b.Advance(T(100));return b.Formation.Position.X+30000; }
            int open=Run(TerrainKind.Open,UnitKind.Infantry),wood=Run(TerrainKind.Forest,UnitKind.Infantry),road=Run(TerrainKind.Road,UnitKind.Infantry);
            Check(road>open&&open>wood,"terrain speeds");Check(Run(TerrainKind.Forest,UnitKind.Cavalry)<Run(TerrainKind.Open,UnitKind.Cavalry),"cavalry woods");
        });
        Test("uphill slope slows actual movement",()=>
        {
            var flat=Make([Unit(1,1,-30000,0)]);var slope=Make([Unit(1,1,-30000,0)],new(p=>new(TerrainKind.Open,p.X/2)));
            flat.IssueMove(new(0,0));slope.IssueMove(new(0,0));flat.Advance(T(80));slope.Advance(T(80));Check(slope.Formation.Position.X<flat.Formation.Position.X,"slope ignored");
        });
        Test("footprint path routes around rock without intersection",()=>
        {
            var map=new TerrainMap(p=>new(Math.Abs(p.X)<3000&&Math.Abs(p.Z)<16000?TerrainKind.Rock:TerrainKind.Open,0));
            var path=Navigation.FindPath(map,new(-30000,0),new(30000,0),3000,UnitKind.Infantry);Check(path.Length>1,"no detour");
            var previous=new Position(-30000,0);foreach(var p in path) { Check(map.SegmentClear(previous,p,3000),"unsafe segment");previous=p; }
            Check(path[^1]==new Position(30000,0)&&path.Any(p=>Math.Abs(p.Z)>18000),"wrong route");
            var b=Make([Unit(1,1,-30000,0)],map);b.IssueMove(new(30000,0));for(int i=0;i<600;i++){b.Advance(T(1));Check(map.CanOccupy(b.Formation.Position,3000),"moved through obstacle");}
        });
        Test("narrow gap accepts point scout but rejects formation footprint",()=>
        {
            var map=new TerrainMap(p=>new(Math.Abs(p.X)<3000&&Math.Abs(p.Z)>2000?TerrainKind.Rock:TerrainKind.Open,0));
            Check(Navigation.FindPath(map,new(-30000,0),new(30000,0),500,UnitKind.Infantry).Length>0,"small passage");
            Check(Navigation.FindPath(map,new(-30000,0),new(30000,0),3000,UnitKind.Infantry).Length==0,"formation threaded blocked gap");
        });
        Test("ridge and dense forest obstruct functional LOS",()=>
        {
            Check(!TerrainMap.Battlefield().HasLineOfSight(new(-5000,0),new(30000,0)),"ridge visibility");
            var woods=new TerrainMap(p=>new(Math.Abs(p.X)<6000?TerrainKind.Forest:TerrainKind.Open,0));
            Check(!woods.HasLineOfSight(new(-15000,0),new(15000,0)),"forest visibility");Check(new TerrainMap().HasLineOfSight(new(-15000,0),new(15000,0)),"open visibility");
        });
        Test("concealed enemy cannot be targeted",()=>
        {
            var b=new Battle(Scenarios.Surprise());Check(!b.Perception(2).IsVisible(2),"hidden reserve revealed");
            Check(b.IssueGroup(2,[101],OrderKind.Attack,new(22000,14000),2) is not null&&b.PendingOrders.Count==0,"hidden attack accepted");
        });
        Test("melee inflicts simultaneous casualties and morale loss",()=>
        {
            var b=Make([Unit(1,1,-3000,0),Unit(2,2,3000,0,facing:2700)],combat:true,morale:true);b.Advance(T(60));
            Check(b.GetFormation(1).Casualties>0&&b.GetFormation(2).Casualties>0,"melee missing");
            Check(b.GetFormation(1).Morale<900&&b.GetFormation(2).Morale<900,"casualty morale");
            Check(b.Events.Any(e=>e.Kind=="Melee"),"telemetry");
        });
        Test("ranged fire costs ammunition and inflicts casualties",()=>
        {
            var b=Make([Unit(1,1,0,0,UnitKind.Archers),Unit(2,2,18000,0)],combat:true);b.Advance(T(80));
            Check(b.Formation.Ammunition==28&&b.GetFormation(2).Casualties>0,"volley mechanics");
        });
        Test("range and LOS prevent illegitimate ranged damage",()=>
        {
            var far=Make([Unit(1,1,-20000,0,UnitKind.Archers),Unit(2,2,20000,0)],combat:true);far.Advance(T(80));
            Check(far.Formation.Ammunition==30&&far.GetFormation(2).Casualties==0,"range");
            var map=new TerrainMap(p=>new(Math.Abs(p.X)<6000?TerrainKind.Forest:TerrainKind.Open,0));
            var blocked=Make([Unit(1,1,-13000,0,UnitKind.Archers),Unit(2,2,13000,0)],map,combat:true);blocked.Advance(T(80));
            Check(blocked.Formation.Ammunition==30&&blocked.GetFormation(2).Casualties==0,"shooting through woods");
        });
        Test("ammunition exhaustion prevents later volleys",()=>
        {
            var b=Make([Unit(1,1,0,0,UnitKind.Archers),Unit(2,2,18000,0,strength:500)],combat:true);b.Advance(T(1200));int loss=b.GetFormation(2).Casualties;
            Check(b.Formation.Ammunition==0,"ammo not spent");b.Advance(T(100));Check(b.GetFormation(2).Casualties==loss,"free arrows");
        });
        Test("firing exposes a forest-edge archer under documented spotting",()=>
        {
            var map=new TerrainMap(p=>new(p.X>=0&&p.X<2000?TerrainKind.Forest:TerrainKind.Open,0));
            var b=Make([Unit(1,1,1000,1000,UnitKind.Archers),Unit(2,2,15000,1000,UnitKind.Archers)],map,combat:true);
            Check(!b.Perception(2).IsVisible(1),"concealment absent");b.Advance(T(2));Check(b.Perception(2).IsVisible(1)&&b.Formation.Ammunition==29,"firing did not expose");
        });
        Test("spear front discourages cavalry and rear exposure increases damage",()=>
        {
            var front=Make([Unit(1,1,-3500,0,UnitKind.Cavalry),Unit(2,2,3500,0,UnitKind.Spearmen,facing:2700)]);
            var rear=Make([Unit(1,1,-3500,0,UnitKind.Cavalry),Unit(2,2,3500,0,UnitKind.Spearmen,facing:900)]);
            Check(CombatRules.Power(rear.Formation,rear.GetFormation(2),rear.Map,false)>CombatRules.Power(front.Formation,front.GetFormation(2),front.Map,false)*2,"front/rear factors");
        });
        Test("fatigue builds while moving and recovers while holding",()=>
        {
            var b=Make([Unit(1,1,-30000,0)],morale:true);b.IssueMove(new(30000,0));b.Advance(T(100));Check(b.Formation.Fatigue>=90,"no fatigue");
            b.IssueGroup(1,[1],OrderKind.Hold,b.Formation.Position);b.Advance(T(80));Check(b.Formation.Fatigue==0,"no recovery");
        });
        Test("woods reduce cohesion while formation moves",()=>
        {
            var b=Make([Unit(1,1,-30000,0)],new(p=>new(TerrainKind.Forest,0)),morale:true);b.IssueMove(new(30000,0));b.Advance(T(100));Check(b.Formation.Cohesion<950,"woods cohesion");
        });
        Test("controlled withdrawal is distinct from a rout",()=>
        {
            var b=Make([Unit(1,1,-20000,0)],morale:true);b.IssueGroup(1,[1],OrderKind.Withdraw,new(-30000,0));b.Advance(T(30));
            Check(b.Formation.Status==FormationStatus.Withdrawing&&b.Formation.Position.X<-20000&&!b.Events.Any(e=>e.Kind=="Rout"),"withdrawal became rout");
        });
        Test("rout panic affects nearby friendlies and safe rally restores survivors",()=>
        {
            var b=Make([Unit(1,1,-20000,0,morale:100),Unit(2,1,-20000,10000)],morale:true);b.Advance(T(20));
            Check(b.Formation.Status==FormationStatus.Routing&&b.GetFormation(2).Morale<900,"panic");int strength=b.Formation.Strength;
            b.IssueGroup(1,[1],OrderKind.Rally,b.Formation.Position);b.Advance(T(120));
            Check(b.Formation.Status==FormationStatus.Ready&&b.Formation.Morale>=600&&b.Formation.Strength==strength,"rally or free troops");
        });
        Test("rally near a known enemy is rejected at delivery",()=>
        {
            var b=Make([Unit(1,1,0,0,morale:100),Unit(2,2,10000,0)],morale:true);b.Advance(T(1));b.IssueGroup(1,[1],OrderKind.Rally,new(0,0));b.Advance(T(1));
            Check(b.Formation.Status==FormationStatus.Routing&&b.Formation.CommandStatus.Contains("Cannot rally"),"unsafe rally");
        });
        Test("commander API accepts observations not authoritative battle state",()=>
        {
            var method=typeof(TacticalCommander).GetMethod("Plan")!;
            Check(method.GetParameters().Select(p=>p.ParameterType).SequenceEqual(new[]{typeof(ArmyKnowledge),typeof(TerrainMap)}),"planner boundary");
            Check(typeof(EnemyObservation).GetProperties().All(p=>p.PropertyType!=typeof(Formation)&&p.PropertyType!=typeof(Battle)),"live enemy reference");
        });
        Test("hidden enemy differences cannot change commander decision",()=>
        {
            var scenario=Scenarios.Surprise();var a=new Battle(scenario);var units=scenario.Formations.Select(f=>f.Id==2?f with {Position=new(24000,16000),Strength=100}:f).ToArray();
            var b=new Battle(scenario with {Formations=units});
            var da=new TacticalCommander().Plan(a.Knowledge(2),a.Map);var db=new TacticalCommander().Plan(b.Knowledge(2),b.Map);
            Check(JsonSerializer.Serialize(da)==JsonSerializer.Serialize(db),"hidden state influenced decision");
            Check(da.All(d=>!d.ObservedEnemies.Contains(2)),"hidden ID leaked");
        });
        Test("commander avoids frontal cavalry attack on known spearmen",()=>
        {
            var b=Make([Unit(101,2,0,0,UnitKind.Cavalry),Unit(1,1,14000,0,UnitKind.Spearmen,facing:2700)]);
            var d=new TacticalCommander().Plan(b.Knowledge(2),b.Map);Check(d.Any(p=>p.Plan=="AvoidSpears"&&p.Commands[0].Kind==OrderKind.Withdraw),"suicidal attack");
        });
        Test("commander withdraws threatened archers and assigns a screen",()=>
        {
            var b=Make([Unit(101,2,0,0,UnitKind.Archers),Unit(102,2,0,10000),Unit(1,1,10000,0,UnitKind.Cavalry,facing:2700)]);
            var d=new TacticalCommander().Plan(b.Knowledge(2),b.Map);Check(d.Any(p=>p.Plan=="ProtectArchers")&&d.Any(p=>p.Plan=="ScreenArchers"),"no ranged protection");
        });
        Test("unexpected enemy discovery changes actual commander plan",()=>
        {
            var b=new Battle(Scenarios.Surprise(),77);b.IssueGroup(1,[2],OrderKind.Move,new(8000,8000),deliveryTick:40);b.Advance(T(500));
            var before=b.Decisions.FirstOrDefault(d=>d.Plan=="Exploit");var after=b.Decisions.FirstOrDefault(d=>d.Plan=="AvoidSpears"&&d.ObservedEnemies.Contains(2));
            Check(before is not null&&!before.ObservedEnemies.Contains(2)&&after is not null&&after.Tick>before.Tick,"no evidence-driven plan change");
            Check(b.Events.Any(e=>e.Kind=="OrderReceived"&&e.FormationId==101&&e.Detail=="Withdraw"),"changed plan did not produce executed order");
            Console.WriteLine($"SCENARIO surprise: {before?.Plan}@{before?.Tick} -> {after?.Plan}@{after?.Tick}; observed reserve=2");
        });
        Test("stale intelligence retains last seen position rather than tracking hidden unit",()=>
        {
            var map=new TerrainMap(p=>new(p.X>=18000?TerrainKind.Forest:TerrainKind.Open,0));
            var b=Make([Unit(101,2,0,0),Unit(1,1,14000,0,UnitKind.Cavalry)],map);
            var captured=b.Knowledge(2).Enemies.Single();b.IssueGroup(1,[1],OrderKind.Move,new(30000,0));b.Advance(T(40));
            var stale=b.Perception(2).Contacts.Single();Check(!stale.CurrentlyVisible&&stale.Position!=b.GetFormation(1).Position,"omniscient stale update");
            Check(captured.Position==new Position(14000,0),"snapshot mutated");
        });
        Test("combat outcome and AI are frame-independent for fixed seed and inputs",()=>
        {
            Battle Run(int[] frames,uint seed) {
                var b=new Battle(new ScenarioDefinition("Determinism",new TerrainMap(),[
                    Unit(1,1,-10000,-5000),Unit(2,1,-22000,6000,UnitKind.Archers),
                    Unit(101,2,10000,-5000),Unit(102,2,22000,6000,UnitKind.Archers)],true,true,true,false),seed);
                b.IssueGroup(1,[1],OrderKind.Attack,new(10000,-5000),101,deliveryTick:1);
                b.IssueGroup(1,[2],OrderKind.Move,new(-6000,6000),deliveryTick:50);
                foreach(int ms in frames)b.Advance(TimeSpan.FromMilliseconds(ms));return b;
            }
            var a=Run(Enumerable.Repeat(10,2000).ToArray(),42);var b=Run(Enumerable.Repeat(125,160).ToArray(),42);var c=Run(Enumerable.Repeat(50,400).ToArray(),42);
            Check(a.Fingerprint()==b.Fingerprint()&&a.Fingerprint()==c.Fingerprint(),"frame-dependent battle");
            Check(a.Events.SequenceEqual(b.Events)&&JsonSerializer.Serialize(a.Decisions)==JsonSerializer.Serialize(b.Decisions),"frame-dependent events/AI");
            Check(a.Events.Any(e=>e.Kind=="Casualty")&&a.Decisions.Count>0,"scenario never exercised combat/AI");
            var other=Run(Enumerable.Repeat(50,400).ToArray(),43);Check(a.RandomState!=other.RandomState,"seed unused");
            Console.WriteLine($"SCENARIO battle: casualties={a.Formations.Sum(f=>f.Casualties)}, AI decisions={a.Decisions.Count}, RNG={a.RandomState}");
        });
        Test("forest contact reveals opponents and permits melee, not distant arrows",()=>
        {
            var b=Make([Unit(1,1,-3000,0),Unit(2,2,3000,0,facing:2700)],new(p=>new(TerrainKind.Forest,0)),combat:true);
            Check(b.Perception(1).IsVisible(2)&&b.Perception(2).IsVisible(1),"contact concealed");b.Advance(T(40));Check(b.Formation.Casualties>0&&b.GetFormation(2).Casualties>0,"forest melee blocked");
        });
        Test("flanking causes morale and cohesion pressure without casualties",()=>
        {
            var b=Make([Unit(1,1,0,0),Unit(2,2,-10000,0)],morale:true);b.Advance(T(20));
            Check(b.Formation.Casualties==0&&b.Formation.Morale<900&&b.Formation.Cohesion<1000,"flank pressure absent");
        });
        Test("default playable skirmish actually reaches combat with opposing commander",()=>
        {
            var b=new Battle(Scenarios.Skirmish(),42);b.Advance(T(3200));
            Check(b.Events.Any(e=>e.Kind=="Casualty")&&b.Decisions.Count>0,"default scenario never fought");
            Console.WriteLine($"SCENARIO default: tick={b.Tick}, casualties={b.Formations.Sum(f=>f.Casualties)}, plans={b.Decisions.Count}, outcome={b.Outcome??"ongoing"}");
        });
        Test("versioned scenario data drives the runtime roster and weapons",()=>
        {
            var scenario=Scenarios.Skirmish();var b=new Battle(scenario);
            Check(scenario.Units?.Count==4&&b.Formations.Count==12,"data not applied");
            Check(b.GetFormation(2).Definition.Weapon==WeaponKind.Spear&&b.GetFormation(3).Definition.Weapon==WeaponKind.Bow,"weapons");
        });
        Test("malformed or unsupported scenario data is rejected",()=>
        {
            bool rejected=false;try{ScenarioCodec.Parse("{\"schemaVersion\":3,\"name\":\"bad\",\"terrain\":\"flat\",\"formations\":[],\"units\":[]}");}catch(FormatException){rejected=true;}
            Check(rejected,"unsupported schema accepted");
        });
        Test("archer commander respects its own exhausted ammunition",()=>
        {
            var scenario=new ScenarioDefinition("Empty arrows",new TerrainMap(),[Unit(101,2,0,0,UnitKind.Archers),Unit(1,1,18000,0)],false,false,false,false,
                new Dictionary<UnitKind,UnitDefinition>{{UnitKind.Archers,UnitDefinition.For(UnitKind.Archers) with {Ammo=0}}});
            var b=new Battle(scenario);var plan=new TacticalCommander().Plan(b.Knowledge(2),b.Map);
            Check(plan.Any(p=>p.Plan=="OutOfArrows"&&p.Commands[0].Kind!=OrderKind.Attack),"imaginary ammunition");
        });
        Test("victory follows actual defeat or army-wide routing",()=>
        {
            var b=Make([Unit(1,1,-3000,0,strength:100),Unit(2,2,3000,0,strength:1,facing:2700)],combat:true,morale:true,victory:true);b.Advance(T(100));
            Check(b.Outcome=="Blue victory"&&b.GetFormation(2).Strength==0,"victory absent");
            Check(b.IssueGroup(1,[1],OrderKind.Move,new(0,0)) is not null,"accepted ineffective post-battle orders");
        });
    }
}
