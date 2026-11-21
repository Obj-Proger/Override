using Override.Content.Parsing;
using Override.Content.Rendering;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: Override.Content.Builder <language content directory> <output directory>");
    return 2;
}

var site = SiteCompiler.Compile(args[0]);

// One problem per line in the canonical format: MSBuild shows them as build errors and warnings.
foreach (var diagnostic in site.Diagnostics)
    Console.WriteLine(diagnostic);

if (site.HasErrors)
    return 1;

SiteCompiler.Write(site, args[1]);

var warnings = site.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);
Console.WriteLine($"Content compiled: topics {site.Pages.Count}, warnings {warnings}.");
return 0;
