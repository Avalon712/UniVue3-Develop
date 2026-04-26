using TMPro;
using UnityEngine.UI;
using UniVue.UI.Widgets;

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
        ItemData old = _data;
        _data = data;
        if (old != null)
            Rebind(old, data);
        else
            Bind(data, Refresh);
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
