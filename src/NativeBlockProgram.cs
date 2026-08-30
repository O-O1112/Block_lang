using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BlockEngine
{
    // Structured native Block interpreter. The syntax is the one already exposed
    // by the snippets: compound statements end with a standalone "block" line.
    internal static class NativeBlockProgram
    {
        private const int MaxLoopIterations = 10000;
        private const int MaxCallDepth = 64;

        private static readonly Regex IfHeader = new Regex(@"^if\s+(.+):$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ElifHeader = new Regex(@"^elif\s+(.+):$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex WhileHeader = new Regex(@"^while\s+(.+):$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ForHeader = new Regex(@"^for\s+([A-Za-z_][A-Za-z0-9_]*)\s+in\s+(.+):$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex FuncHeader = new Regex(@"^func\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(([^)]*)\)\s*:$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex PrintStatement = new Regex(@"^print\s*\((.*)\)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex Identifier = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

        public static void Execute(string code, Dictionary<string, object> state, Action<string> output)
        {
            if (string.IsNullOrWhiteSpace(code)) return;
            if (state == null) throw new ArgumentNullException("state");
            if (output == null) output = Console.Write;
            string[] lines = code.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            int index = 0;
            string terminator;
            List<Statement> program = ParseSequence(lines, ref index, true, out terminator);
            if (!string.IsNullOrEmpty(terminator)) throw Error(0, "Unexpected '" + terminator + "' at top level.");
            ExecuteStatements(program, new Context(state, output));
        }

        private static List<Statement> ParseSequence(string[] lines, ref int index, bool allowElse, out string terminator)
        {
            List<Statement> result = new List<Statement>();
            terminator = null;
            while (index < lines.Length)
            {
                int lineNumber = index + 1;
                string line = lines[index].Trim();
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("//")) { index++; continue; }
                if (string.Equals(line, "block", StringComparison.OrdinalIgnoreCase)) { index++; terminator = "block"; return result; }
                if (string.Equals(line, "else:", StringComparison.OrdinalIgnoreCase))
                {
                    if (!allowElse) throw Error(lineNumber, "Unexpected else block.");
                    terminator = "else";
                    return result;
                }
                if (ElifHeader.IsMatch(line))
                {
                    if (!allowElse) throw Error(lineNumber, "Unexpected elif block.");
                    terminator = "elif";
                    return result;
                }

                Match match = IfHeader.Match(line);
                if (match.Success)
                {
                    index++;
                    result.Add(ParseIfStatement(lineNumber, match.Groups[1].Value, lines, ref index));
                    continue;
                }

                match = WhileHeader.Match(line);
                if (match.Success)
                {
                    index++;
                    string bodyEnd;
                    List<Statement> body = ParseSequence(lines, ref index, false, out bodyEnd);
                    if (bodyEnd != "block") throw Error(lineNumber, "while block must end with 'block'.");
                    result.Add(new WhileStatement(lineNumber, match.Groups[1].Value, body));
                    continue;
                }

                match = ForHeader.Match(line);
                if (match.Success)
                {
                    index++;
                    string bodyEnd;
                    List<Statement> body = ParseSequence(lines, ref index, false, out bodyEnd);
                    if (bodyEnd != "block") throw Error(lineNumber, "for block must end with 'block'.");
                    result.Add(new ForStatement(lineNumber, match.Groups[1].Value, match.Groups[2].Value, body));
                    continue;
                }

                match = FuncHeader.Match(line);
                if (match.Success)
                {
                    List<string> parameters = new List<string>();
                    string parameterText = match.Groups[2].Value.Trim();
                    if (parameterText.Length > 0)
                    {
                        foreach (string raw in parameterText.Split(','))
                        {
                            string parameter = raw.Trim();
                            if (!Identifier.IsMatch(parameter)) throw Error(lineNumber, "Invalid function parameter: " + parameter);
                            parameters.Add(parameter);
                        }
                    }
                    index++;
                    string bodyEnd;
                    List<Statement> body = ParseSequence(lines, ref index, false, out bodyEnd);
                    if (bodyEnd != "block") throw Error(lineNumber, "func block must end with 'block'.");
                    result.Add(new FunctionStatement(lineNumber, match.Groups[1].Value, parameters, body));
                    continue;
                }

                if (line.StartsWith("else", StringComparison.OrdinalIgnoreCase)) throw Error(lineNumber, "else must be written as 'else:'.");
                result.Add(new SimpleStatement(lineNumber, line));
                index++;
            }
            return result;
        }

        private static IfStatement ParseIfStatement(int lineNumber, string condition, string[] lines, ref int index)
        {
            string bodyEnd;
            List<Statement> thenBody = ParseSequence(lines, ref index, true, out bodyEnd);
            List<Statement> elseBody = new List<Statement>();

            if (bodyEnd == "elif")
            {
                int elifLine = index + 1;
                Match elif = ElifHeader.Match(lines[index].Trim());
                if (!elif.Success) throw Error(elifLine, "Invalid elif block.");
                index++;
                elseBody.Add(ParseIfStatement(elifLine, elif.Groups[1].Value, lines, ref index));
            }
            else if (bodyEnd == "else")
            {
                index++;
                string elseEnd;
                elseBody = ParseSequence(lines, ref index, false, out elseEnd);
                if (elseEnd != "block") throw Error(lineNumber, "if/else block must end with 'block'.");
            }
            else if (bodyEnd != "block")
            {
                throw Error(lineNumber, "if block must end with 'block'.");
            }

            return new IfStatement(lineNumber, condition, thenBody, elseBody);
        }

        private static void ExecuteStatements(List<Statement> statements, Context context)
        {
            foreach (Statement statement in statements)
            {
                FunctionStatement function = statement as FunctionStatement;
                if (function != null) { context.Functions[function.Name] = function; continue; }

                SimpleStatement simple = statement as SimpleStatement;
                if (simple != null) { ExecuteSimple(simple, context); continue; }

                IfStatement conditional = statement as IfStatement;
                if (conditional != null)
                {
                    ExecuteStatements(ToBool(Evaluate(conditional.Condition, context, conditional.Line)) ? conditional.ThenBody : conditional.ElseBody, context);
                    continue;
                }

                WhileStatement loop = statement as WhileStatement;
                if (loop != null)
                {
                    int count = 0;
                    while (ToBool(Evaluate(loop.Condition, context, loop.Line)))
                    {
                        if (++count > MaxLoopIterations) throw Error(loop.Line, "while loop exceeded the 10,000 iteration limit.");
                        context.LoopDepth++;
                        try
                        {
                            ExecuteStatements(loop.Body, context);
                        }
                        catch (ContinueSignal) { }
                        catch (BreakSignal) { break; }
                        finally
                        {
                            context.LoopDepth--;
                        }
                    }
                    continue;
                }

                ForStatement forLoop = statement as ForStatement;
                if (forLoop != null)
                {
                    IEnumerable values = Evaluate(forLoop.Iterable, context, forLoop.Line) as IEnumerable;
                    if (values == null) throw Error(forLoop.Line, "for expression is not iterable.");
                    int count = 0;
                    foreach (object value in values)
                    {
                        if (++count > MaxLoopIterations) throw Error(forLoop.Line, "for loop exceeded the 10,000 iteration limit.");
                        context.SetValue(forLoop.Variable, value);
                        context.LoopDepth++;
                        try
                        {
                            ExecuteStatements(forLoop.Body, context);
                        }
                        catch (ContinueSignal) { }
                        catch (BreakSignal) { break; }
                        finally
                        {
                            context.LoopDepth--;
                        }
                    }
                }
            }
        }

        private static void ExecuteSimple(SimpleStatement statement, Context context)
        {
            string line = statement.Code;
            if (string.Equals(line, "pass", StringComparison.OrdinalIgnoreCase)) return;
            if (string.Equals(line, "break", StringComparison.OrdinalIgnoreCase))
            {
                if (context.LoopDepth <= 0) throw Error(statement.Line, "break can only be used inside a loop.");
                throw new BreakSignal();
            }
            if (string.Equals(line, "continue", StringComparison.OrdinalIgnoreCase))
            {
                if (context.LoopDepth <= 0) throw Error(statement.Line, "continue can only be used inside a loop.");
                throw new ContinueSignal();
            }
            if (string.Equals(line, "print", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("print(", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("print ", StringComparison.OrdinalIgnoreCase))
            {
                Match print = PrintStatement.Match(line);
                if (!print.Success) throw Error(statement.Line, "Invalid print statement. Use print(value, ...).");
                string args = print.Groups[1].Value;
                List<string> parts = SplitArguments(args, statement.Line);
                List<string> values = new List<string>();
                foreach (string part in parts) values.Add(FormatValue(Evaluate(part, context, statement.Line)));
                context.Output(string.Join(" ", values) + Environment.NewLine);
                return;
            }
            if (line.StartsWith("return", StringComparison.OrdinalIgnoreCase) && (line.Length == 6 || char.IsWhiteSpace(line[6])))
            {
                string expression = line.Length == 6 ? "null" : line.Substring(6).Trim();
                throw new ReturnSignal(expression.Length == 0 ? null : Evaluate(expression, context, statement.Line));
            }
            int assignmentIndex = FindAssignmentOperator(line);
            if (assignmentIndex >= 0)
            {
                string target = line.Substring(0, assignmentIndex).Trim();
                string expression = line.Substring(assignmentIndex + 1).Trim();
                AssignTarget(target, Evaluate(expression, context, statement.Line), context, statement.Line);
                return;
            }
            Evaluate(line, context, statement.Line); // bare function call
        }

        private static int FindAssignmentOperator(string line)
        {
            bool quoted = false;
            char quote = '\0';
            bool escaped = false;
            int depth = 0;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (escaped) { escaped = false; continue; }
                if (quoted)
                {
                    if (c == '\\') { escaped = true; continue; }
                    if (c == quote) quoted = false;
                    continue;
                }
                if (c == '\'' || c == '"') { quoted = true; quote = c; continue; }
                if (c == '(' || c == '[' || c == '{') { depth++; continue; }
                if (c == ')' || c == ']' || c == '}') { if (depth > 0) depth--; continue; }
                if (depth == 0 && c == '=')
                {
                    char previous = i > 0 ? line[i - 1] : '\0';
                    char next = i + 1 < line.Length ? line[i + 1] : '\0';
                    if (previous != '=' && previous != '!' && previous != '<' && previous != '>' && next != '=') return i;
                }
            }
            return -1;
        }

        private static void AssignTarget(string target, object value, Context context, int lineNumber)
        {
            Match variable = Regex.Match(target, @"^[A-Za-z_][A-Za-z0-9_]*$");
            if (variable.Success)
            {
                context.SetValue(target, value);
                return;
            }

            Match indexed = Regex.Match(target, @"^([A-Za-z_][A-Za-z0-9_]*)\s*\[([\s\S]*)\]$");
            if (indexed.Success)
            {
                object container;
                if (!context.TryGetValue(indexed.Groups[1].Value, out container))
                    throw Error(lineNumber, "Unknown variable: " + indexed.Groups[1].Value);
                SetIndex(container, Evaluate(indexed.Groups[2].Value, context, lineNumber), value, lineNumber);
                return;
            }

            throw Error(lineNumber, "Invalid assignment target: " + target);
        }

        private static List<string> SplitArguments(string text, int lineNumber)
        {
            List<string> parts = new List<string>();
            StringBuilder current = new StringBuilder();
            bool quoted = false;
            char quote = '\0';
            bool escaped = false;
            int depth = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (escaped) { current.Append(c); escaped = false; continue; }
                if (quoted && c == '\\') { current.Append(c); escaped = true; continue; }
                if (c == '\'' || c == '"') { if (!quoted) { quoted = true; quote = c; } else if (quote == c) quoted = false; current.Append(c); continue; }
                if (!quoted && c == '(') depth++;
                if (!quoted && c == ')') depth--;
                if (!quoted && depth == 0 && c == ',') { parts.Add(current.ToString().Trim()); current.Clear(); } else current.Append(c);
            }
            if (quoted || depth != 0) throw Error(lineNumber, "Unbalanced expression arguments.");
            if (current.Length > 0 || parts.Count > 0) parts.Add(current.ToString().Trim());
            return parts;
        }

        private static object Evaluate(string expression, Context context, int lineNumber)
        {
            ExpressionParser parser = new ExpressionParser(expression, context, lineNumber);
            object value = parser.ParseExpression();
            if (!parser.IsAtEnd) throw Error(lineNumber, "Unexpected token: " + parser.Remaining);
            return value;
        }

        private static object InvokeFunction(string name, List<object> arguments, Context context, int lineNumber)
        {
            if (string.Equals(name, "range", StringComparison.OrdinalIgnoreCase))
            {
                if (arguments.Count < 1 || arguments.Count > 3) throw Error(lineNumber, "range expects 1 to 3 arguments.");
                double start = arguments.Count == 1 ? 0 : Number(arguments[0]);
                double end = arguments.Count == 1 ? Number(arguments[0]) : Number(arguments[1]);
                double step = arguments.Count < 3 ? 1 : Number(arguments[2]);
                if (Math.Abs(step) < double.Epsilon) throw Error(lineNumber, "range step cannot be zero.");
                List<object> values = new List<object>();
                int count = 0;
                if (step > 0)
                {
                    for (double i = start; i < end; i += step)
                    {
                        if (count++ >= MaxLoopIterations) throw Error(lineNumber, "range exceeded the 10,000 item limit.");
                        values.Add(NormalizeNumber(i));
                    }
                }
                else
                {
                    for (double i = start; i > end; i += step)
                    {
                        if (count++ >= MaxLoopIterations) throw Error(lineNumber, "range exceeded the 10,000 item limit.");
                        values.Add(NormalizeNumber(i));
                    }
                }
                return values;
            }

            if (string.Equals(name, "len", StringComparison.OrdinalIgnoreCase))
            {
                RequireArgumentCount(name, arguments, 1);
                object value = arguments[0];
                if (value is string) return ((string)value).Length;
                ICollection collection = value as ICollection;
                if (collection != null) return collection.Count;
                throw new InvalidOperationException("len expects a string, list, or map.");
            }
            if (string.Equals(name, "str", StringComparison.OrdinalIgnoreCase))
            {
                RequireArgumentCount(name, arguments, 1);
                return FormatValue(arguments[0]);
            }
            if (string.Equals(name, "int", StringComparison.OrdinalIgnoreCase))
            {
                RequireArgumentCount(name, arguments, 1);
                return NormalizeNumber(Math.Truncate(Number(arguments[0])));
            }
            if (string.Equals(name, "float", StringComparison.OrdinalIgnoreCase))
            {
                RequireArgumentCount(name, arguments, 1);
                return Number(arguments[0]);
            }
            if (string.Equals(name, "bool", StringComparison.OrdinalIgnoreCase))
            {
                RequireArgumentCount(name, arguments, 1);
                return ToBool(arguments[0]);
            }
            if (string.Equals(name, "type", StringComparison.OrdinalIgnoreCase))
            {
                RequireArgumentCount(name, arguments, 1);
                return TypeName(arguments[0]);
            }
            if (string.Equals(name, "contains", StringComparison.OrdinalIgnoreCase))
            {
                RequireArgumentCount(name, arguments, 2);
                return ContainsValue(arguments[0], arguments[1]);
            }
            if (string.Equals(name, "keys", StringComparison.OrdinalIgnoreCase))
            {
                RequireArgumentCount(name, arguments, 1);
                IDictionary map = arguments[0] as IDictionary;
                if (map == null) throw new InvalidOperationException("keys expects a map.");
                List<object> keys = new List<object>();
                foreach (object key in map.Keys) keys.Add(key == null ? null : key.ToString());
                return keys;
            }
            if (string.Equals(name, "values", StringComparison.OrdinalIgnoreCase))
            {
                RequireArgumentCount(name, arguments, 1);
                IDictionary map = arguments[0] as IDictionary;
                if (map == null) throw new InvalidOperationException("values expects a map.");
                List<object> values = new List<object>();
                foreach (object value in map.Values) values.Add(value);
                return values;
            }
            if (string.Equals(name, "sum", StringComparison.OrdinalIgnoreCase))
            {
                RequireArgumentCount(name, arguments, 1);
                IEnumerable sequence = arguments[0] as IEnumerable;
                if (sequence == null || arguments[0] is string) throw new InvalidOperationException("sum expects a list.");
                double total = 0;
                foreach (object value in sequence) total += Number(value);
                return NormalizeNumber(total);
            }

            FunctionStatement function;
            if (!context.Functions.TryGetValue(name, out function)) throw Error(lineNumber, "Unknown function: " + name);
            if (context.CallDepth >= MaxCallDepth) throw Error(lineNumber, "Function call depth exceeded.");
            if (arguments.Count != function.Parameters.Count) throw Error(lineNumber, string.Format("Function {0} expects {1} argument(s), got {2}.", name, function.Parameters.Count, arguments.Count));

            Dictionary<string, object> localScope = new Dictionary<string, object>(StringComparer.Ordinal);
            for (int i = 0; i < function.Parameters.Count; i++)
            {
                string parameter = function.Parameters[i];
                localScope[parameter] = arguments[i];
            }

            context.CallDepth++;
            int callerLoopDepth = context.LoopDepth;
            object returnValue = null;
            context.PushScope(localScope);
            context.LoopDepth = 0;
            try { ExecuteStatements(function.Body, context); }
            catch (ReturnSignal signal) { returnValue = signal.Value; }
            finally
            {
                context.LoopDepth = callerLoopDepth;
                context.PopScope();
                context.CallDepth--;
            }
            return returnValue;
        }

        private static void RequireArgumentCount(string name, List<object> arguments, int expected)
        {
            if (arguments.Count != expected)
                throw new InvalidOperationException(string.Format("{0} expects {1} argument(s), got {2}.", name, expected, arguments.Count));
        }

        private static string TypeName(object value)
        {
            if (value == null) return "null";
            if (value is bool) return "bool";
            if (value is string) return "string";
            if (value is IDictionary) return "map";
            if (value is IEnumerable && !(value is string)) return "list";
            if (value is byte || value is short || value is int || value is long || value is float || value is double || value is decimal)
                return "number";
            return value.GetType().Name.ToLowerInvariant();
        }

        private static bool ContainsValue(object container, object needle)
        {
            if (container is string) return ((string)container).Contains(FormatValue(needle));
            IDictionary map = container as IDictionary;
            if (map != null) return map.Contains(needle) || map.Contains(FormatValue(needle));
            IEnumerable sequence = container as IEnumerable;
            if (sequence != null)
            {
                foreach (object value in sequence) if (Equal(value, needle)) return true;
                return false;
            }
            throw new InvalidOperationException("contains expects a string, list, or map.");
        }

        private static object GetMember(object value, string member, int lineNumber)
        {
            if (string.Equals(member, "length", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(member, "count", StringComparison.OrdinalIgnoreCase))
            {
                if (value is string) return ((string)value).Length;
                ICollection collection = value as ICollection;
                if (collection != null) return collection.Count;
            }

            IDictionary map = value as IDictionary;
            if (map != null)
            {
                if (map.Contains(member)) return map[member];
                return null;
            }
            throw Error(lineNumber, "Unknown member '" + member + "' on " + TypeName(value) + ".");
        }

        private static object GetIndex(object value, object index, int lineNumber)
        {
            IList list = value as IList;
            if (list != null)
            {
                int position = ToIndex(index, lineNumber);
                if (position < 0 || position >= list.Count) throw Error(lineNumber, "List index is out of range: " + position + ".");
                return list[position];
            }
            string text = value as string;
            if (text != null)
            {
                int position = ToIndex(index, lineNumber);
                if (position < 0 || position >= text.Length) throw Error(lineNumber, "String index is out of range: " + position + ".");
                return text[position].ToString();
            }
            IDictionary map = value as IDictionary;
            if (map != null)
            {
                if (map.Contains(index)) return map[index];
                string key = FormatValue(index);
                return map.Contains(key) ? map[key] : null;
            }
            throw Error(lineNumber, "Value of type " + TypeName(value) + " is not indexable.");
        }

        private static void SetIndex(object value, object index, object replacement, int lineNumber)
        {
            IList list = value as IList;
            if (list != null)
            {
                int position = ToIndex(index, lineNumber);
                if (position < 0 || position >= list.Count) throw Error(lineNumber, "List index is out of range: " + position + ".");
                list[position] = replacement;
                return;
            }
            IDictionary map = value as IDictionary;
            if (map != null)
            {
                map[FormatValue(index)] = replacement;
                return;
            }
            throw Error(lineNumber, "Value of type " + TypeName(value) + " cannot be assigned by index.");
        }

        private static int ToIndex(object value, int lineNumber)
        {
            double number = Number(value);
            if (Math.Abs(number - Math.Round(number)) > 0.0000000001)
                throw Error(lineNumber, "Collection indexes must be integers.");
            return (int)Math.Round(number);
        }

        private static object NormalizeNumber(double value) { return Math.Abs(value - Math.Round(value)) < 0.0000000001 ? (object)(long)Math.Round(value) : value; }
        private static double Number(object value)
        {
            if (value is bool) return (bool)value ? 1 : 0;
            if (value == null) return 0;
            double result;
            if (double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out result)) return result;
            throw new InvalidOperationException("Expected a numeric value in native Block expression.");
        }
        private static bool ToBool(object value) { if (value == null) return false; if (value is bool) return (bool)value; if (value is string) return !string.IsNullOrEmpty((string)value); return Math.Abs(Number(value)) > double.Epsilon; }
        private static object Add(object left, object right) { return left is string || right is string ? (object)(FormatValue(left) + FormatValue(right)) : NormalizeNumber(Number(left) + Number(right)); }
        private static bool Equal(object left, object right)
        {
            if (left == null || right == null) return left == null && right == null;
            if (left is bool || right is bool)
            {
                if (left is bool && right is bool) return (bool)left == (bool)right;
                return false;
            }
            IList listA = left as IList;
            IList listB = right as IList;
            if (listA != null || listB != null)
            {
                if (listA == null || listB == null || listA.Count != listB.Count) return false;
                for (int i = 0; i < listA.Count; i++)
                {
                    if (!Equal(listA[i], listB[i])) return false;
                }
                return true;
            }
            IDictionary mapA = left as IDictionary;
            IDictionary mapB = right as IDictionary;
            if (mapA != null || mapB != null)
            {
                if (mapA == null || mapB == null || mapA.Count != mapB.Count) return false;
                foreach (object key in mapA.Keys)
                {
                    if (key == null) continue;
                    string k = key.ToString();
                    object valB = null;
                    if (mapB.Contains(key)) valB = mapB[key];
                    else if (mapB.Contains(k)) valB = mapB[k];
                    else return false;
                    if (!Equal(mapA[key], valB)) return false;
                }
                return true;
            }
            double a, b;
            if (double.TryParse(Convert.ToString(left, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out a) && double.TryParse(Convert.ToString(right, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out b)) return Math.Abs(a - b) < 0.0000000001;
            return string.Equals(left.ToString(), right.ToString(), StringComparison.Ordinal);
        }
        private static int Compare(object left, object right)
        {
            double a, b;
            if (double.TryParse(Convert.ToString(left, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out a) && double.TryParse(Convert.ToString(right, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out b)) return a.CompareTo(b);
            return string.Compare(Convert.ToString(left, CultureInfo.InvariantCulture), Convert.ToString(right, CultureInfo.InvariantCulture), StringComparison.Ordinal);
        }
        private static string FormatValue(object value)
        {
            if (value == null) return "null";
            if (value is bool) return (bool)value ? "true" : "false";
            IFormattable formattable = value as IFormattable;
            return formattable == null ? value.ToString() : formattable.ToString(null, CultureInfo.InvariantCulture);
        }
        private static InvalidOperationException Error(int line, string message) { return new InvalidOperationException(line > 0 ? string.Format("Native Block error at line {0}: {1}", line, message) : "Native Block error: " + message); }

        private abstract class Statement { protected Statement(int line) { Line = line; } public int Line; }
        private sealed class SimpleStatement : Statement { public SimpleStatement(int line, string code) : base(line) { Code = code; } public string Code; }
        private sealed class IfStatement : Statement { public IfStatement(int line, string condition, List<Statement> thenBody, List<Statement> elseBody) : base(line) { Condition = condition; ThenBody = thenBody; ElseBody = elseBody; } public string Condition; public List<Statement> ThenBody; public List<Statement> ElseBody; }
        private sealed class WhileStatement : Statement { public WhileStatement(int line, string condition, List<Statement> body) : base(line) { Condition = condition; Body = body; } public string Condition; public List<Statement> Body; }
        private sealed class ForStatement : Statement { public ForStatement(int line, string variable, string iterable, List<Statement> body) : base(line) { Variable = variable; Iterable = iterable; Body = body; } public string Variable; public string Iterable; public List<Statement> Body; }
        private sealed class FunctionStatement : Statement { public FunctionStatement(int line, string name, List<string> parameters, List<Statement> body) : base(line) { Name = name; Parameters = parameters; Body = body; } public string Name; public List<string> Parameters; public List<Statement> Body; }
        private sealed class Context
        {
            private readonly List<Dictionary<string, object>> scopes = new List<Dictionary<string, object>>();

            public Context(Dictionary<string, object> state, Action<string> output)
            {
                State = state;
                Output = output;
                Functions = new Dictionary<string, FunctionStatement>(StringComparer.OrdinalIgnoreCase);
            }

            public Dictionary<string, object> State;
            public Action<string> Output;
            public Dictionary<string, FunctionStatement> Functions;
            public int CallDepth;
            public int LoopDepth;

            public void PushScope(Dictionary<string, object> scope) { scopes.Add(scope); }
            public void PopScope() { if (scopes.Count > 0) scopes.RemoveAt(scopes.Count - 1); }

            public bool TryGetValue(string name, out object value)
            {
                for (int i = scopes.Count - 1; i >= 0; i--)
                    if (scopes[i].TryGetValue(name, out value)) return true;
                return State.TryGetValue(name, out value);
            }

            public void SetValue(string name, object value)
            {
                if (scopes.Count == 0) State[name] = value;
                else scopes[scopes.Count - 1][name] = value;
            }
        }
        private sealed class ReturnSignal : Exception { public ReturnSignal(object value) { Value = value; } public object Value; }
        private sealed class BreakSignal : Exception { }
        private sealed class ContinueSignal : Exception { }

        private sealed class ExpressionParser
        {
            private readonly string text; private readonly Context context; private readonly int line; private int position;
            private bool suppressEvaluation;
            public ExpressionParser(string value, Context owner, int sourceLine) { text = value ?? ""; context = owner; line = sourceLine; }
            public bool IsAtEnd { get { Skip(); return position >= text.Length; } }
            public string Remaining { get { return position < text.Length ? text.Substring(position) : ""; } }
            public object ParseExpression() { return ParseOr(); }
            private object ParseOr()
            {
                object left = ParseAnd();
                while (Match("||"))
                {
                    if (suppressEvaluation)
                    {
                        ParseAnd();
                        left = null;
                    }
                    else if (ToBool(left))
                    {
                        ParseWithoutEvaluation(delegate { return ParseAnd(); });
                        left = true;
                    }
                    else
                    {
                        left = ToBool(ParseAnd());
                    }
                }
                return left;
            }

            private object ParseAnd()
            {
                object left = ParseEquality();
                while (Match("&&"))
                {
                    if (suppressEvaluation)
                    {
                        ParseEquality();
                        left = null;
                    }
                    else if (!ToBool(left))
                    {
                        ParseWithoutEvaluation(delegate { return ParseEquality(); });
                        left = false;
                    }
                    else
                    {
                        left = ToBool(ParseEquality());
                    }
                }
                return left;
            }

            private object ParseEquality()
            {
                object left = ParseComparison();
                while (true)
                {
                    if (Match("=="))
                    {
                        object right = ParseComparison();
                        left = suppressEvaluation ? (object)null : (object)Equal(left, right);
                    }
                    else if (Match("!="))
                    {
                        object right = ParseComparison();
                        left = suppressEvaluation ? (object)null : (object)!Equal(left, right);
                    }
                    else return left;
                }
            }

            private object ParseComparison()
            {
                object left = ParseTerm();
                while (true)
                {
                    if (Match("<=")) { object right = ParseTerm(); left = suppressEvaluation ? (object)null : (object)(Compare(left, right) <= 0); }
                    else if (Match(">=")) { object right = ParseTerm(); left = suppressEvaluation ? (object)null : (object)(Compare(left, right) >= 0); }
                    else if (Match("<")) { object right = ParseTerm(); left = suppressEvaluation ? (object)null : (object)(Compare(left, right) < 0); }
                    else if (Match(">")) { object right = ParseTerm(); left = suppressEvaluation ? (object)null : (object)(Compare(left, right) > 0); }
                    else return left;
                }
            }
            private object ParseTerm()
            {
                object left = ParseFactor();
                while (true)
                {
                    if (Match("+")) { object right = ParseFactor(); left = suppressEvaluation ? null : Add(left, right); }
                    else if (Match("-")) { object right = ParseFactor(); left = suppressEvaluation ? null : NormalizeNumber(Number(left) - Number(right)); }
                    else return left;
                }
            }

            private object ParseFactor()
            {
                object left = ParseUnary();
                while (true)
                {
                    if (Match("*")) { object right = ParseUnary(); left = suppressEvaluation ? null : NormalizeNumber(Number(left) * Number(right)); }
                    else if (Match("/"))
                    {
                        object right = ParseUnary();
                        if (suppressEvaluation) { left = null; continue; }
                        double divisor = Number(right);
                        if (Math.Abs(divisor) < double.Epsilon) throw Error(line, "Division by zero.");
                        left = Number(left) / divisor;
                    }
                    else if (Match("%")) { object right = ParseUnary(); left = suppressEvaluation ? null : NormalizeNumber(Number(left) % Number(right)); }
                    else return left;
                }
            }
            private object ParseUnary()
            {
                if (Match("!")) { object value = ParseUnary(); return suppressEvaluation ? (object)null : (object)!ToBool(value); }
                if (Match("-")) { object value = ParseUnary(); return suppressEvaluation ? (object)null : (object)-Number(value); }
                return ParsePrimary();
            }
            private object ParsePrimary()
            {
                Skip();
                if (Match("(")) { object value = ParseExpression(); Require(")"); return ParsePostfix(value); }
                if (position < text.Length && (text[position] == '\'' || text[position] == '"'))
                {
                    string value = ParseString();
                    return ParsePostfix(suppressEvaluation ? null : value);
                }
                if (Match("["))
                {
                    List<object> values = new List<object>();
                    if (!Match("]")) { while (true) { values.Add(ParseExpression()); if (Match("]")) break; Require(","); } }
                    return ParsePostfix(suppressEvaluation ? null : values);
                }
                if (Match("{"))
                {
                    Dictionary<string, object> map = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    if (!Match("}"))
                    {
                        while (true)
                        {
                            Skip();
                            string key = position < text.Length && (text[position] == '\'' || text[position] == '"')
                                ? ParseString() : ReadIdentifier();
                            Require(":");
                            map[key] = ParseExpression();
                            if (Match("}")) break;
                            Require(",");
                        }
                    }
                    return ParsePostfix(suppressEvaluation ? null : map);
                }

                string token = ReadToken();
                double number;
                if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out number)) return suppressEvaluation ? null : NormalizeNumber(number);
                if (token.Equals("true", StringComparison.OrdinalIgnoreCase)) return suppressEvaluation ? null : (object)true;
                if (token.Equals("false", StringComparison.OrdinalIgnoreCase)) return suppressEvaluation ? null : (object)false;
                if (token.Equals("null", StringComparison.OrdinalIgnoreCase)) return null;
                object resolvedValue;
                if (Match("("))
                {
                    List<object> arguments = new List<object>();
                    if (!Match(")")) { while (true) { arguments.Add(ParseExpression()); if (Match(")")) break; Require(","); } }
                    resolvedValue = suppressEvaluation ? null : InvokeFunction(token, arguments, context, line);
                }
                else
                {
                    if (suppressEvaluation) resolvedValue = null;
                    else if (!context.TryGetValue(token, out resolvedValue)) throw Error(line, "Unknown variable: " + token);
                }
                return ParsePostfix(resolvedValue);
            }

            private object ParsePostfix(object value)
            {
                while (true)
                {
                    if (Match("["))
                    {
                        object index = ParseExpression();
                        Require("]");
                        value = suppressEvaluation ? null : GetIndex(value, index, line);
                    }
                    else if (Match("."))
                    {
                        string member = ReadIdentifier();
                        value = suppressEvaluation ? null : GetMember(value, member, line);
                    }
                    else
                    {
                        return value;
                    }
                }
            }

            private object ParseWithoutEvaluation(Func<object> parser)
            {
                bool previous = suppressEvaluation;
                suppressEvaluation = true;
                try { return parser(); }
                finally { suppressEvaluation = previous; }
            }

            private string ReadToken()
            {
                Skip();
                if (position >= text.Length) throw Error(line, "Expected a value.");
                int start = position;
                if (char.IsDigit(text[position]))
                {
                    while (position < text.Length && char.IsDigit(text[position])) position++;
                    if (position < text.Length && text[position] == '.')
                    {
                        position++;
                        while (position < text.Length && char.IsDigit(text[position])) position++;
                    }
                    if (position < text.Length && (text[position] == 'e' || text[position] == 'E'))
                    {
                        position++;
                        if (position < text.Length && (text[position] == '+' || text[position] == '-')) position++;
                        while (position < text.Length && char.IsDigit(text[position])) position++;
                    }
                }
                else
                {
                    return ReadIdentifier();
                }
                return text.Substring(start, position - start);
            }

            private string ReadIdentifier()
            {
                Skip();
                if (position >= text.Length || !(char.IsLetter(text[position]) || text[position] == '_'))
                    throw Error(line, "Expected an identifier.");
                int start = position++;
                while (position < text.Length && (char.IsLetterOrDigit(text[position]) || text[position] == '_')) position++;
                return text.Substring(start, position - start);
            }
            private string ParseString()
            {
                char quote = text[position++]; StringBuilder result = new StringBuilder();
                while (position < text.Length) { char c = text[position++]; if (c == quote) return result.ToString(); if (c == '\\' && position < text.Length) { char e = text[position++]; result.Append(e == 'n' ? '\n' : e == 'r' ? '\r' : e == 't' ? '\t' : e); } else result.Append(c); }
                throw Error(line, "Unterminated string.");
            }
            private void Require(string token) { if (!Match(token)) throw Error(line, "Expected '" + token + "'."); }
            private bool Match(string token) { Skip(); if (text.Length - position < token.Length || string.Compare(text, position, token, 0, token.Length, StringComparison.Ordinal) != 0) return false; position += token.Length; return true; }
            private void Skip() { while (position < text.Length && char.IsWhiteSpace(text[position])) position++; }
        }
    }
}
