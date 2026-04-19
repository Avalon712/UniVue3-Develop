using System.Collections.Generic;
using UniVue.Model;
using UniVue.UI;
using UniVue.UI.Widegts;

public sealed partial class VLoopListComponent : BaseComponent
{
    private List<ItemData> _items;

    protected override void OnCreate()
    {
        VLoopList.BindItemRender<VLoopListItem>(OnItemRender);
    }

    private void OnItemRender(int index, VLoopListItem item)
    {
        item.SetData(_items[index]);
    }

    public void SetData(List<ItemData> data)
    {
        _items = data;
        VLoopList.Count = data.Count;
    }
}

public sealed class ItemData : BaseModel
{
    public string Label { get; set; }

    public bool IsSelected { get; set; }
}

#region UniVue Auto-Generated — DO NOT MODIFY

partial class VLoopListComponent
{
    private LoopList _VLoopList;

    public LoopList VLoopList =>
        _VLoopList ? _VLoopList : _VLoopList = FindByPath($"{UI.name}/VLoopList")?.GetComponent<LoopList>();
}

#endregion // UniVue Auto-Generated