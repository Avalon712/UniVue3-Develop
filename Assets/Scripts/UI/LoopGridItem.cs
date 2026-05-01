using UniVue.UI;
using UniVue.UI.Widgets;

public partial class LoopGridItem : LoopItem
{
    private ItemData _data;
    
    public void SetData(ItemData data)
    {
        ItemData old = _data;
        _data = data;
        if (old == null)
            Bind(data, Refresh, nameof(data.Index))();
        else
            Rebind(old, data);
    }

    private void Refresh()
    {
        if(_data == null) return;
        IndexTxt.text = _data.Index.ToString();
    }
}

#region UniVue Auto-Generated — DO NOT MODIFY
partial class LoopGridItem
{
    [UniVue.UI.LazyInitUI("/IndexTxt")]
    public TMPro.TextMeshProUGUI IndexTxt { get; }
}
#endregion // UniVue Auto-Generated
