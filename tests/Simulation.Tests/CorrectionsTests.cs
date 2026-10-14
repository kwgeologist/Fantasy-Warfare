using FantasyWarfare.Simulation;
public static class CorrectionsTests
{
    public static void Run(Action<string,Action> test,Action<bool,string> check)
    {
        var results=TerrainTrials.Cases.Select(TerrainTrials.Run).ToArray();
        foreach(var r in results)Console.WriteLine($"MEASURE {r}");
        test("playable infantry forest travel penalty",()=>check(results[1].Seconds>results[0].Seconds*1.4,"forest not measurably slower"));
        test("playable cavalry forest travel penalty",()=>check(results[3].Seconds>results[2].Seconds*2.5,"cavalry forest penalty missing"));
        test("playable uphill speed and fatigue",()=>check(results[5].Seconds>results[4].Seconds*1.5&&results[5].Fatigue>results[4].Fatigue*3&&results[5].HeightGain>4000,"hill has no functional effort"));
        test("playable road faster than open",()=>check(results[6].Seconds<results[0].Seconds,"road advantage missing"));
        test("playable movement trial repeatability",()=>check(results.SequenceEqual(TerrainTrials.Cases.Select(TerrainTrials.Run)),"nonrepeatable measurements"));
        test("playable terrain movement frame independence",()=>
        {
            var trial=TerrainTrials.Cases[5];Battle Run(int ms,int count){var b=TerrainTrials.Create(trial);b.IssueGroup(1,[1],OrderKind.Move,trial.Target);for(int i=0;i<count;i++)b.Advance(TimeSpan.FromMilliseconds(ms));return b;}
            check(Run(10,500).Fingerprint()==Run(125,40).Fingerprint(),"terrain frame dependence");
        });
        test("arrival measurements survive long rendering frames",()=>
        {
            var trial=TerrainTrials.Cases[5];var b=TerrainTrials.Create(trial);b.IssueGroup(1,[1],OrderKind.Move,trial.Target);
            for(int i=0;i<8;i++)b.Advance(TimeSpan.FromSeconds(1));var r=TerrainTrials.Run(trial);
            check(b.Formation.LastArrivalTick==r.Ticks&&b.Formation.DistanceTravelled==r.Distance&&b.Formation.LastArrivalFatigue==r.Fatigue,"render frames corrupted arrival metrics");
        });
        test("all classes respect forest and uphill terrain",()=>
        {
            var map=Scenarios.Skirmish().Map;
            foreach(var kind in Enum.GetValues<UnitKind>())check(map.MovementPercent(new(-30000,-24000),kind,new(-20000,-24000))<map.MovementPercent(new(-30000,-38000),kind,new(-20000,-38000))&&map.MovementPercent(new(6000,0),kind,new(10000,0))<100,$"terrain ignored by {kind}");
        });
        test("terrain routing avoids clear but expensive forest",()=>
        {
            var map=new TerrainMap(p=>new(Math.Abs(p.X)<6000&&Math.Abs(p.Z)<6000?TerrainKind.Forest:TerrainKind.Open,0));
            Position start=new(-12000,0),target=new(12000,0);var path=Navigation.FindPath(map,start,target,3000,UnitKind.Cavalry);int cost=0;var last=start;
            foreach(var p in path){check(map.SegmentClear(last,p,3000),"footprint collision");cost+=map.TravelCost(last,p,UnitKind.Cavalry);last=p;}
            check(path.Length>1&&last==target&&cost<map.TravelCost(start,target,UnitKind.Cavalry),"direct shortcut/smoothing erased cheaper route");
        });
        test("terrain routing uses nearby road",()=>
        {
            var map=new TerrainMap(p=>new(p.Z>=2000&&p.Z<6000?TerrainKind.Road:TerrainKind.Open,0));
            Position start=new(-40000,0),target=new(40000,0);var path=Navigation.FindPath(map,start,target,3000,UnitKind.Infantry);int cost=0;var last=start;foreach(var p in path){cost+=map.TravelCost(last,p,UnitKind.Infantry);last=p;}
            check(path.Any(p=>p.Z>=2000)&&cost<map.TravelCost(start,target,UnitKind.Infantry),"road costs ignored");
        });
        test("slope cost is directional and steep slopes block",()=>
        {
            var map=Scenarios.Skirmish().Map;Position a=new(6000,0),b=new(10000,0);check(map.TravelCost(a,b,UnitKind.Infantry)>map.TravelCost(b,a,UnitKind.Infantry),"backward slope costing");
            var steep=new TerrainMap(p=>new(TerrainKind.Open,p.X));check(!steep.SegmentClear(new(0,0),new(4000,0),3000),"100 percent grade passable");
            check(map.MovementPercent(a,UnitKind.Infantry,b)<50,"steep playable ridge insufficiently restricted");
        });
    }
}
