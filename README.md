# Unity3D响应式UI渲染框架UniVue

#### **UniVue 是一个面向 Unity 的运行时通用高性能轻量级的响应式UI框架**

UniVue目前有针对UGUI的扩展实现，后续有时间再慢慢增加对FariyGUI和UIToolkit的实现，当然你也可以自己扩展实现，这些扩展往往是针对不同底层UI框架组件的实现，大多数通用功能模块在UniVue中已经实现。

## 一些想说的话

目前已经参加工作近一年，这一年里的工作经验彻底让我舍弃了过去的UniVue的设计，因为过去的设计不适合团队协作，以及程序员的心力负担较重。由于要上班，也是最近几个月零零散散的写框架，每天写一些，唉，怀念大学那会儿每天单纯写代码的日子，无忧无虑~

有时候在想现在的AI已经这么nb还需要做这些事情吗？每个人都有自己的看法吧，我不知道，我只是出于乐趣写这些东西，或许打发无聊的时间吧。

---

## 核心模块

1. **响应式渲染**（`UniVue.UI` + `UniVue.Model`）
2. **高性能无GC的红点系统的实现**（`UniVue.UI.RedPointMgr`）
3. 协程（`UniVue.Coroutine`）
4. 事件（`UniVue.Event`）
5. 定时器（`UniVue.Timer`）

---

## 1) 响应式渲染（核心）

UniVue的响应式渲染逻辑并不复杂，本质上只是监听了数据的变化和事件的触发这两种。数据的变化在编译器会被执行IL注入而实现对数据变化的监听。底层的渲染结构是有向无环的图结构**RenderGraph**。

### 作用

- `BaseModel` 负责数据变更通知
- `BaseUI` 负责绑定渲染函数（每个BaseUI会有一个RenderGraph渲染数据结构）
- `RenderContext` 统一收集变更并按频率触发渲染
- `BaseComponent/BaseView`组织UI结构、UI行为

### 推荐写法：自动分析 Bind（首推）

注：自动分析的Bind方法不支持Event模式，如果需要通过事件触发渲染你仍然需要手动链式调用设置。

```csharp
Bind(() => RenderUI());
Bind(true, () => RenderUI());
```

编译期 CodeGen 会尝试分析渲染方法访问的模型字段/属性，自动补全 `On(...).Build()`。

> 建议：默认优先使用自动分析 Bind。仅在分析不到依赖或你需要精确控制依赖范围时，再改用手动链式 `Bind`。

### 手动链式 Bind

可变参数（数量不超过10个）这里在编译时会被自动替换为无GC的重载方法，无需担心GC Alloc。

```csharp
Bind(() =>
{
    txtName.text = playerModel.Name;
    txtLv.text = playerModel.Level.ToString();
})
    .On(playerModel, nameof(PlayerModel.Name), nameof(PlayerModel.Level))
    .On(UIEvent.PlayerRefreshed)
    .Build();
```

---

## 2) 红点系统

**采用位编码实现树结构的一种创新性高性能的红点系统。**

### 核心规则

- `RedPointKey : ulong` 编码树结构和 Or/And 规则
- 业务仅直接设置叶子：`SetActive(leafKey, bool)`
- 父节点状态由子节点自动聚合（帧末刷新）

### 常用 API

```csharp
UIMgr.RedPointMgr.SetActive(RedPointKey.Mail_Unread, true);
UIMgr.RedPointMgr.ListenerRedPointStatus(RedPointKey.Mail, OnMailRedChanged);
UIMgr.RedPointMgr.UnListenerRedPointStatus(RedPointKey.Mail, OnMailRedChanged);
```

### 动态依赖

```csharp
ulong root = UIMgr.RedPointMgr.CreateRedPointTree(RedPointRule.Or, "RuntimeRoot");
ulong child = UIMgr.RedPointMgr.AddDependency(root, RedPointRule.Or);
UIMgr.RedPointMgr.DeleteDependency(child);
```

### 编辑器工具

- `UniVue/Windows/RedPointTreeEditor`：编辑与导出 `RedPointKey.g.cs`
- `UniVue/Windows/RedPointTreeViewer`：运行时查看与调试红点状态

---

## 3) 协程（CoroutineMgr）

这是我过去写的一个独立模块，[Avalon712/VCoroutine: C# Coroutine For Unity3D](https://github.com/Avalon712/VCoroutine)，现在直接内置在框架中成为一个核心的依赖模块。

统一协程调度器，不依赖 `MonoBehaviour.StartCoroutine`：

```csharp
CoroutineID id = CoroutineMgr.Run(MyRoutine());
CoroutineMgr.Stop(id);
CoroutineMgr.Resume(id);
CoroutineMgr.Kill(id);
```

- 支持 `Update / LateUpdate / FixedUpdate` 运行环境
- 支持协程依赖（等待依赖结束后再运行）
- 支持自定义 Yield 处理（`CoroutineYieldHandleContext`）

---

## 4) 事件（EventMgr）

```csharp
EventMgr.On(GameEvent.PlayerChanged, OnPlayerChanged);
EventMgr.On<int>(GameEvent.GoldChanged, OnGoldChanged);

EventMgr.Dispatch(GameEvent.PlayerChanged);
EventMgr.Dispatch(GameEvent.GoldChanged, 100);
```

### 同一 key 下，传参与不传参区别

- `Dispatch(key)`：只触发无参 `Action`
- `Dispatch<T>(key, arg)`：触发匹配的 `Action<T>`，也会触发无参 `Action`
- `Action<TOther>` 与当前 `T` 不匹配时不会被调用

---

## 5) 定时器（TimerMgr）

```csharp
ulong timer = TimerMgr.Create()
    .OfDelay(1f)
    .OfInterval(0.5f)
    .OfCount(5)
    .OfCallback(() => { })
    .Build();

TimerMgr.AddTimer(...) //如果你不喜欢链式调用可以调用此方法设置参数

TimerMgr.Kill(timer);
```

- 支持延迟、间隔、次数、执行条件、取消条件
- 由 `CoroutineMgr` 驱动
