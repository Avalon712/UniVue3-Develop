using TMPro;
using UniVue.UI;

public sealed partial class TipView : BaseView
{
    public override int Layer { get; } = 5;

    protected override void OnInit()
    {
    }
}

#region UniVue Auto-Generated — DO NOT MODIFY
partial class TipView
{
    [UniVue.UI.LazyInitUI("/CloseBtn")]
    public CloseBtnUI CloseBtn { get; }

    [UniVue.UI.LazyInitUI("/MessageTxt")]
    public TMPro.TextMeshProUGUI MessageTxt { get; }

    [UniVue.UI.LazyInitUI("/TitleTxt")]
    public TMPro.TextMeshProUGUI TitleTxt { get; }
}
#endregion // UniVue Auto-Generated
