using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
#if NET8_0 || NET6_0 || NET7_0 || NETCOREAPP
using System.Text.Json;
#endif
namespace TextFSM
{
    // Base exception classes
    public class TextFSMError : Exception
    {
        public TextFSMError(string message) : base(message) { }
    }

    public class TextFSMTemplateError : TextFSMError
    {
        public TextFSMTemplateError(string message) : base(message) { }
    }

    // FSM Action exceptions
    public class FSMAction : Exception
    {
        public FSMAction(string message) : base(message) { }
    }

    public class SkipRecord : FSMAction
    {
        public SkipRecord(string message) : base(message) { }
    }

    public class SkipValue : FSMAction
    {
        public SkipValue(string message) : base(message) { }
    }

    // Value options implementation
    public static class TextFSMOptions
    {
        public static string[] ValidOptions() => new[] { "Required", "Filldown", "Fillup", "Key", "List" };

public static Func<TextFSMValue, IValueOption>? GetOption(string name) => name switch
{
    "Required" => value => new Required(value),
    "Filldown" => value => new Filldown(value),
    "Fillup" => value => new Fillup(value),
    "Key" => value => new Key(value),
    "List" => value => new List(value),
    _ => null
};
        public interface IValueOption
        {
            string Name { get; }
            TextFSMValue Value { get; }
            void OnCreateOptions();
            void OnClearVar();
            void OnClearAllVar();
            void OnAssignVar();
            void OnGetValue();
            void OnSaveRecord();
        }

        public class Required : IValueOption
        {
            public string Name => "Required";
            public TextFSMValue Value { get; }

            public Required(TextFSMValue value)
            {
                Value = value;
            }

            public void OnCreateOptions() { }
            public void OnClearVar() { }
            public void OnClearAllVar() { }
            public void OnAssignVar() { }
            public void OnGetValue() { }

            public void OnSaveRecord()
            {
                // For List values, check if the list is empty
                if (Value.Value is IList<object> list && list.Count == 0)
                {
                    throw new SkipRecord($"Required value '{Value.Name}' has no entries");
                }
                // For scalar values, check if the value is null or empty
                else if (Value.Value == null || Value.Value.ToString() == "")
                {
                    throw new SkipRecord($"Required value '{Value.Name}' is empty");
                }
            }
        }

        public class Filldown : IValueOption
        {
            public string Name => "Filldown";
            public TextFSMValue Value { get; }
            private object? _myvar; // Mark as nullable with '?'

            public Filldown(TextFSMValue value)
            {
                Value = value;
                _myvar = null;
            }

            public void OnCreateOptions() { }

            public void OnAssignVar()
            {
                _myvar = Value.Value;
            }

            public void OnClearVar()
            {
                Value.Value = _myvar;
            }

            public void OnClearAllVar()
            {
                _myvar = null;
            }

            public void OnGetValue() { }
            public void OnSaveRecord() { }
        }

        public class Fillup : IValueOption
        {
            public string Name => "Fillup";
            public TextFSMValue Value { get; }

            public Fillup(TextFSMValue value)
            {
                Value = value;
            }

            public void OnCreateOptions() { }
            public void OnClearVar() { }
            public void OnClearAllVar() { }
            public void OnGetValue() { }
            public void OnSaveRecord() { }

            public void OnAssignVar()
            {
                 // If value is set, copy up the results table, until we see a set item
    if (Value.Value != null && Value.Fsm != null) // Add null check for Fsm
    {
        // Get index of relevant result column
        int valueIdx = Value.Fsm.Values.IndexOf(Value);

        // Go up the list from the end until we see a filled value
        var results = Value.Fsm._result;
        for (int i = results.Count - 1; i >= 0; i--)
        {
            if (results[i][valueIdx] != null && results[i][valueIdx].ToString() != "")
            {
                // Stop when a record has this column already
                break;
            }
            // Otherwise set the column value
            results[i][valueIdx] = Value.Value;
        }
    }
            }
        }

        public class Key : IValueOption
        {
            public string Name => "Key";
            public TextFSMValue Value { get; }

            public Key(TextFSMValue value)
            {
                Value = value;
            }

            public void OnCreateOptions() { }
            public void OnClearVar() { }
            public void OnClearAllVar() { }
            public void OnAssignVar() { }
            public void OnGetValue() { }

            public void OnSaveRecord()
            {
                
    // Skip if the value is empty
    if (Value.Value == null || Value.Value.ToString() == "")
    {
        return;
    }

    // Skip if Fsm is null
    if (Value.Fsm == null)
    {
        return;
    }

    // Get all values with Key option to form a composite key
    var keyValues = Value.Fsm.Values
        .Where(v => v.Options.Any(opt => opt.Name == "Key"))
        .Select(v => v.Value)
        .ToList();

    // Create a string key - in C# we could use JSON serialization or another method
    var keyString = string.Join("|", keyValues.Select(v => v?.ToString() ?? ""));

    // Check if this key has been seen before
    if (Value.Fsm._seenKeys.Contains(keyString))
    {
        throw new SkipRecord($"Duplicate key: {keyString}");
    }

    // Add the key to the seen keys set
    Value.Fsm._seenKeys.Add(keyString);
            }
        }

        public class List : IValueOption
        {
            public string Name => "List";
            public TextFSMValue Value { get; }
            private IList<object> _value;

            public List(TextFSMValue value)
            {
                Value = value;
                _value = new List<object>();
            }

            public void OnCreateOptions()
            {
                OnClearAllVar();
            }

            public void OnAssignVar()
            {
                // Handle nested matches with groups
                Match? match = null;
                if (Value.CompiledRegex != null && Value.CompiledRegex.ToString().Contains("(?<"))
                {
                    match = Value.CompiledRegex.Match(Value.Value?.ToString() ?? "");
                }

                // If the List-value regex has match-groups defined, add the group values
                if (match != null && match.Success && match.Groups.Count > 1)
                {
                    // Get group names and exclude the default group
var groupNames = new List<string>();
for (int i = 0; i < match.Groups.Count; i++)
{
    string name = Value.CompiledRegex.GroupNameFromNumber(i);
    if (!int.TryParse(name, out _) && name != Value.Name)
    {
        groupNames.Add(name);
    }
}

                    if (groupNames.Count > 0)
                    {
                        // Create dictionary of captured groups
                        var groups = new Dictionary<string, string>();
                        foreach (var name in groupNames)
                        {
                            groups[name] = match.Groups[name].Value;
                        }
                        _value.Add(groups);
                    }
                    else
                    {
                        _value.Add(Value.Value!); // Use null-forgiving operator since we checked above
                    }
                }
                else
                {
                    _value.Add(Value.Value!); // Use null-forgiving operator
                }
            }

            public void OnClearVar()
            {
                // Check if Filldown is present in options
                bool hasFilldown = Value.Options.Any(option => option.Name == "Filldown");
                if (!hasFilldown)
                {
                    _value.Clear();
                }
                // When Filldown is present, keep the current values
            }

            public void OnClearAllVar()
            {
                _value.Clear();
            }

            public void OnGetValue() { }

            public void OnSaveRecord()
            {
                // Create a copy of the list
                Value.Value = _value.ToList();
            }
        }
    }

    public class TextFSMValue
    {
        public int MaxNameLen { get; }
        public string Name { get; private set; } = string.Empty; // Initialize with empty string
        public List<TextFSMOptions.IValueOption> Options { get; }
        public string Regex { get; private set; } = string.Empty; // Initialize with empty string
        public object? Value { get; set; } // Mark as nullable
        public TextFSM? Fsm { get; } // Mark as nullable
        public Regex? CompiledRegex { get; private set; } // Mark as nullable
        public string Template { get; private set; } = string.Empty; // Initialize with empty string

        public TextFSMValue(TextFSM? fsm = null, int maxNameLen = 48)
        {
            MaxNameLen = maxNameLen;
            Options = new List<TextFSMOptions.IValueOption>();
            Fsm = fsm;
        }

        public void AssignVar(object value)
        {
            Value = value;
            foreach (var option in Options)
            {
                option.OnAssignVar();
            }
        }

        public void ClearVar()
        {
            Value = null;
            foreach (var option in Options)
            {
                option.OnClearVar();
            }
        }

        public void ClearAllVar()
        {
            Value = null;
            foreach (var option in Options)
            {
                option.OnClearAllVar();
            }
        }

        public string Header()
        {
            foreach (var option in Options)
            {
                option.OnGetValue();
            }
            return Name;
        }

        public List<string> OptionNames()
        {
            return Options.Select(option => option.Name).ToList();
        }

        public void Parse(string value)
        {
            
    string[] valueLine = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    if (valueLine.Length < 3)
    {
        throw new TextFSMTemplateError("Expect at least 3 tokens on line.");
    }

    if (!valueLine[2].StartsWith("("))
    {
        // Options are present
        string options = valueLine[1];
        foreach (var option in options.Split(','))
        {
            _AddOption(option);
        }

        // Call option OnCreateOptions callbacks
        foreach (var option in Options)
        {
            option.OnCreateOptions();
        }

        Name = valueLine[2];
        Regex = string.Join(" ", valueLine.Skip(3));
    }
    else
    {
        // No options, treat argument as name
        Name = valueLine[1];
        Regex = string.Join(" ", valueLine.Skip(2));
    }

    if (Name.Length > MaxNameLen)
    {
        throw new TextFSMTemplateError($"Invalid Value name '{Name}' or name too long.");
    }

    if (!Regex.StartsWith("(") || !Regex.EndsWith(")") || Regex[Regex.Length - 2] == '\\')
    {
        throw new TextFSMTemplateError($"Value '{Regex}' must be contained within a '()' pair.");
    }

    try
    {
        // Modify the regex to use named capture groups - this is the key fix
        Template = System.Text.RegularExpressions.Regex.Replace(Regex, @"^\(", $"(?<{Name}>"); 
        CompiledRegex = new Regex(Regex, RegexOptions.Compiled);
    }
    catch (Exception e)
    {
        throw new TextFSMTemplateError(e.Message);
    }
        }

        private void _AddOption(string name)
        {
            // Check for duplicate option declaration
            if (Options.Any(option => option.Name == name))
            {
                throw new TextFSMTemplateError($"Duplicate option \"{name}\"");
            }

            // Create option object
            var optionFactory = TextFSMOptions.GetOption(name);
            if (optionFactory == null)
            {
                throw new TextFSMTemplateError($"Unknown option \"{name}\"");
            }

            var option = optionFactory(this);
            Options.Add(option);
        }

        public void OnSaveRecord()
        {
            foreach (var option in Options)
            {
                option.OnSaveRecord();
            }
        }

        public override string ToString()
        {
            if (Options.Count > 0)
            {
                return $"Value {string.Join(",", OptionNames())} {Name} {Regex}";
            }
            else
            {
                return $"Value {Name} {Regex}";
            }
        }
    }

    public class TextFSMRule
    {
        // Constants for pattern matching
        public static readonly string[] LINE_OP = { "Continue", "Next", "Error" };
        public static readonly string[] RECORD_OP = { "Clear", "Clearall", "Record", "NoRecord" };

        // Regex patterns for parsing rule actions
        private static readonly string LINE_OP_RE = $"(?<ln_op>{string.Join("|", LINE_OP)})";
        private static readonly string RECORD_OP_RE = $"(?<rec_op>{string.Join("|", RECORD_OP)})";
        private static readonly string OPERATOR_RE = $"({LINE_OP_RE}(\\.{RECORD_OP_RE})?)";
        private static readonly string NEWSTATE_RE = @"(?<new_state>\w+|"".*"")";

        // Compiled regexes for action parsing
        private static readonly Regex MATCH_ACTION = new Regex(@"(?<match>.*?)(\s->(?<action>.*))", RegexOptions.Compiled);
        private static readonly Regex ACTION_RE = new Regex($"\\s+{OPERATOR_RE}(\\s+{NEWSTATE_RE})?$", RegexOptions.Compiled);
        private static readonly Regex ACTION2_RE = new Regex($"\\s+{RECORD_OP_RE}(\\s+{NEWSTATE_RE})?$", RegexOptions.Compiled);
        private static readonly Regex ACTION3_RE = new Regex($"(\\s+{NEWSTATE_RE})?$", RegexOptions.Compiled);

        public string Match { get; }
        public string Regex { get; }
        public Regex RegexObj { get; }
        public string LineOp { get; }
        public string RecordOp { get; }
        public string NewState { get; }
        public int LineNum { get; }
        public bool Multiline { get; }

        public TextFSMRule(string line, int lineNum = -1, Dictionary<string, string>? varMap = null)
        {
            Match = "";
            Regex = "";
            RegexObj = null!; // Will be initialized below
            LineOp = "";  // Equivalent to 'Next'
            RecordOp = "";  // Equivalent to 'NoRecord'
            NewState = "";  // Equivalent to current state
            LineNum = lineNum;
            Multiline = false;

            // Don't trim line - preserve whitespace
            string trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine))
            {
                throw new TextFSMTemplateError($"Null data in FSMRule. Line: {LineNum}");
            }

            // Check for -> action
            var matchAction = MATCH_ACTION.Match(trimmedLine);
            if (matchAction.Success)
            {
                Match = matchAction.Groups["match"].Value;
            }
            else
            {
                Match = trimmedLine;
            }

            // Replace ${varname} entries (template substitution)
            Regex = Match;
            if (varMap != null)
            {
                try
                {
                    // C# version of template substitution
                    Regex = System.Text.RegularExpressions.Regex.Replace(
                        Match,
                        @"\${(\w+)}",
                        m =>
                        {
                            var name = m.Groups[1].Value;
                            if (!varMap.ContainsKey(name))
                            {
                                throw new TextFSMTemplateError(
                                    $"Invalid variable substitution: '{name}'. Line: {LineNum}"
                                );
                            }
                            return varMap[name];
                        }
                    );
                }
                catch (Exception ex) // Renamed to 'ex' to avoid unused variable warning
                {
                    throw new TextFSMTemplateError(
                        $"Error in template substitution. Line: {LineNum}. {ex.Message}"
                    );
                }
            }

            // Check if this is a multi-line pattern
            Multiline = Regex.Contains("\\n");

            try
            {
                // Create regex with appropriate options
                RegexOptions options = RegexOptions.Compiled;
                if (Multiline)
                {
                    options |= RegexOptions.Singleline;  // Equivalent to JavaScript 's' flag
                }
                RegexObj = new Regex(Regex, options);
            }
            catch (Exception e)
            {
                throw new TextFSMTemplateError($"Invalid regular expression: '{Regex}'. Line: {LineNum}");
            }

            // No -> present, so we're done
            if (!matchAction.Success)
            {
                return;
            }

            // Process action part
            string action = matchAction.Groups["action"].Value;
            Match actionRe = ACTION_RE.Match(action);
            if (!actionRe.Success)
            {
                actionRe = ACTION2_RE.Match(action);
                if (!actionRe.Success)
                {
                    actionRe = ACTION3_RE.Match(action);
                    if (!actionRe.Success)
                    {
                        throw new TextFSMTemplateError($"Badly formatted rule '{trimmedLine}'. Line: {LineNum}");
                    }
                }
            }

            // Process line operator
            if (actionRe.Groups["ln_op"].Success)
            {
                LineOp = actionRe.Groups["ln_op"].Value;
            }

            // Process record operator
            if (actionRe.Groups["rec_op"].Success)
            {
                RecordOp = actionRe.Groups["rec_op"].Value;
            }

            // Process new state
            if (actionRe.Groups["new_state"].Success)
            {
                NewState = actionRe.Groups["new_state"].Value;
            }

            // Validate: only 'Next' line operator can have a new_state
            if (LineOp == "Continue" && !string.IsNullOrEmpty(NewState))
            {
                throw new TextFSMTemplateError($"Action '{LineOp}' with new state {NewState} specified. Line: {LineNum}");
            }

            // Validate state name
            if (LineOp != "Error" && !string.IsNullOrEmpty(NewState))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(NewState, @"^\w+$"))
                {
                    throw new TextFSMTemplateError($"Alphanumeric characters only in state names. Line: {LineNum}");
                }
            }
        }

        public override string ToString()
        {
            string operation = "";
            if (!string.IsNullOrEmpty(LineOp) && !string.IsNullOrEmpty(RecordOp))
            {
                operation = ".";
            }
            operation = $"{LineOp}{operation}{RecordOp}";

            string newState = !string.IsNullOrEmpty(operation) && !string.IsNullOrEmpty(NewState) 
                ? $" {NewState}" 
                : NewState;

            // Print with implicit defaults
            if (string.IsNullOrEmpty(operation) && string.IsNullOrEmpty(newState))
            {
                return $"  {Match}";
            }

            // Non defaults
            return $"  {Match} -> {operation}{newState}";
        }
    }

    public class TextFSM
    {
        public const int MAX_NAME_LEN = 48;

        public Dictionary<string, List<TextFSMRule>> States { get; private set; }
        public List<string> StateList { get; private set; }
        public List<TextFSMValue> Values { get; private set; }
        public Dictionary<string, string> ValueMap { get; private set; }

        private int _lineNum;
        private List<TextFSMRule>? _curState; // Mark as nullable
        private string? _curStateName; // Mark as nullable
        public List<List<object>> _result = new List<List<object>>(); // Initialize in declaration
        private string _lineBuffer = string.Empty; // Initialize with empty string
        public HashSet<string> _seenKeys;

        public TextFSM(string template)
        {
            States = new Dictionary<string, List<TextFSMRule>>();
            StateList = new List<string>();
            Values = new List<TextFSMValue>();
            ValueMap = new Dictionary<string, string>();
            _lineNum = 0;
            _seenKeys = new HashSet<string>();

            // Parse the template
            _Parse(template);

            // Initialize starting data
            Reset();
        }

        public void Reset()
        {
            // Set current state to Start
            _curState = States["Start"];
            _curStateName = "Start";

            // Clear results and current record
            _result = new List<List<object>>();
            _seenKeys.Clear();

            _ClearAllRecord();
        }

        public List<string> Header
        {
            get { return _GetHeader(); }
        }

        private List<string> _GetHeader()
        {
            var header = new List<string>();
            foreach (var value in Values)
            {
                try
                {
                    var headerValue = value.Header();
                    if (headerValue != null)
                    {
                        header.Add(headerValue);
                    }
                }
                catch (SkipValue)
                {
                    // Skip this value
                }
            }
            return header;
        }

        private TextFSMValue? _GetValue(string name) // Mark return as nullable
        {
            return Values.FirstOrDefault(value => value.Name == name);
        }

        private void _AppendRecord()
        {
            // If no values then don't output
            if (Values.Count == 0)
            {
                return;
            }

            var curRecord = new List<object>();
            try
            {
                foreach (var value in Values)
                {
                    try
                    {
                        value.OnSaveRecord();
                    }
                    catch (SkipRecord)
                    {
                        _ClearRecord();
                        return;
                    }
                    catch (SkipValue)
                    {
                        continue;
                    }

                    // Build current record
                    curRecord.Add(value.Value!); // Use null-forgiving operator
                }
            }
            catch (SkipRecord)
            {
                _ClearRecord();
                return;
            }

            // If no values in template or whole record is empty, don't output
            if (curRecord.Count == 0 || curRecord.All(val => val == null || 
                (val is IList<object> list && list.Count == 0)))
            {
                return;
            }

            // Replace null entries with empty string
            for (int i = 0; i < curRecord.Count; i++)
            {
                if (curRecord[i] == null)
                {
                    curRecord[i] = "";
                }
            }

            _result.Add(curRecord);
            _ClearRecord();
        }

        private bool _ValidateConsistency()
        {
            // Check for undefined value references in rules
            foreach (var stateName in States.Keys)
            {
                foreach (var rule in States[stateName])
                {
                    var pattern = new Regex(@"\${(\w+)}");
                    var matches = pattern.Matches(rule.Match);
                    foreach (Match match in matches)
                    {
                        string valueRef = match.Groups[1].Value;
                        if (!ValueMap.ContainsKey(valueRef))
                        {
                            throw new TextFSMTemplateError(
                                $"Rule in state '{stateName}' references undefined value '{valueRef}'"
                            );
                        }
                    }
                }
            }

            // Validate regex patterns in values are valid
            foreach (var value in Values)
            {
                try
                {
                    if (value.CompiledRegex == null)
                    {
                        throw new Exception($"Value '{value.Name}' has no compiled regex");
                    }
                    // Test the regex with a simple string to verify it compiles
                    value.CompiledRegex.IsMatch("");
                }
                catch (Exception e)
                {
                    throw new TextFSMTemplateError(
                        $"Invalid regex in value '{value.Name}': {e.Message}"
                    );
                }
            }

            // Check for unreachable states
            var reachableStates = new HashSet<string> { "Start" };
            bool statesAdded = true;

            // Keep adding states until no new states are found
            while (statesAdded)
            {
                statesAdded = false;
                foreach (var stateName in reachableStates.ToList())
                {
                    if (!States.ContainsKey(stateName)) continue;
                    foreach (var rule in States[stateName])
                    {
                        if (!string.IsNullOrEmpty(rule.NewState) &&
                            rule.NewState != "End" &&
                            rule.NewState != "EOF" &&
                            !reachableStates.Contains(rule.NewState))
                        {
                            reachableStates.Add(rule.NewState);
                            statesAdded = true;
                        }
                    }
                }
            }

            // Find unreachable states
            var unreachableStates = StateList
                .Where(state => state != "End" && state != "EOF" && !reachableStates.Contains(state))
                .ToList();

            if (unreachableStates.Count > 0)
            {
                throw new TextFSMTemplateError(
                    $"Unreachable states found: {string.Join(", ", unreachableStates)}"
                );
            }

            // Validate option combinations
            foreach (var value in Values)
            {
                var options = value.OptionNames();
                // Additional checks beyond what's in _ValidateOptions
                if (options.Contains("Required") && options.Contains("Filldown"))
                {
                    throw new TextFSMTemplateError(
                        $"Value '{value.Name}' has both 'Required' and 'Filldown' options, which may cause unexpected behavior"
                    );
                }
            }

            return true;
        }

        private void _Parse(string template)
        {
            if (string.IsNullOrEmpty(template))
            {
                throw new TextFSMTemplateError("Null template.");
            }

            // Split template into lines, handling different line endings
            // var lines = template.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            var lines = template.Replace("\r\n", "\n").Replace("\r", "\n").Split(new[] { '\n' });
            // Parse Variables section
            int lineIndex = _ParseVariables(lines);

            // Parse States
            while (lineIndex < lines.Length)
            {
                lineIndex = _ParseState(lines, lineIndex);
            }

            // Validate FSM
            _ValidateFSM();

            // Perform additional validations
            _ValidateConsistency();
        }

        private int _ParseVariables(string[] lines)
        {
            Values.Clear();
            int lineIndex = 0;

            for (; lineIndex < lines.Length; lineIndex++)
            {
                _lineNum = lineIndex + 1;
                string line = lines[lineIndex].Trim();

                // Blank line signifies end of Value definitions
                if (string.IsNullOrEmpty(line))
                {
                    return lineIndex + 1;
                }

                // Skip commented lines
                if (line.StartsWith("#"))
                {
                    continue;
                }

                if (line.StartsWith("Value "))
                {
                    try
                    {
                        var value = new TextFSMValue(this, MAX_NAME_LEN);
                        value.Parse(line);

                        if (Header.Contains(value.Name))
                        {
                            throw new TextFSMTemplateError(
                                $"Duplicate declarations for Value '{value.Name}'. Line: {_lineNum}"
                            );
                        }

                        _ValidateOptions(value);
                        Values.Add(value);
                        ValueMap[value.Name] = value.Template;
                    }
                    catch (TextFSMTemplateError e)
                    {
                        throw new TextFSMTemplateError($"{e.Message} Line {_lineNum}.");
                    }
                }
                else if (Values.Count == 0)
                {
                    throw new TextFSMTemplateError("No Value definitions found.");
                }
                else
                {
                    throw new TextFSMTemplateError(
                        $"Expected blank line after last Value entry. Line: {_lineNum}."
                    );
                }
            }

            return lineIndex;
        }

        private void _ValidateOptions(TextFSMValue value)
        {
            // Check for incompatible options
            var options = value.OptionNames();

            // Cannot have both Key and List
            if (options.Contains("Key") && options.Contains("List"))
            {
                throw new TextFSMTemplateError($"Value cannot have both 'Key' and 'List' options: '{value.Name}'");
            }

            // Cannot have both Filldown and Fillup
            if (options.Contains("Filldown") && options.Contains("Fillup"))
            {
                throw new TextFSMTemplateError($"Value cannot have both 'Filldown' and 'Fillup' options: '{value.Name}'");
            }

            // Additional validation can be added here
        }

private int _ParseState(string[] lines, int startIndex)
        {
            int lineIndex = startIndex;
            string stateName = "";

            // Find state definition
            for (; lineIndex < lines.Length; lineIndex++)
            {
                _lineNum = lineIndex + 1;
                string line = lines[lineIndex];
                string trimmedLine = line.Trim();

                // Skip blank lines and comments
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                {
                    continue;
                }

                // First non-blank, non-comment line is state definition
                var stateNameRe = new Regex(@"^(\w+)$");
                if (!stateNameRe.IsMatch(trimmedLine) ||
                    trimmedLine.Length > MAX_NAME_LEN ||
                    TextFSMRule.LINE_OP.Contains(trimmedLine) ||
                    TextFSMRule.RECORD_OP.Contains(trimmedLine))
                {
                    throw new TextFSMTemplateError(
                        $"Invalid state name: '{trimmedLine}'. Line: {_lineNum}"
                    );
                }

                stateName = trimmedLine;
                if (States.ContainsKey(stateName))
                {
                    throw new TextFSMTemplateError(
                        $"Duplicate state name: '{trimmedLine}'. Line: {_lineNum}"
                    );
                }

                States[stateName] = new List<TextFSMRule>();
                StateList.Add(stateName);
                lineIndex++;
                break;
            }

            if (string.IsNullOrEmpty(stateName))
            {
                return lines.Length; // End of file
            }

            // Parse rules in this state
            for (; lineIndex < lines.Length; lineIndex++)
            {
                _lineNum = lineIndex + 1;
                // Use original line here to preserve whitespace
                string line = lines[lineIndex];
                string trimmedLine = line.Trim();

                // Blank line ends the state
                if (string.IsNullOrEmpty(trimmedLine))
                {
                    return lineIndex + 1;
                }

                // Skip comments
                if (trimmedLine.StartsWith("#"))
                {
                    continue;
                }

                // Check rule format
                bool hasValidPrefix = new[] { " ^", "  ^", "\t^" }.Any(prefix => line.StartsWith(prefix));
                if (!hasValidPrefix)
                {
                    throw new TextFSMTemplateError(
                        $"Missing white space or carat ('^') before rule. Line: {_lineNum}. Content: \"{line}\""
                    );
                }

                // Add rule to state
                States[stateName].Add(
                    new TextFSMRule(line, _lineNum, ValueMap)
                );
            }

            return lines.Length; // End of file
        }

        private bool _ValidateFSM()
        {
            // Must have 'Start' state
            if (!States.ContainsKey("Start"))
            {
                throw new TextFSMTemplateError("Missing state 'Start'.");
            }

            // 'End' state (if specified) must be empty
            if (States.ContainsKey("End") && States["End"].Count > 0)
            {
                throw new TextFSMTemplateError("Non-Empty 'End' state.");
            }

            // Remove 'End' state
            if (States.ContainsKey("End"))
            {
                States.Remove("End");
                StateList.Remove("End");
            }

            // Ensure jump states are all valid
            foreach (var state in States.Keys)
            {
                foreach (var rule in States[state])
                {
                    if (rule.LineOp == "Error")
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(rule.NewState) || rule.NewState == "End" || rule.NewState == "EOF")
                    {
                        continue;
                    }

                    if (!States.ContainsKey(rule.NewState))
                    {
                        throw new TextFSMTemplateError(
                            $"State '{rule.NewState}' not found, referenced in state '{state}'"
                        );
                    }
                }
            }

            return true;
        }

        private void _ClearRecord()
        {
            // Remove non-Filldown record entries
            foreach (var value in Values)
            {
                value.ClearVar();
            }
        }

        private void _ClearAllRecord()
        {
            // Remove all record entries
            foreach (var value in Values)
            {
                value.ClearAllVar();
            }
        }

        public List<List<object>> ParseText(string text, bool eof = true)
        {
            if (string.IsNullOrEmpty(text))
            {
                return _result;
            }

            // Split text into lines, handling different line endings
            string[] lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

            // Process each line
            foreach (string line in lines)
            {
                _ProcessLine(line);
                if (_curStateName == "End")
                {
                    break;
                }
            }

            // Handle EOF state if it exists
            if (_curStateName != "End" && eof)
            {
                if (States.ContainsKey("EOF"))
                {
                    // Process rules in the EOF state
                    _curState = States["EOF"];
                    _curStateName = "EOF";
                    _ProcessLine(""); // Process with empty line to trigger EOF rules
                }
                else
                {
                    // No EOF state defined, just append the current record
                    _AppendRecord();
                }
            }

            return _result;
        }

        private void _ProcessLine(string line)
        {
            // Pre-process the line before checking rules
            string trimmedLine = _PreprocessLine(line);
            _CheckLine(trimmedLine);
        }

        private string _PreprocessLine(string line)
        {
            // Remove trailing whitespace
            return line.TrimEnd();
        }

        private void _CheckLine(string line)
        {
    // Ensure _curState is not null
    if (_curState == null)
    {
        throw new InvalidOperationException("Current state is null in _CheckLine");
    }

    foreach (var rule in _curState)
    {
        Match? matched = _CheckRule(rule, line);
        if (matched != null && matched.Success)
        {
            // Process captured groups
           string[] groupNames = rule.RegexObj.GetGroupNames();
foreach (string groupName in groupNames)
{
    // Skip numeric groups (these are the overall match and positional captures)
    if (!int.TryParse(groupName, out _)) 
    {
        // Only process named groups
        _AssignVar(matched, groupName);
    }
}

            if (_Operations(rule, line))
            {
                // Not a Continue, so check for state transition
                if (!string.IsNullOrEmpty(rule.NewState))
                {
                    if (rule.NewState != "End" && rule.NewState != "EOF")
                    {
                        _curState = States[rule.NewState];
                    }
                    _curStateName = rule.NewState;
                }
                break;
            }
        }
        }
       }
        

        private Match? _CheckRule(TextFSMRule rule, string line)
        {
            // This is a separate method so it can be overridden for debugging
            return rule.RegexObj.Match(line);
        }

        private void _AssignVar(Match matched, string value)
        
{
    TextFSMValue? fsmValue = _GetValue(value);
    if (fsmValue != null)
    {
        // If we have a matched group, use it
        if (matched.Groups[value].Success)
        {
            fsmValue.AssignVar(matched.Groups[value].Value);
        }
    }
}

        private bool _Operations(TextFSMRule rule, string line)
        {
            // Process record operators
            if (rule.RecordOp == "Record")
            {
                _AppendRecord();
            }
            else if (rule.RecordOp == "Clear")
            {
                _ClearRecord();
            }
            else if (rule.RecordOp == "Clearall")
            {
                _ClearAllRecord();
            }

            // Process line operators
            if (rule.LineOp == "Error")
            {
                if (!string.IsNullOrEmpty(rule.NewState))
                {
                    throw new TextFSMError(
                        $"Error: {rule.NewState}. Rule Line: {rule.LineNum}. Input Line: {line}."
                    );
                }
                throw new TextFSMError(
                    $"State Error raised. Rule Line: {rule.LineNum}. Input Line: {line}."
                );
            }
            else if (rule.LineOp == "Continue")
            {
                // Continue with current line
                return false;
            }

            // Return to start of current state with new line
            return true;
        }

        // Additional methods for processing results
        public List<Dictionary<string, object>> ParseTextToDicts(string text, bool eof = true)
        {
            var resultLists = ParseText(text, eof);
            var resultDicts = new List<Dictionary<string, object>>();

            foreach (var row in resultLists)
            {
                var dict = new Dictionary<string, object>();
                for (int i = 0; i < Header.Count; i++)
                {
                    // Use the header value as the property name
                    dict[Header[i]] = row[i];

                    // If the value is a List and contains objects with named properties,
                    // preserve those object structures
                    if (row[i] is IList<object> list && list.Count > 0 && list[0] is Dictionary<string, object>)
                    {
                        // Keep the object structure for each item in the list
                        dict[Header[i]] = list;
                    }
                }
                resultDicts.Add(dict);
            }
            return resultDicts;
        }

        // Method to parse text and return objects with named properties
        public List<Dictionary<string, object>> ParseTextToNamedGroups(string text, bool eof = true)
        {
            // First parse the text regularly
            ParseText(text, eof);

            // Then convert the result to a list of dictionaries with named properties
            var result = new List<Dictionary<string, object>>();
            foreach (var row in _result)
            {
                var obj = new Dictionary<string, object>();
                for (int i = 0; i < Values.Count && i < row.Count; i++)
                {
                    string valueName = Values[i].Name;
                    obj[valueName] = row[i];
                }
                result.Add(obj);
            }
            return result;
        }

        // Method to get values with specific attribute
        public List<string> GetValuesByAttrib(string attribute)
        {
            if (!TextFSMOptions.ValidOptions().Contains(attribute))
            {
                throw new ArgumentException($"'{attribute}': Not a valid attribute.");
            }

            return Values
                .Where(value => value.OptionNames().Contains(attribute))
                .Select(value => value.Name)
                .ToList();
        }

        // ToString() implementation for TextFSM
        public override string ToString()
        {
            var result = string.Join("\n", Values.Select(value => value.ToString()));
            result += "\n";

            foreach (var state in StateList)
            {
                result += $"\n{state}\n";
                if (States[state].Count > 0)
                {
                    result += string.Join("\n", States[state].Select(rule => rule.ToString())) + "\n";
                }
            }

            return result;
        }
    }
}