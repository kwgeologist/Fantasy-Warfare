namespace FantasyWarfare.Simulation;

public sealed record PlannedCommand(int FormationId,OrderKind Kind,Position Target,int? TargetId=null);
public sealed record PlanDecision(long Tick,string Plan,IReadOnlyList<int> ObservedEnemies,string Reason,IReadOnlyList<PlannedCommand> Commands,IReadOnlyList<EnemyObservation> Beliefs);
/// <summary>Boundary: accepts only owned snapshots, observed contacts and public terrain. Never Battle or Formation.</summary>
public sealed class TacticalCommander
{
    private readonly Dictionary<int,(string Plan,long Tick,int? Target)> commitments=new();
    public IReadOnlyList<PlanDecision> Plan(ArmyKnowledge knowledge,TerrainMap map)
    {
        var decisions=new List<PlanDecision>();
        var enemies=knowledge.Enemies.Where(e=>e.CurrentlyVisible&&e.Strength>0).OrderBy(e=>e.Id).ToArray();
        foreach(var own in knowledge.Own.OrderBy(o=>o.Id))
        {
            if(own.Status is FormationStatus.Destroyed or FormationStatus.Rallying) continue;
            var nearby=enemies.Where(e=>Position.Distance(e.Position,own.Position)<=36000).ToArray();
            var threat=nearby.OrderBy(e=>Position.Distance(e.Position,own.Position)).FirstOrDefault();
            var meleeThreat=nearby.Where(e=>e.Kind!=UnitKind.Archers).OrderBy(e=>Position.Distance(e.Position,own.Position)).FirstOrDefault();
            string plan="Advance",reason="No current enemy contact; advance toward the public battlefield center.";
            var goal=new Position(own.Position.X<=-5000?-22000:-6000,Math.Clamp(own.Position.Z,-34000,34000));int? target=null;OrderKind kind=OrderKind.Move;
            var dangerousSpear=nearby.FirstOrDefault(e=>e.Kind==UnitKind.Spearmen&&e.Status!=FormationStatus.Routing&&
                Battle.FacingDifference(e.Facing,Battle.FacingTo(e.Position,own.Position))<700&&Position.Distance(e.Position,own.Position)<24000);
            var threatenedArcher=knowledge.Own.Where(o=>o.Kind==UnitKind.Archers&&o.Status!=FormationStatus.Routing)
                .SelectMany(a=>enemies.Where(e=>e.Kind!=UnitKind.Archers&&Position.Distance(e.Position,a.Position)<20000).Select(e=>(Archer:a,Enemy:e)))
                .OrderBy(pair=>Position.Distance(pair.Archer.Position,pair.Enemy.Position)).FirstOrDefault();
            if(own.Status==FormationStatus.Routing)
            {
                if(nearby.Any(e=>Position.Distance(e.Position,own.Position)<12000)) continue;
                plan="Rally";reason="No observed nearby threat; rally routed survivors without replacements.";goal=own.Position;kind=OrderKind.Rally;
            }
            else if(own.Morale<400||own.Fatigue>850)
            { plan="Regroup";reason="Own morale/fatigue is unsafe.";goal=threat is null?own.Position:Retreat(own.Position,threat.Position);kind=threat is null?OrderKind.Hold:OrderKind.Withdraw; }
            else if(own.Kind==UnitKind.Cavalry&&dangerousSpear is not null)
            { plan="AvoidSpears";reason=$"New/current observed spear formation {dangerousSpear.Id} presents a steady front.";goal=Retreat(own.Position,dangerousSpear.Position);kind=OrderKind.Withdraw; }
            else if(own.Kind==UnitKind.Archers&&own.Ammo==0)
            { plan="OutOfArrows";reason="Own archers have no ammunition; do not pretend ranged engagement remains viable.";goal=threat is null?own.Position:Retreat(own.Position,threat.Position);kind=threat is null?OrderKind.Hold:OrderKind.Withdraw; }
            else if(own.Kind==UnitKind.Archers&&meleeThreat is not null&&Position.Distance(own.Position,meleeThreat.Position)<14000)
            { plan="ProtectArchers";reason=$"Observed melee threat {meleeThreat.Id} is approaching vulnerable archers.";goal=Retreat(own.Position,meleeThreat.Position);kind=OrderKind.Withdraw; }
            else if(own.Kind is UnitKind.Infantry or UnitKind.Spearmen && threatenedArcher.Archer is not null)
            { plan="ScreenArchers";reason=$"Screen own archer {threatenedArcher.Archer.Id} from observed formation {threatenedArcher.Enemy.Id}.";
                goal=Position.Toward(threatenedArcher.Archer.Position,threatenedArcher.Enemy.Position,9000); }
            else if(threat is not null)
            {
                int strength=nearby.Where(e=>Position.Distance(e.Position,own.Position)<18000&&e.Status!=FormationStatus.Routing).Sum(e=>e.Strength);
                int support=knowledge.Own.Where(o=>o.Status!=FormationStatus.Routing&&Position.Distance(o.Position,own.Position)<18000).Sum(o=>o.Strength);
                if(strength>support*3/2 && own.Kind!=UnitKind.Archers)
                { plan="Regroup";reason="Observed local enemy strength exceeds available own support.";goal=Retreat(own.Position,threat.Position);kind=OrderKind.Withdraw; }
                else
                { plan=threat.Kind==UnitKind.Archers&&own.Kind!=UnitKind.Archers?"Exploit":"Engage";reason=$"Favorable supported engagement with observed formation {threat.Id}.";kind=OrderKind.Attack;goal=threat.Position;target=threat.Id; }
            }
            if(!map.CanOccupy(goal,own.Radius))
            {
                goal=own.Position;kind=OrderKind.Hold;target=null;reason+=" Destination blocked; hold rather than bypass terrain.";
            }
            bool emergency=plan is "AvoidSpears" or "ProtectArchers" or "Regroup" or "OutOfArrows";
            if(commitments.TryGetValue(own.Id,out var prior))
            {
                if(prior.Plan==plan&&prior.Target==target&&knowledge.Tick-prior.Tick<100) continue;
                if(!emergency&&knowledge.Tick-prior.Tick<60) continue;
            }
            commitments[own.Id]=(plan,knowledge.Tick,target);
            decisions.Add(new(knowledge.Tick,plan,enemies.Select(e=>e.Id).ToArray(),reason,[new(own.Id,kind,goal,target)],knowledge.Enemies.ToArray()));
        }
        return decisions;
    }
    private static Position Retreat(Position own,Position threat)
    {
        int dx=own.X-threat.X,dz=own.Z-threat.Z;
        if(dx==0&&dz==0) dx=1000;
        var p=Position.Toward(own,new(own.X+dx*20,own.Z+dz*20),12000);
        return new(Math.Clamp(p.X,-44000,44000),Math.Clamp(p.Z,-44000,44000));
    }
}
