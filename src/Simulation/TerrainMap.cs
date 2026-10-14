namespace FantasyWarfare.Simulation;

public enum TerrainKind { Open, Forest, Road, Rock }
public readonly record struct TerrainCell(TerrainKind Kind,int Height);
/// <summary>Immutable authored 2m grid. All tactical queries are engine-independent.</summary>
public sealed class TerrainMap
{
    public const int CellSize=2000, Count=50, HalfSize=50000;
    private readonly TerrainCell[,] cells=new TerrainCell[Count,Count];
    private readonly int[,] corners=new int[Count+1,Count+1];
    public bool Uniform { get; }
    public TerrainMap(Func<Position,TerrainCell>? author=null)
    {
        for(int x=0;x<Count;x++) for(int z=0;z<Count;z++) cells[x,z]=author?.Invoke(Center(x,z))??new(TerrainKind.Open,0);
        Uniform=cells.Cast<TerrainCell>().All(c=>c==cells[0,0]);
        for(int x=0;x<=Count;x++) for(int z=0;z<=Count;z++)
            corners[x,z]=(cells[Math.Max(0,x-1),Math.Max(0,z-1)].Height+cells[Math.Min(Count-1,x),Math.Max(0,z-1)].Height+cells[Math.Max(0,x-1),Math.Min(Count-1,z)].Height+cells[Math.Min(Count-1,x),Math.Min(Count-1,z)].Height)/4;
    }
    public static Position Center(int x,int z)=>new(-HalfSize+x*CellSize+CellSize/2,-HalfSize+z*CellSize+CellSize/2);
    public static (int X,int Z) Index(Position p)=>(Math.Clamp((p.X+HalfSize)/CellSize,0,Count-1),Math.Clamp((p.Z+HalfSize)/CellSize,0,Count-1));
    public TerrainCell Cell(Position p) { var (x,z)=Index(p); return cells[x,z]; }
    public int Height(Position p)
    {
        // The same two triangles per cell are used by the rendered mesh and collision surface.
        int x=Math.Clamp(p.X+HalfSize,0,Count*CellSize),z=Math.Clamp(p.Z+HalfSize,0,Count*CellSize);
        int ix=Math.Min(x/CellSize,Count-1),iz=Math.Min(z/CellSize,Count-1),fx=x-ix*CellSize,fz=z-iz*CellSize;
        int a=corners[ix,iz],b=corners[ix+1,iz],c=corners[ix,iz+1],d=corners[ix+1,iz+1];
        return fx+fz<=CellSize?a+(int)(((long)(b-a)*fx+(long)(c-a)*fz)/CellSize)
            :d+(int)(((long)(c-d)*(CellSize-fx)+(long)(b-d)*(CellSize-fz))/CellSize);
    }

    public bool CanOccupy(Position p,int radius)
    {
        if(Math.Abs((long)p.X)+radius>HalfSize || Math.Abs((long)p.Z)+radius>HalfSize) return false;
        var (ax,az)=Index(new(p.X-radius,p.Z-radius));var (bx,bz)=Index(new(p.X+radius,p.Z+radius));
        for(int x=ax;x<=bx;x++) for(int z=az;z<=bz;z++) if(cells[x,z].Kind==TerrainKind.Rock)
        {
            var c=Center(x,z);long dx=Math.Max(0,Math.Abs((long)p.X-c.X)-1000),dz=Math.Max(0,Math.Abs((long)p.Z-c.Z)-1000);
            if(dx*dx+dz*dz<=(long)radius*radius) return false;
        }
        return true;
    }
    public bool SegmentClear(Position a,Position b,int radius)
    {
        int length=Position.Distance(a,b),steps=Math.Max(1,(length+499)/500);
        for(int i=0;i<=steps;i++)
        {
            Position p=new(a.X+(int)((long)(b.X-a.X)*i/steps),a.Z+(int)((long)(b.Z-a.Z)*i/steps));
            if(!CanOccupy(p,radius)) return false;
            if(i>0) { var before=new Position(a.X+(int)((long)(b.X-a.X)*(i-1)/steps),a.Z+(int)((long)(b.Z-a.Z)*(i-1)/steps));
                if(Math.Abs(Height(p)-Height(before))*1000L>Math.Max(1,Position.Distance(p,before))*800L) return false; }
        }
        return true;
    }
    public int MovementPercent(Position p,UnitKind kind,Position next)
    {
        int percent=Cell(p).Kind switch { TerrainKind.Forest=>kind==UnitKind.Cavalry?35:65,TerrainKind.Road=>120,TerrainKind.Rock=>0,_=>100 };
        int grade=GradePermille(p,next);
        int uphillFactor=kind==UnitKind.Cavalry?14:kind==UnitKind.Spearmen?12:11;
        int slope=Math.Clamp(1000-Math.Max(0,grade)*uphillFactor/10-Math.Max(0,-grade)/3,150,1000);
        return percent*slope/1000;
    }
    public int GradePermille(Position from,Position to)
    {
        var end=Position.Toward(from,to,1000);int distance=Position.Distance(from,end);
        return distance==0?0:(Height(end)-Height(from))*1000/distance;
    }
    public int TravelCost(Position from,Position to,UnitKind kind)
    {
        int length=Position.Distance(from,to),steps=Math.Max(1,(length+499)/500),cost=0;
        var before=from;
        for(int i=1;i<=steps;i++)
        {
            var p=new Position(from.X+(int)((long)(to.X-from.X)*i/steps),from.Z+(int)((long)(to.Z-from.Z)*i/steps));
            cost+=Position.Distance(before,p)*100/Math.Max(1,MovementPercent(before,kind,p));before=p;
        }
        return cost;
    }
    public bool HasLineOfSight(Position a,Position b)
    {
        int length=Position.Distance(a,b);
        if(length>36000) return false;
        int steps=Math.Max(1,(length+499)/500),h0=Height(a)+1800,h1=Height(b)+1800,forest=0;
        for(int i=1;i<steps;i++)
        {
            Position p=new(a.X+(int)((long)(b.X-a.X)*i/steps),a.Z+(int)((long)(b.Z-a.Z)*i/steps));
            if(Height(p)>h0+(int)((long)(h1-h0)*i/steps)) return false;
            var kind=Cell(p).Kind;
            if(kind==TerrainKind.Rock) return false;
            if(kind==TerrainKind.Forest) { forest+=length/steps; if(forest>4000) return false; }
        }
        return true;
    }
    public static TerrainMap Battlefield()=>new(p=>
    {
        int ridge=7000*Math.Max(0,10000-Math.Abs(p.X-12000))/10000*Math.Max(0,45000-Math.Abs(p.Z))/45000;
        int valley=4000*Math.Max(0,10000-Math.Abs(p.X+10000))/10000*Math.Max(0,12000-Math.Abs(p.Z-22000))/12000;
        var kind=(p.X>=-34000&&p.X<=-18000&&p.Z>=-32000&&p.Z<=-14000) || (p.X>=24000&&p.X<=36000&&p.Z>=12000&&p.Z<=28000)?TerrainKind.Forest:TerrainKind.Open;
        if(Math.Abs(p.Z+6000)<=2000) kind=TerrainKind.Road;
        if(p.X>=-2000&&p.X<=6000&&p.Z>=16000&&p.Z<=26000) kind=TerrainKind.Rock;
        return new(kind,ridge-valley);
    });
}
public static class Navigation
{
    public static Position[] FindPath(TerrainMap map,Position start,Position target,int radius,UnitKind kind)
    {
        if(!map.CanOccupy(target,radius)) return [];
        if(map.Uniform && map.SegmentClear(start,target,radius)) return [target];
        if(TerrainMap.Index(start)==TerrainMap.Index(target)) return map.SegmentClear(start,target,radius)?[target]:[];
        var (sx,sz)=TerrainMap.Index(start);var (tx,tz)=TerrainMap.Index(target);
        int s=sx*50+sz,t=tx*50+tz;
        var costs=new int[2500];Array.Fill(costs,int.MaxValue);costs[s]=0;
        var previous=new int[2500];Array.Fill(previous,-1);
        var open=new PriorityQueue<int,(int,int)>();open.Enqueue(s,(0,s));
        int[] dx=[-1,0,1,0,-1,-1,1,1],dz=[0,-1,0,1,-1,1,-1,1];
        while(open.TryDequeue(out int node,out _))
        {
            if(node==t) break;
            int x=node/50,z=node%50;var from=node==s?start:TerrainMap.Center(x,z);
            for(int d=0;d<8;d++)
            {
                int nx=x+dx[d],nz=z+dz[d];if(nx<0||nz<0||nx>=50||nz>=50) continue;
                int n=nx*50+nz;var to=n==t?target:TerrainMap.Center(nx,nz);
                if(!map.SegmentClear(from,to,radius)) continue;
                int cost=costs[node]+map.TravelCost(from,to,kind);
                if(cost>=costs[n]) continue;
                costs[n]=cost;previous[n]=node;open.Enqueue(n,(cost+Position.Distance(to,target)*100/120,n));
            }
        }
        if(costs[t]==int.MaxValue) return [];
        var route=new List<Position>();for(int n=t;n!=s;n=previous[n]) route.Add(n==t?target:TerrainMap.Center(n/50,n%50));route.Reverse();
        // A clear direct segment is only preferable if its terrain cost is no greater.
        if(map.SegmentClear(start,target,radius)&&map.TravelCost(start,target,kind)<=costs[t]) return [target];
        var smooth=new List<Position>();var current=start;
        for(int i=0;i<route.Count;)
        {
            int far=i,routeCost=map.TravelCost(current,route[i],kind);
            for(int j=i+1;j<route.Count;j++)
            {
                routeCost+=map.TravelCost(route[j-1],route[j],kind);
                if(map.SegmentClear(current,route[j],radius)&&map.TravelCost(current,route[j],kind)<=routeCost) far=j;
            }
            smooth.Add(route[far]);current=route[far];i=far+1;
        }
        return smooth.ToArray();
    }
}
