#nullable enable
using System.Runtime.CompilerServices;

// Os testes alcançam peças internas que não fazem parte da superfície pública da conta:
// TokenRenewal (renovação única), os leitores (catálogo, recusas de deck, corpos de conta)
// e UnrecognizedCatalogCard. Expor tudo isso como público só para testar vazaria detalhe
// de implementação para a apresentação e para as features seguintes.
[assembly: InternalsVisibleTo("Anathema.Net.Account.Tests")]
