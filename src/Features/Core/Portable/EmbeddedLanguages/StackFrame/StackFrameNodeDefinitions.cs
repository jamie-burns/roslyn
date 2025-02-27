﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame
{
    using StackFrameNodeOrToken = EmbeddedSyntaxNodeOrToken<StackFrameKind, StackFrameNode>;
    using StackFrameToken = EmbeddedSyntaxToken<StackFrameKind>;

    internal abstract class StackFrameNode : EmbeddedSyntaxNode<StackFrameKind, StackFrameNode>
    {
        protected StackFrameNode(StackFrameKind kind) : base(kind)
        {
        }

        public abstract void Accept(IStackFrameNodeVisitor visitor);
    }

    internal abstract class StackFrameDeclarationNode : StackFrameNode
    {
        protected StackFrameDeclarationNode(StackFrameKind kind) : base(kind)
        {
        }
    }

    internal sealed class StackFrameMethodDeclarationNode : StackFrameDeclarationNode
    {
        public readonly StackFrameQualifiedNameNode MemberAccessExpression;
        public readonly StackFrameTypeArgumentList? TypeArguments;
        public readonly StackFrameParameterList ArgumentList;

        public StackFrameMethodDeclarationNode(
            StackFrameQualifiedNameNode memberAccessExpression,
            StackFrameTypeArgumentList? typeArguments,
            StackFrameParameterList argumentList)
            : base(StackFrameKind.MethodDeclaration)
        {
            MemberAccessExpression = memberAccessExpression;
            TypeArguments = typeArguments;
            ArgumentList = argumentList;
        }

        internal override int ChildCount => 3;

        public override void Accept(IStackFrameNodeVisitor visitor)
            => visitor.Visit(this);

        internal override StackFrameNodeOrToken ChildAt(int index)
             => index switch
             {
                 0 => MemberAccessExpression,
                 1 => TypeArguments,
                 2 => ArgumentList,
                 _ => throw new InvalidOperationException(),
             };
    }

    /// <summary>
    /// Base class for all type nodes
    /// </summary>
    internal abstract class StackFrameTypeNode : StackFrameNode
    {
        protected StackFrameTypeNode(StackFrameKind kind) : base(kind)
        {
        }
    }

    /// <summary>
    /// Base class for all name nodes
    /// </summary>
    /// <remarks>
    /// All of these are <see cref="StackFrameTypeNode" />. If a node requires an identifier or name that 
    /// is not a type then it should use <see cref="StackFrameToken"/> with <see cref="StackFrameKind.IdentifierToken"/>
    /// directly.
    /// </remarks>
    internal abstract class StackFrameNameNode : StackFrameTypeNode
    {
        protected StackFrameNameNode(StackFrameKind kind) : base(kind)
        {
        }
    }

    /// <summary>
    /// Base class for <see cref="StackFrameIdentifierNameNode"/> and <see cref="StackFrameGenericNameNode"/>
    /// </summary>
    internal abstract class StackFrameSimpleNameNode : StackFrameNameNode
    {
        public readonly StackFrameToken Identifier;

        protected StackFrameSimpleNameNode(StackFrameToken identifier, StackFrameKind kind) : base(kind)
        {
            Debug.Assert(identifier.Kind == StackFrameKind.IdentifierToken);
            Identifier = identifier;
        }
    }

    /// <summary>
    /// Represents a qualified name, such as "MyClass.MyMethod"
    /// </summary>
    internal sealed class StackFrameQualifiedNameNode : StackFrameNameNode
    {
        public readonly StackFrameNameNode Left;
        public readonly StackFrameToken DotToken;
        public readonly StackFrameSimpleNameNode Right;

        public StackFrameQualifiedNameNode(StackFrameNameNode left, StackFrameToken dotToken, StackFrameSimpleNameNode right) : base(StackFrameKind.MemberAccess)
        {
            Debug.Assert(dotToken.Kind == StackFrameKind.DotToken);

            Left = left;
            DotToken = dotToken;
            Right = right;
        }

        internal override int ChildCount => 3;

        public override void Accept(IStackFrameNodeVisitor visitor)
            => visitor.Visit(this);

        internal override StackFrameNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => Left,
                1 => DotToken,
                2 => Right,
                _ => throw new InvalidOperationException()
            };
    }

    /// <summary>
    /// The simplest identifier node, which wraps a <see cref="StackFrameKind.IdentifierToken" />
    /// </summary>
    internal sealed class StackFrameIdentifierNameNode : StackFrameSimpleNameNode
    {
        internal override int ChildCount => 1;

        public StackFrameIdentifierNameNode(StackFrameToken identifier)
            : base(identifier, StackFrameKind.TypeIdentifier)
        {
        }

        public override void Accept(IStackFrameNodeVisitor visitor)
            => visitor.Visit(this);

        internal override StackFrameNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => Identifier,
                _ => throw new InvalidOperationException()
            };
    }

    /// <summary>
    /// An identifier with an arity, such as "MyNamespace.MyClass`1" 
    /// </summary>
    internal sealed class StackFrameGenericNameNode : StackFrameSimpleNameNode
    {
        /// <summary>
        /// The "`" token in arity identifiers. Must be <see cref="StackFrameKind.GraveAccentToken"/>
        /// </summary>
        public readonly StackFrameToken GraveAccentToken;

        public readonly StackFrameToken NumberToken;

        internal override int ChildCount => 3;

        public StackFrameGenericNameNode(StackFrameToken identifier, StackFrameToken graveAccentToken, StackFrameToken numberToken)
            : base(identifier, StackFrameKind.GenericTypeIdentifier)
        {
            Debug.Assert(graveAccentToken.Kind == StackFrameKind.GraveAccentToken);
            Debug.Assert(numberToken.Kind == StackFrameKind.NumberToken);

            GraveAccentToken = graveAccentToken;
            NumberToken = numberToken;
        }

        public override void Accept(IStackFrameNodeVisitor visitor)
            => visitor.Visit(this);

        internal override StackFrameNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => Identifier,
                1 => GraveAccentToken,
                2 => NumberToken,
                _ => throw new InvalidOperationException()
            };
    }

    /// <summary>
    /// Represents an array type declaration, such as string[,][]
    /// </summary>
    internal sealed class StackFrameArrayTypeNode : StackFrameTypeNode
    {
        /// <summary>
        /// The type identifier without the array indicators.
        /// string[][]
        /// ^----^
        /// </summary>
        public readonly StackFrameNameNode TypeIdentifier;

        /// <summary>
        /// Each unique array identifier for the type
        /// string[,][]
        ///        ^--- First array expression = "[,]"
        ///           ^- Second array expression = "[]" 
        /// </summary>
        public ImmutableArray<StackFrameArrayRankSpecifier> ArrayExpressions;

        public StackFrameArrayTypeNode(StackFrameNameNode typeIdentifier, ImmutableArray<StackFrameArrayRankSpecifier> arrayExpressions) : base(StackFrameKind.ArrayTypeExpression)
        {
            Debug.Assert(!arrayExpressions.IsDefaultOrEmpty);
            TypeIdentifier = typeIdentifier;
            ArrayExpressions = arrayExpressions;
        }

        internal override int ChildCount => 1 + ArrayExpressions.Length;

        public override void Accept(IStackFrameNodeVisitor visitor)
            => visitor.Visit(this);

        internal override StackFrameNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => TypeIdentifier,
                _ => ArrayExpressions[index - 1]
            };
    }

    internal sealed class StackFrameArrayRankSpecifier : StackFrameNode
    {
        public readonly StackFrameToken OpenBracket;
        public readonly StackFrameToken CloseBracket;
        public readonly ImmutableArray<StackFrameToken> CommaTokens;

        public StackFrameArrayRankSpecifier(StackFrameToken openBracket, StackFrameToken closeBracket, ImmutableArray<StackFrameToken> commaTokens)
            : base(StackFrameKind.ArrayExpression)
        {
            Debug.Assert(!commaTokens.IsDefault);
            Debug.Assert(openBracket.Kind == StackFrameKind.OpenBracketToken);
            Debug.Assert(closeBracket.Kind == StackFrameKind.CloseBracketToken);
            Debug.Assert(commaTokens.All(static t => t.Kind == StackFrameKind.CommaToken));

            OpenBracket = openBracket;
            CloseBracket = closeBracket;
            CommaTokens = commaTokens;
        }

        internal override int ChildCount => 2 + CommaTokens.Length;

        public override void Accept(IStackFrameNodeVisitor visitor)
            => visitor.Visit(this);

        internal override StackFrameNodeOrToken ChildAt(int index)
        {
            if (index == 0)
            {
                return OpenBracket;
            }

            if (index == ChildCount - 1)
            {
                return CloseBracket;
            }

            return CommaTokens[index - 1];
        }
    }

    /// <summary>
    /// The type argument list for a method declaration. 
    /// 
    /// <code>
    /// Ex: MyType.MyMethod[T, U, V](T t, U u, V v) 
    ///                    ^-----------------------  "[" = Open Token 
    ///                     ^------^   ------------  "T, U, V" = SeparatedStackFrameNodeList&lt;StackFrameTypeArgumentNode&gt;
    ///                             ^--------------  "]" = Close Token
    /// </code>
    /// 
    /// </summary>
    internal sealed class StackFrameTypeArgumentList : StackFrameNode
    {
        public readonly StackFrameToken OpenToken;
        public readonly EmbeddedSeparatedSyntaxNodeList<StackFrameKind, StackFrameNode, StackFrameIdentifierNameNode> TypeArguments;
        public readonly StackFrameToken CloseToken;

        public StackFrameTypeArgumentList(
            StackFrameToken openToken,
            EmbeddedSeparatedSyntaxNodeList<StackFrameKind, StackFrameNode, StackFrameIdentifierNameNode> typeArguments,
            StackFrameToken closeToken)
            : base(StackFrameKind.TypeArgument)
        {
            Debug.Assert(openToken.Kind is StackFrameKind.OpenBracketToken or StackFrameKind.LessThanToken);
            Debug.Assert(typeArguments.Length > 0);
            Debug.Assert(openToken.Kind == StackFrameKind.OpenBracketToken ? closeToken.Kind == StackFrameKind.CloseBracketToken : closeToken.Kind == StackFrameKind.GreaterThanToken);

            OpenToken = openToken;
            TypeArguments = typeArguments;
            CloseToken = closeToken;
        }

        internal override int ChildCount => TypeArguments.NodesAndTokens.Length + 2;

        public override void Accept(IStackFrameNodeVisitor visitor)
            => visitor.Visit(this);

        internal override StackFrameNodeOrToken ChildAt(int index)
        {
            if (index >= ChildCount)
            {
                throw new InvalidOperationException();
            }

            if (index == 0)
            {
                return OpenToken;
            }

            if (index == ChildCount - 1)
            {
                return CloseToken;
            }

            // Includes both the nodes and separator tokens as children
            return TypeArguments.NodesAndTokens[index - 1];
        }
    }

    internal sealed class StackFrameParameterDeclarationNode : StackFrameDeclarationNode
    {
        public readonly StackFrameTypeNode Type;
        public readonly StackFrameToken Identifier;

        internal override int ChildCount => 2;

        public StackFrameParameterDeclarationNode(StackFrameTypeNode type, StackFrameToken identifier)
            : base(StackFrameKind.Parameter)
        {
            Debug.Assert(identifier.Kind == StackFrameKind.IdentifierToken);
            Type = type;
            Identifier = identifier;
        }

        public override void Accept(IStackFrameNodeVisitor visitor)
            => visitor.Visit(this);

        internal override StackFrameNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => Type,
                1 => Identifier,
                _ => throw new InvalidOperationException()
            };
    }

    internal sealed class StackFrameParameterList : StackFrameNode
    {
        public readonly StackFrameToken OpenParen;
        public readonly EmbeddedSeparatedSyntaxNodeList<StackFrameKind, StackFrameNode, StackFrameParameterDeclarationNode> Parameters;
        public readonly StackFrameToken CloseParen;

        public StackFrameParameterList(
            StackFrameToken openToken,
            EmbeddedSeparatedSyntaxNodeList<StackFrameKind, StackFrameNode, StackFrameParameterDeclarationNode> parameters,
            StackFrameToken closeToken)
            : base(StackFrameKind.ParameterList)
        {
            Debug.Assert(openToken.Kind == StackFrameKind.OpenParenToken);
            Debug.Assert(closeToken.Kind == StackFrameKind.CloseParenToken);

            OpenParen = openToken;
            Parameters = parameters;
            CloseParen = closeToken;
        }

        internal override int ChildCount => 2 + Parameters.NodesAndTokens.Length;

        public override void Accept(IStackFrameNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override StackFrameNodeOrToken ChildAt(int index)
        {
            if (index == 0)
            {
                return OpenParen;
            }

            if (index == ChildCount - 1)
            {
                return CloseParen;
            }

            // Include both nodes and tokens here as children of the StackFrameParameterList
            return Parameters.NodesAndTokens[index - 1];
        }
    }

    internal sealed class StackFrameFileInformationNode : StackFrameNode
    {
        public readonly StackFrameToken Path;
        public readonly StackFrameToken? Colon;
        public readonly StackFrameToken? Line;

        public StackFrameFileInformationNode(StackFrameToken path, StackFrameToken? colon, StackFrameToken? line) : base(StackFrameKind.FileInformation)
        {
            Debug.Assert(path.Kind == StackFrameKind.PathToken);
            Debug.Assert(colon.HasValue == line.HasValue);
            Debug.Assert(!line.HasValue || line.Value.Kind == StackFrameKind.NumberToken);

            Path = path;
            Colon = colon;
            Line = line;
        }

        internal override int ChildCount => 3;

        public override void Accept(IStackFrameNodeVisitor visitor)
            => visitor.Visit(this);

        internal override StackFrameNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => Path,
                1 => Colon.HasValue ? Colon.Value : null,
                2 => Line.HasValue ? Line.Value : null,
                _ => throw new InvalidOperationException()
            };
    }
}
