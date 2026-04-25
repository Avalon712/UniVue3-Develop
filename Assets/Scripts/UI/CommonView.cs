using System;
using System.Collections.Generic;
using UnityEngine;
using UniVue.UI;

public sealed partial class CommonView : BaseView
{
    private readonly List<ItemData> _items = new(1000);
    public override int Layer { get; } = 1;

    protected override void OnInit()
    {
        enableUpdatePerSecond = true;
        //这里Add之后在OnOpen中去获取可能会获得为null，因为这里具有延时，调用不一定同步
        AddComponent<VLoopListComponent>(Container, (success, component) =>
        {
            if (!success) return;
            for (int i = 0; i < 1000; i++) _items.Add(new ItemData { Label = $"Label {i}", IsSelected = i % 2 == 0 });
            component.SetData(_items);
        });
    }

    protected override void OnUpdatePerSecond()
    {
        for (int i = 0; i < _items.Count; i++)
        {
            ItemData item = _items[i];
            item.Label = $"Label {i} {DateTime.Now.Second}";
        }
    }

    protected override void OnOpen()
    {
        base.OnOpen();
        if (TryGetViewComponent(out VLoopListComponent component))
        {
            component.SetData(_items);
            component.Show();
        }
    }
}

#region UniVue Auto-Generated — DO NOT MODIFY
partial class CommonView
{
    [UniVue.UI.LazyInitUI("/#Container")]
    public UnityEngine.RectTransform Container { get; }

    [UniVue.UI.LazyInitUI("/CloseBtnUI")]
    public CloseBtnUI CloseBtnUI { get; }
}
#endregion // UniVue Auto-Generated
