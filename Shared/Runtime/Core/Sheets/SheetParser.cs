using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;

namespace SlyPirates.Shared.Core.Sheets
{
    public static class SheetParser
    {
        private const char Split = '/';

        public static JsonArray DsvToJsonObject(string dsv, int headerRows, char separator) =>
            ParseTable(ParseDsv(dsv, separator), headerRows);

        private static JsonArray ParseTable(string?[,] table, int headerRows)
        {
            var fields = Enumerable.Range(0, table.GetLength(1))
                .Select(column => (Column: column, Path: BuildPath(table, column, headerRows)))
                .Where(field => field.Path.Length != 0).ToArray();
            int[] verticalColumns = fields.Where(field => field.Path.Any(part => part.EndsWith("[]", StringComparison.Ordinal)))
                .Select(field => field.Column).ToArray();
            var result = new JsonArray();
            foreach ((int start, int end) in PreParseRows(table, headerRows, verticalColumns))
            {
                result.Add(ParseObject(table, fields, start, end));
            }
            return result;
        }

        private static IEnumerable<(int Start, int End)> PreParseRows(string?[,] table, int headerRows, int[] verticalColumns)
        {
            int start = headerRows;
            for (int r = headerRows; r < table.GetLength(0); r++)
            {
                if (table[r, 0] == null && verticalColumns.Any(column => table[r, column] != null))
                {
                    if (table[start, 0] == null)
                    {
                        throw new InvalidOperationException($"Row {r + 1}: array continuation has no parent record.");
                    }

                    continue;
                }
                if (r > start)
                {
                    yield return (start, r);
                }

                start = r;
            }
            if (start < table.GetLength(0))
            {
                yield return (start, table.GetLength(0));
            }
        }

        private static JsonObject ParseObject(string?[,] table, (int Column, string[] Path)[] fields, int start, int end)
        {
            var result = new JsonObject();
            foreach (var group in fields.GroupBy(field => field.Path[0]))
            {
                string key = group.Key;
                int marker = key.EndsWith(']') ? key.IndexOf('[') : -1;
                string? separator = marker >= 0 ? key.Substring(marker + 1, key.Length - marker - 2) : null;
                if (marker >= 0)
                {
                    key = key.Substring(0, marker);
                }

                if (key.Length == 0)
                {
                    throw new InvalidOperationException("An array header must have a field name.");
                }

                var children = group.Select(field => (field.Column, Path: field.Path.Skip(1).ToArray())).ToArray();
                bool leaf = children.All(field => field.Path.Length == 0);
                if (!leaf && children.Any(field => field.Path.Length == 0))
                {
                    throw new InvalidOperationException($"Field '{key}' is declared as both a value and an object.");
                }

                if (separator == string.Empty)
                {
                    var array = new JsonArray();
                    if (leaf)
                    {
                        string[] values = ReadValues(table, children, start, end).ToArray();
                        if (values.Length > 1 && values.Contains("[]"))
                        {
                            throw new InvalidOperationException($"Empty-array marker conflicts with items in '{key}'.");
                        }

                        foreach (string value in values.Where(value => value != "[]"))
                        {
                            array.Add(ParseValue(value));
                        }
                    }
                    else
                    {
                        for (int r = start; r < end; r++)
                        {
                            if (ReadValues(table, children, r, r + 1).Any())
                            {
                                array.Add(ParseObject(table, children, r, r + 1));
                            }
                        }
                    }
                    result[key] = array;
                }
                else if (leaf)
                {
                    string? value = ReadValues(table, children, start, end).LastOrDefault();
                    if (separator != null)
                    {
                        result[key] = value == null ? new JsonArray() : ParseArray(value, separator);
                    }
                    else if (value != null)
                    {
                        result[key] = ParseValue(value);
                    }
                }
                else
                {
                    if (separator != null)
                    {
                        throw new InvalidOperationException("An inline-array delimiter belongs on the final field header.");
                    }

                    JsonObject child = ParseObject(table, children, start, end);
                    if (child.Count != 0)
                    {
                        result[key] = child;
                    }
                }
            }
            return result;
        }

        private static IEnumerable<string> ReadValues(string?[,] table, (int Column, string[] Path)[] fields, int start, int end)
        {
            for (int r = start; r < end; r++)
            {
                foreach (var field in fields)
                {
                    if (table[r, field.Column] is string value)
                    {
                        yield return value;
                    }
                }
            }
        }

        private static string?[,] ParseDsv(string dsv, char separator)
        {
            string[][] rows = dsv.Split('\n').Select(line => line.TrimEnd())
                .Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => line.Split(separator)).ToArray();
            string?[,] table = new string?[rows.Length, rows.Max(row => row.Length)];
            for (int r = 0; r < rows.Length; r++)
            for (int c = 0; c < rows[r].Length; c++)
            {
                table[r, c] = string.IsNullOrWhiteSpace(rows[r][c]) ? null : rows[r][c];
            }

            return table;
        }

        private static string[] BuildPath(string?[,] table, int column, int headerRows)
        {
            var path = new List<string>();
            int row = headerRows - 1;
            while (column >= 0 && row >= 0)
            {
                int header = row;
                while (header >= 0 && table[header, column] == null)
                {
                    header--;
                }

                if (header < 0)
                {
                    column--;
                    continue;
                }
                path.InsertRange(0, table[header, column]!.Split(Split));
                row = header - 1;
            }
            return path.ToArray();
        }

        private static JsonArray ParseArray(string value, string separator) => value == "[]"
            ? new JsonArray()
            : new JsonArray(value.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries).Select(ParseValue).ToArray());

        private static JsonNode ParseValue(string value)
        {
            if (int.TryParse(value, out int integer))
            {
                return JsonValue.Create(integer);
            }

            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal number))
            {
                return JsonValue.Create(number);
            }

            if (bool.TryParse(value, out bool boolean))
            {
                return JsonValue.Create(boolean);
            }

            return JsonValue.Create(value);
        }
    }
}
