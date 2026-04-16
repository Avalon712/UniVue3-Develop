using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace UniVue.CodeGen
{
    public sealed class NotifyPropertyChangedILPostProcessor : ILPostProcessor
    {
        private const string UniVueRuntimeAssemblyName = "UniVue.Runtime";

        public override ILPostProcessor GetInstance()
        {
            return this;
        }

        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
            return ReferencesUniVueRuntime(compiledAssembly);
        }

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            List<DiagnosticMessage> diagnostics = new();
            if (!WillProcess(compiledAssembly))
                return new ILPostProcessResult(compiledAssembly.InMemoryAssembly, diagnostics);

            try
            {
                using MemoryStream peStream = new(compiledAssembly.InMemoryAssembly.PeData);
                using MemoryStream pdbInputStream = HasPdb(compiledAssembly)
                    ? new MemoryStream(compiledAssembly.InMemoryAssembly.PdbData)
                    : null;
                using PostProcessorAssemblyResolver resolver = new(compiledAssembly);

                ReaderParameters readerParameters = new()
                {
                    AssemblyResolver = resolver,
                    ReadingMode = ReadingMode.Immediate
                };

                if (pdbInputStream != null)
                {
                    readerParameters.ReadSymbols = true;
                    readerParameters.SymbolReaderProvider = new PortablePdbReaderProvider();
                    readerParameters.SymbolStream = pdbInputStream;
                }

                AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(peStream, readerParameters);
                bool modified = NotifyPropertyChangedInjector.Inject(assemblyDefinition);
                if (!modified) return new ILPostProcessResult(compiledAssembly.InMemoryAssembly, diagnostics);

                using MemoryStream peOutputStream = new();
                using MemoryStream pdbOutputStream = pdbInputStream != null ? new MemoryStream() : null;

                WriterParameters writerParameters = new();
                if (pdbOutputStream != null)
                {
                    writerParameters.WriteSymbols = true;
                    writerParameters.SymbolWriterProvider = new PortablePdbWriterProvider();
                    writerParameters.SymbolStream = pdbOutputStream;
                }

                assemblyDefinition.Write(peOutputStream, writerParameters);
                InMemoryAssembly inMemoryAssembly = new(
                                                        peOutputStream.ToArray(),
                                                        pdbOutputStream != null
                                                            ? pdbOutputStream.ToArray()
                                                            : compiledAssembly.InMemoryAssembly.PdbData);

                return new ILPostProcessResult(inMemoryAssembly, diagnostics);
            }
            catch (Exception exception)
            {
                diagnostics.Add(new DiagnosticMessage
                {
                    DiagnosticType = DiagnosticType.Error,
                    MessageData = $"NotifyPropertyChanged IL injection failed: {exception}"
                });
                return new ILPostProcessResult(compiledAssembly.InMemoryAssembly, diagnostics);
            }
        }

        private static bool HasPdb(ICompiledAssembly compiledAssembly)
        {
            return compiledAssembly?.InMemoryAssembly.PdbData != null &&
                   compiledAssembly.InMemoryAssembly.PdbData.Length > 0;
        }

        private static bool ReferencesUniVueRuntime(ICompiledAssembly compiledAssembly)
        {
            if (compiledAssembly?.References == null || IgnoreILInjectAssembly.Ignore(compiledAssembly.Name))
                return false;

            foreach (string reference in compiledAssembly.References)
            {
                string assemblyName = Path.GetFileNameWithoutExtension(reference);
                if (string.Equals(assemblyName, UniVueRuntimeAssemblyName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }

    internal sealed class PostProcessorAssemblyResolver : DefaultAssemblyResolver
    {
        private readonly Dictionary<string, string> _assemblyPaths = new(StringComparer.OrdinalIgnoreCase);

        public PostProcessorAssemblyResolver(ICompiledAssembly compiledAssembly)
        {
            foreach (string reference in compiledAssembly.References)
            {
                if (string.IsNullOrEmpty(reference)) continue;

                string assemblyName = Path.GetFileNameWithoutExtension(reference);
                if (!_assemblyPaths.ContainsKey(assemblyName)) _assemblyPaths.Add(assemblyName, reference);

                string searchDirectory = Path.GetDirectoryName(reference);
                if (!string.IsNullOrEmpty(searchDirectory)) AddSearchDirectory(searchDirectory);
            }
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            if (name != null && _assemblyPaths.TryGetValue(name.Name, out string assemblyPath) &&
                File.Exists(assemblyPath)) return AssemblyDefinition.ReadAssembly(assemblyPath, parameters);

            return base.Resolve(name, parameters);
        }
    }
}