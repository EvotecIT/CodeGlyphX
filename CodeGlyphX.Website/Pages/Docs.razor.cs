using Microsoft.AspNetCore.Components;

namespace CodeGlyphX.Website.Pages;

#if DOCS_BUILD
[Route("/")]
[Route("/{Section}")]
#endif
public partial class Docs
{
}
