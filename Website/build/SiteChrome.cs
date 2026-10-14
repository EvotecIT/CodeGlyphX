// Keeps every codeglyphx.com page in one header and footer.
//
//   dotnet run build/SiteChrome.cs -- export <site> <out>
//     Copies the header and footer the site build rendered into <site>/index.html (themes/codeglyphx/partials/header.html
//     and footer.html) into fragments for the API reference generator: api-header.html and ps-header.html (with the
//     API or PowerShell destination marked current) and footer.html. The API pages therefore carry the same markup.
//
//   dotnet run build/SiteChrome.cs -- playground <published index.html> <site playground index.html>
//     The /playground/ page is rendered by the site build around the Blazor app's #app element. This fills its import
//     map and fingerprinted blazor.webassembly script from the index.html that dotnet publish wrote.
using System.Text;
using System.Text.RegularExpressions;

var utf8 = new UTF8Encoding(false);
if (args.Length < 3) return Fail("usage: export <site> <out> | playground <published index.html> <site index.html>");

switch (args[0]) {
    case "export": {
        var home = File.ReadAllText(Path.Combine(args[1], "index.html"));
        var header = Slice(home, "<a class=\"cgx-skip\"", "</header>");
        var footer = Slice(home, "<footer class=\"footer\">", "</footer>");
        Directory.CreateDirectory(args[2]);
        // The theme is applied before the header paints, as the site layouts do in <head>.
        const string theme = "<script>(function(){let t='auto';try{t=localStorage.getItem('theme')||'auto';}catch(e){}document.documentElement.dataset.theme=t;})();</script>\n";
        File.WriteAllText(Path.Combine(args[2], "api-header.html"), theme + MarkCurrent(header, "/api/"), utf8);
        File.WriteAllText(Path.Combine(args[2], "ps-header.html"), theme + MarkCurrent(header, "/powershell/"), utf8);
        File.WriteAllText(Path.Combine(args[2], "footer.html"), footer + "\n<script src=\"/js/site.js\" defer></script>\n", utf8);
        Console.WriteLine($"Site chrome: header {header.Length} chars, footer {footer.Length} chars -> {args[2]}");
        return 0;
    }
    case "playground": {
        var published = File.ReadAllText(args[1]);
        var map = Regex.Match(published, "<script type=\"importmap\">([\\s\\S]*?)</script>");
        var blazor = Regex.Match(published, "<script src=\"(_framework/blazor\\.webassembly[^\"]*\\.js)\"");
        if (!map.Success || !blazor.Success) return Fail("The published index.html has no import map or Blazor script.");
        var page = File.ReadAllText(args[2]);
        var before = page;
        page = Regex.Replace(page, "<script[^>]*data-cgx-importmap[^>]*>[\\s\\S]*?</script>",
            "<script type=\"importmap\" data-cgx-importmap>" + map.Groups[1].Value.Trim() + "</script>");
        page = Regex.Replace(page, "<script[^>]*data-cgx-blazor[^>]*></script>",
            "<script src=\"" + blazor.Groups[1].Value + "\" data-cgx-blazor autostart=\"false\"></script>");
        if (page == before || !page.Contains(blazor.Groups[1].Value, StringComparison.Ordinal)) return Fail("The site playground page has no data-cgx-importmap or data-cgx-blazor placeholder.");
        File.WriteAllText(args[2], page, utf8);
        Console.WriteLine($"Playground shell: {blazor.Groups[1].Value} and its import map -> {args[2]}");
        return 0;
    }
    default:
        return Fail("unknown command " + args[0]);
}

static string Slice(string html, string start, string end) {
    var i = html.IndexOf(start, StringComparison.Ordinal);
    var j = i < 0 ? -1 : html.IndexOf(end, i, StringComparison.Ordinal);
    if (i < 0 || j < 0) throw new InvalidOperationException($"Could not find {start} ... {end} in the rendered page.");
    return html.Substring(i, j + end.Length - i);
}

// Marks the destination whose URL is given as current, the way the header partial does for the page it renders.
static string MarkCurrent(string header, string url) {
    var item = header.IndexOf($"data-cgx-navitem=\"{url}\"", StringComparison.Ordinal);
    if (item < 0) return header;
    var li = header.LastIndexOf("<li class=\"cgx-navitem\"", item, StringComparison.Ordinal);
    var group = header.LastIndexOf("<li class=\"cgx-navgroup\"", item, StringComparison.Ordinal);
    var linkTag = $"<a class=\"cgx-navitem__link\" href=\"{url}\"";
    var link = header.IndexOf(linkTag, item, StringComparison.Ordinal);
    if (li < 0 || group < 0 || link < 0) return header;
    var textStart = header.IndexOf('>', link) + 1;
    var title = header[textStart..header.IndexOf("</a>", textStart, StringComparison.Ordinal)];

    var sb = new StringBuilder(header);
    // Edit from the end so earlier offsets stay valid.
    sb.Insert(link + linkTag.Length, " aria-current=\"true\"");
    sb.Insert(li + "<li class=\"cgx-navitem".Length, " is-current");
    var here = sb.ToString().IndexOf("data-cgx-here hidden>", group, StringComparison.Ordinal);
    if (here >= 0 && here < li) sb.Remove(here + "data-cgx-here".Length, " hidden".Length);
    var slot = sb.ToString().IndexOf("<span data-cgx-here-title></span>", group, StringComparison.Ordinal);
    if (slot >= 0 && slot < li) sb.Insert(slot + "<span data-cgx-here-title>".Length, title);
    sb.Insert(group + "<li class=\"cgx-navgroup".Length, " is-current");
    return sb.ToString();
}

static int Fail(string message) {
    Console.Error.WriteLine("SiteChrome: " + message);
    return 1;
}
