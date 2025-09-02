using System.Globalization;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace Finance.Tracking;

public static class CsvPriceLoader
{
    public static List<Price> LoadPricesFromCsv(string filePath)
    {
        var prices = new List<Price>();
        string path = PathHelper.GetPath(filePath);

        var lines = File.ReadAllLines(path, Encoding.UTF8)
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToArray();

        if (lines.Length < 2)
            throw new InvalidOperationException("CSV must contain header + data rows.");

        // Header symbols (skip Date column)
        var headers = lines[0].Split(',').Skip(1).Select(h => h.Trim('"')).ToList();

        // Map headers to Symbol enum (ignore if not defined)
        var symbolMap = headers.Select(h =>
        {
            var normalized = h.Replace(".", "_").Replace(" ", "_");
            return Enum.TryParse<Symbol>(normalized, ignoreCase: true, out var sym)
                ? sym
                : (Symbol?)null;
        }).ToList();

        // Parse rows
        for (int i = 1; i < lines.Length; i++)
        {
            var cols = lines[i].Split(',');

            var date = DateOnly.ParseExact(cols[0].Trim('"'), "yyyy/MM/dd", CultureInfo.InvariantCulture);

            for (int j = 1; j < cols.Length; j++)
            {
                var sym = symbolMap[j - 1];
                if (sym == null) continue; // skip unmapped columns

                if (double.TryParse(cols[j], NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                {
                    prices.Add(new Price
                    {
                        Symbol = sym.Value,
                        Value = value,
                        Date = date
                    });
                }
            }
        }

        return prices;
    }
}
