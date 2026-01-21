using Microsoft.AspNetCore.Components;

namespace CodeGlyphX.Website.Pages;

#if !DOCS_BUILD && !PLAYGROUND_BUILD
[Route("/")]
[Route("/home")]
#endif
public partial class Index
{
}
