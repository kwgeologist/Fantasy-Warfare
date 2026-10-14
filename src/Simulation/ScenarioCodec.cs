using System.Text.Json;
using System.Text.Json.Serialization;
namespace FantasyWarfare.Simulation;

public static class ScenarioCodec
{
    private sealed record Document(int SchemaVersion,string Name,string Terrain,FormationSpec[] Formations,UnitDefinition[] Units);
    public static ScenarioDefinition Parse(string json)
    {
        var options=new JsonSerializerOptions { PropertyNamingPolicy=JsonNamingPolicy.CamelCase,UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase,allowIntegerValues:false));
        Document doc;
        try { doc=JsonSerializer.Deserialize<Document>(json,options)??throw new FormatException("Empty scenario."); }
        catch(JsonException e) { throw new FormatException("Invalid scenario JSON.",e); }
        if(doc.SchemaVersion!=2||string.IsNullOrWhiteSpace(doc.Name)||doc.Formations is null||doc.Units is null||doc.Units.Length!=4||doc.Units.Select(u=>u.Kind).Distinct().Count()!=4)
            throw new FormatException("Scenario needs version 2, a name, formations and all four unique unit definitions.");
        foreach(var unit in doc.Units)
            if(unit.Speed<100||unit.Speed>10000||unit.Radius<500||unit.Radius>5000||unit.MeleePower<1||unit.MeleePower>200||unit.Armor<0||unit.Armor>80||unit.Range<0||unit.Range>36000||unit.Ammo<0||unit.Ammo>100||unit.Discipline<0||unit.Discipline>100)
                throw new FormatException("Unit statistic is outside supported bounds.");
        var map=doc.Terrain switch { "ridge"=>TerrainMap.Battlefield(),"flat"=>new TerrainMap(),_=>throw new FormatException("Unknown authored terrain.") };
        var result=new ScenarioDefinition(doc.Name,map,doc.Formations,Units:doc.Units.ToDictionary(u=>u.Kind));
        // Validate IDs, deployment and runtime formation bounds with the same constructor used to play.
        try { _=new Battle(result with {Commander=false,Combat=false,Morale=false,Victory=false}); }
        catch(ArgumentException e) { throw new FormatException("Invalid scenario deployment.",e); }
        return result;
    }
}
