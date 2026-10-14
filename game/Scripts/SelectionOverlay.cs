using Godot;
public partial class SelectionOverlay : Control
{
    public Rect2 Box { get; set; }
    public override void _Draw() { if(Box.Size.Length()>8) { DrawRect(Box,new Color(0.3f,0.65f,1,0.12f));DrawRect(Box,new Color(0.5f,0.8f,1),false,2); } }
}
