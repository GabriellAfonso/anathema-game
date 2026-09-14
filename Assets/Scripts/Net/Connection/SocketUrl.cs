#nullable enable
using System;

namespace Anathema.Net.Connection
{
    /// <summary>Validação única de URL de socket, para rotas e alvos não divergirem.</summary>
    internal static class SocketUrl
    {
        internal static Uri RequireAbsoluteSocket(Uri? url, string name)
        {
            if (url == null || !url.IsAbsoluteUri || (url.Scheme != "ws" && url.Scheme != "wss"))
                throw new ArgumentException($"{name} is '{url}': expected an absolute ws:// or wss:// url", name);

            return url;
        }
    }
}
