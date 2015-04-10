using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AssimilationSoftware.TodoSort.Core.Search;
using AssimilationSoftware.PimData.Model;

namespace AssimilationSoftware.TodoSort.CLI
{
    public class ExpressionParser
    {
        public static ISearchSpecification<ActionItem> Parse(string expression)
        {
            // Verify bracket and quote matching.
            int cardcount = 0;
            bool inquotes = false;
            for (int x = 0; x < expression.Length; x++)
            {
                if (expression[x] == '(' && !inquotes)
                {
                    cardcount++;
                }
                else if (expression[x] == ')' && !inquotes)
                {
                    cardcount--;
                }
                else if (inquotes && expression[x] == '\\')
                {
                    // Escape character.
                    x++;
                }
                else if (expression[x] == '"')
                {
                    inquotes = !inquotes;
                }
            }
            if (cardcount != 0)
            {
                throw new ArgumentException("Unbalanced paretheses in expression.");
            }
            if (inquotes)
            {
                throw new ArgumentException("Unbalanced quotes in expression.");
            }

            Queue<string> tokens = Tokenise(expression);
            object parsedexp = Parse(ref tokens);
            return Compile(parsedexp);
        }

        /// <summary>
        /// Breaks up a string into parsing tokens.
        /// </summary>
        /// <param name="expression">The expression to process.</param>
        /// <returns>A queue of tokens represented in the original string.</returns>
        private static Queue<string> Tokenise(string exp)
        {
            Queue<string> tokens = new Queue<string>();
            string oneCharSpecialTokens = "()";
            // Ensure parentheses are counted as tokens and add a sentinel space at the end.
            bool in_quote = false;
            StringBuilder token = new StringBuilder();
            for (int x = 0; x < exp.Length; x++)
            {
                if (exp[x] == '"')
                {
                    in_quote = !in_quote;
                }
                else if (exp[x] == '\\' && in_quote)
                {
                    // Escape character.
                    x++;
                    token.Append(exp[x]);
                }
                else if (in_quote)
                {
                    token.Append(exp[x]);
                }
                else if (oneCharSpecialTokens.Contains(exp[x])) // Special tokens.
                {
                    if (token.Length > 0)
                    {
                        // Enqueue any pending token.
                        tokens.Enqueue(token.ToString());
                        token = new StringBuilder();
                    }
                    // Enqueue the parenthesis as its own token.
                    tokens.Enqueue(exp[x].ToString());
                }
                else if (exp[x] == ' ') // End of token.
                {
                    if (token.Length > 0)
                    {
                        tokens.Enqueue(token.ToString());
                        token = new StringBuilder();
                    }
                }
                else
                {
                    token.Append(exp[x]);
                }
            }
            if (token.Length > 0)
            {
                tokens.Enqueue(token.ToString());
            }
            return tokens;
        }

        /// <summary>
        /// Turns a nested list of strings into an ISearchSpecification object tree.
        /// </summary>
        /// <param name="parsedexp">The parsed expression.</param>
        /// <returns>A search condition object.</returns>
        private static ISearchSpecification<ActionItem> Compile(object parsedexp)
        {
            if (parsedexp is string)
            {
                return new FullTextSearchSpecification((string)parsedexp);
            }
            else if (parsedexp is List<object>)
            {
                List<object> exlist = (List<object>)parsedexp;
                ISearchSpecification<ActionItem> result = null;
				switch ((string)exlist[0])
				{
					case "and":
					case "all-of":
                        List<ISearchSpecification<ActionItem>> andTerms = new List<ISearchSpecification<ActionItem>>();
						for (int x = 1; x < exlist.Count; x++)
						{
							andTerms.Add(Compile(exlist[x]));
						}
                        result = new AndSpecification<ActionItem>(andTerms.ToArray());
						break;
					case "or":
					case "any-of":
                        List<ISearchSpecification<ActionItem>> orTerms = new List<ISearchSpecification<ActionItem>>();
						for (int x = 1; x < exlist.Count; x++)
						{
                            orTerms.Add(Compile(exlist[x]));
						}
                        result = new OrSpecification<ActionItem>(orTerms.ToArray());
						break;
					case "not":
					case "none-of":
                        List<ISearchSpecification<ActionItem>> notTerms = new List<ISearchSpecification<ActionItem>>();
						for (int x = 1; x < exlist.Count; x++)
						{
                            notTerms.Add(Compile(exlist[x]));
						}
                        result = new NotSpecification<ActionItem>(notTerms.ToArray());
						break;
					default:
                        throw new ArgumentException(string.Format("Search Specification parser error: unknown function '{0}'", exlist[0]));
                }
                return result;
            }
            else
            {
                return new TrueSpecification<ActionItem>();
            }
        }

        private static object Parse(ref Queue<string> tokens)
        {
            if (tokens.Count == 0)
            {
                return null;
            }
            string token = tokens.Dequeue();
            if (token == "(")
            {
                List<object> result = new List<object>();
                while (tokens.Peek() != ")")
                {
                    object subexpression = Parse(ref tokens);
                    if (subexpression is string)
                    {
                        result.Add((string)subexpression);
                    }
                    else
                    {
                        result.Add((List<object>)subexpression);
                    }
                }
                tokens.Dequeue(); // pop off ')'
                return result;
            }
            else if (")" == token)
            {
                throw new Exception("unexpected closing parenthesis ')'");
            }
            else
            {
                return token;
            }
        }
    }
}
