using Application.Uploads;
using ClosedXML.Excel;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Infrastructure.Services.Uploads
{
    public class ExcelUploadIngestionService : IUploadIngestionService
    {
        private readonly TenderPriceDbContext _db;

        public ExcelUploadIngestionService(TenderPriceDbContext db) => _db = db;

        public async Task<UploadPreviewResult> PreviewExcelAsync(Stream fileStream, string fileName, CancellationToken ct)
        {
            using var ms = new MemoryStream();
            await fileStream.CopyToAsync(ms, ct);
            ms.Position = 0;

            using var wb = new XLWorkbook(ms);
            var ws = wb.Worksheets.First()!;

            // ----- detect header row -----
            var headerRowNum = DetectHeaderRow(ws);
            if (headerRowNum <= 0) throw new InvalidOperationException("Header row with 'Bezeichnung' not found.");

            // column names present in header
            var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 1;
            var headerNames = new Dictionary<int, string>();
            for (int c = 1; c <= lastCol; c++)
            {
                var t = ws.Cell(headerRowNum, c).GetString()?.Trim();
                if (!string.IsNullOrEmpty(t)) headerNames[c] = t!;
            }

            // find Pos. and Bezeichnung columns
            var posCol = FindHeader(headerNames, "Pos.", "Pos");
            var bezCol = FindHeader(headerNames, "Bezeichnung", "Name", "Benennung");
            if (bezCol is null) throw new InvalidOperationException("'Bezeichnung' column not found.");

            var res = new UploadPreviewResult { FileName = fileName, SheetName = ws.Name };

            string? currentMain = null;
            string? currentSub = null;

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? headerRowNum;
            for (int r = headerRowNum + 1; r <= lastRow; r++)
            {
                var pos = posCol is null ? null : ws.Cell(r, posCol.Value).GetString()?.Trim();
                var bez = GetCellConsideringMerge(ws, r, bezCol.Value);

                // skip empty line
                if (string.IsNullOrWhiteSpace(pos) && string.IsNullOrWhiteSpace(bez))
                    continue;

                var level = GetLevel(pos);

                // categories
                if (level == 1)
                {
                    currentMain = FirstLine(bez);
                    currentSub = null;
                    continue;
                }
                if (level == 2)
                {
                    currentSub = FirstLine(bez);
                    continue;
                }

                // only products: level >= 3
                if (level >= 3)
                {
                    var startRow = r;
                    var lines0 = SplitLines(bez).ToList();
                    var nameLine = lines0.FirstOrDefault();
                    var descLines = lines0.Skip(1).ToList();

                    string? sku = ExtractSku(bez);
                    string? brand = null;                          // ignore for now
                    string? material = null;                       // ignore for now

                    // gather continuation lines until next Pos. (same as before)
                    int rr = r + 1;
                    for (; rr <= lastRow; rr++)
                    {
                        var pos2 = posCol is null ? null : ws.Cell(rr, posCol.Value).GetString()?.Trim();
                        var bez2 = GetCellConsideringMerge(ws, rr, bezCol.Value);
                        if (!string.IsNullOrWhiteSpace(pos2)) break;
                        if (!string.IsNullOrWhiteSpace(bez2))
                        {
                            var extra = SplitLines(bez2);
                            descLines.AddRange(extra);

                            // also try to pick SKU if not found yet
                            sku ??= ExtractSku(bez2);
                        }
                    }

                    // --- NEW: robust Size extraction from description lines (single line only) ---
                    var size = ExtractSize(descLines);

                    // Clean description: remove any explicit SKU line
                    var description = RemoveLine(string.Join("\n", descLines),
                                                 @"(?i)\b(Artikel(?:nr|nummer)?|Art\.-?Nr)\.?:");

                    // small raw dump
                    var raw = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                    foreach (var kv in headerNames)
                    {
                        var v = ws.Cell(startRow, kv.Key).GetString()?.Trim();
                        if (!string.IsNullOrEmpty(v)) raw[kv.Value] = v;
                    }

                    res.Rows.Add(new UploadPreviewRow
                    {
                        RowIndex = startRow,
                        Position = pos,
                        MainCategory = currentMain,
                        SubCategory = currentSub,
                        Name = nameLine,
                        Description = string.IsNullOrWhiteSpace(description) ? null : description,
                        Sku = sku,
                        Size = size,                // NEW
                        Brand = brand,
                        Material = material,
                        Raw = raw
                    });

                    r = rr - 1;
                }
            }

            return res;
        }

        public async Task<int> SaveAsync(UploadPreviewResult preview, string uploadedByUserId, string currency, CancellationToken ct)
        {
            // 1) UploadedFile
            var file = new UploadedFile
            {
                OriginalFileName = preview.FileName,
                UploadedByUserId = uploadedByUserId,
                UploadedUtc = DateTime.UtcNow,
                Status = UploadedFileStatus.Parsed, // Parsed
                Notes = "Imported via preview pipeline"
            };
            _db.UploadedFiles.Add(file);
            await _db.SaveChangesAsync(ct);

            // 2) UploadedSheet
            var sheet = new UploadedSheet
            {
                UploadedFileId = file.Id,
                SheetName = preview.SheetName,
                RowCount = preview.Rows.Count,
                ParseStatus = ParseStatus.Parsed
            };
            _db.UploadedSheets.Add(sheet);
            await _db.SaveChangesAsync(ct);

            // 3) Rows
            foreach (var r in preview.Rows)
            {
                var payload = JsonSerializer.Serialize(r.Raw);
                var normalized = BuildNormalizedText(r);

                var row = new UploadedRow
                {
                    UploadedSheetId = sheet.Id,
                    RowIndex = r.RowIndex,
                    Position = r.Position,
                    MainCategory = r.MainCategory,
                    SubCategory = r.SubCategory,
                    Description = r.Description,
                    Size = r.Size,                 // NEW
                    JsonPayload = JsonSerializer.Serialize(r.Raw),
                    NormalizedText = BuildNormalizedText(r),
                    Sku = r.Sku,
                    Name = r.Name,
                    // Brand / Material left null for now
                    Price = r.Price,
                    Currency = r.Price.HasValue ? (r.Currency ?? currency) : null
                };

                _db.UploadedRows.Add(row);

                // Optional: upsert into ProductDescription by SKU (if provided)
                if (!string.IsNullOrWhiteSpace(r.Sku))
                {
                    var existing = await _db.ProductDescriptions.FirstOrDefaultAsync(p => p.Sku == r.Sku, ct);
                    if (existing is null)
                    {
                        _db.ProductDescriptions.Add(new ProductDescription
                        {
                            Sku = r.Sku,
                            Name = r.Name,
                            Brand = r.Brand,
                            Material = r.Material,
                            SearchText = normalized,
                            SourceFileId = file.Id
                        });
                    }
                    else
                    {
                        existing.Name = existing.Name ?? r.Name ?? existing.Name;
                        existing.Brand ??= r.Brand;
                        existing.Material ??= r.Material;
                        existing.SourceFileId ??= file.Id;
                        existing.SearchText ??= normalized;
                    }
                }
            }

            var saved = await _db.SaveChangesAsync(ct);
            return saved;
        }
        private static int DetectHeaderRow(IXLWorksheet ws)
        {
            var last = ws.LastRowUsed()?.RowNumber() ?? 1;
            for (int r = 1; r <= Math.Min(last, 50); r++)
            {
                var text = string.Join("|", ws.Row(r).CellsUsed().Select(c => (c.GetString() ?? "").Trim().ToLowerInvariant()));
                if (text.Contains("bezeichnung") && (text.Contains("menge") || text.Contains("einheit") || text.Contains("|ep|") || text.Contains("|gp|")))
                    return r;
            }
            int bestRow = 0, bestCnt = 0;
            for (int r = 1; r <= Math.Min(last, 50); r++)
            {
                var cnt = ws.Row(r).CellsUsed().Count();
                if (cnt > bestCnt) { bestCnt = cnt; bestRow = r; }
            }
            return bestRow;
        }

        private static int? FindHeader(Dictionary<int, string> headers, params string[] aliases)
        {
            foreach (var kv in headers)
                if (aliases.Any(a => string.Equals(kv.Value, a, StringComparison.OrdinalIgnoreCase)))
                    return kv.Key;
            return null;
        }

        private static string? GetCellConsideringMerge(IXLWorksheet ws, int r, int c)
        {
            var cell = ws.Cell(r, c);
            var s = cell.GetString()?.Trim();
            if (!string.IsNullOrEmpty(s)) return s;

            var merged = ws.MergedRanges.FirstOrDefault(range => range.Contains(cell.AsRange()));
            if (merged is not null) return merged.FirstCell().GetString()?.Trim();

            return null;
        }

        private static int GetLevel(string? pos)
        {
            if (string.IsNullOrWhiteSpace(pos)) return -1;
            if (!Regex.IsMatch(pos, @"^\d+(\.\d+)*$")) return -1;
            return pos.Count(ch => ch == '.') + 1;
        }

        private static IEnumerable<string> SplitLines(string s) =>
            (s ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());

        private static string? FirstLine(string? s) => SplitLines(s ?? "").FirstOrDefault();

        private static string? ExtractSku(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var m = Regex.Match(text, @"(?is)\b(Artikel(?:nr|nummer)?|Art\.-?Nr)\.?\s*:\s*([A-Za-z0-9\-/_.]+)");
            return m.Success ? m.Groups[2].Value.Trim() : null;
        }

        static string? ExtractSize(IEnumerable<string> lines)
        {
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;

                if (line.Contains("Hersteller", StringComparison.OrdinalIgnoreCase)) continue;
                if (line.StartsWith("Gewinde", StringComparison.OrdinalIgnoreCase)) continue;

                // Nennweite: 12
                var m0 = Regex.Match(line, @"^(?i)\s*Nennweite\s*:\s*([^\r\n]+?)\s*$");
                if (m0.Success) return m0.Groups[1].Value.Trim();

                // DN 15  | DN15
                var m1 = Regex.Match(line, @"^(?i)\s*DN\s*([0-9]+(?:\s*/\s*[0-9]+)?)\s*$");
                if (m1.Success) return $"DN {m1.Groups[1].Value.Trim()}";

                // Ø 18  | Ø18 | O 18 (sometimes Ø renders as 'O')
                var m2 = Regex.Match(line, @"^(?i)\s*[ØO]\s*([0-9]+(?:\.[0-9]+)?(?:\s*mm)?)\s*$");
                if (m2.Success) return m2.Groups[1].Value.Trim();

                // Composites like 12*12*15 or 12 x 12 x 15
                var m3 = Regex.Match(line, @"\b\d+(?:\.\d+)?(?:\s*[x\*]\s*\d+(?:\.\d+)?){1,3}\b");
                if (m3.Success) return m3.Value.Replace(" ", "");
            }
            return null;
        }

        private static string RemoveLine(string text, string lineStartsWithPattern)
        {
            var lines = SplitLines(text).ToList();
            var re = new Regex($"^{lineStartsWithPattern}", RegexOptions.IgnoreCase);
            lines.RemoveAll(l => re.IsMatch(l));
            return string.Join("\n", lines);
        }

        private static string BuildNormalizedText(UploadPreviewRow r)
        {
            var parts = new[] { r.Sku, r.Name, r.Description, r.Size, r.MainCategory, r.SubCategory }
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim().ToLowerInvariant());
            return string.Join(' ', parts);
        }
    }
}