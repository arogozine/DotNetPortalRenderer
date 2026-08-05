namespace BuildAssetLoader.Con
{
    /// <summary>
    /// Cursor over a flat <see cref="ConToken"/> stream used while building a <see cref="Command"/> tree.
    /// Statements never span a NewLine-comment marker, so the caller is expected to pre-filter those out.
    /// AI Assisted
    /// </summary>
    internal sealed class ConTreeCursor(IReadOnlyList<ConToken> tokens)
    {
        private int _index;

        public bool AtEnd => _index >= tokens.Count;

        public int Position => _index;

        public bool IsBlockStart => !AtEnd && tokens[_index].ConTokenType == ConTokenType.BlockStart;

        public bool IsBlockEnd => !AtEnd && tokens[_index].ConTokenType == ConTokenType.BlockEnd;

        public bool IsValue => !AtEnd && tokens[_index].ConTokenType == ConTokenType.Value;

        public bool TryPeekCommand(out CommandList command)
        {
            if (!AtEnd && tokens[_index] is CommandToken commandToken)
            {
                command = commandToken.Command;
                return true;
            }

            command = default;
            return false;
        }

        public bool IsCommand(CommandList command) => TryPeekCommand(out CommandList peeked) && peeked == command;

        public CommandList ExpectCommand()
        {
            if (!TryPeekCommand(out CommandList command))
            {
                throw new FormatException($"Expected a command token at index {_index}, got {DescribeCurrent()}.");
            }

            _index++;
            return command;
        }

        public void ExpectCommand(CommandList command)
        {
            CommandList actual = ExpectCommand();

            if (actual != command)
            {
                throw new FormatException($"Expected command '{command}' but found '{actual}' at index {_index - 1}.");
            }
        }

        public void ExpectBlockStart()
        {
            if (!IsBlockStart)
            {
                throw new FormatException($"Expected '{{' at index {_index}, got {DescribeCurrent()}.");
            }

            _index++;
        }

        public void ExpectBlockEnd()
        {
            if (!IsBlockEnd)
            {
                throw new FormatException($"Expected '}}' at index {_index}, got {DescribeCurrent()}.");
            }

            _index++;
        }

        /// <summary>Number of consecutive value tokens starting at the cursor, without advancing it.</summary>
        public int CountContiguousValues()
        {
            int i = _index;
            int count = 0;

            for (; i < tokens.Count && tokens[i].ConTokenType == ConTokenType.Value; i++)
            {
                count++;
            }

            return count;
        }

        public string ReadValue()
        {
            if (!IsValue)
            {
                throw new FormatException($"Expected a value token at index {_index}, got {DescribeCurrent()}.");
            }

            string value = ((ValueToken)tokens[_index]).Value;
            _index++;
            return value;
        }

        public string? TryReadValue() => IsValue ? ReadValue() : null;

        public int ReadInt() => int.Parse(ReadValue());

        public int? TryReadInt()
        {
            string? value = TryReadValue();
            return value is null ? null : int.Parse(value);
        }

        public string[] ReadValues(int count)
        {
            string[] values = new string[count];

            for (int i = 0; i < count; i++)
            {
                values[i] = ReadValue();
            }

            return values;
        }

        public string[] ReadAllContiguousValues() => ReadValues(CountContiguousValues());

        /// <summary>Reads all contiguous values and joins them with a single space (free-text fields like quote/level names).</summary>
        public string ReadJoinedRemainder() => string.Join(' ', ReadAllContiguousValues());

        private string DescribeCurrent() => AtEnd ? "<eof>" : tokens[_index].ToString() ?? tokens[_index].ConTokenType.ToString();
    }
}
