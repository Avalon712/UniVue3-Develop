using System;
using System.Collections.Generic;
using UnityEngine;
using UniVue.UI;

namespace UniVue.Editor
{
    internal sealed class InternalUICodeGenRule : UICodeGenRule
    {
        public override int Order => 0;

        internal override int InternalRuleOrder { get; set; } = int.MinValue;

        private static readonly Dictionary<UGUIType, string> UGUITypeFullNameMap = new()
        {
            [UGUIType.Image] = "UnityEngine.UI.Image",
            [UGUIType.RawImage] = "UnityEngine.UI.RawImage",
            [UGUIType.Text] = "UnityEngine.UI.Text",
            [UGUIType.Button] = "UnityEngine.UI.Button",
            [UGUIType.Toggle] = "UnityEngine.UI.Toggle",
            [UGUIType.Slider] = "UnityEngine.UI.Slider",
            [UGUIType.Scrollbar] = "UnityEngine.UI.Scrollbar",
            [UGUIType.Dropdown] = "UnityEngine.UI.Dropdown",
            [UGUIType.InputField] = "UnityEngine.UI.InputField",
            [UGUIType.ScrollRect] = "UnityEngine.UI.ScrollRect",
            [UGUIType.HorizontalLayoutGroup] = "UnityEngine.UI.HorizontalLayoutGroup",
            [UGUIType.VerticalLayoutGroup] = "UnityEngine.UI.VerticalLayoutGroup",
            [UGUIType.GridLayoutGroup] = "UnityEngine.UI.GridLayoutGroup",
            [UGUIType.LayoutElement] = "UnityEngine.UI.LayoutElement",
            [UGUIType.ContentSizeFitter] = "UnityEngine.UI.ContentSizeFitter",
            [UGUIType.AspectRatioFitter] = "UnityEngine.UI.AspectRatioFitter",
            [UGUIType.Canvas] = "UnityEngine.Canvas",
            [UGUIType.CanvasGroup] = "UnityEngine.CanvasGroup",
            [UGUIType.CanvasScaler] = "UnityEngine.UI.CanvasScaler",
            [UGUIType.GraphicRaycaster] = "UnityEngine.UI.GraphicRaycaster",
            [UGUIType.Mask] = "UnityEngine.UI.Mask",
            [UGUIType.RectMask2D] = "UnityEngine.UI.RectMask2D",
            [UGUIType.Outline] = "UnityEngine.UI.Outline",
            [UGUIType.Shadow] = "UnityEngine.UI.Shadow",
            [UGUIType.PositionAsUV1] = "UnityEngine.UI.PositionAsUV1",
            [UGUIType.ToggleGroup] = "UnityEngine.UI.ToggleGroup",
            [UGUIType.Selectable] = "UnityEngine.UI.Selectable",
            [UGUIType.TextMeshProUGUI] = "TMPro.TextMeshProUGUI",
            [UGUIType.TMP_InputField] = "TMPro.TMP_InputField",
            [UGUIType.TMP_Dropdown] = "TMPro.TMP_Dropdown",
        };

        private static readonly HashSet<string> CSharpKeywords = new()
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
            "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
            "void", "volatile", "while"
        };

        protected override bool Filter(GameObject prefab, BaseUI baseUI)
        {
            if (baseUI is not BaseView && baseUI is not BaseComponent) return false;
            var attr = (DontGenUICodeAttribute)
                Attribute.GetCustomAttribute(baseUI.GetType(), typeof(DontGenUICodeAttribute));
            return attr is not { Code: UIGenCode.Class };
        }

        protected override bool TryGenProperties(Type clazz, GameObject go, HashSet<GeneratedProperty> properties)
        {
            UniVueEditorSettings settings = UniVueEditorSettings.instance;
            CollectFields(go.transform, properties, go.name,
                          settings.uiTypes, settings.typeSuffixes);
            return properties.Count > 0;
        }

        private static void CollectFields(Transform parent, HashSet<GeneratedProperty> properties, string parentPath,
                                          UGUIType[] uiTypes, string[] suffixes)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                string goName = child.gameObject.name;
                string childPath = parentPath + "/" + goName;

                if (child.TryGetComponent(out BaseUI ui))
                {
                    if (ui is BaseView) continue;

                    Type t = ui.GetType();
                    bool skipProperty = HasDontGenPropertyAttribute(t);

                    if (ui is BaseComponent)
                    {
                        if (!skipProperty)
                            TryAddProperty(goName, ToCSharpTypeName(t), childPath, properties);
                        continue;
                    }

                    if (!skipProperty)
                        TryAddProperty(goName, ToCSharpTypeName(t), childPath, properties);
                    CollectFields(child, properties, childPath, uiTypes, suffixes);
                    continue;
                }

                if (TryMatchUGUISuffix(goName, uiTypes, suffixes, out string uguiFullName))
                    TryAddProperty(goName, uguiFullName, childPath, properties);

                CollectFields(child, properties, childPath, uiTypes, suffixes);
            }
        }

        private static void TryAddProperty(string goName, string typeName, string path,
                                           HashSet<GeneratedProperty> properties)
        {
            if (!IsValidCSharpIdentifier(goName)) return;
            properties.Add(new GeneratedProperty(typeName, goName, path));
        }

        private static bool TryMatchUGUISuffix(string goName, UGUIType[] uiTypes, string[] suffixEntries,
                                               out string uguiFullName)
        {
            uguiFullName = null;
            if (uiTypes == null || suffixEntries == null) return false;
            int len = Math.Min(uiTypes.Length, suffixEntries.Length);

            for (int i = 0; i < len; i++)
            {
                if (string.IsNullOrEmpty(suffixEntries[i])) continue;
                foreach (string part in suffixEntries[i].Split(','))
                {
                    string suffix = part.Trim();
                    if (suffix.Length > 0 && suffix.Length <= goName.Length &&
                        goName.EndsWith(suffix, StringComparison.Ordinal) &&
                        UGUITypeFullNameMap.TryGetValue(uiTypes[i], out uguiFullName))
                        return true;
                }
            }

            return false;
        }

        private static bool HasDontGenPropertyAttribute(Type type)
        {
            var attr = (DontGenUICodeAttribute)
                Attribute.GetCustomAttribute(type, typeof(DontGenUICodeAttribute));
            return attr is { Code: UIGenCode.Property };
        }

        private static string ToCSharpTypeName(Type type)
        {
            return (type.FullName ?? type.Name).Replace('+', '.');
        }

        private static bool IsValidCSharpIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (CSharpKeywords.Contains(name)) return false;
            if (!char.IsLetter(name[0]) && name[0] != '_') return false;
            for (int i = 1; i < name.Length; i++)
                if (!char.IsLetterOrDigit(name[i]) && name[i] != '_')
                    return false;
            return true;
        }
    }
}
