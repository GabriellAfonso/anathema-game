#nullable enable
using System.Runtime.CompilerServices;

// Os testes da fachada alcançam as peças internas (dono das transições, abertura de partida, busca da linha
// do histórico, relé de saúde, composição sobre portas): elas não fazem parte da superfície que a apresentação
// consome (specs/005-presentation-facade/contracts/presentation-surface.md).
[assembly: InternalsVisibleTo("Anathema.Net.Facade.Tests")]

// A composição real monta a fachada a partir das portas dos adaptadores Unity; o construtor da fachada fica
// fechado para a apresentação (specs/005-presentation-facade/research.md, R8).
[assembly: InternalsVisibleTo("Anathema.Net.Unity")]

// Os testes da borda Unity e os LiveServer da 003 compõem conta e conexões pelas peças internas da fachada
// (specs/005-presentation-facade/plan.md, "Código anterior tocado").
[assembly: InternalsVisibleTo("Anathema.Net.Unity.Tests")]
