namespace FantasyWarfare.Simulation;

/// <summary>Isolated movement trials on the playable skirmish map and its unchanged unit definitions.</summary>
public static class TerrainTrials
{
    public sealed record Trial(string Name,UnitKind Kind,Position Start,Position Target);
    public sealed record Result(string Name,UnitKind Kind,long Ticks,int Distance,int HeightGain,int Fatigue)
    {
        public double Seconds=>Ticks/20d;
        public double MetresPerSecond=>Distance/1000d/Seconds;
        public override string ToString()=>$"{Name}: {Ticks} ticks, {Seconds:F2} s, {Distance/1000d:F3} m, {MetresPerSecond:F3} m/s, climb {HeightGain/1000d:F3} m, fatigue {Fatigue}/1000";
    }
    public static readonly Trial[] Cases=[
        new("Infantry open",UnitKind.Infantry,new(-32000,-38000),new(-20000,-38000)),
        new("Infantry forest",UnitKind.Infantry,new(-32000,-24000),new(-20000,-24000)),
        new("Cavalry open",UnitKind.Cavalry,new(-32000,-38000),new(-20000,-38000)),
        new("Cavalry forest",UnitKind.Cavalry,new(-32000,-24000),new(-20000,-24000)),
        new("Infantry level",UnitKind.Infantry,new(-40000,0),new(-32000,0)),
        new("Infantry uphill",UnitKind.Infantry,new(2000,0),new(10000,0)),
        new("Infantry road",UnitKind.Infantry,new(-32000,-6000),new(-20000,-6000))];
    public static Battle Create(Trial trial,uint seed=42)
    {
        var playable=Scenarios.Skirmish();
        return new(new ScenarioDefinition(trial.Name,playable.Map,[new FormationSpec(1,1,trial.Kind,trial.Start)],false,false,true,false,playable.Units),seed);
    }
    public static Result Run(Trial trial)
    {
        var battle=Create(trial);var f=battle.Formation;int distance=0;
        var error=battle.IssueGroup(1,[1],OrderKind.Move,trial.Target);if(error is not null)throw new Exception(error);
        do
        {
            var before=f.Position;battle.Advance(TimeSpan.FromTicks(Battle.TickDuration));distance+=Position.Distance(before,f.Position);
            if(battle.Tick>2000)throw new Exception($"{trial.Name} failed to arrive: {f.CommandStatus}");
        }while(f.Position!=trial.Target||f.Destination is not null);
        return new(trial.Name,trial.Kind,battle.Tick,distance,battle.Map.Height(trial.Target)-battle.Map.Height(trial.Start),f.Fatigue);
    }
}
