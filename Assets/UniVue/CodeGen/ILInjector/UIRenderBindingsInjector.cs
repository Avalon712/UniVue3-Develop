using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.CompilationPipeline.Common.Diagnostics;

namespace UniVue.CodeGen
{
    internal static class UIInjector
    {
        private const string BaseUIFullName = "UniVue.UI.BaseUI";
        private const string BaseModelFullName = "UniVue.Model.BaseModel";
        private const string RenderNodeBuilderFullName = "UniVue.UI.RenderNodeBuilder";

        private static readonly string moduleActionTypeFullName = typeof(Action).FullName;

        public static bool Inject(AssemblyDefinition assemblyDefinition, List<DiagnosticMessage> diagnostics)
        {
            if (assemblyDefinition == null) return false;

            bool modified = false;
            foreach (ModuleDefinition module in assemblyDefinition.Modules)
                foreach (TypeDefinition type in module.Types)
                    modified |= InjectType(module, type, diagnostics);
            return modified;
        }

        private static bool InjectType(ModuleDefinition module, TypeDefinition type,
                                       List<DiagnosticMessage> diagnostics)
        {
            if (type == null) return false;

            bool modified = false;
            foreach (MethodDefinition method in type.Methods)
                modified |= InjectMethod(module, method, diagnostics);

            foreach (TypeDefinition nestedType in type.NestedTypes)
                modified |= InjectType(module, nestedType, diagnostics);
            return modified;
        }

        private static bool InjectMethod(ModuleDefinition module, MethodDefinition method,
                                         List<DiagnosticMessage> diagnostics)
        {
            if (method == null || !method.HasBody) return false;

            bool modified = false;
            ILProcessor il = method.Body.GetILProcessor();
            MethodReference bindBuilderRef = FindBindBuilderMethod(module, method);
            MethodReference onModelRef = FindBuilderOnModelMethod(module, method);
            MethodReference onModelWithPropertiesRef = FindBuilderOnModelWithPropertiesMethod(module, method);
            MethodReference buildRef = FindBuilderBuildMethod(module, method);
            MethodReference actionTargetGetter = CreateDelegateTargetGetter(module);

            if (bindBuilderRef == null || onModelRef == null || onModelWithPropertiesRef == null || buildRef == null ||
                actionTargetGetter == null)
                return false;

            method.Body.InitLocals = true;
            VariableDefinition boolVar = new(module.TypeSystem.Boolean);
            VariableDefinition actionVar = new(module.ImportReference(bindBuilderRef.Parameters[0].ParameterType));
            VariableDefinition builderVar = new(module.ImportReference(onModelRef.DeclaringType));
            method.Body.Variables.Add(boolVar);
            method.Body.Variables.Add(actionVar);
            method.Body.Variables.Add(builderVar);

            IList<Instruction> instructions = method.Body.Instructions;
            for (int i = 0; i < instructions.Count; i++)
            {
                Instruction callInst = instructions[i];
                if (!IsTargetBindCall(callInst, out bool hasInvokeOnBindArg)) continue;

                if (!TryGetRenderDelegateMethod(instructions, i, out MethodDefinition renderMethod))
                {
                    diagnostics
                       .Add(CreateWarning($"AutoBind skipped at {method.FullName}: cannot resolve render delegate method."));
                    continue;
                }

                List<ModelAccessPath> modelPaths = CollectModelAccessPaths(renderMethod);
                if (modelPaths.Count == 0)
                    diagnostics.Add(CreateWarning(
                                                  $"AutoBind found no BaseModel reference in render method {renderMethod.FullName}. Bind(bool, Action) will not register model listeners."));

                il.InsertBefore(callInst, Instruction.Create(OpCodes.Stloc, actionVar));
                if (hasInvokeOnBindArg)
                {
                    il.InsertBefore(callInst, Instruction.Create(OpCodes.Stloc, boolVar));
                }
                else
                {
                    il.InsertBefore(callInst, Instruction.Create(OpCodes.Ldc_I4_0));
                    il.InsertBefore(callInst, Instruction.Create(OpCodes.Stloc, boolVar));
                }

                il.InsertBefore(callInst, Instruction.Create(OpCodes.Ldloc, actionVar));
                il.InsertBefore(callInst, Instruction.Create(OpCodes.Ldloc, boolVar));
                il.InsertBefore(callInst, Instruction.Create(OpCodes.Call, bindBuilderRef));
                il.InsertBefore(callInst, Instruction.Create(OpCodes.Stloc, builderVar));

                foreach (ModelAccessPath modelPath in modelPaths)
                {
                    il.InsertBefore(callInst, Instruction.Create(OpCodes.Ldloc, builderVar));
                    il.InsertBefore(callInst, Instruction.Create(OpCodes.Ldloc, actionVar));
                    il.InsertBefore(callInst, Instruction.Create(OpCodes.Callvirt, actionTargetGetter));
                    il.InsertBefore(callInst,
                                    Instruction.Create(OpCodes.Castclass,
                                                       module.ImportReference(modelPath.TargetType)));

                    foreach (FieldReference field in modelPath.Fields)
                        il.InsertBefore(callInst, Instruction.Create(OpCodes.Ldfld, module.ImportReference(field)));

                    if (modelPath.Properties.Count > 0)
                    {
                        il.InsertBefore(callInst, CreateLoadIntInstruction(modelPath.Properties.Count));
                        il.InsertBefore(callInst, Instruction.Create(OpCodes.Newarr, module.TypeSystem.String));
                        for (int propIndex = 0; propIndex < modelPath.Properties.Count; propIndex++)
                        {
                            il.InsertBefore(callInst, Instruction.Create(OpCodes.Dup));
                            il.InsertBefore(callInst, CreateLoadIntInstruction(propIndex));
                            il.InsertBefore(callInst,
                                            Instruction.Create(OpCodes.Ldstr, modelPath.Properties[propIndex]));
                            il.InsertBefore(callInst, Instruction.Create(OpCodes.Stelem_Ref));
                        }

                        il.InsertBefore(callInst, Instruction.Create(OpCodes.Callvirt, onModelWithPropertiesRef));
                    }
                    else
                    {
                        il.InsertBefore(callInst, Instruction.Create(OpCodes.Callvirt, onModelRef));
                    }

                    il.InsertBefore(callInst, Instruction.Create(OpCodes.Stloc, builderVar));
                }

                il.InsertBefore(callInst, Instruction.Create(OpCodes.Ldloc, builderVar));
                il.InsertBefore(callInst, Instruction.Create(OpCodes.Callvirt, buildRef));

                callInst.OpCode = OpCodes.Nop;
                callInst.Operand = null;
                modified = true;
            }

            return modified;
        }

        private static bool IsTargetBindCall(Instruction instruction, out bool hasInvokeOnBindArg)
        {
            hasInvokeOnBindArg = false;
            if (instruction == null) return false;
            if (instruction.OpCode != OpCodes.Call && instruction.OpCode != OpCodes.Callvirt) return false;
            if (instruction.Operand is not MethodReference methodRef) return false;
            if (methodRef.Name != "Bind") return false;
            if (methodRef.ReturnType == null) return false;
            if (methodRef.ReturnType.MetadataType != MetadataType.Void) return false;
            if (!IsTypeOrDerivedFrom(methodRef.DeclaringType, BaseUIFullName)) return false;

            if (methodRef.Parameters.Count == 2)
            {
                if (methodRef.Parameters[0].ParameterType.MetadataType != MetadataType.Boolean) return false;
                if (methodRef.Parameters[1].ParameterType.FullName != moduleActionTypeFullName) return false;
                hasInvokeOnBindArg = true;
                return true;
            }

            if (methodRef.Parameters.Count == 1)
            {
                if (methodRef.Parameters[0].ParameterType.FullName != moduleActionTypeFullName) return false;
                hasInvokeOnBindArg = false;
                return true;
            }

            return false;
        }

        private static bool TryGetRenderDelegateMethod(IList<Instruction> instructions, int bindCallIndex,
                                                       out MethodDefinition renderMethod)
        {
            renderMethod = null;
            if (bindCallIndex < 2) return false;

            for (int i = bindCallIndex - 1; i >= Math.Max(0, bindCallIndex - 8); i--)
            {
                Instruction inst = instructions[i];
                if (inst.OpCode != OpCodes.Ldftn && inst.OpCode != OpCodes.Ldvirtftn) continue;
                if (inst.Operand is not MethodReference methodRef) continue;
                renderMethod = SafeResolve(methodRef);
                return renderMethod != null;
            }

            return false;
        }

        private static List<ModelAccessPath> CollectModelAccessPaths(MethodDefinition renderMethod)
        {
            List<ModelAccessPath> result = new();
            if (renderMethod == null || !renderMethod.HasBody || renderMethod.IsStatic) return result;

            Dictionary<string, ModelAccessPathBuilder> map = new(StringComparer.Ordinal);
            IList<Instruction> instructions = renderMethod.Body.Instructions;
            for (int i = 0; i < instructions.Count; i++)
            {
                Instruction instruction = instructions[i];
                if (instruction.OpCode == OpCodes.Ldfld &&
                    instruction.Operand is FieldReference fieldRef &&
                    IsTypeOrDerivedFrom(fieldRef.FieldType, BaseModelFullName) &&
                    TryBuildFieldPath(instructions, i, out List<FieldReference> directPath))
                {
                    AddOrUpdateModelPath(map, directPath, null);
                    continue;
                }

                if (TryGetPropertyAccess(instruction, out string propertyName) &&
                    TryBuildModelPathForPropertyGetter(instructions, i, out List<FieldReference> propertyPath))
                    AddOrUpdateModelPath(map, propertyPath, propertyName);
            }

            foreach (ModelAccessPathBuilder builder in map.Values)
                result.Add(new ModelAccessPath(builder.TargetType, builder.Fields, builder.Properties.ToList()));

            return result;
        }

        private static void AddOrUpdateModelPath(Dictionary<string, ModelAccessPathBuilder> map,
                                                 List<FieldReference> path,
                                                 string propertyName)
        {
            if (path == null || path.Count <= 0) return;
            string key = string.Join("->", path.Select(f => f.FullName));
            if (!map.TryGetValue(key, out ModelAccessPathBuilder builder))
            {
                builder = new ModelAccessPathBuilder(path[0].DeclaringType, path);
                map[key] = builder;
            }

            if (!string.IsNullOrEmpty(propertyName))
                builder.Properties.Add(propertyName);
        }

        private static bool TryBuildFieldPath(IList<Instruction> instructions, int baseModelLoadIndex,
                                              out List<FieldReference> path)
        {
            path = new List<FieldReference>();
            if (baseModelLoadIndex < 0 || baseModelLoadIndex >= instructions.Count) return false;
            if (instructions[baseModelLoadIndex].Operand is not FieldReference lastField) return false;

            path.Add(lastField);
            TypeReference expectedObjectType = lastField.DeclaringType;
            int cursor = baseModelLoadIndex - 1;

            while (cursor >= 0)
            {
                Instruction prev = instructions[cursor];
                if (IsLdarg0(prev)) return true;

                if (prev.OpCode == OpCodes.Ldfld && prev.Operand is FieldReference prevField &&
                    AreSameType(prevField.FieldType, expectedObjectType))
                {
                    path.Insert(0, prevField);
                    expectedObjectType = prevField.DeclaringType;
                    cursor--;
                    continue;
                }

                return false;
            }

            return false;
        }

        private static bool TryBuildModelPathForPropertyGetter(IList<Instruction> instructions, int getterCallIndex,
                                                               out List<FieldReference> path)
        {
            path = null;
            int objLoadIndex = FindPreviousNonNopInstructionIndex(instructions, getterCallIndex - 1);
            if (objLoadIndex < 0) return false;
            if (instructions[objLoadIndex].OpCode != OpCodes.Ldfld) return false;
            return TryBuildFieldPath(instructions, objLoadIndex, out path);
        }

        private static bool TryGetPropertyAccess(Instruction instruction, out string propertyName)
        {
            propertyName = null;
            if (instruction == null) return false;
            if (instruction.OpCode != OpCodes.Call && instruction.OpCode != OpCodes.Callvirt) return false;
            if (instruction.Operand is not MethodReference methodRef) return false;
            if (!methodRef.Name.StartsWith("get_", StringComparison.Ordinal)) return false;
            if (methodRef.Parameters.Count != 0) return false;
            if (!IsTypeOrDerivedFrom(methodRef.DeclaringType, BaseModelFullName)) return false;
            propertyName = methodRef.Name.Substring(4);
            return !string.IsNullOrEmpty(propertyName);
        }

        private static int FindPreviousNonNopInstructionIndex(IList<Instruction> instructions, int startIndex)
        {
            for (int i = startIndex; i >= 0; i--)
                if (instructions[i].OpCode != OpCodes.Nop)
                    return i;

            return -1;
        }

        private static bool IsLdarg0(Instruction instruction)
        {
            return instruction.OpCode == OpCodes.Ldarg_0;
        }

        private static Instruction CreateLoadIntInstruction(int value)
        {
            return value switch
            {
                -1 => Instruction.Create(OpCodes.Ldc_I4_M1),
                0 => Instruction.Create(OpCodes.Ldc_I4_0),
                1 => Instruction.Create(OpCodes.Ldc_I4_1),
                2 => Instruction.Create(OpCodes.Ldc_I4_2),
                3 => Instruction.Create(OpCodes.Ldc_I4_3),
                4 => Instruction.Create(OpCodes.Ldc_I4_4),
                5 => Instruction.Create(OpCodes.Ldc_I4_5),
                6 => Instruction.Create(OpCodes.Ldc_I4_6),
                7 => Instruction.Create(OpCodes.Ldc_I4_7),
                8 => Instruction.Create(OpCodes.Ldc_I4_8),
                <= sbyte.MaxValue and >= sbyte.MinValue => Instruction.Create(OpCodes.Ldc_I4_S, (sbyte)value),
                _ => Instruction.Create(OpCodes.Ldc_I4, value)
            };
        }

        private static bool AreSameType(TypeReference left, TypeReference right)
        {
            if (left == null || right == null) return false;
            return string.Equals(left.FullName, right.FullName, StringComparison.Ordinal);
        }

        private static bool IsTypeOrDerivedFrom(TypeReference typeReference, string baseTypeFullName)
        {
            if (typeReference == null || string.IsNullOrEmpty(baseTypeFullName)) return false;

            TypeReference current = typeReference;
            while (current != null)
            {
                if (string.Equals(current.FullName, baseTypeFullName, StringComparison.Ordinal)) return true;
                TypeDefinition resolved = SafeResolve(current);
                if (resolved == null) return false;
                current = resolved.BaseType;
            }

            return false;
        }

        private static MethodReference FindBindBuilderMethod(ModuleDefinition module, MethodDefinition contextMethod)
        {
            TypeDefinition current = contextMethod.DeclaringType;
            while (current != null)
            {
                foreach (MethodDefinition method in current.Methods)
                {
                    if (method.Name != "Bind" || method.Parameters.Count != 2) continue;
                    if (method.Parameters[0].ParameterType.FullName != moduleActionTypeFullName) continue;
                    if (method.Parameters[1].ParameterType.MetadataType != MetadataType.Boolean) continue;
                    if (method.ReturnType.FullName != RenderNodeBuilderFullName) continue;
                    return module.ImportReference(method);
                }

                TypeReference baseType = current.BaseType;
                if (baseType == null) break;
                current = SafeResolve(baseType);
            }

            return null;
        }

        private static MethodReference FindBuilderOnModelMethod(ModuleDefinition module, MethodDefinition contextMethod)
        {
            MethodReference bindMethod = FindBindBuilderMethod(module, contextMethod);
            TypeDefinition builderType = SafeResolve(bindMethod?.ReturnType);
            if (builderType == null) return null;

            foreach (MethodDefinition method in builderType.Methods)
            {
                if (method.Name != "On" || method.Parameters.Count != 1) continue;
                if (method.Parameters[0].ParameterType.FullName != BaseModelFullName) continue;
                return module.ImportReference(method);
            }

            return null;
        }

        private static MethodReference FindBuilderOnModelWithPropertiesMethod(ModuleDefinition module,
                                                                              MethodDefinition contextMethod)
        {
            MethodReference bindMethod = FindBindBuilderMethod(module, contextMethod);
            TypeDefinition builderType = SafeResolve(bindMethod?.ReturnType);
            if (builderType == null) return null;

            foreach (MethodDefinition method in builderType.Methods)
            {
                if (method.Name != "On" || method.Parameters.Count != 2) continue;
                if (method.Parameters[0].ParameterType.FullName != BaseModelFullName) continue;
                if (method.Parameters[1].ParameterType is not ArrayType arrayType ||
                    arrayType.ElementType.MetadataType != MetadataType.String)
                    continue;
                return module.ImportReference(method);
            }

            return null;
        }

        private static MethodReference FindBuilderBuildMethod(ModuleDefinition module, MethodDefinition contextMethod)
        {
            MethodReference bindMethod = FindBindBuilderMethod(module, contextMethod);
            TypeDefinition builderType = SafeResolve(bindMethod?.ReturnType);
            if (builderType == null) return null;

            foreach (MethodDefinition method in builderType.Methods)
                if (method.Name == "Build" && method.Parameters.Count == 0)
                    return module.ImportReference(method);

            return null;
        }

        private static DiagnosticMessage CreateWarning(string message)
        {
            return new DiagnosticMessage
            {
                DiagnosticType = DiagnosticType.Warning,
                MessageData = message
            };
        }

        private static TypeDefinition SafeResolve(TypeReference typeReference)
        {
            if (typeReference == null) return null;
            try
            {
                return typeReference.Resolve();
            }
            catch
            {
                return null;
            }
        }

        private static MethodDefinition SafeResolve(MethodReference methodReference)
        {
            if (methodReference == null) return null;
            try
            {
                return methodReference.Resolve();
            }
            catch
            {
                return null;
            }
        }

        private static MethodReference CreateDelegateTargetGetter(ModuleDefinition module)
        {
            if (module == null) return null;
            TypeReference delegateType = new(
                                             "System",
                                             "Delegate",
                                             module,
                                             module.TypeSystem.CoreLibrary,
                                             false);

            MethodReference getter = new("get_Target", module.TypeSystem.Object, delegateType)
            {
                HasThis = true
            };
            return module.ImportReference(getter);
        }

        private sealed class ModelAccessPathBuilder
        {
            public ModelAccessPathBuilder(TypeReference targetType, List<FieldReference> fields)
            {
                TargetType = targetType;
                Fields = fields;
                Properties = new HashSet<string>(StringComparer.Ordinal);
            }

            public TypeReference TargetType { get; }
            public List<FieldReference> Fields { get; }
            public HashSet<string> Properties { get; }
        }

        private readonly struct ModelAccessPath
        {
            public ModelAccessPath(TypeReference targetType, List<FieldReference> fields, List<string> properties)
            {
                TargetType = targetType;
                Fields = fields;
                Properties = properties;
            }

            public TypeReference TargetType { get; }
            public List<FieldReference> Fields { get; }
            public List<string> Properties { get; }
        }
    }
}