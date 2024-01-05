using System.Diagnostics.CodeAnalysis;

[assembly:
    SuppressMessage("StyleCop.CSharp.NamingRules", "SA1309:Field names should not begin with underscore",
        Justification = "Supressed in favor of CA1500. Use _camelCase for private fields.",
        Scope = "namespaceanddescendants", Target = "~N:CustomizePlus")]