#nullable enable
using System.Runtime.CompilerServices;

// Os testes alcançam peças internas que não fazem parte da superfície pública da conta:
// TokenRenewal (renovação única), os leitores (catálogo, recusas de deck, corpos de conta)
// e UnrecognizedCatalogCard. Expor tudo isso como público só para testar vazaria detalhe
// de implementação para a apresentação e para as features seguintes.
[assembly: InternalsVisibleTo("Anathema.Net.Account.Tests")]

// O FakeAccessTokenSource da feature 003 cria AccessToken pelo construtor interno. O construtor
// continua fechado para o código de produção: a assembly de fakes é só de editor e só existe com
// UNITY_INCLUDE_TESTS (specs/003-authenticated-socket-queue/research.md, R5).
[assembly: InternalsVisibleTo("Anathema.Net.Fakes")]

// Os testes da partida montam LoadedCatalog pelo leitor interno do catálogo, sem sessão de conta nem
// HTTP falso: o construtor continua fechado para o código de produção
// (specs/004-match-session/research.md, R13).
[assembly: InternalsVisibleTo("Anathema.Net.Match.Tests")]

// A estratégia dos bots do marco da feature 004 é testada com um catálogo montado pelo mesmo leitor interno
// (specs/004-match-session/research.md, R14).
[assembly: InternalsVisibleTo("Anathema.Net.Unity.Tests")]
