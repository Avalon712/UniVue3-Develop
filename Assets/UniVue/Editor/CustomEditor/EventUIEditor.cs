using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UniVue.UI;

namespace UniVue.Editor
{
    [CustomEditor(typeof(EventUI))]
    public sealed class EventUIEditor : UnityEditor.Editor
    {
        private static readonly EventUI.EventUIType[] OrderedUiTypes =
        {
            EventUI.EventUIType.Button,
            EventUI.EventUIType.InputField,
            EventUI.EventUIType.Slider,
            EventUI.EventUIType.Dropdown,
            EventUI.EventUIType.Toggle,
            EventUI.EventUIType.Scrollbar,
            EventUI.EventUIType.ScrollRect,
        };

        private SerializedProperty _eventName;
        private SerializedProperty _eventUIType;
        private SerializedProperty _triggerType;
        private SerializedProperty _arguments;

        private bool _argumentsFoldout = true;
        private readonly List<bool> _argumentItemFoldouts = new List<bool>();

        private static readonly GUIContent ValueLabel = new GUIContent("Value");
        private static GUIStyle _miniToolOverlayLabelStyle;

        private void OnEnable()
        {
            _eventName = serializedObject.FindProperty("_eventName");
            _eventUIType = serializedObject.FindProperty("_eventUIType");
            _triggerType = serializedObject.FindProperty("_triggerType");
            _arguments = serializedObject.FindProperty("_arguments");
            SyncArgumentItemFoldouts();
        }

        /// <summary>
        /// 仅用 GUI.Button+GUIContent 在部分 Unity/主题下内容不绘制；在 Repaint 中自行叠画图标或文字。
        /// </summary>
        private static bool MiniToolButton(Rect r, string textFallback, string tooltip, params string[] iconNames)
        {
            bool pressed = GUI.Button(r, new GUIContent(string.Empty, tooltip), EditorStyles.miniButton);
            if (UnityEngine.Event.current.type != EventType.Repaint)
                return pressed;

            Texture tex = TryLoadBuiltinIcon(iconNames);
            if (tex != null)
            {
                const float pad = 2f;
                float s = Mathf.Min(r.width - pad * 2f, r.height - pad * 2f);
                if (s > 1f)
                {
                    float x = r.x + (r.width - s) * 0.5f;
                    float y = r.y + (r.height - s) * 0.5f;
                    GUI.DrawTexture(new Rect(x, y, s, s), tex, ScaleMode.ScaleToFit, true);
                }
            }
            else
            {
                if (_miniToolOverlayLabelStyle == null)
                {
                    _miniToolOverlayLabelStyle = new GUIStyle(EditorStyles.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold,
                        clipping = TextClipping.Clip,
                    };
                }

                GUI.Label(r, textFallback, _miniToolOverlayLabelStyle);
            }

            return pressed;
        }

        private static Texture TryLoadBuiltinIcon(params string[] iconNames)
        {
            foreach (string name in iconNames)
            {
                GUIContent c = EditorGUIUtility.IconContent(name);
                if (c != null && c.image != null && c.image.width > 4 && c.image.height > 4)
                    return c.image;
            }

            return null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var eventUi = (EventUI)target;
            List<EventUI.EventUIType> available = CollectAvailableTypes(eventUi.gameObject);

            if (available.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "当前物体上未检测到 EventUI 支持的 UI 组件（Button、InputField/TMP_InputField、Slider、Dropdown/TMP_Dropdown、Toggle、Scrollbar、ScrollRect）。",
                    MessageType.Warning);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            if (available.Count == 1)
            {
                int desired = (int)available[0];
                if (_eventUIType.enumValueIndex != desired)
                    _eventUIType.enumValueIndex = desired;

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(_eventUIType);
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                var current = (EventUI.EventUIType)_eventUIType.enumValueIndex;
                int selected = available.IndexOf(current);
                if (selected < 0)
                {
                    selected = 0;
                    _eventUIType.enumValueIndex = (int)available[0];
                }

                var labels = new string[available.Count];
                for (int i = 0; i < available.Count; i++)
                    labels[i] = ObjectNames.NicifyVariableName(available[i].ToString());

                EditorGUI.BeginChangeCheck();
                int newSelected = EditorGUILayout.Popup("Event UI Type", selected, labels);
                if (EditorGUI.EndChangeCheck() && newSelected != selected)
                    _eventUIType.enumValueIndex = (int)available[newSelected];
            }

            var uiType = (EventUI.EventUIType)_eventUIType.enumValueIndex;
            if (uiType == EventUI.EventUIType.InputField)
                EditorGUILayout.PropertyField(_triggerType);

            EditorGUILayout.PropertyField(_eventName);

            EditorGUILayout.Space();
            DrawArgumentsList(_arguments);

            serializedObject.ApplyModifiedProperties();
        }

        private void SyncArgumentItemFoldouts()
        {
            if (_arguments == null) return;
            while (_argumentItemFoldouts.Count < _arguments.arraySize)
                _argumentItemFoldouts.Add(true);
            while (_argumentItemFoldouts.Count > _arguments.arraySize)
                _argumentItemFoldouts.RemoveAt(_argumentItemFoldouts.Count - 1);
        }

        private void DrawArgumentsList(SerializedProperty listProp)
        {
            SyncArgumentItemFoldouts();

            _argumentsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_argumentsFoldout, "Arguments");
            if (_argumentsFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Size");
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.IntField(listProp.arraySize, GUILayout.Width(52));
                EditorGUI.EndDisabledGroup();
                GUILayout.FlexibleSpace();
                Rect addBtnRect = GUILayoutUtility.GetRect(30f, 20f, GUILayout.Width(30f), GUILayout.Height(20f));
                if (MiniToolButton(addBtnRect, "+", "添加参数", "Toolbar Plus", "d_Toolbar Plus",
                        "Toolbar Plus More", "CreateAddNew"))
                {
                    AddArgumentItem(listProp);
                    SyncArgumentItemFoldouts();
                }

                EditorGUILayout.EndHorizontal();

                if (TryGetDuplicateKeyName(listProp, out string dupKey))
                    EditorGUILayout.HelpBox($"存在重复的 Key Name：\"{dupKey}\"，请保证非空 Key Name 唯一。", MessageType.Error);

                for (int i = 0; i < listProp.arraySize; i++)
                {
                    SerializedProperty el = listProp.GetArrayElementAtIndex(i);
                    string header = GetArgumentHeaderLabel(el, i);

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    const float removeBtnW = 30f;
                    Rect headerLine = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                    Rect foldRect = new Rect(headerLine.x, headerLine.y,
                        Mathf.Max(0f, headerLine.width - removeBtnW - 2f), headerLine.height);
                    Rect removeRect = new Rect(headerLine.xMax - removeBtnW, headerLine.y, removeBtnW,
                        headerLine.height);
                    _argumentItemFoldouts[i] = EditorGUI.Foldout(foldRect, _argumentItemFoldouts[i], header, true);
                    if (MiniToolButton(removeRect, "-", "删除此项"))
                    {
                        listProp.DeleteArrayElementAtIndex(i);
                        if (i < _argumentItemFoldouts.Count)
                            _argumentItemFoldouts.RemoveAt(i);
                        serializedObject.ApplyModifiedProperties();
                        GUIUtility.ExitGUI();
                    }

                    if (_argumentItemFoldouts[i])
                    {
                        EditorGUI.indentLevel++;
                        DrawArgumentElement(el);
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.EndVertical();
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawArgumentElement(SerializedProperty arg)
        {
            SerializedProperty typeProp = arg.FindPropertyRelative("type");
            EditorGUILayout.PropertyField(typeProp);
            EditorGUILayout.PropertyField(arg.FindPropertyRelative("keyName"));

            var argType = (EventUI.ArgumentType)typeProp.enumValueIndex;
            switch (argType)
            {
                case EventUI.ArgumentType.None:
                    break;
                case EventUI.ArgumentType.Bool:
                    EditorGUILayout.PropertyField(arg.FindPropertyRelative("boolV"), ValueLabel);
                    break;
                case EventUI.ArgumentType.Int:
                    EditorGUILayout.PropertyField(arg.FindPropertyRelative("intV"), ValueLabel);
                    break;
                case EventUI.ArgumentType.Float:
                    EditorGUILayout.PropertyField(arg.FindPropertyRelative("floatV"), ValueLabel);
                    break;
                case EventUI.ArgumentType.String:
                    EditorGUILayout.PropertyField(arg.FindPropertyRelative("stringV"), ValueLabel);
                    break;
                case EventUI.ArgumentType.Vector2:
                    EditorGUILayout.PropertyField(arg.FindPropertyRelative("vector2V"), ValueLabel);
                    break;
            }
        }

        private static string GetArgumentHeaderLabel(SerializedProperty el, int index)
        {
            SerializedProperty keyProp = el.FindPropertyRelative("keyName");
            string key = keyProp != null ? keyProp.stringValue : null;
            if (!string.IsNullOrEmpty(key))
                return key;
            return $"Element {index}";
        }

        private static bool TryGetDuplicateKeyName(SerializedProperty listProp, out string duplicateKey)
        {
            duplicateKey = null;
            var seen = new HashSet<string>();
            for (int i = 0; i < listProp.arraySize; i++)
            {
                SerializedProperty kn = listProp.GetArrayElementAtIndex(i).FindPropertyRelative("keyName");
                if (kn == null) continue;
                string v = kn.stringValue;
                if (string.IsNullOrEmpty(v)) continue;
                if (!seen.Add(v))
                {
                    duplicateKey = v;
                    return true;
                }
            }

            return false;
        }

        private static void AddArgumentItem(SerializedProperty listProp)
        {
            int n = listProp.arraySize;
            listProp.arraySize = n + 1;
            ResetArgumentElement(listProp.GetArrayElementAtIndex(n));
        }

        private static void ResetArgumentElement(SerializedProperty el)
        {
            el.FindPropertyRelative("type").enumValueIndex = (int)EventUI.ArgumentType.None;
            el.FindPropertyRelative("keyName").stringValue = "";
            el.FindPropertyRelative("boolV").boolValue = false;
            el.FindPropertyRelative("intV").intValue = 0;
            el.FindPropertyRelative("floatV").floatValue = 0f;
            el.FindPropertyRelative("stringV").stringValue = "";
            el.FindPropertyRelative("vector2V").vector2Value = Vector2.zero;
        }

        private static List<EventUI.EventUIType> CollectAvailableTypes(GameObject go)
        {
            var list = new List<EventUI.EventUIType>();
            foreach (EventUI.EventUIType t in OrderedUiTypes)
            {
                if (HasUiType(go, t))
                    list.Add(t);
            }

            return list;
        }

        private static bool HasUiType(GameObject go, EventUI.EventUIType type)
        {
            switch (type)
            {
                case EventUI.EventUIType.Button:
                    return go.GetComponent<Button>();
                case EventUI.EventUIType.InputField:
                    return go.GetComponent<InputField>() || HasComponentWithFullName(go, "TMPro.TMP_InputField");
                case EventUI.EventUIType.Slider:
                    return go.GetComponent<Slider>();
                case EventUI.EventUIType.Dropdown:
                    return go.GetComponent<Dropdown>() || HasComponentWithFullName(go, "TMPro.TMP_Dropdown");
                case EventUI.EventUIType.Toggle:
                    return go.GetComponent<Toggle>();
                case EventUI.EventUIType.Scrollbar:
                    return go.GetComponent<Scrollbar>();
                case EventUI.EventUIType.ScrollRect:
                    return go.GetComponent<ScrollRect>();
                default:
                    return false;
            }
        }

        private static bool HasComponentWithFullName(GameObject go, string typeFullName)
        {
            foreach (Component c in go.GetComponents<Component>())
            {
                if (c == null) continue;
                if (c.GetType().FullName == typeFullName)
                    return true;
            }

            return false;
        }
    }
}
