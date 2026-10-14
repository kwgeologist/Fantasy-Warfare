namespace FantasyWarfare.Simulation;

public readonly record struct Position(int X, int Z);
public readonly record struct MoveOrder(int SchemaVersion, long Id, long DeliveryTick, Position Target, bool Append);
public sealed class Formation
{
    public int Id => 1;
    public int Strength => 24;
    public Position Position { get; internal set; } = new(-20000, 0);
    public Position? Destination { get; internal set; }
}
/// <summary>Single authoritative fixed-tick state; no Godot dependency. Coordinates are millimetres.</summary>
public sealed class Battle
{
    public const int SchemaVersion = 1;
    public const long TickDuration = TimeSpan.TicksPerSecond / 20;
    public const int StepMillimetres = 200;
    public uint Seed { get; }
    public long Tick { get; private set; }
    public bool Paused { get; private set; }
    public Formation Formation { get; } = new();
    private long remainder, nextId;
    private readonly List<MoveOrder> pending = new();
    private readonly Queue<Position> waypoints = new();
    public IReadOnlyList<MoveOrder> Pending => pending.AsReadOnly();
    public Position[] Waypoints => waypoints.ToArray();
    public Battle(uint seed = 1) { Seed = seed; }
    public void SetPaused(bool value) { Paused = value; }
    public MoveOrder IssueMove(Position target, bool append = false, long? deliveryTick = null)
    {
        if (Math.Abs((long)target.X) > 48000 || Math.Abs((long)target.Z) > 48000)
            throw new ArgumentOutOfRangeException(nameof(target));
        var delivery = deliveryTick ?? Tick + 1;
        if (delivery <= Tick) throw new ArgumentOutOfRangeException(nameof(deliveryTick));
        var order = new MoveOrder(SchemaVersion, ++nextId, delivery, target, append);
        pending.Add(order);
        return order;
    }
    public void Advance(TimeSpan elapsed)
    {
        if (elapsed.Ticks < 0) throw new ArgumentOutOfRangeException(nameof(elapsed));
        if (Paused) return; // Wall time during pause is discarded, never caught up.
        remainder = checked(remainder + elapsed.Ticks);
        while (remainder >= TickDuration) { remainder -= TickDuration; Step(); }
    }
    private void Step()
    {
        Tick++;
        foreach (var order in pending.Where(o => o.DeliveryTick <= Tick).OrderBy(o => o.DeliveryTick).ThenBy(o => o.Id))
        {
            if (!order.Append) { waypoints.Clear(); Formation.Destination = null; }
            waypoints.Enqueue(order.Target);
        }
        pending.RemoveAll(o => o.DeliveryTick <= Tick);
        if (Formation.Destination is null && waypoints.Count > 0) Formation.Destination = waypoints.Dequeue();
        if (Formation.Destination is not Position target) return;
        var current = Formation.Position;
        long dx = target.X - current.X, dz = target.Z - current.Z;
        var distance = Math.Sqrt(dx * dx + dz * dz);
        if (distance <= StepMillimetres) { Formation.Position = target; Formation.Destination = null; return; }
        Formation.Position = new(current.X + (int)Math.Round(dx * StepMillimetres / distance),
                                 current.Z + (int)Math.Round(dz * StepMillimetres / distance));
    }
}
public static class Terrain
{
    // Same height field is sampled by presentation; no collision/graphics state enters Battle.
    public static float Height(float x, float z) =>
        7f * MathF.Exp(-MathF.Pow((x - 12f) / 8f, 2)) * MathF.Exp(-MathF.Pow(z / 35f, 2))
        - 4f * MathF.Exp(-MathF.Pow((x + 10f) / 9f, 2)) * MathF.Exp(-MathF.Pow((z - 22f) / 12f, 2));
}
