#nullable enable
using System.Runtime.CompilerServices;

// Os testes da borda alcançam as peças internas: adaptadores, relógio com salto, fábrica de socket derrubável,
// probe. Só a composição é pública (specs/005-presentation-facade/research.md, R8).
[assembly: InternalsVisibleTo("Anathema.Net.Unity.Tests")]

// A configuração lê o host de lançamento e registra pelo log de console; a checagem de build e os testes da
// configuração usam as mesmas peças.
[assembly: InternalsVisibleTo("Anathema.Config")]
[assembly: InternalsVisibleTo("Anathema.Net.Editor")]
[assembly: InternalsVisibleTo("Anathema.Config.Tests")]
