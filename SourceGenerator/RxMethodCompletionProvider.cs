using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;

namespace RxSourceGenerator
{
    [ExportCompletionProvider(name: nameof(RxMethodCompletionProvider), language: LanguageNames.CSharp), Shared]
    internal class RxMethodCompletionProvider : CompletionProvider
    {
        public override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, OptionSet options)
        {
            switch (trigger.Kind)
            {
                case CompletionTriggerKind.Insertion:
                    int insertedCharacterPosition = caretPosition - 1;
                    if (insertedCharacterPosition <= 0) return false;
                    char ch = text[insertedCharacterPosition];
                    char previousCh = text[insertedCharacterPosition - 1];
                    return ch == '.' && !char.IsWhiteSpace(previousCh) && previousCh != '\t' && previousCh != '\r' && previousCh != '\n';

                default:
                    return false;
            }
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            SyntaxNode? syntaxNode = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (!(syntaxNode?.FindNode(context.CompletionListSpan) is ExpressionStatementSyntax
                expressionStatementSyntax)) return;
            if (!(expressionStatementSyntax.Expression is MemberAccessExpressionSyntax syntax)) return;
            if (!(await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false) is { }
                model)) return;

            ITypeSymbol? typeSymbol = model.GetSymbolInfo(syntax.Expression).Symbol switch
            {
                IMethodSymbol s => s.ReturnType,
                ILocalSymbol s => s.Type,
                IPropertySymbol s => s.Type,
                IFieldSymbol s => s.Type,
                IParameterSymbol s => s.Type,
                _ => null
            };
            if (typeSymbol == null) return;

            foreach (IEventSymbol ev in typeSymbol.GetMembersOfType<IEventSymbol>())
            {
                if (!(ev.Type is INamedTypeSymbol namedTypeSymbol)) continue;
                if (namedTypeSymbol.DelegateInvokeMethod == null) continue;

                List<ITypeSymbol>? typeArguments = namedTypeSymbol.DelegateInvokeMethod.Parameters.Select(m => m.Type).ToList();
                string fullType = namedTypeSymbol.ToDisplayString();

                List<string>? taggedTexts = new List<string>() { WellKnownTags.ExtensionMethod };

                switch (typeArguments.Count)
                {
                    case 0:
                        taggedTexts.Add(TextTags.Class);
                        taggedTexts.Add(fullType);
                        break;
                    case 1:
                        taggedTexts.Add(typeArguments.First().IsValueType ?
                            TextTags.Struct :
                            TextTags.Class);

                        taggedTexts.Add(typeArguments.First().ToDisplayString());

                        break;
                    default:
                        taggedTexts.Add(TextTags.Text);
                        taggedTexts.Add("(");

                        foreach ((ITypeSymbol? type, int i) in typeArguments.Select((type, i) => (type, i)))
                        {
                            taggedTexts.Add(typeArguments.First().IsValueType ?
                                TextTags.Struct :
                                TextTags.Class);
                            taggedTexts.Add(type.ToDisplayString());

                            taggedTexts.Add(TextTags.Parameter);
                            taggedTexts.Add($" Item{i + 1}");

                            taggedTexts.Add(TextTags.Text);
                            taggedTexts.Add(", ");
                        }

                        taggedTexts.RemoveAt(taggedTexts.Count - 1);
                        taggedTexts.RemoveAt(taggedTexts.Count - 1);
                        taggedTexts.Add(TextTags.Text);
                        taggedTexts.Add(") ");
                        taggedTexts.Add(TextTags.Class);
                        taggedTexts.Add($"RxGeneratedMethods.");
                        taggedTexts.Add(TextTags.Method);
                        taggedTexts.Add($"Rx{ev.Name}()");
                        break;
                }

                CompletionItem item = CompletionItem.Create($"Rx{ev.Name}", tags: ImmutableArray.Create(taggedTexts.ToArray()));
                context.AddItem(item);
            }
        }

        public override Task<CompletionDescription> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            List<TaggedText> taggedTexts = new List<TaggedText>();

            for (int i = 1; i < item.Tags.Length; i = 2 + i)
            {
                taggedTexts.Add(new TaggedText(item.Tags[i], item.Tags[i + 1]));
            }
            return Task.FromResult(CompletionDescription.Create(ImmutableArray.Create(taggedTexts.ToArray())));
        }

        public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item,
            char? commitKey, CancellationToken cancellationToken)
        {
            if (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is CompilationUnitSyntax
                rootNode)
            {

                if (rootNode.Usings.All(u => u.Name.GetText().ToString() != $"System.Reactive.Linq"))
                {
                    rootNode = rootNode.InsertNodesAfter(rootNode.Usings.Last(),
                        new[]
                        {
                            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName($"System.Reactive.Linq"))
                                .NormalizeWhitespace().WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)
                        });
                }

                if (rootNode.Usings.All(u => u.Name.GetText().ToString() != "RxMethodGenerator"))
                {
                    rootNode = rootNode.InsertNodesAfter(rootNode.Usings.Last(),
                        new[]
                        {
                            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("RxMethodGenerator"))
                                .NormalizeWhitespace().WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)
                        });
                }

                Document? newDocument = document.WithSyntaxRoot(rootNode);
                document.Project.Solution.Workspace.TryApplyChanges(newDocument.Project.Solution);
            }

            string newText = $".{item.DisplayText}()";
            TextSpan newSpan = new TextSpan(item.Span.Start - 1, 1);

            TextChange textChange = new TextChange(newSpan, newText);
            return await Task.FromResult(CompletionChange.Create(textChange));
        }
    }
}