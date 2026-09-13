#nullable enable
using System.Runtime.CompilerServices;

// OverrideHost(raw, developmentBuild) é interno: o teste precisa simular build de produção,
// e Debug.isDebugBuild é sempre verdadeiro no editor.
[assembly: InternalsVisibleTo("Anathema.Config.Tests")]
