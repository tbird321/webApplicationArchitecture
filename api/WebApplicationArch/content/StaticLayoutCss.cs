using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace WebApplicationArch.content
{
    /// <summary>
    /// Loads the structural stylesheet that every pre-rendered public page inlines.
    ///
    /// The CSS itself lives in infra/static/page-layout.css and is compiled in as an
    /// embedded resource. That file is the single source of truth -- publish-static-pages.ps1
    /// reads the same file for the bulk backfill, so the save-hook renderer and the backfill
    /// tool cannot drift apart and produce differently-styled pages.
    ///
    /// See the header comment in page-layout.css for why this is needed at all: the site's
    /// layout and menu rules are compiled into the SPA bundle, which a static page never
    /// loads, and theme.css contains only colours and fonts.
    /// </summary>
    public static class StaticLayoutCss
    {
        private static readonly Lazy<string> _css = new Lazy<string>(Load);

        /// <summary>The stylesheet, ready to drop inside a &lt;style&gt; tag.</summary>
        public static string Css => _css.Value;

        private static string Load()
        {
            var asm = Assembly.GetExecutingAssembly();

            // Embedded as "<RootNamespace>.infra.static.page-layout.css" or similar depending
            // on how the csproj declares it, so match on the file name rather than guessing.
            var name = asm.GetManifestResourceNames()
                          .FirstOrDefault(n => n.EndsWith("page-layout.css", StringComparison.OrdinalIgnoreCase));

            if (name == null)
            {
                // A static page without this stylesheet loses its padding, grid and menu
                // styling silently -- it still renders, so nothing downstream would notice.
                // Fail loudly instead.
                throw new InvalidOperationException(
                    "page-layout.css is not embedded in the assembly. Check the EmbeddedResource " +
                    "entry in WebApplicationArch.csproj -- without it every rendered page loses " +
                    "its layout and navigation styling.");
            }

            using var stream = asm.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
