using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Tags;

namespace RxSourceGenerator
{
    [ExportCompletionProvider(nameof(RxCompletionProvider), LanguageNames.CSharp)]
    [Shared]
    public class RxCompletionProvider : CompletionProvider
    {
        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var token = root?.FindToken(context.Position);
            var node = token?.Parent?.AncestorsAndSelf().OfType<MemberAccessExpressionSyntax>().FirstOrDefault();
            if (node == null) return;

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var typeInfo = semanticModel.GetTypeInfo(node.Expression, context.CancellationToken);
            if (typeInfo.Type is not INamedTypeSymbol typeSymbol) return;

            // Получаем все extension-методы для типа
            var extensionMethods = semanticModel?.LookupSymbols(node.SpanStart, container: null, name: null)
                .OfType<IMethodSymbol>()
                .Where(m => m.IsExtensionMethod && SymbolEqualityComparer.Default.Equals(m.Parameters.FirstOrDefault()?.Type, typeSymbol))
                .Select(m => m.Name)
                .ToHashSet();

            foreach (var ev in typeSymbol.GetMembers().OfType<IEventSymbol>())
            {
                var rxName = $"Rx{ev.Name}";
                // Если extension-метод уже есть — не добавляем в автодополнение
                if (extensionMethods != null && extensionMethods.Contains(rxName))
                    continue;
                var item = CompletionItem.Create(rxName, tags: [WellKnownTags.ExtensionMethod]);
                context.AddItem(item);
            }
        }

        public override Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey = null, CancellationToken cancellationToken = default)
        {
            var span = item.Span;
            var newText = $".{item.DisplayText}()";
            return Task.FromResult(CompletionChange.Create(new TextChange(span, newText)));
        }
    }
}