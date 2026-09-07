using System.Collections.Generic;

namespace AssetFlow.Models
{
    // What the import screen shows once a file has been chewed through. Rows that
    // failed are kept with their reason rather than counted, because "3 rows were
    // skipped" without saying which ones is useless to whoever has to fix the file.
    public class AssetImportResult
    {
        public string FileName { get; set; } = string.Empty;

        public bool WasDryRun { get; set; }

        public List<AssetImportRow> Rows { get; } = new();

        // A problem with the file as a whole - missing header, unreadable, no data.
        public List<string> FileErrors { get; } = new();

        public int ValidCount => Rows.FindAll(r => r.IsValid).Count;

        public int InvalidCount => Rows.FindAll(r => !r.IsValid).Count;

        public bool HasAnythingToImport => ValidCount > 0 && FileErrors.Count == 0;
    }

    public class AssetImportRow
    {
        // The line in the uploaded file, header included, so the number matches what
        // the person sees in Excel.
        public int LineNumber { get; set; }

        public string Name { get; set; } = string.Empty;

        public string SerialNumber { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public Asset? Asset { get; set; }

        public List<string> Errors { get; } = new();

        public bool IsValid => Errors.Count == 0;
    }
}
