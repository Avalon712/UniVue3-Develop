using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UniVue.Editor;
using UniVue.UI;

namespace UniVue.CodeGen
{
    internal sealed class UICodeAutoGen : AssetPostprocessor
    {
        private const string RegionStart = "#region UniVue Auto-Generated \u2014 DO NOT MODIFY";
        private const string RegionEnd = "#endregion // UniVue Auto-Generated";

        private static List<UICodeGenRule> _rules;

        private static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            ManifestData manifest = LoadManifest();
            bool dirty = false;

            foreach (string path in importedAssets)
            {
                if (!path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) continue;
                dirty |= ProcessPrefab(path, manifest);
            }

            foreach (string path in deletedAssets)
            {
                if (!path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) continue;
                dirty |= HandleDeletedPrefab(path, manifest);
            }

            for (int i = 0; i < movedAssets.Length; i++)
            {
                if (!movedAssets[i].EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) continue;
                PrefabRecord record = manifest.records.Find(r => r.prefabPath == movedFromAssetPaths[i]);
                if (record != null)
                {
                    record.prefabPath = movedAssets[i];
                    dirty = true;
                }
            }

            if (dirty) SaveManifest(manifest);
        }

        [MenuItem("UniVue/Code Gen/GenerateUICode")]
        public static void ForceGenerateAll()
        {
            string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab");
            ManifestData manifest = LoadManifest();

            foreach (PrefabRecord record in manifest.records)
                StripAutoGenRegionFromFile(record.scriptPath);

            manifest.records.Clear();
            bool dirty = false;

            foreach (string guid in allPrefabs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                dirty |= ProcessPrefab(path, manifest);
            }

            if (dirty) SaveManifest(manifest);
            AssetDatabase.Refresh();
        }

        #region Rule Discovery

        private static List<UICodeGenRule> GetRules()
        {
            if (_rules != null) return _rules;

            TypeCache.TypeCollection allTypes = TypeCache.GetTypesDerivedFrom<UICodeGenRule>();
            List<Type> concreteTypes = new();
            foreach (Type t in allTypes)
                if (!t.IsAbstract)
                    concreteTypes.Add(t);

            _rules = new List<UICodeGenRule>();
            foreach (Type t in concreteTypes)
            {
                bool isLeaf = true;
                foreach (Type other in concreteTypes)
                {
                    if (other != t && t.IsAssignableFrom(other))
                    {
                        isLeaf = false;
                        break;
                    }
                }
                if (isLeaf)
                    _rules.Add((UICodeGenRule)Activator.CreateInstance(t));
            }

            _rules.Sort();
            return _rules;
        }

        #endregion

        #region Prefab Processing

        private static bool ProcessPrefab(string prefabPath, ManifestData manifest)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (!prefab || !prefab.TryGetComponent(out BaseUI rootUI)) return false;

            List<UICodeGenRule> rules = GetRules();
            Type rootType = rootUI.GetType();

            HashSet<GeneratedProperty> properties = new();
            bool anyAccepted = false;

            foreach (UICodeGenRule rule in rules)
            {
                if (!rule.InvokeFilter(prefab, rootUI)) continue;
                anyAccepted = true;
                rule.InvokeTryGenProperties(rootType, prefab, properties);
            }

            if (!anyAccepted) return false;

            string newHash = ComputeHash(properties);
            PrefabRecord record = manifest.records.Find(r => r.prefabPath == prefabPath);
            if (record != null && record.fieldsHash == newHash)
                return false;

            MonoScript monoScript = MonoScript.FromMonoBehaviour(rootUI);
            if (!monoScript) return false;
            string scriptAssetPath = AssetDatabase.GetAssetPath(monoScript);
            if (string.IsNullOrEmpty(scriptAssetPath)) return false;

            string fullPath = Path.GetFullPath(scriptAssetPath);
            if (!File.Exists(fullPath)) return false;

            if (record != null && record.scriptPath != scriptAssetPath)
                StripAutoGenRegionFromFile(record.scriptPath);

            string content = File.ReadAllText(fullPath, Encoding.UTF8);
            content = StripAutoGenRegion(content);

            if (properties.Count > 0)
            {
                string code = BuildPartialClass(rootType.Namespace, rootType.Name, properties);
                content = content.TrimEnd() + "\n\n" + code + "\n";
            }

            File.WriteAllText(fullPath, content, Encoding.UTF8);

            if (record != null)
            {
                record.scriptPath = scriptAssetPath;
                record.typeFullName = rootType.FullName;
                record.fieldsHash = newHash;
            }
            else
            {
                manifest.records.Add(new PrefabRecord
                {
                    prefabPath = prefabPath,
                    scriptPath = scriptAssetPath,
                    typeFullName = rootType.FullName,
                    fieldsHash = newHash
                });
            }

            return true;
        }

        private static bool HandleDeletedPrefab(string prefabPath, ManifestData manifest)
        {
            int idx = manifest.records.FindIndex(r => r.prefabPath == prefabPath);
            if (idx < 0) return false;

            StripAutoGenRegionFromFile(manifest.records[idx].scriptPath);
            manifest.records.RemoveAt(idx);
            return true;
        }

        #endregion

        #region Code Generation

        private static string BuildPartialClass(string ns, string className, HashSet<GeneratedProperty> properties)
        {
            List<GeneratedProperty> sorted = new(properties);
            sorted.Sort((a, b) => string.Compare(a.path, b.path, StringComparison.Ordinal));

            StringBuilder sb = new();
            sb.AppendLine(RegionStart);

            bool hasNs = !string.IsNullOrEmpty(ns);
            string ci = hasNs ? "    " : "";
            string mi = hasNs ? "        " : "    ";

            if (hasNs)
            {
                sb.AppendLine($"namespace {ns}");
                sb.AppendLine("{");
            }

            sb.AppendLine($"{ci}partial class {className}");
            sb.AppendLine($"{ci}{{");

            for (int i = 0; i < sorted.Count; i++)
            {
                GeneratedProperty p = sorted[i];
                string bk = $"_{p.propertyName}";
                sb.AppendLine($"{mi}private {p.propertyTypeFullName} {bk};");
                sb.AppendLine(
                    $"{mi}public {p.propertyTypeFullName} {p.propertyName} => {bk} ? {bk} : ({bk} = FindByPath(\"{p.path}\")?.GetComponent<{p.propertyTypeFullName}>());");
                if (i < sorted.Count - 1) sb.AppendLine();
            }

            sb.AppendLine($"{ci}}}");
            if (hasNs) sb.AppendLine("}");
            sb.Append(RegionEnd);
            return sb.ToString();
        }

        private static void StripAutoGenRegionFromFile(string scriptAssetPath)
        {
            if (string.IsNullOrEmpty(scriptAssetPath)) return;
            string fullPath = Path.GetFullPath(scriptAssetPath);
            if (!File.Exists(fullPath)) return;
            string content = File.ReadAllText(fullPath, Encoding.UTF8);
            string stripped = StripAutoGenRegion(content);
            if (stripped != content)
                File.WriteAllText(fullPath, stripped.TrimEnd() + "\n", Encoding.UTF8);
        }

        private static string StripAutoGenRegion(string content)
        {
            int start = content.IndexOf(RegionStart, StringComparison.Ordinal);
            if (start < 0) return content;
            int end = content.IndexOf(RegionEnd, start, StringComparison.Ordinal);
            string before = content.Substring(0, start).TrimEnd();
            if (end < 0) return before;
            end += RegionEnd.Length;
            while (end < content.Length && (content[end] == '\r' || content[end] == '\n')) end++;
            string after = content.Substring(end).TrimEnd();
            return string.IsNullOrEmpty(after) ? before : before + "\n\n" + after;
        }

        #endregion

        #region Manifest

        private static ManifestData LoadManifest()
        {
            string json = UICodeAutoGenManifest.instance.manifestJson;
            if (string.IsNullOrEmpty(json)) return new ManifestData();
            try { return JsonUtility.FromJson<ManifestData>(json) ?? new ManifestData(); }
            catch { return new ManifestData(); }
        }

        private static void SaveManifest(ManifestData data)
        {
            UICodeAutoGenManifest.instance.manifestJson = JsonUtility.ToJson(data, false);
            UICodeAutoGenManifest.instance.Save();
        }

        private static string ComputeHash(HashSet<GeneratedProperty> properties)
        {
            List<GeneratedProperty> sorted = new(properties);
            sorted.Sort((a, b) => string.Compare(a.path, b.path, StringComparison.Ordinal));

            StringBuilder sb = new();
            foreach (GeneratedProperty p in sorted)
                sb.Append(p.propertyTypeFullName).Append(':').Append(p.propertyName).Append(':').Append(p.path).Append(';');
            using MD5 md5 = MD5.Create();
            return Convert.ToBase64String(md5.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString())));
        }

        [Serializable]
        private class ManifestData { public List<PrefabRecord> records = new(); }

        [Serializable]
        private class PrefabRecord
        {
            public string prefabPath;
            public string scriptPath;
            public string typeFullName;
            public string fieldsHash;
        }

        #endregion
    }
}
