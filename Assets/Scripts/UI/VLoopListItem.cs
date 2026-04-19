using TMPro;
using UnityEngine.UI;
using UniVue.UI.Widegts;

public sealed partial class VLoopListItem : LoopItem
{
    private ItemData _data;

    protected override void OnCreate()
    {
        SelectedToggle.onValueChanged.AddListener(isOn =>
        {
            if (_data != null && _data.IsSelected != isOn)
                _data.IsSelected = isOn;
        });
    }

    public void SetData(ItemData data)
    {
        if (_data != null)
            Unbind(_data);
        _data = data;
        Bind(true, Refresh);
    }

    private void Refresh()
    {
        if (_data == null) return;
        LabelTxt.text = _data.Label;
        SelectedToggle.isOn = _data.IsSelected;
    }
}

#region UniVue Auto-Generated — DO NOT MODIFY

partial class VLoopListItem
{
    private Image _BgImg;

    private TextMeshProUGUI _LabelTxt;

    private Toggle _SelectedToggle;
    public Image BgImg => _BgImg ? _BgImg : _BgImg = FindByPath($"{UI.name}/BgImg")?.GetComponent<Image>();

    public Toggle SelectedToggle => _SelectedToggle
        ? _SelectedToggle
        : _SelectedToggle = FindByPath($"{UI.name}/SelectedToggle")?.GetComponent<Toggle>();

    public TextMeshProUGUI LabelTxt => _LabelTxt
        ? _LabelTxt
        : _LabelTxt = FindByPath($"{UI.name}/SelectedToggle/LabelTxt")?.GetComponent<TextMeshProUGUI>();
}

#endregion // UniVue Auto-Generated