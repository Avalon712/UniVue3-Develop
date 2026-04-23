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
    [global::UniVue.UI.LazyInitUIAttribute("/CloseBtn")]
    public CloseBtnUI CloseBtn { get; }

    [global::UniVue.UI.LazyInitUIAttribute("/MessageTxt")]
    public TMPro.TextMeshProUGUI MessageTxt { get; }

    [global::UniVue.UI.LazyInitUIAttribute("/TitleTxt")]
    public TMPro.TextMeshProUGUI TitleTxt { get; }
}
#endregion // UniVue Auto-Generated
