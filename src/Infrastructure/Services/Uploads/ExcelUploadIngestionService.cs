using Application.Uploads;
using ClosedXML.Excel;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
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

            // ---- duplicate guard (by content hash) ----
            ms.Position = 0;
            var hash = await ComputeSha256Async(ms, ct);
            var size = ms.Length;
            ms.Position = 0;

            var existing = await _db.UploadedFiles
                .AsNoTracking()
                .Where(f => f.ContentHash == hash)
                .Select(f => new { f.Id, f.OriginalFileName })
                .FirstOrDefaultAsync(ct);

            if (existing != null)
            {
                return new UploadPreviewResult
                {
                    FileName = fileName,
                    SheetName = string.Empty,
                    Rows = new(),
                    ContentHash = hash,
                    SizeBytes = size,
                    IsDuplicate = true,
                    DuplicateFileId = existing.Id,
                    DuplicateFileName = existing.OriginalFileName
                };
            }

            // ---- proceed with parsing (not a duplicate) ----
            ms.Position = 0;
            using var wb = new XLWorkbook(ms);
            var ws = wb.Worksheets.First()!;

            var headerRowNum = DetectHeaderRow(ws);
            if (headerRowNum <= 0) throw new InvalidOperationException("Header row with 'Bezeichnung' not found.");

            var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 1;
            var headerNames = new Dictionary<int, string>();
            for (int c = 1; c <= lastCol; c++)
            {
                var t = ws.Cell(headerRowNum, c).GetString()?.Trim();
                if (!string.IsNullOrEmpty(t)) headerNames[c] = t!;
            }

            var posCol = FindHeader(headerNames, "Pos.", "Pos");
            var bezCol = FindHeader(headerNames, "Bezeichnung", "Name", "Benennung");
            if (bezCol is null) throw new InvalidOperationException("'Bezeichnung' column not found.");

            var res = new UploadPreviewResult
            {
                FileName = fileName,
                SheetName = ws.Name,
                ContentHash = hash,
                SizeBytes = size
            };

            string? currentMain = null;
            string? currentSub = null;

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? headerRowNum;
            for (int r = headerRowNum + 1; r <= lastRow; r++)
            {
                var pos = posCol is null ? null : ws.Cell(r, posCol.Value).GetString()?.Trim();
                var bez = GetCellConsideringMerge(ws, r, bezCol.Value);

                if (string.IsNullOrWhiteSpace(pos) && string.IsNullOrWhiteSpace(bez))
                    continue;

                var level = GetLevel(pos);

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

                if (level >= 3)
                {
                    var startRow = r;
                    var lines0 = SplitLines(bez).ToList();
                    var nameLine = lines0.FirstOrDefault();
                    var descLines = lines0.Skip(1).ToList();

                    string? sku = ExtractSku(bez);
                    string? brand = null;
                    string? material = null;

                    int rr = r + 1;
                    for (; rr <= lastRow; rr++)
                    {
                        var pos2 = posCol is null ? null : ws.Cell(rr, posCol.Value).GetString()?.Trim();
                        var bez2 = GetCellConsideringMerge(ws, rr, bezCol.Value);
                        if (!string.IsNullOrWhiteSpace(pos2)) break;
                        if (!string.IsNullOrWhiteSpace(bez2))
                        {
                            descLines.AddRange(SplitLines(bez2));
                            sku ??= ExtractSku(bez2);
                        }
                    }

                    var sizeVal = ExtractSize(descLines);
                    var description = RemoveLine(string.Join("\n", descLines),
                        @"(?i)\b(Artikel(?:nr|nummer)?|Art\.-?Nr)\.?:");

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
                        Size = sizeVal,
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
            if (preview is null) throw new ArgumentNullException(nameof(preview));
            if (preview.IsDuplicate)
                throw new InvalidOperationException($"Duplicate content. Already uploaded as file #{preview.DuplicateFileId} ({preview.DuplicateFileName}).");

            var contentHash = preview.ContentHash;
            if (string.IsNullOrWhiteSpace(contentHash))
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(preview);
                using var ms = new MemoryStream(bytes);
                contentHash = await ComputeSha256Async(ms, ct);
            }

            var dup = await _db.UploadedFiles.AsNoTracking()
                .FirstOrDefaultAsync(f => f.ContentHash == contentHash, ct);
            if (dup != null)
                throw new InvalidOperationException($"Duplicate content. Already uploaded as file #{dup.Id} ({dup.OriginalFileName}).");

            var (baseName, ext) = SplitName(preview.FileName);
            var uniqueFileName = await EnsureUniqueFileNameAsync(baseName, ext, uploadedByUserId, ct);

            using var tx = await _db.Database.BeginTransactionAsync(ct);

            var file = new UploadedFile
            {
                OriginalFileName = uniqueFileName,
                UploadedByUserId = uploadedByUserId,
                UploadedUtc = DateTime.UtcNow,
                Status = UploadedFileStatus.Parsed,
                Notes = "Imported via preview pipeline",
                ContentHash = contentHash!,
                ByteSize = preview.SizeBytes
            };
            _db.UploadedFiles.Add(file);

            var sheet = new UploadedSheet
            {
                UploadedFile = file, // NAVIGATION (avoid FK race)
                SheetName = preview.SheetName,
                RowCount = preview.Rows.Count,
                ParseStatus = ParseStatus.Parsed
            };
            _db.UploadedSheets.Add(sheet);

            var wantedSkus = preview.Rows
                .Where(r => !string.IsNullOrWhiteSpace(r.Sku))
                .Select(r => r.Sku!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            Dictionary<string, ProductDescription> existingSkus;
            if (wantedSkus.Count == 0)
            {
                existingSkus = new Dictionary<string, ProductDescription>(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                var existingList = await _db.ProductDescriptions
                    .Where(p => p.Sku != null && wantedSkus.Contains(p.Sku))
                    .ToListAsync(ct); // tracked
                existingSkus = existingList.ToDictionary(p => p.Sku!, p => p, StringComparer.OrdinalIgnoreCase);
            }

            var rowsToAdd = new List<UploadedRow>(preview.Rows.Count);

            foreach (var r in preview.Rows)
            {
                var normalized = BuildNormalizedText(r);

                var row = new UploadedRow
                {
                    UploadedSheet = sheet, // NAVIGATION
                    RowIndex = r.RowIndex,
                    Position = r.Position,
                    MainCategory = r.MainCategory,
                    SubCategory = r.SubCategory,
                    Description = r.Description,
                    Size = r.Size,
                    JsonPayload = JsonSerializer.Serialize(r.Raw),
                    NormalizedText = normalized,
                    Sku = r.Sku,
                    Name = r.Name,
                    Price = r.Price,
                    Currency = r.Price.HasValue ? (r.Currency ?? currency) : null
                };
                rowsToAdd.Add(row);

                if (!string.IsNullOrWhiteSpace(r.Sku))
                {
                    if (!existingSkus.TryGetValue(r.Sku!, out var pd))
                    {
                        pd = new ProductDescription
                        {
                            Sku = r.Sku!,
                            Name = r.Name,
                            Brand = r.Brand,
                            Material = r.Material,
                            SearchText = normalized,
                            SourceFile = file // NAVIGATION
                        };
                        _db.ProductDescriptions.Add(pd);
                        existingSkus[r.Sku!] = pd;
                    }
                    else
                    {
                        if (pd.Name == null && r.Name != null) pd.Name = r.Name;
                        if (pd.Brand == null && r.Brand != null) pd.Brand = r.Brand;
                        if (pd.Material == null && r.Material != null) pd.Material = r.Material;
                        if (pd.SearchText == null) pd.SearchText = normalized;
                        if (pd.SourceFile == null) pd.SourceFile = file;
                    }
                }
            }

            _db.UploadedRows.AddRange(rowsToAdd);

            var saved = await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
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

        private static IEnumerable<string> SplitLines(string? s) => (s ?? string.Empty).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());

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

        private static async Task<string> ComputeSha256Async(Stream s, CancellationToken ct)
        {
            using var sha = SHA256.Create();
            s.Position = 0;
            var bytes = await sha.ComputeHashAsync(s, ct);
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        private static (string Base, string Ext) SplitName(string fileName)
        {
            var ext = Path.GetExtension(fileName);
            var b = Path.GetFileNameWithoutExtension(fileName);
            return (b, ext);
        }

        private async Task<string> EnsureUniqueFileNameAsync(string baseName, string ext, string userId, CancellationToken ct)
        {
            var names = await _db.UploadedFiles
                .Where(f => f.UploadedByUserId == userId && (
                    f.OriginalFileName == baseName + ext ||
                    (f.OriginalFileName.StartsWith(baseName + " (") && f.OriginalFileName.EndsWith(")" + ext))
                ))
                .Select(f => f.OriginalFileName)
                .ToListAsync(ct);

            if (!names.Contains(baseName + ext)) return baseName + ext;

            var max = 0;
            foreach (var n in names)
            {
                var m = Regex.Match(n, $"^{Regex.Escape(baseName)} \\((\\d+)\\){Regex.Escape(ext)}$");
                if (m.Success && int.TryParse(m.Groups[1].Value, out var k) && k > max) max = k;
            }
            return $"{baseName} ({max + 1}){ext}";
        }
    }
}