using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;

namespace DVG.SkyPirates.Shared.Core.Sheets
{
	public static class SheetParser
	{
		private const char Split = '/';
		private const string Insertion = "{0}";
		public static JsonArray DsvToJsonObject(string dsv, int headerRows, char separator) =>
			ParseTable(ParseDsv(dsv, separator), headerRows);

		private static JsonArray ParseTable(string[,] table, int headerRows)
		{
			int rowCount = table.GetLength(0);
			int colCount = table.GetLength(1);

			var result = new JsonArray();

			for (int r = headerRows; r < rowCount; r++)
			{
				var obj = new JsonObject();
				bool hasData = false;

				for (int c = 0; c < colCount; c++)
				{
					var value = table[r, c];
					if (value == null)
						continue;

					var path = BuildPath(table, c, headerRows);
					var replace = path.IndexOf(Insertion);
					path.RemoveAt(replace);
					path.InsertRange(replace, value.Split(Split));

					Insert(obj, path);
					hasData = true;
				}

				if (hasData)
					result.Add(obj);
			}

			return result;
		}


		private static string[,] ParseDsv(string csv, char divisor)
		{
			var lines = csv
				.Split('\n')
				.Select(l => l.TrimEnd())
				.Where(l => !string.IsNullOrWhiteSpace(l))
				.ToList();

			int rows = lines.Count;
			int cols = lines.Max(l => l.Split(divisor).Length);

			var table = new string[rows, cols];

			for (int r = 0; r < rows; r++)
			{
				var cells = lines[r].Split(divisor);
				for (int c = 0; c < cells.Length; c++)
					table[r, c] = string.IsNullOrWhiteSpace(cells[c]) ? null : cells[c];
			}

			return table;
		}

		private static List<string> BuildPath(string[,] table, int col, int headerRows)
		{
			var path = new List<string>();

			int startRow = headerRows - 1;
			int startCol = col;

			int c = startCol;
			int r;

			while (c >= 0)
			{
				r = startRow;

				while (r >= 0 && table[r, c] == null)
					r--;

				if (r < 0)
				{
					c--;
				}
				else
				{
					path.AddRange(table[r, c].Split(Split).Reverse());

					startRow = r - 1;

					if (r == 0)
						break;
				}
			}

			path.Reverse();

			if (!path.Contains(Insertion))
				path.Add(Insertion);

			return path;
		}

		private static void Insert(JsonObject root, List<string> path)
		{
			JsonObject current = root;
			for (int i = 0; i < path.Count - 2; i++)
			{
				var key = path[i];
				if (!current.TryGetPropertyValue(key, out var next))
				{
					next = new JsonObject();
					current[key] = next;
				}
				current = next.AsObject();
			}

			current[path[^2]] = ParseValue(path[^1]);
			return;
		}



		private static JsonNode ParseValue(string value)
		{
			if (int.TryParse(value, out var i))
				return JsonValue.Create(i);

			if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var f))
				return JsonValue.Create(f);

			if (bool.TryParse(value, out var b))
				return JsonValue.Create(b);

			return JsonValue.Create(value);
		}
	}
}
