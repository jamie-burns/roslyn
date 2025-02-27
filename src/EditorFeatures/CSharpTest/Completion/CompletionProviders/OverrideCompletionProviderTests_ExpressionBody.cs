﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    // OverrideCompletionProviderTests overrides SetWorkspaceOptions to disable
    // expression-body members. This class does the opposite.
    public class OverrideCompletionProviderTests_ExpressionBody : AbstractCSharpCompletionProviderTests
    {
        internal override Type GetCompletionProviderType()
            => typeof(OverrideCompletionProvider);

        protected override OptionSet WithChangedNonCompletionOptions(OptionSet options)
        {
            return base.WithChangedNonCompletionOptions(options)
                .WithChangedOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement)
                .WithChangedOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement)
                .WithChangedOption(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement);
        }

        [WorkItem(16331, "https://github.com/dotnet/roslyn/issues/16334")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitProducesExpressionBodyProperties()
        {
            var markupBeforeCommit = @"class B
{
    public virtual int A { get; set; }
    class C : B
    {
        override A$$
    }
}";

            var expectedCodeAfterCommit = @"class B
{
    public virtual int A { get; set; }
    class C : B
    {
        public override int A { get => base.A$$; set => base.A = value; }
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "A", expectedCodeAfterCommit);
        }

        [WorkItem(16331, "https://github.com/dotnet/roslyn/issues/16334")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitProducesExpressionBodyGetterOnlyProperty()
        {
            var markupBeforeCommit = @"class B
{
    public virtual int A { get; }
    class C : B
    {
        override A$$
    }
}";

            var expectedCodeAfterCommit = @"class B
{
    public virtual int A { get; }
    class C : B
    {
        public override int A => base.A;$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "A", expectedCodeAfterCommit);
        }

        [WorkItem(16331, "https://github.com/dotnet/roslyn/issues/16334")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitProducesExpressionBodyMethod()
        {
            var markupBeforeCommit = @"class B
{
    public virtual int A() => 2;
    class C : B
    {
        override A$$
    }
}";

            var expectedCodeAfterCommit = @"class B
{
    public virtual int A() => 2;
    class C : B
    {
        public override int A() => base.A();$$
    }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "A()", expectedCodeAfterCommit);
        }
    }
}
