using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniVue.Event;
using UniVue.Model;
using UniVue.UI;
using UniVue.UI.Widegts;
using Debug = UnityEngine.Debug;

namespace Game
{
    public partial class TestView : BaseView
    {
        public LoopList loopListV;
        public LoopList loopListH;
        public LoopGrid loopGridV;
        public LoopGrid loopGridH;

        private readonly TestData _data = new() { Value = 10 };

        private int i;

        protected override void OnInit()
        {
            //不指定Target对象
            // EventMgr.On(1, () => Debug.Log("Event 1 Triggered"));
            // EventMgr.On(1, Test);
            // EventMgr.On(2, (string msg) => Debug.Log($"Event 2 Triggered With Args: {msg}"));
            // EventMgr.On(2, () => Debug.Log("Event 2 Triggered Without Args"));

            // EventMgr.On(1, this, () => Debug.Log("Event 1 Triggered"));
            // EventMgr.On(1, this, Test);
            // EventMgr.On(2, this, (string msg) => Debug.Log($"Event 2 Triggered With Args: {msg}"));
            // EventMgr.On(2, this, () => Debug.Log("Event 2 Triggered Without Args"));
            //
            // RunCoroutine(EventTest());

            // Bind(() => { Debug.Log($"Data Changed Rerender:  Value = {_data.Value}"); }, false)
            //    .On(_data)
            //    .Build();

            // Bind(() => { Debug.Log($"Data Changed Rerender:  Value = {_data.Value} Name = {_data.Name}"); });
            Bind(Test);
            // Bind(true, () => Test2(_data));
            // Bind(() =>
            //          Debug.Log($"A1 = {_data.A1} A2 = {_data.A2} A3 = {_data.A3} A4 = {_data.A4} A5 = {_data.A5} A6 = {_data.A6} A7 = {_data.A7} A8 = {_data.A8} A9 = {_data.A9} A10 = {_data.A10}"));
            // Bind(() =>
            //          Debug.Log($"Value = {_data.Name} A1 = {_data.A1} A2 = {_data.A2} A3 = {_data.A3} A4 = {_data.A4} A5 = {_data.A5} A6 = {_data.A6} A7 = {_data.A7} A8 = {_data.A8} A9 = {_data.A9} A10 = {_data.A10}"));
            // Bind(false, () => print("test")); //警告

            // Bind(1, () => Debug.Log($"事件{UIMgr.Renderer.CurrentTriggerRenderEvent}触发渲染函数调用"));
            // Bind(() => print($"事件{UIMgr.Renderer.CurrentTriggerRenderEvent}触发渲染函数调用"), 2, 3, 4, 5);

            // Unbind(1);
            // Unbind(2, 3, 4);
            // Unbind(Params<EventKey>._(1, new EventKey(2), 3, 4));
            // Unbind(Test);

            // Unbind(_data, nameof(_data.Name), nameof(_data.Value));
            // Unbind(_data, "Name", "Value", "A1", "A2", "A3", "A4", "A5", "A6", "A7", "A8", "A9", "A10");

            //本帧内的多次改动之后触发一次渲染
            _data.Value = 20;
            _data.Value = 22;
            _data.Value = 24;

            if (loopListV)
            {
                loopListV.Count = 12;
                loopListV.ScrollTo(9);
            }

            if (loopListH)
            {
                loopListH.Count = 19;
                loopListH.ScrollTo(14);
            }

            if (loopGridV)
                loopGridV.Count = 100;
            if (loopGridH)
                loopGridH.Count = 100;

            RunCoroutine(TestRender());
            
            // Debug.Log(ScrollList_Horizontal);
        }

        private IEnumerator TestRender()
        {
            yield return new WaitForSeconds(1);
            _data.Name = "Test";
            _data.A1 = 10;
            _data.Value = 12;
            yield return new WaitForSeconds(1);
            EventMgr.Dispatch(1);
            yield return new WaitForSeconds(1);
            EventMgr.Dispatch(2);
            yield return new WaitForSeconds(1);
            EventMgr.Dispatch(5);

            // UIMgr.Close<TestView>();
            _data.Value = 9999;
        }

        private IEnumerator EventTest()
        {
            yield return new WaitForSeconds(1);
            EventMgr.Dispatch(1, true);

            yield return new WaitForSeconds(1);
            EventMgr.Dispatch(2, "Hello Event 2");

            yield return new WaitForSeconds(1);
            EventMgr.Dispatch(2, true);

            yield return new WaitForSeconds(1);
            EventMgr.Off(this); //Lambda表达式捕获的实例如果不是当前对象则不会被移除，所以只会移除Test函数的监听，其他三个监听仍然存在
            print(nameof(TestView) + "所有事件监听已经注销");

            yield return new WaitForSeconds(1);
            i++;
            print(nameof(TestView) + "测试注销是否成功，没有任何输出则成功" + " i = " + i);
            EventMgr.Dispatch(1, true);
            EventMgr.Dispatch(2, "Hello Event 2");
            EventMgr.Dispatch(2, true);
        }

        private void Test()
        {
            Debug.Log($"{nameof(Test)} {_data.Value} Time = {_data.Time}");
        }

        private static void Test2(TestData data)
        {
            Debug.Log($"{nameof(Test2)} {data.Value}");
        }

        private enum MyEnum
        {
            Value
        }

        private sealed class TestData : BaseModel
        {
            public int Value { get; set; }

            public string Name { get; set; }

            public float Time { get; set; }

            public int A1 { get; set; }

            public int A2 { get; set; }
            public int A3 { get; set; }
            public int A4 { get; set; }
            public int A5 { get; set; }
            public int A6 { get; set; }
            public int A7 { get; set; }
            public int A8 { get; set; }
            public int A9 { get; set; }
            public int A10 { get; set; }
        }
    }

    public partial class TestView
    {
        public void Test3()
        {
            Debug.Log($"{nameof(Test3)}");
        }
    }
}

#region UniVue Auto-Generated — DO NOT MODIFY
namespace Game
{
    partial class TestView
    {
        private UnityEngine.UI.Image _Image;
        public UnityEngine.UI.Image Image => _Image ? _Image : (_Image = FindByPath("TestView/Image").GetComponent<UnityEngine.UI.Image>());

        private UniVue.UI.Widegts.LoopList _ScrollList_Horizontal;
        public UniVue.UI.Widegts.LoopList ScrollList_Horizontal => _ScrollList_Horizontal ? _ScrollList_Horizontal : (_ScrollList_Horizontal = FindByPath("TestView/ScrollList_Horizontal").GetComponent<UniVue.UI.Widegts.LoopList>());

        private UniVue.UI.Widegts.LoopList _ScrollList_Vertical;
        public UniVue.UI.Widegts.LoopList ScrollList_Vertical => _ScrollList_Vertical ? _ScrollList_Vertical : (_ScrollList_Vertical = FindByPath("TestView/ScrollList_Vertical").GetComponent<UniVue.UI.Widegts.LoopList>());
    }
}
#endregion // UniVue Auto-Generated
