#nullable enable
using System.Runtime.CompilerServices;

// A prova só enxerga o público das outras assemblies e ninguém a enxerga por dentro, exceto os próprios testes: estratégia,
// cobertura e log da prova são peças internas testadas na suíte normal (specs/005-presentation-facade/plan.md, Complexity Tracking).
[assembly: InternalsVisibleTo("Anathema.Client.Proof.Tests")]
