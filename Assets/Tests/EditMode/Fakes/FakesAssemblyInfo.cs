#nullable enable
using System.Runtime.CompilerServices;

// Os fakes implementam portas que desde a 005 são internas, então também são internos, e cada assembly de teste que
// os usa é amiga (specs/005-presentation-facade/research.md, R2).
[assembly: InternalsVisibleTo("Anathema.Net.Core.Tests")]
[assembly: InternalsVisibleTo("Anathema.Net.Json.Tests")]
[assembly: InternalsVisibleTo("Anathema.Net.Account.Tests")]
[assembly: InternalsVisibleTo("Anathema.Net.Connection.Tests")]
[assembly: InternalsVisibleTo("Anathema.Net.Match.Tests")]
[assembly: InternalsVisibleTo("Anathema.Net.Facade.Tests")]
[assembly: InternalsVisibleTo("Anathema.Net.Unity.Tests")]
[assembly: InternalsVisibleTo("Anathema.Config.Tests")]
[assembly: InternalsVisibleTo("Anathema.Net.Editor.Tests")]
[assembly: InternalsVisibleTo("Anathema.Client.Proof.Tests")]
