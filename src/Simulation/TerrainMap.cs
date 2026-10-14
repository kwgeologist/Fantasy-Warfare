namespace FantasyWarfare.Simulation;

public enum TerrainKind { Open, Forest, Road, Rock }
public readonly record struct TerrainCell(TerrainKind Kind,int Height);
/// <summary>Immutable authored 2m grid. All tactical queries are engine-independent.</summary>
public sealed class TerrainMap
{
    public const int CellSize=2000, Count=50, HalfSize=50000;
    private readonly TerrainCell[,] cells=new TerrainCell[Count,Count];
    public TerrainMap(Func<Position,TerrainCell>? author=null)
    {
        for(int x=0;x<Count;x++) for(int z=0;z<Count;z++) cells[x,z]=author?.Invoke(Center(x,z))??new(TerrainKind.Open,0);
    }
    public static Position Center(int x,int z)=>new(-HalfSize+x*CellSize+CellSize/2,-HalfSize+z*CellSize+CellSize/2);
    public static (int X,int Z) Index(Position p)=>(Math.Clamp((p.X+HalfSize)/CellSize,0,Count-1),Math.Clamp((p.Z+HalfSize)/CellSize,0,Count-1));
    public TerrainCell Cell(Position p) { var (x,z)=Index(p); return cells[x,z]; }
    public int Height(Position p)
    {
        int x=Math.Clamp(p.X+HalfSize-1000,0,98000),z=Math.Clamp(p.Z+HalfSize-1000,0,98000);
        int ix=x/CellSize, iz=z/CellSize,fx=x%CellSize,fz=z%CellSize;
        long a=(long)cells[ix,iz].Height*(CellSize-fx)+(long)cells[Math.Min(ix+1,49),iz].Height*fx;
        long b=(long)cells[ix,Math.Min(iz+1,49)].Height*(CellSize-fx)+(long)cells[Math.Min(ix+1,49),Math.Min(iz+1,49)].Height*fx;
        return (int)((a*(CellSize-fz)+b*fz)/(CellSize*CellSize));
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
                if(Math.Abs(Height(p)-Height(before))>Math.Max(1,Position.Distance(p,before))) return false; }
        }
        return true;
    }
    public int MovementPercent(Position p,UnitKind kind,Position next)
    {
        int percent=Cell(p).Kind switch { TerrainKind.Forest=>kind==UnitKind.Cavalry?35:65,TerrainKind.Road=>120,TerrainKind.Rock=>0,_=>100 };
        int delta=Height(Position.Toward(p,next,1000))-Height(p);
        return Math.Clamp(percent-Math.Max(0,delta)/12-Math.Max(0,-delta)/35,20,120);
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
        if(map.SegmentClear(start,target,radius)) return [target];
        var (sx,sz)=TerrainMap.Index(start);var (tx,tz)=TerrainMap.Index(target);
        int s=sx*50+sz,t=tx*50+tz;
        var costs=new int[2500];Array.Fill(costs,int.MaxValue);costs[s]=0;
        var previous=new int[2500];Array.Fill(previous,-1);
        var open=new PriorityQueue<int,(int,int)>();open.Enqueue(s,(0,s));
        int[] dx=[-1,0,1,0],dz=[0,-1,0,1];
        while(open.TryDequeue(out int node,out _))
        {
            if(node==t) break;
            int x=node/50,z=node%50;var from=node==s?start:TerrainMap.Center(x,z);
            for(int d=0;d<4;d++)
            {
                int nx=x+dx[d],nz=z+dz[d];if(nx<0||nz<0||nx>=50||nz>=50) continue;
                int n=nx*50+nz;var to=n==t?target:TerrainMap.Center(nx,nz);
                if(!map.SegmentClear(from,to,radius)) continue;
                int cost=costs[node]+200000/Math.Max(1,map.MovementPercent(to,kind,from));
                if(cost>=costs[n]) continue;
                costs[n]=cost;previous[n]=node;open.Enqueue(n,(cost+(Math.Abs(nx-tx)+Math.Abs(nz-tz))*1600,n));
            }
        }
        if(costs[t]==int.MaxValue) return [];
        var route=new List<Position>();for(int n=t;n!=s;n=previous[n]) route.Add(n==t?target:TerrainMap.Center(n/50,n%50));route.Reverse();
        // Keep collision-safe segments, preserving the target's exact millimetre coordinate.
        var smooth=new List<Position>();var current=start;
        for(int i=0;i<route.Count;) { int far=i;for(int j=i+1;j<route.Count;j++) if(map.SegmentClear(current,route[j],radius)) far=j; else break; smooth.Add(route[far]);current=route[far];i=far+1; }
        return smooth.ToArray();
    }
}
