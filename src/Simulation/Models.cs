namespace FantasyWarfare.Simulation;

public readonly record struct Position(int X, int Z)
{
    public static long DistanceSquared(Position a, Position b) { long x=(long)a.X-b.X, z=(long)a.Z-b.Z; return x*x+z*z; }
    public static int Distance(Position a, Position b) => (int)Math.Sqrt(DistanceSquared(a,b));
    public static Position Toward(Position from, Position to, int distance)
    {
        long dx=(long)to.X-from.X, dz=(long)to.Z-from.Z;
        double length=Math.Sqrt(dx*dx+dz*dz);
        return length<=distance ? to : new(from.X+(int)Math.Round(dx*distance/length),from.Z+(int)Math.Round(dz*distance/length));
    }
}
public enum WeaponKind { Sword, Spear, Bow, Lance }
public enum UnitKind { Infantry, Spearmen, Archers, Cavalry }
public enum FormationStatus { Ready, Withdrawing, Routing, Rallying, Destroyed }
public enum OrderKind { Move, Attack, Hold, Withdraw, Rally }
public sealed record UnitDefinition(UnitKind Kind, int Speed, int Radius, int MeleePower, int Armor, int Range, int Ammo, int Discipline)
{
    public WeaponKind Weapon=>Kind switch { UnitKind.Spearmen=>WeaponKind.Spear,UnitKind.Archers=>WeaponKind.Bow,UnitKind.Cavalry=>WeaponKind.Lance,_=>WeaponKind.Sword };
    public static UnitDefinition For(UnitKind kind) => kind switch {
        UnitKind.Spearmen=>new(kind,3400,3000,65,40,0,0,80),
        UnitKind.Archers=>new(kind,3800,3000,25,15,28000,30,65),
        UnitKind.Cavalry=>new(kind,6500,3400,100,35,0,0,70),
        _=>new(kind,4000,3000,75,45,0,0,80) };
}
public readonly record struct MoveOrder(int SchemaVersion, long Id, long DeliveryTick, Position Target, bool Append);
public sealed record FormationSpec(int Id, int Faction, UnitKind Kind, Position Position, int Strength=24, int Facing=900, int Morale=900);
public sealed record ScenarioDefinition(string Name, TerrainMap Map, IReadOnlyList<FormationSpec> Formations,
    bool Combat=true, bool Commander=true, bool Morale=true, bool Victory=true, IReadOnlyDictionary<UnitKind,UnitDefinition>? Units=null);
public sealed record TacticalOrder(int SchemaVersion, long Id, long DeliveryTick, int Faction, int FormationId,
    OrderKind Kind, Position Target, int? TargetId=null, bool Append=false, int GroupSpeed=0);
public sealed class Formation
{
    public int Id { get; }
    public int Faction { get; }
    public UnitDefinition Definition { get; }
    public UnitKind Kind => Definition.Kind;
    public int InitialStrength { get; }
    public int Strength { get; internal set; }
    public int Casualties => InitialStrength-Strength;
    public int Ammunition { get; internal set; }
    public int Facing { get; internal set; } // tenths of degrees; 0 = north (-Z), 900 = east
    public int Morale { get; internal set; }
    public int Fatigue { get; internal set; }
    public int Cohesion { get; internal set; } = 1000;
    public FormationStatus Status { get; internal set; }
    public Position Position { get; internal set; }
    public Position? Destination { get; internal set; }
    public IReadOnlyList<Position> PlannedRoute=>Route.ToArray();
    public TacticalOrder? CurrentOrder { get; internal set; }
    public string CommandStatus { get; internal set; } = "Holding";
    public bool Active => Strength>0 && Status!=FormationStatus.Destroyed;
    internal Queue<Position> Route = new();
    internal Queue<Position> QueuedDestinations = new();
    internal int BlockedTicks;
    internal long LastCombatTick=-10000, LastShotTick=-10000;
    internal bool MovedThisTick, EngagedThisTick;
    internal int GroupSpeed;
    internal Formation(FormationSpec s,UnitDefinition? definition=null)
    {
        Id=s.Id; Faction=s.Faction; Definition=definition??UnitDefinition.For(s.Kind); Position=s.Position;
        InitialStrength=Strength=s.Strength; Facing=s.Facing; Morale=s.Morale; Ammunition=Definition.Ammo;
    }
}
public sealed record BattleEvent(long Tick,string Kind,int FormationId,int? OtherId,string Detail);
public sealed class SeededRandom
{
    public uint State { get; private set; }
    public SeededRandom(uint seed) { State=seed==0?0x9e3779b9:seed; }
    public int Next(int max) { uint x=State; x^=x<<13; x^=x>>17; x^=x<<5; State=x; return (int)(x%(uint)max); }
}
