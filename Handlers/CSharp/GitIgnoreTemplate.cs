namespace UniSentinel.Handlers.CSharp;

internal static class GitIgnoreTemplate
{
    public static string GetContent() => @"
# CSharp GitIgnore
bin/
obj/
*.suo
*.user
.vs/
.vscode/
BuildCache/
.uni-sentinel-score
.uni_config/
.uni-sentinel/
";
}
