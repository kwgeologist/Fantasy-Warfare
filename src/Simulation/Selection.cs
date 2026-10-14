namespace FantasyWarfare.Simulation;

/// <summary>UI selection holds IDs only, never competing formation state.</summary>
public sealed class FormationSelection
{
    private readonly SortedSet<int> ids=new();
    public int[] Ids=>ids.ToArray();
    public void Select(int id,IEnumerable<Formation> owned,bool toggle=false)
    {
        if(!owned.Any(f=>f.Id==id && f.Active)) return;
        if(!toggle) ids.Clear();
        if(toggle && ids.Contains(id)) ids.Remove(id);else ids.Add(id);
    }
    public void SelectMany(IEnumerable<int> candidates,IEnumerable<Formation> owned,bool additive=false)
    {
        if(!additive) ids.Clear();var valid=owned.Where(f=>f.Active).Select(f=>f.Id).ToHashSet();
        foreach(int id in candidates) if(valid.Contains(id)) ids.Add(id);
    }
    public void Clear()=>ids.Clear();
    public void Prune(IEnumerable<Formation> owned) { var live=owned.Where(f=>f.Active).Select(f=>f.Id).ToHashSet();ids.RemoveWhere(id=>!live.Contains(id)); }
}
