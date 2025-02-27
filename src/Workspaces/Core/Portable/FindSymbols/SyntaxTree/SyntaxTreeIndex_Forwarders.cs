﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Collections;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal sealed partial class SyntaxTreeIndex
    {
        public ImmutableArray<DeclaredSymbolInfo> DeclaredSymbolInfos => _declarationInfo.DeclaredSymbolInfos;

        public ImmutableDictionary<string, ImmutableArray<int>> ReceiverTypeNameToExtensionMethodMap
            => _extensionMethodInfo.ReceiverTypeNameToExtensionMethodMap;

        public bool ContainsExtensionMethod => _extensionMethodInfo.ContainsExtensionMethod;

        public bool ProbablyContainsIdentifier(string identifier) => _identifierInfo.ProbablyContainsIdentifier(identifier);
        public bool ProbablyContainsEscapedIdentifier(string identifier) => _identifierInfo.ProbablyContainsEscapedIdentifier(identifier);

        public bool ContainsPredefinedType(PredefinedType type) => _contextInfo.ContainsPredefinedType(type);
        public bool ContainsPredefinedOperator(PredefinedOperator op) => _contextInfo.ContainsPredefinedOperator(op);

        public bool ProbablyContainsStringValue(string value) => _literalInfo.ProbablyContainsStringValue(value);
        public bool ProbablyContainsInt64Value(long value) => _literalInfo.ProbablyContainsInt64Value(value);

        public bool ContainsForEachStatement => _contextInfo.ContainsForEachStatement;
        public bool ContainsDeconstruction => _contextInfo.ContainsDeconstruction;
        public bool ContainsAwait => _contextInfo.ContainsAwait;
        public bool ContainsImplicitObjectCreation => _contextInfo.ContainsImplicitObjectCreation;
        public bool ContainsLockStatement => _contextInfo.ContainsLockStatement;
        public bool ContainsUsingStatement => _contextInfo.ContainsUsingStatement;
        public bool ContainsQueryExpression => _contextInfo.ContainsQueryExpression;
        public bool ContainsThisConstructorInitializer => _contextInfo.ContainsThisConstructorInitializer;
        public bool ContainsBaseConstructorInitializer => _contextInfo.ContainsBaseConstructorInitializer;
        public bool ContainsElementAccessExpression => _contextInfo.ContainsElementAccessExpression;
        public bool ContainsIndexerMemberCref => _contextInfo.ContainsIndexerMemberCref;
        public bool ContainsTupleExpressionOrTupleType => _contextInfo.ContainsTupleExpressionOrTupleType;
        public bool ContainsGlobalSuppressMessageAttribute => _contextInfo.ContainsGlobalSuppressMessageAttribute;
        public bool ContainsConversion => _contextInfo.ContainsConversion;

        /// <summary>
        /// Same as <see cref="DeclaredSymbolInfos"/>, just stored as a set for easy containment checks.
        /// </summary>
        public HashSet<DeclaredSymbolInfo> DeclaredSymbolInfoSet => _declaredSymbolInfoSet.Value;

        /// <summary>
        /// Gets the set of global aliases that point to something with the provided name and arity.
        /// For example of there is <c>global alias X = A.B.C&lt;int&gt;</c>, then looking up with
        /// <c>name="C"</c> and arity=1 will return <c>X</c>.
        /// </summary>
        public ImmutableArray<string> GetGlobalAliases(string name, int arity)
        {
            if (_globalAliasInfo == null)
                return ImmutableArray<string>.Empty;

            using var result = TemporaryArray<string>.Empty;

            foreach (var (alias, aliasName, aliasArity) in _globalAliasInfo)
            {
                if (aliasName == name && aliasArity == arity)
                    result.Add(alias);
            }

            return result.ToImmutableAndClear();
        }
    }
}
