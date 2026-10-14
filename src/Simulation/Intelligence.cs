namespace FantasyWarfare.Simulation;

public sealed record EnemyObservation(int Id,UnitKind Kind,Position Position,int Strength,int Facing,int Radius,
    FormationStatus Status,long ObservedTick,bool CurrentlyVisible,int Confidence);
public sealed record OwnFormationView(int Id,UnitKind Kind,Position Position,int Strength,int Facing,int Radius,
    int Morale,int Fatigue,int Ammo,FormationStatus Status,OrderKind? Order,int? TargetId);
/// <summary>No live enemy references; stale contacts retain their last observation, never actual location.</summary>
public sealed class FactionPerception
{
    public int Faction { get; }
    private readonly Dictionary<int,EnemyObservation> contacts=new();
    public IReadOnlyList<EnemyObservation> Contacts=>contacts.Values.OrderBy(e=>e.Id).ToArray();
    public IReadOnlyList<EnemyObservation> Visible=>contacts.Values.Where(e=>e.CurrentlyVisible).OrderBy(e=>e.Id).ToArray();
    public bool IsVisible(int id)=>contacts.TryGetValue(id,out var contact)&&contact.CurrentlyVisible;
    public FactionPerception(int faction)=>Faction=faction;
    internal void BeginTick(long tick)
    {
        foreach(int id in contacts.Keys.ToArray())
        {
            var contact=contacts[id];long age=tick-contact.ObservedTick;
            if(age>200) contacts.Remove(id);
            else contacts[id]=contact with { CurrentlyVisible=false,Confidence=Math.Max(0,100-(int)age/2) };
        }
    }
    internal void Observe(Formation f,long tick)=>contacts[f.Id]=new(f.Id,f.Kind,f.Position,f.Strength,f.Facing,f.Definition.Radius,f.Status,tick,true,100);
}
public sealed record ArmyKnowledge(int Faction,long Tick,IReadOnlyList<OwnFormationView> Own,IReadOnlyList<EnemyObservation> Enemies);
public sealed partial class Battle
{
    private void UpdatePerceptions()
    {
        foreach(var p in perceptions.Values)
        {
            p.BeginTick(Tick);
            foreach(var enemy in formations.Where(f=>f.Faction!=p.Faction&&f.Active))
                if(formations.Where(f=>f.Faction==p.Faction&&f.Active).Any(observer=>
                    (Position.Distance(observer.Position,enemy.Position)<=observer.Definition.Radius+enemy.Definition.Radius+1200&&Map.SegmentClear(observer.Position,enemy.Position,0)) ||
                    (Position.Distance(observer.Position,enemy.Position)<=(Map.Cell(enemy.Position).Kind==TerrainKind.Forest&&Tick-enemy.LastShotTick>40?10000:36000)
                    &&Map.HasLineOfSight(observer.Position,enemy.Position)))) p.Observe(enemy,Tick);
        }
    }
    public ArmyKnowledge Knowledge(int faction)=>new(faction,Tick,
        OwnFormations(faction).Where(f=>f.Active).Select(f=>new OwnFormationView(f.Id,f.Kind,f.Position,f.Strength,f.Facing,f.Definition.Radius,f.Morale,f.Fatigue,f.Ammunition,f.Status,f.CurrentOrder?.Kind,f.CurrentOrder?.TargetId)).ToArray(),
        perceptions[faction].Contacts.ToArray());
    private void RunCommander()
    {
        var knowledge=Knowledge(2);
        foreach(var decision in commander.Plan(knowledge,Map))
        {
            decisions.Add(decision);
            foreach(var command in decision.Commands)
            {
                var f=GetFormation(command.FormationId);
                // Commitment and emergency replanning are handled by the knowledge-only planner.
                IssueSingle(2,f.Id,command.Kind,command.Target,command.TargetId,false,0);
            }
        }
    }
}
