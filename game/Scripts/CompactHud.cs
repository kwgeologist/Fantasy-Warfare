using Godot;
using System;
using System.Linq;
using FantasyWarfare.Simulation;

/// <summary>Presentation only. All commands use the existing battle/input adapter.</summary>
public partial class CompactHud : Control
{
    public const int BarHeight=76;
    public PanelContainer Bar { get; private set; }=null!;
    public PanelContainer Details { get; private set; }=null!;
    public PanelContainer Help { get; private set; }=null!;
    public bool DetailsExpanded { get; private set; }=true;
    public Button PauseButton { get; private set; }=null!;
    public Button HoldButton { get; private set; }=null!;
    private Label overview=null!,detailText=null!,helpText=null!;
    private Button detailsButton=null!,helpButton=null!;
    public void Build(Action<Key> command)
    {
        MouseFilter=MouseFilterEnum.Ignore;
        Bar=Panel();AddChild(Bar);var rows=new VBoxContainer();rows.AddThemeConstantOverride("separation",3);Bar.AddChild(rows);
        overview=Text(13);overview.CustomMinimumSize=new(0,22);overview.TextOverrunBehavior=TextServer.OverrunBehavior.TrimEllipsis;rows.AddChild(overview);
        var buttons=new HBoxContainer();buttons.AddThemeConstantOverride("separation",4);rows.AddChild(buttons);
        Button Add(string text,Key key,string tooltip)
        {
            var b=new Button { Text=text,TooltipText=tooltip,FocusMode=FocusModeEnum.None };b.AddThemeFontSizeOverride("font_size",13);b.Pressed+=()=>command(key);buttons.AddChild(b);return b;
        }
        PauseButton=Add("Pause [Space]",Key.Space,"Freeze simulation; issue or queue orders, then resume.");
        HoldButton=Add("Hold [H]",Key.H,"Stop selected formations.");Add("Withdraw [X]",Key.X,"Then right-click a safe destination.");Add("Rally [R]",Key.R,"Rally selected formations away from known enemies.");
        Add("RTS [1]",Key.Key1,"Elevated camera");Add("Free [2]",Key.Key2,"Free camera; middle drag to orbit");Add("Map [3]",Key.Key3,"Overhead camera");
        Add("−",Key.Minus,"Reduce simulation speed");Add("+",Key.Equal,"Increase simulation speed");
        detailsButton=Add("Details [F3]",Key.F3,"Collapse or expand selected formation information");helpButton=Add("Help [F2]",Key.F2,"Controls, combat information and game controls");
        Details=Panel();AddChild(Details);var scroll=new ScrollContainer { CustomMinimumSize=new(410,106),HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled };Details.AddChild(scroll);
        detailText=Text(13);detailText.SizeFlagsHorizontal=SizeFlags.ExpandFill;scroll.AddChild(detailText);
        Help=Panel();AddChild(Help);var helpRows=new VBoxContainer();Help.AddChild(helpRows);helpText=Text(13);helpRows.AddChild(helpText);
        helpText.Text="Click: select | Shift-click/drag: multi-select\nTab: all | Right-click ground: group move\nRight-click visible enemy: attack | Shift: queue\nSpace: pause | H: hold | X + right-click: withdraw\nR: safe rally | 1/2/3: cameras | WASD: pan\nWheel: zoom | Middle drag: free camera orbit\n− / +: speed | F1: HUD | F2: help | F3: details\nM/C/F: morale/cohesion/fatigue (percent)\nRoads faster; woods and uphill slower.\nF9: restart skirmish";
        var restart=new Button { Text="Restart skirmish [F9]",FocusMode=FocusModeEnum.None };restart.Pressed+=()=>command(Key.F9);helpRows.AddChild(restart);Help.Visible=false;
    }
    private static PanelContainer Panel()=>new() { MouseFilter=MouseFilterEnum.Stop };
    private static Label Text(int size)
    {
        var label=new Label { MouseFilter=MouseFilterEnum.Ignore };label.AddThemeFontSizeOverride("font_size",size);return label;
    }
    public void ToggleDetails()=>DetailsExpanded=!DetailsExpanded;
    public void ToggleHelp()=>Help.Visible=!Help.Visible;
    public void Update(Battle battle,int[] ids,float speed,int camera,string message,Vector2 viewport)
    {
        Size=viewport;Bar.Position=new(8,viewport.Y-BarHeight-8);Bar.Size=new(viewport.X-16,BarHeight);
        Details.Position=new(8,Bar.Position.Y-124);Details.Size=new(422,116);Details.Visible=ids.Length>0&&DetailsExpanded;
        Help.Position=new(viewport.X-410,Bar.Position.Y-270);Help.Size=new(402,262);
        string state=battle.Outcome??(battle.Paused?"PAUSED":"RUNNING");
        overview.Text=$"{state} | {speed:0.0}x | {new[]{"RTS","Free","Overhead"}[camera]} | Selected {ids.Length} | Enemies seen {battle.Perception(1).Visible.Count} | Queued {battle.PendingOrders.Count(o=>o.Faction==1)} | {message}";
        overview.TooltipText=overview.Text;PauseButton.Text=battle.Paused?"Resume [Space]":"Pause [Space]";
        detailsButton.Text=DetailsExpanded?"Details ▾ [F3]":"Details ▸ [F3]";helpButton.Text=Help.Visible?"Help ▾ [F2]":"Help ▸ [F2]";
        var units=ids.Select(battle.GetFormation).ToArray();
        detailText.Text=string.Join("\n",units.Select(f=>$"{f.Id} {f.Kind} | {f.Strength}/{f.InitialStrength} | Ammo {f.Ammunition} | Face {f.Facing/10}°\nM {f.Morale/10}% C {f.Cohesion/10}% F {f.Fatigue/10}% | {f.MovementSpeed/1000d:0.00} m/s | {battle.Map.Cell(f.Position).Kind}\n{f.Status} | {f.CurrentOrder?.Kind.ToString()??"Hold"}: {f.CommandStatus}"));
        if(battle.Outcome is not null) helpText.Text="Battle ended. F9 restarts.\nAI after-action: "+string.Join("\n",battle.Decisions.TakeLast(4).Select(d=>$"{d.Tick}: {d.Plan}"));
    }
}
