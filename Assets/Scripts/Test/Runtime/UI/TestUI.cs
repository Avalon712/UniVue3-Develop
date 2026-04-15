using System;
using Game.UI;
using UnityEngine;
using UniVue.Model;
using UniVue.Timer;
using UniVue.UI;

namespace Game
{
    public class TestUI : MonoBehaviour
    {
        // Start is called before the first frame update
        private void Start()
        {
            // MyClass myClass = new();
            // myClass.Age = 19;
            // myClass.Name = "Test";
            // myClass.Description = "This is a test.";
            // Debug.Log(myClass);
            // myClass.OnPropertyChanged += (model, propertyName, newValue) =>
            // {
            //     Debug.Log($"MyClass property changed: {propertyName} = {newValue}");
            // };
            // myClass.Age = 20;
            // myClass.Name = "Test2";
            // myClass.Description = "This is another test.";
            // Debug.Log(myClass);
            //
            // MyClass2 myClass2 = new();
            // myClass2.Age = 25;
            // myClass2.Name = "Test3";
            // myClass2.Description = "This is yet another test.";
            // myClass2.Height = 20;
            // Debug.Log(myClass2);
            //
            // myClass2.OnPropertyChanged += (model, propertyName, newValue) =>
            // {
            //     Debug.Log($"MyClass2 property changed: {propertyName} = {newValue}");
            // };
            // myClass2.Age = 26;
            // myClass2.Name = "Test4";
            // myClass2.Description = "This is yet another test.";
            // myClass2.Height = 21;
            // Debug.Log(myClass2);


            UIMgr.Initialize(typeof(RedPointKey), new TestUIPrefabLoader(), DefaultUILayerMgr.Default);


            TimerMgr.AddTimer(1, 0, 1, () =>
            {
                Debug.Log("open TestView");
                UIMgr.Open<TestView>();
            });
            //
            // TimerMgr.AddTimer(0,1,10, () =>
            // {
            //     Debug.Log("test timer");
            // });
            //
            // TimerMgr.AddTimer(0,1,-1, () =>
            // {
            //     Debug.Log("test timer 2 (repeat forever)");
            // });
            //
            // int i = 0;
            // TimerMgr.AddTimer(5, 1, 10, () =>
            // {
            //     Debug.Log($"test timer 3 (delay 5s) execute {++i}");
            // }, () => i < 5);
            //
            // TimerMgr.AddTimer(2, 1, 10, () =>
            // {
            //     Debug.Log($"test timer 4 (delay 2s) cancel {++i}");
            // }, null, ()=> i >= 5);
        }

        private sealed class TestUIPrefabLoader : IUIPrefabLoader
        {
            public void LoadUIPrefabAsync(Type uiType, Action<GameObject> callback)
            {
                GameObject prefab = Resources.Load<GameObject>($"{uiType.Name}");
                callback?.Invoke(prefab);
            }
        }

        private class MyClass : BaseModel
        {
            public string _description;

            public string _name;
            public int Age { get; set; }

            public string Name
            {
                get => _name;
                set => _name = value;
            }

            public string Description
            {
                get => _description;
                set => _description = $"Age: {Age}, Name: {Name}, Extra: {value}";
            }

            public override string ToString()
            {
                return $"{nameof(MyClass)}@{GetHashCode()}{{Age: {Age}, Name: {Name}, Description: {Description}}}";
            }
        }

        private class MyClass2 : MyClass
        {
            public float Height { get; set; }

            public override string ToString()
            {
                return
                    $"{nameof(MyClass2)}@{GetHashCode()}{{Age: {Age}, Name: {Name}, Description: {Description}, Height: {Height}}}";
            }
        }
    }
}