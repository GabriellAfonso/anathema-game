#nullable enable
using System.Runtime.CompilerServices;

// Os testes alcançam as peças internas da conexão (tentativa de socket, vigia de silêncio,
// suspensão): elas não fazem parte da superfície que a fila, a partida e a fachada consomem,
// e expô-las como públicas só para testar vazaria detalhe de implementação.
[assembly: InternalsVisibleTo("Anathema.Net.Connection.Tests")]
