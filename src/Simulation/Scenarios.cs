namespace FantasyWarfare.Simulation;

public static class Scenarios
{
    public static ScenarioDefinition Skirmish()
    {
        var specs=new List<FormationSpec>();
        UnitKind[] kinds=[UnitKind.Infantry,UnitKind.Spearmen,UnitKind.Archers,UnitKind.Infantry,UnitKind.Archers,UnitKind.Cavalry];
        for(int i=0;i<6;i++)
        {
            specs.Add(new(i+1,1,kinds[i],new(-42000,-35000+i*13000),24,900));
            specs.Add(new(i+101,2,kinds[i],new(42000,-35000+i*13000),24,2700));
        }
        return new("Ridge skirmish",TerrainMap.Battlefield(),specs);
    }
    public static ScenarioDefinition Surprise()
    {
        var forest=new TerrainMap(p=>new(p.X>=18000&&p.X<=26000&&p.Z>=10000&&p.Z<=20000?TerrainKind.Forest:TerrainKind.Open,0));
        return new("Unexpected spear reserve",forest,[
            new(101,2,UnitKind.Cavalry,new(0,0),24,900),new(102,2,UnitKind.Archers,new(-14000,-10000),24,900),
            new(1,1,UnitKind.Archers,new(14000,0),12,2700),new(2,1,UnitKind.Spearmen,new(22000,14000),48,2700)],false,true,false,false);
    }
}
