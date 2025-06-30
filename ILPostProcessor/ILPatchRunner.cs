using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using Mono.CecilX;
using Mono.CecilX.Cil;
using Mirror.Weaver;

namespace Omni.ILPatch
{
    internal class ILPatchRunner : ILPostProcessor
    {
        public override ILPostProcessor GetInstance() => new ILPatchRunner();
        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            List<DiagnosticMessage> diagnostics = new();

            try
            {
                using MemoryStream pdbSymbols = new(compiledAssembly.InMemoryAssembly.PdbData);
                using ILPostProcessorAssemblyResolver asmResolver = new(compiledAssembly);

                ReaderParameters asmReaderParams = new()
                {
                    SymbolStream = pdbSymbols,
                    ReadWrite = true,
                    ReadSymbols = true,
                    AssemblyResolver = asmResolver,
                };

                using MemoryStream peStream = new(compiledAssembly.InMemoryAssembly.PeData);
                using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(peStream, asmReaderParams);

                // Run patches
                NetworkVariablePatch.Process(assembly, diagnostics);
                StripCodePatch.Process(assembly, diagnostics);

                using MemoryStream pdbOut = new();
                WriterParameters asmWriterParams = new()
                {
                    SymbolWriterProvider = new PortablePdbWriterProvider(),
                    SymbolStream = pdbOut,
                    WriteSymbols = true
                };

                using MemoryStream peOut = new();
                assembly.Write(peOut, asmWriterParams);
                return new ILPostProcessResult(new InMemoryAssembly(peOut.ToArray(), pdbOut.ToArray()), diagnostics);
            }
            catch (System.Exception ex)
            {
                diagnostics.Add(new DiagnosticMessage()
                {
                    DiagnosticType = DiagnosticType.Error,
                    MessageData = ex.ToString()
                });

                return new ILPostProcessResult(null, diagnostics); // return 'null' to avoid crashing the editor
            }
        }

        private static bool HasDefine(ICompiledAssembly assembly, string define)
        {
            return assembly.Defines != null && assembly.Defines.Contains(define);
        }

        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
            string name = compiledAssembly.Name;
            if (name.StartsWith("Unity.") || name.StartsWith("UnityEngine.") || name.StartsWith("UnityEditor.") || name.StartsWith("Omni."))
                return false;

            if (!compiledAssembly.References.Any(r => r.EndsWith("Omni.Core.dll")))
                return false;

            return !HasDefine(compiledAssembly, "ILPP_IGNORE");
        }
    }
}