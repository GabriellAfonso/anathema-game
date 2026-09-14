#nullable enable
using System.Runtime.CompilerServices;

// Os testes alcançam as peças internas da partida (entrada de frames no espelho e no relógio, leitor de
// recusa, narrador, montagem dos comandos): elas não fazem parte da superfície que a apresentação e a
// fachada da feature 5 consomem, e expô-las como públicas só para testar vazaria detalhe de implementação.
[assembly: InternalsVisibleTo("Anathema.Net.Match.Tests")]
