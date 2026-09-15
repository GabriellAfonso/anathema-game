#nullable enable
using System.Runtime.CompilerServices;

// Os testes alcançam as peças internas da conexão (tentativa de socket, vigia de silêncio,
// suspensão): elas não fazem parte da superfície que a fila, a partida e a fachada consomem,
// e expô-las como públicas só para testar vazaria detalhe de implementação.
[assembly: InternalsVisibleTo("Anathema.Net.Connection.Tests")]

// A fachada compõe as duas conexões e a fila e traduz o estado delas para a saúde da conexão; os testes dela
// roteirizam as mesmas peças (specs/005-presentation-facade/research.md, R2, R8).
[assembly: InternalsVisibleTo("Anathema.Net.Facade")]
[assembly: InternalsVisibleTo("Anathema.Net.Facade.Tests")]

// Desde a 005 conexão autenticada, fila, portas e rotas são internas (research R2): a partida usa a conexão, a borda
// compõe e a configuração monta as rotas; os testes dessas assemblies montam as mesmas peças.
[assembly: InternalsVisibleTo("Anathema.Net.Match")]
[assembly: InternalsVisibleTo("Anathema.Net.Unity")]
[assembly: InternalsVisibleTo("Anathema.Config")]
[assembly: InternalsVisibleTo("Anathema.Net.Match.Tests")]
[assembly: InternalsVisibleTo("Anathema.Net.Unity.Tests")]
[assembly: InternalsVisibleTo("Anathema.Config.Tests")]
