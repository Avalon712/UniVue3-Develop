using UniVue.UI;

public sealed partial class GMView : BaseView
{
    public override int Layer { get; } = 9;

    protected override void OnInit()
    {
        CloseViewOp.BindOp(new OpComponent.OpCode { Name = "Close View" }, op => UIMgr.Close(op.Code));
        OpenViewOp.BindOp(new OpComponent.OpCode { Name = "Open View" }, op => UIMgr.Open(op.Code));
    }
}

#region UniVue Auto-Generated — DO NOT MODIFY

partial class GMView
{
    private OpComponent _CloseViewOp;

    private OpComponent _OpenViewOp;

    public OpComponent CloseViewOp => _CloseViewOp
        ? _CloseViewOp
        : _CloseViewOp = FindByPath($"{UI.name}/CloseViewOp")?.GetComponent<OpComponent>();

    public OpComponent OpenViewOp => _OpenViewOp
        ? _OpenViewOp
        : _OpenViewOp = FindByPath($"{UI.name}/OpenViewOp")?.GetComponent<OpComponent>();
}

#endregion // UniVue Auto-Generated