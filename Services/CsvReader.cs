using System;
using System.Collections.Generic;
using System.Text;

namespace AssetFlow.Services
{
    // A small RFC 4180 reader. Splitting on commas is what everybody tries first and
    // it breaks the moment somebody exports a description containing one, which is
    // most real inventory exports. This handles quoted fields, commas and newlines
    // inside quotes, and the doubled "" escape.
    //
    // Deliberately not a NuGet dependency - it is forty lines and the alternative is
    // pulling a package in for one screen of the app.
    public static class CsvReader
    {
        public static List<List<string>> Parse(string content)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            var inQuotes = false;

            // Excel writes \r\n, most other things write \n. Normalising up front means
            // the state machine below only ever has to think about \n.
            content = content.Replace("\r\n", "\n").Replace('\r', '\n');

            for (var i = 0; i < content.Length; i++)
            {
                var c = content[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        // A doubled quote inside a quoted field is a literal quote.
                        if (i + 1 < content.Length && content[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        field.Append(c);
                    }

                    continue;
                }

                switch (c)
                {
                    case '"':
                        inQuotes = true;
                        break;

                    case ',':
                        row.Add(field.ToString());
                        field.Clear();
                        break;

                    case '\n':
                        row.Add(field.ToString());
                        field.Clear();
                        rows.Add(row);
                        row = new List<string>();
                        break;

                    default:
                        field.Append(c);
                        break;
                }
            }

            // Whatever is still in hand when the text runs out is the last field, unless
            // the file ended with a newline and there is genuinely nothing left.
            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                rows.Add(row);
            }

            // Drop rows that are entirely empty - a trailing blank line is not a record.
            rows.RemoveAll(r => r.TrueForAll(string.IsNullOrWhiteSpace));

            return rows;
        }
    }
}
