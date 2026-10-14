namespace FantasyWarfare.Simulation;

public static class CombatRules
{
    public static int Power(Formation attacker,Formation defender,TerrainMap map,bool ranged)
    {
        int power=ranged?40:attacker.Definition.MeleePower;
        int angle=Battle.FacingDifference(defender.Facing,Battle.FacingTo(defender.Position,attacker.Position));
        if(angle>1200) power=power*170/100;else if(angle>650) power=power*130/100;
        if(attacker.Definition.Weapon==WeaponKind.Lance&&defender.Definition.Weapon==WeaponKind.Spear&&angle<650&&defender.Cohesion>500&&defender.Status!=FormationStatus.Routing) power=power*30/100;
        if(attacker.Definition.Weapon==WeaponKind.Spear&&defender.Definition.Weapon==WeaponKind.Lance) power=power*140/100;
        if(map.Cell(attacker.Position).Kind==TerrainKind.Forest&&attacker.Kind==UnitKind.Cavalry) power=power/2;
        if(ranged&&map.Cell(defender.Position).Kind==TerrainKind.Forest) power=power*65/100;
        power=power*Math.Clamp(1000-attacker.Fatigue/2,500,1000)/1000*Math.Max(300,attacker.Cohesion)/1000;
        if(attacker.Status==FormationStatus.Withdrawing) power=power*60/100;
        int elevation=map.Height(attacker.Position)-map.Height(defender.Position);
        power=power*Math.Clamp(100+elevation/300,80,120)/100;
        return Math.Max(1,power*(100-defender.Definition.Armor/2)/100);
    }
}
public sealed partial class Battle
{
    private void ResolveCombat()
    {
        var damage=new Dictionary<int,int>();
        var facingUpdates=new Dictionary<int,int>();
        foreach(var attacker in formations.Where(f=>f.Active&&f.Status is not FormationStatus.Routing and not FormationStatus.Rallying))
        {
            var visible=perceptions[attacker.Faction].Visible;
            var candidates=formations.Where(f=>f.Faction!=attacker.Faction&&f.Active&&visible.Any(e=>e.Id==f.Id)).OrderBy(f=>Position.DistanceSquared(attacker.Position,f.Position)).ThenBy(f=>f.Id).ToArray();
            var melee=candidates.FirstOrDefault(f=>Position.Distance(attacker.Position,f.Position)<=attacker.Definition.Radius+f.Definition.Radius+1200&&Map.SegmentClear(attacker.Position,f.Position,0));
            bool ranged=melee is null&&attacker.Kind==UnitKind.Archers&&attacker.Ammunition>0&&!attacker.MovedThisTick;
            var target=melee??(ranged?candidates.FirstOrDefault(f=>Position.Distance(attacker.Position,f.Position)<=attacker.Definition.Range&&Map.HasLineOfSight(attacker.Position,f.Position)):null);
            if(target is null) continue;
            attacker.EngagedThisTick=true;
            int cooldown=ranged?40:20;
            if(Tick-attacker.LastCombatTick<cooldown) continue;
            attacker.LastCombatTick=Tick;
            facingUpdates[attacker.Id]=FacingTo(attacker.Position,target.Position);
            if(ranged) { attacker.Ammunition--;attacker.LastShotTick=Tick; }
            int power=CombatRules.Power(attacker,target,Map,ranged);
            int effective=attacker.Strength*power*(80+random.Next(41))/100;
            int hits=Math.Max(0,effective/(ranged?850:600));
            // Stochastic remainder permits weak formations to inflict nonzero damage without guaranteed minimum casualties.
            int denominator=ranged?850:600;
            if(random.Next(denominator)<effective%denominator) hits++;
            damage[target.Id]=damage.GetValueOrDefault(target.Id)+hits;
            events.Add(new(Tick,ranged?"Volley":"Melee",attacker.Id,target.Id,$"{hits} projected casualties"));
        }
        foreach(var face in facingUpdates) GetFormation(face.Key).Facing=face.Value;
        // All attacks use pre-resolution strengths; losses apply simultaneously to avoid first-faction advantage.
        foreach(var pair in damage.OrderBy(p=>p.Key))
        {
            var target=GetFormation(pair.Key);int loss=Math.Min(target.Strength,pair.Value);target.Strength-=loss;
            target.Morale=Math.Max(0,target.Morale-loss*1500/target.InitialStrength);
            target.Cohesion=Math.Max(0,target.Cohesion-loss*900/target.InitialStrength);
            if(loss>0) events.Add(new(Tick,"Casualty",target.Id,null,$"{loss} lost; {target.Strength} remain"));
            if(target.Strength==0) { target.Status=FormationStatus.Destroyed;target.Destination=null;target.Route.Clear();target.QueuedDestinations.Clear(); }
        }
    }
    private void UpdateMorale()
    {
        var routers=formations.Where(f=>f.Active&&f.Status==FormationStatus.Routing).Select(f=>(f.Id,f.Faction,f.Position)).ToArray();
        foreach(var f in formations.Where(f=>f.Active))
        {
            bool woods=Map.Cell(f.Position).Kind==TerrainKind.Forest;
            if(f.MovedThisTick) { f.Fatigue=Math.Min(1000,f.Fatigue+(woods?2:1));if(woods) f.Cohesion=Math.Max(0,f.Cohesion-1); }
            else if(f.EngagedThisTick) f.Fatigue=Math.Min(1000,f.Fatigue+2);
            else { f.Fatigue=Math.Max(0,f.Fatigue-2);f.Cohesion=Math.Min(1000,f.Cohesion+1); }
            if(Tick%20==0)
            {
                var nearby=perceptions[f.Faction].Visible.Where(e=>Position.Distance(e.Position,f.Position)<12000).ToArray();
                if(nearby.Any(e=>FacingDifference(f.Facing,FacingTo(f.Position,e.Position))>1000)) { f.Morale=Math.Max(0,f.Morale-25);f.Cohesion=Math.Max(0,f.Cohesion-10); }
                if(routers.Any(o=>o.Id!=f.Id&&o.Faction==f.Faction&&Position.Distance(o.Position,f.Position)<14000)) f.Morale=Math.Max(0,f.Morale-40);
                if(nearby.Length==0&&f.Status!=FormationStatus.Routing) f.Morale=Math.Min(900,f.Morale+5);
                if(f.Status==FormationStatus.Withdrawing&&nearby.Length>0) f.Cohesion=Math.Max(0,f.Cohesion-Math.Max(1,(100-f.Definition.Discipline)/20));
                if(f.Fatigue>850) f.Morale=Math.Max(0,f.Morale-5);
            }
            if(f.Status==FormationStatus.Rallying)
            {
                if(perceptions[f.Faction].Visible.Any(e=>Position.Distance(e.Position,f.Position)<12000)) { f.Status=FormationStatus.Routing;f.CommandStatus="Rally interrupted by threat"; }
                else { f.Morale=Math.Min(650,f.Morale+6);f.Cohesion=Math.Min(700,f.Cohesion+3);
                    if(f.Morale>=600&&f.Cohesion>=500&&f.Fatigue<=600) { f.Status=FormationStatus.Ready;f.CommandStatus="Rallied";events.Add(new(Tick,"Rallied",f.Id,null,"Survivors regrouped; casualties unchanged")); } }
            }
            if(f.Status is not FormationStatus.Routing and not FormationStatus.Rallying && (f.Morale<250||f.Cohesion<180))
            {
                f.Status=FormationStatus.Routing;f.CurrentOrder=null;f.Route.Clear();f.QueuedDestinations.Clear();f.Destination=null;
                f.QueuedDestinations.Enqueue(new(f.Faction==1?-44000:44000,Math.Clamp(f.Position.Z,-40000,40000)));BeginNextDestination(f);
                f.CommandStatus="Routing";events.Add(new(Tick,"Rout",f.Id,null,"Morale/cohesion broke"));
            }
        }
    }
}
