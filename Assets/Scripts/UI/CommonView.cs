using System.Collections.Generic;
using UnityEngine;
using UniVue.UI;

public sealed partial class CommonView : BaseView
{
    public override int Layer { get; } = 1;
    
    private List<ItemData> _items = new(1000);

    protected override void OnInit()
    {
        //这里Add之后在OnOpen中去获取可能会获得为null，因为这里具有延时，调用不一定同步
        AddComponent<VLoopListComponent>(Container, (success, component) =>
        {
            if (!success) return;
            for (int i = 0; i < 1000; i++) _items.Add(new ItemData { Label = $"Label {i}", IsSelected = i % 2 == 0 });
            component.SetData(_items);
        });
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
    private CloseBtnUI _CloseBtnUI;
    private RectTransform _Container;

    public RectTransform Container => _Container
        ? _Container
        : _Container = FindByPath($"{UI.name}/#Container")?.GetComponent<RectTransform>();

    public CloseBtnUI CloseBtnUI => _CloseBtnUI
        ? _CloseBtnUI
        : _CloseBtnUI = FindByPath($"{UI.name}/CloseBtnUI")?.GetComponent<CloseBtnUI>();
}

#endregion // UniVue Auto-Generated