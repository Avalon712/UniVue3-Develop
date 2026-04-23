using System;
using System.Collections;
using System.Collections.Generic;
using Framwork.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UniVue.Event;

namespace UniVue.UI
{
    [DisallowMultipleComponent]
    public sealed class EventUI : BaseUI
    {
        public enum ArgumentType
        {
            None,
            Bool,
            Int,
            Float,
            String,
            Vector2,
        }

        public enum EventUIType
        {
            Button,
            InputField,
            Slider,
            Dropdown,
            Toggle,
            Scrollbar,
            ScrollRect
        }

        public enum InputFieldEventTriggerType
        {
            OnValueChanged,
            OnEndEdit
        }

        [SerializeField] private string _eventName;
        [SerializeField] private EventUIType _eventUIType;
        [SerializeField] private InputFieldEventTriggerType _triggerType = InputFieldEventTriggerType.OnEndEdit;
        [SerializeField] private List<Argument> _arguments;

        public string EventName => _eventName;

        public EventUIType UIType => _eventUIType;

        protected override void OnCreate()
        {
            switch (_eventUIType)
            {
                case EventUIType.Button:
                    {
                        Button button = transform.GetComponent<Button>();
                        ExceptionUtils.ThrowIfNull(button, $"Button component not found on {gameObject}");

                        _arguments.Insert(0, new Argument { type = ArgumentType.None });

                        button?.onClick.AddListener(() =>
                        {
                            EventMgr.Dispatch(_eventName, new Arguments(_eventUIType, _arguments));
                        });
                        break;
                    }
                case EventUIType.InputField:
                    {
                        TMP_InputField inputFieldTMP = transform.GetComponent<TMP_InputField>();
                        if (inputFieldTMP)
                        {
                            _arguments.Insert(0, new Argument { type = ArgumentType.String });

                            switch (_triggerType)
                            {
                                case InputFieldEventTriggerType.OnValueChanged:
                                    inputFieldTMP.onValueChanged.AddListener(value =>
                                    {
                                        _arguments[0].stringV = inputFieldTMP.text;
                                        EventMgr.Dispatch(_eventName, new Arguments(_eventUIType, _arguments));
                                    });
                                    break;
                                case InputFieldEventTriggerType.OnEndEdit:
                                    inputFieldTMP.onEndEdit.AddListener(value =>
                                    {
                                        _arguments[0].stringV = inputFieldTMP.text;
                                        EventMgr.Dispatch(_eventName, new Arguments(_eventUIType, _arguments));
                                    });
                                    break;
                            }
                        }
                        else
                        {
                            InputField inputField = transform.GetComponent<InputField>();
                            if (inputField)
                            {
                                _arguments.Insert(0, new Argument { type = ArgumentType.String });

                                switch (_triggerType)
                                {
                                    case InputFieldEventTriggerType.OnValueChanged:
                                        inputField.onValueChanged.AddListener(value =>
                                        {
                                            EventMgr.Dispatch(_eventName,
                                                              new Arguments(_eventUIType, _arguments));
                                        });
                                        break;
                                    case InputFieldEventTriggerType.OnEndEdit:
                                        inputField.onEndEdit.AddListener(value =>
                                        {
                                            EventMgr.Dispatch(_eventName,
                                                              new Arguments(_eventUIType, _arguments));
                                        });
                                        break;
                                }
                            }

                            ExceptionUtils.ThrowIfNull(inputField, $"InputField component not found on {gameObject}");
                        }

                        ExceptionUtils.ThrowIfNull(inputFieldTMP, $"InputFieldTMP component not found on {gameObject}");
                        break;
                    }
                case EventUIType.Slider:
                    {
                        Slider slider = transform.GetComponent<Slider>();
                        ExceptionUtils.ThrowIfNull(slider, $"Slider component not found on {gameObject}");

                        _arguments.Insert(0, new Argument { type = ArgumentType.Float });

                        slider?.onValueChanged.AddListener(value =>
                        {
                            _arguments[0].floatV = value;
                            EventMgr.Dispatch(_eventName, new Arguments(_eventUIType, _arguments));
                        });
                        break;
                    }
                case EventUIType.Dropdown:
                    {
                        TMP_Dropdown dropdownTMP = GetComponent<TMP_Dropdown>();
                        if (dropdownTMP)
                        {
                            _arguments.Insert(0, new Argument { type = ArgumentType.Int });

                            dropdownTMP.onValueChanged.AddListener(value =>
                            {
                                _arguments[0].intV = value;
                                EventMgr.Dispatch(_eventName, new Arguments(_eventUIType, _arguments));
                            });
                        }
                        else
                        {
                            Dropdown dropdown = transform.GetComponent<Dropdown>();
                            if (dropdown)
                            {
                                _arguments.Insert(0, new Argument { type = ArgumentType.Int });

                                dropdown.onValueChanged.AddListener(value =>
                                {
                                    _arguments[0].intV = value;
                                    EventMgr.Dispatch(_eventName, new Arguments(_eventUIType, _arguments));
                                });
                            }

                            ExceptionUtils.ThrowIfNull(dropdown, $"Dropdown component not found on {gameObject}");
                        }

                        ExceptionUtils.ThrowIfNull(dropdownTMP, $"TMP_Dropdown component not found on {gameObject}");
                        break;
                    }
                case EventUIType.Toggle:
                    {
                        Toggle toggle = transform.GetComponent<Toggle>();
                        ExceptionUtils.ThrowIfNull(toggle, $"Toggle component not found on {gameObject}");

                        _arguments.Insert(0, new Argument { type = ArgumentType.Bool });

                        toggle?.onValueChanged.AddListener(value =>
                        {
                            _arguments[0].boolV = value;
                            EventMgr.Dispatch(_eventName, new Arguments(_eventUIType, _arguments));
                        });
                        break;
                    }
                case EventUIType.Scrollbar:
                    {
                        Scrollbar scrollbar = transform.GetComponent<Scrollbar>();
                        ExceptionUtils.ThrowIfNull(scrollbar, $"Scrollbar component not found on {gameObject}");

                        _arguments.Insert(0, new Argument { type = ArgumentType.Float });

                        scrollbar?.onValueChanged.AddListener(value =>
                        {
                            _arguments[0].floatV = value;
                            EventMgr.Dispatch(_eventName, new Arguments(_eventUIType, _arguments));
                        });
                        break;
                    }
                case EventUIType.ScrollRect:
                    {
                        ScrollRect scrollRect = transform.GetComponent<ScrollRect>();
                        ExceptionUtils.ThrowIfNull(scrollRect, $"ScrollRect component not found on {gameObject}");

                        _arguments.Insert(0, new Argument { type = ArgumentType.Vector2 });

                        scrollRect?.onValueChanged.AddListener(value =>
                        {
                            _arguments[0].vector2V = value;
                            EventMgr.Dispatch(_eventName, new Arguments(_eventUIType, _arguments));
                        });
                        break;
                    }
            }
        }

        [Serializable]
        public sealed class Argument
        {
            public ArgumentType type;
            public string keyName;
            public bool boolV;
            public int intV;
            public float floatV;
            public string stringV;
            public Vector2 vector2V;

            public override string ToString()
            {
                return type switch
                {
                    ArgumentType.None => $"[NONE]{keyName} = null",
                    ArgumentType.Bool => $"[BOOL]{keyName} = {boolV}",
                    ArgumentType.Int => $"[INT]{keyName} = {intV}",
                    ArgumentType.Float => $"[FLOAT]{keyName} = {floatV}",
                    ArgumentType.String => $"[STRING]{keyName} = {stringV}",
                    ArgumentType.Vector2 => $"[VECTOR2]{keyName} = {vector2V}",
                    _ => $"[UNKNOWN]{keyName} = {type}"
                };
            }
        }

        public readonly struct Arguments : IEquatable<Arguments>, IEnumerable<Argument>
        {
            private readonly List<Argument> _arguments;
            public readonly string defaultKey;

            internal Arguments(EventUIType eventUIType, List<Argument> arguments)
            {
                _arguments = arguments;
                defaultKey = eventUIType switch
                {
                    EventUIType.Button => "Button",
                    EventUIType.InputField => "InputField",
                    EventUIType.Slider => "Slider",
                    EventUIType.Dropdown => "Dropdown",
                    EventUIType.Toggle => "Toggle",
                    EventUIType.Scrollbar => "Scrollbar",
                    EventUIType.ScrollRect => "ScrollRect",
                    _ => throw new ArgumentException($"Invalid UI type: {eventUIType}")
                };
            }

            /// <summary>
            /// TMP_Dropdown, Dropdown
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public bool TryGetUIValue(out int value)
            {
                return TryGetValue(defaultKey, out value);
            }

            /// <summary>
            /// Slider, Scrollbar
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public bool TryGetUIValue(out float value)
            {
                return TryGetValue(defaultKey, out value);
            }

            /// <summary>
            /// Toggle
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public bool TryGetUIValue(out bool value)
            {
                return TryGetValue(defaultKey, out value);
            }

            /// <summary>
            /// InputField, TMP_InputField
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public bool TryGetUIValue(out string value)
            {
                return TryGetValue(defaultKey, out value);
            }

            /// <summary>
            /// ScrollRect
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public bool TryGetUIValue(out Vector2 value)
            {
                return TryGetValue(defaultKey, out value);
            }

            public bool TryGetValue(string key, out int value)
            {
                foreach (Argument arg in _arguments)
                    if (arg.keyName == key && arg.type == ArgumentType.Int)
                    {
                        value = arg.intV;
                        return true;
                    }

                value = 0;
                return false;
            }

            public bool TryGetValue(string key, out float value)
            {
                foreach (Argument arg in _arguments)
                    if (arg.keyName == key && arg.type == ArgumentType.Float)
                    {
                        value = arg.floatV;
                        return true;
                    }

                value = 0f;
                return false;
            }

            public bool TryGetValue(string key, out string value)
            {
                foreach (Argument arg in _arguments)
                    if (arg.keyName == key && arg.type == ArgumentType.String)
                    {
                        value = arg.stringV;
                        return true;
                    }

                value = null;
                return false;
            }

            public bool TryGetValue(string key, out bool value)
            {
                foreach (Argument arg in _arguments)
                    if (arg.keyName == key && arg.type == ArgumentType.Bool)
                    {
                        value = arg.boolV;
                        return true;
                    }

                value = false;
                return false;
            }

            public bool TryGetValue(string key, out Vector2 value)
            {
                foreach (Argument arg in _arguments)
                    if (arg.keyName == key && arg.type == ArgumentType.Vector2)
                    {
                        value = arg.vector2V;
                        return true;
                    }

                value = Vector2.zero;
                return false;
            }

            public bool Equals(Arguments other)
            {
                return Equals(_arguments, other._arguments);
            }

            public IEnumerator<Argument> GetEnumerator()
            {
                return _arguments.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}