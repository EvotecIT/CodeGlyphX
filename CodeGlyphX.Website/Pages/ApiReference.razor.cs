using Microsoft.AspNetCore.Components;

namespace CodeGlyphX.Website.Pages;

#if DOCS_BUILD
[Route("/api")]
[Route("/api/{TypeSlug}")]
#endif
public partial class ApiReference
{
}
