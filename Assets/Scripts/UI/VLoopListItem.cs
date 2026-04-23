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
    [UniVue.UI.LazyInitUI("/BgImg")]
    public UnityEngine.UI.Image BgImg { get; }

    [UniVue.UI.LazyInitUI("/SelectedToggle")]
    public UnityEngine.UI.Toggle SelectedToggle { get; }

    [UniVue.UI.LazyInitUI("/SelectedToggle/LabelTxt")]
    public TMPro.TextMeshProUGUI LabelTxt { get; }
}
#endregion // UniVue Auto-Generated
