using System;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace SkyPirates.Shared.Core.Sheets
{
    public class SheetLoader
    {
        private const string TsvFormat = "https://docs.google.com/spreadsheets/d/{0}/export?format=tsv&gid={1}";
        private const string CsvFormat = "https://docs.google.com/spreadsheets/d/{0}/export?format=csv&gid={1}";
        private readonly HttpClient _client = new();
        private readonly string _tableId;

        public SheetLoader(string tableId)
        {
            _tableId = tableId;
        }

        public async Task<JsonArray> LoadAsDsv(Sheet sheet, char separator)
        {
            var url = string.Format(separator == '\t' ? TsvFormat : CsvFormat, _tableId, sheet.Id);
            try
            {
                using var response = await _client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return SheetParser.DsvToJsonObject(content, sheet.HeaderRows, separator);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"{sheet.Name}: {exception.Message}", exception);
            }
        }
    }
}
