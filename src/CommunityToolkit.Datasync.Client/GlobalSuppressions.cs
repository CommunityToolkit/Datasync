// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0058:Expression value is never used",
    Justification = "This is used in reflection and parameter checking.",
    Scope = "namespaceanddescendants", Target = "~N:CommunityToolkit.Datasync.Client")]

[assembly: SuppressMessage("Performance", "SYSLIB1045:Convert to 'GeneratedRegexAttribute'.",
    Justification = "We don't need the additional functionality of the generated regular expressions",
    Scope = "namespaceanddescendants", Target = "~N:CommunityToolkit.Datasync.Client")]
