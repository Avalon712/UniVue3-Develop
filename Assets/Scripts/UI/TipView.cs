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
    private CloseBtnUI _CloseBtn;

    private TextMeshProUGUI _MessageTxt;

    private TextMeshProUGUI _TitleTxt;

    public CloseBtnUI CloseBtn =>
        _CloseBtn ? _CloseBtn : _CloseBtn = FindByPath($"{UI.name}/CloseBtn")?.GetComponent<CloseBtnUI>();

    public TextMeshProUGUI MessageTxt => _MessageTxt
        ? _MessageTxt
        : _MessageTxt = FindByPath($"{UI.name}/MessageTxt")?.GetComponent<TextMeshProUGUI>();

    public TextMeshProUGUI TitleTxt => _TitleTxt
        ? _TitleTxt
        : _TitleTxt = FindByPath($"{UI.name}/TitleTxt")?.GetComponent<TextMeshProUGUI>();
}

#endregion // UniVue Auto-Generated