using System.Globalization;
using ClothesSystem.Application.ClothingItems;
using ClothesSystem.Application.Common.Interfaces;
using ClothesSystem.Domain.Entities;
using ClothesSystem.Domain.Enums;
using ClothesSystem.Infrastructure.Identity;
using ClothesSystem.Infrastructure.Services.Workbook;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using A = DocumentFormat.OpenXml.Drawing;
using Xdr = DocumentFormat.OpenXml.Drawing.Spreadsheet;

namespace ClothesSystem.Infrastructure.Services;

public class ClothingWorkbookTemplateService : IClothingWorkbookTemplateService
{
    private const string WorkbookContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const long EmusPerPixel = 9525L;

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDataPathProvider _dataPathProvider;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SingleStyleWorkbookTemplateDefinition _templateDefinition = SingleStyleWorkbookTemplateDefinition.Default;
    private readonly BlankDesignTemplateDefinition _blankTemplateDefinition = BlankDesignTemplateDefinition.Default;

    public ClothingWorkbookTemplateService(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IDataPathProvider dataPathProvider,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _dataPathProvider = dataPathProvider;
        _userManager = userManager;
    }

    public async Task<ClothingWorkbookTemplateFileDto?> ExportPlaceholderAsync(
        Guid clothingItemId,
        CancellationToken cancellationToken = default)
    {
        var item = await GetVisibleItemAsync(clothingItemId, cancellationToken);
        if (item == null)
        {
            return null;
        }

        var templatePath = GetTemplatePath();
        if (!File.Exists(templatePath))
        {
            throw new InvalidOperationException($"Workbook template was not found: {templatePath}");
        }

        var templateBytes = await File.ReadAllBytesAsync(templatePath, cancellationToken);
        await using var outputStream = new MemoryStream();
        await outputStream.WriteAsync(templateBytes, cancellationToken);
        outputStream.Position = 0;

        using (var spreadsheet = SpreadsheetDocument.Open(outputStream, true))
        {
            var workbookPart = spreadsheet.WorkbookPart
                ?? throw new InvalidOperationException("Workbook part is missing from the template.");
            var worksheetPart = GetWorksheetPart(workbookPart, _templateDefinition.WorksheetName);

            PopulateHeaderValues(worksheetPart.Worksheet, item);
            PopulateStructuredSections(workbookPart, worksheetPart.Worksheet, item);

            var primaryImage = await ResolvePrimaryImageAsync(item, cancellationToken);
            if (primaryImage != null)
            {
                ClearCellValue(worksheetPart.Worksheet, _templateDefinition.ImagePlaceholderCellReference);
                InsertImage(worksheetPart, primaryImage.Value);
            }

            worksheetPart.Worksheet.Save();
            workbookPart.Workbook.Save();
        }

        return new ClothingWorkbookTemplateFileDto
        {
            FileName = $"{item.StyleNumber}-设计单.xlsx",
            ContentType = WorkbookContentType,
            Content = outputStream.ToArray()
        };
    }

    public async Task<ClothingWorkbookTemplateFileDto?> ExportDesignSheetAsync(
        Guid clothingItemId,
        CancellationToken cancellationToken = default)
    {
        var item = await GetVisibleItemAsync(clothingItemId, cancellationToken);
        if (item == null)
        {
            return null;
        }

        var templatePath = Path.Combine(_dataPathProvider.TemplatePath, _blankTemplateDefinition.RelativeTemplatePath);
        if (!File.Exists(templatePath))
        {
            throw new InvalidOperationException($"设计单模板未找到: {templatePath}");
        }

        var templateBytes = await File.ReadAllBytesAsync(templatePath, cancellationToken);
        await using var outputStream = new MemoryStream();
        await outputStream.WriteAsync(templateBytes, cancellationToken);
        outputStream.Position = 0;

        using (var spreadsheet = SpreadsheetDocument.Open(outputStream, true))
        {
            var workbookPart = spreadsheet.WorkbookPart
                ?? throw new InvalidOperationException("Workbook part is missing from the template.");
            var worksheetPart = GetWorksheetPart(workbookPart, _blankTemplateDefinition.WorksheetName);

            await PopulateDesignSheetHeaderAsync(worksheetPart.Worksheet, item);
            PopulateDesignSheetFabricBlocks(workbookPart, worksheetPart.Worksheet, item);

            var primaryImage = await ResolvePrimaryImageAsync(item, cancellationToken);
            if (primaryImage != null)
            {
                ClearCellValue(worksheetPart.Worksheet, _blankTemplateDefinition.ImagePlaceholderCellReference);
                InsertImage(worksheetPart, primaryImage.Value);
            }

            var secondaryImage = await ResolveSecondaryImageAsync(item, cancellationToken);
            if (secondaryImage != null)
            {
                // Place second image at G4:J18 (modification notes area)
                ClearCellValue(worksheetPart.Worksheet, _blankTemplateDefinition.SecondImagePlaceholderCellReference);
                var secondPlacement = CalculateImagePlacement(
                    worksheetPart.Worksheet,
                    _blankTemplateDefinition.SecondImagePlacement,
                    secondaryImage.Value.Metadata);
                InsertImageWithPlacement(worksheetPart, secondaryImage.Value, secondPlacement);

                // Put modification notes text in a table below the second image (G19:J24)
                PopulateDesignSheetNotesBelowImage(workbookPart, worksheetPart.Worksheet, item);
            }
            else
            {
                // No second image - put notes at original location G4
                PopulateDesignSheetNotes(workbookPart, worksheetPart.Worksheet, item);
            }

            worksheetPart.Worksheet.Save();
            workbookPart.Workbook.Save();
        }

        return new ClothingWorkbookTemplateFileDto
        {
            FileName = $"{item.StyleNumber}-设计单.xlsx",
            ContentType = WorkbookContentType,
            Content = outputStream.ToArray()
        };
    }

    public async Task<ClothingWorkbookImportResult> ImportPlaceholderAsync(
        Guid clothingItemId,
        string fileName,
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        var item = await GetVisibleItemAsync(clothingItemId, cancellationToken);
        if (item == null)
        {
            return new ClothingWorkbookImportResult
            {
                Accepted = false,
                Message = "未找到目标款式，无法导入设计单模板。"
            };
        }

        var extension = Path.GetExtension(fileName);
        if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return new ClothingWorkbookImportResult
            {
                Accepted = false,
                Message = "当前设计单接口仅接收 .xlsx 文件。"
            };
        }

        if (fileStream.CanSeek && fileStream.Length == 0)
        {
            return new ClothingWorkbookImportResult
            {
                Accepted = false,
                Message = "上传文件为空，请重新选择后再试。"
            };
        }

        return new ClothingWorkbookImportResult
        {
            Accepted = true,
            Message = $"已接收 {item.StyleNumber} 的设计单文件。当前已按固定模板完成导出，导入解析接口仍保留为后续扩展点。"
        };
    }

    public async Task<ClothingDesignSheetImportResult> ImportDesignSheetAsync(
        string fileName,
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        var result = new ClothingDesignSheetImportResult();

        var extension = Path.GetExtension(fileName);
        if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            result.Message = "仅支持 .xlsx 文件格式。";
            return result;
        }

        if (fileStream.CanSeek && fileStream.Length == 0)
        {
            result.Message = "上传文件为空。";
            return result;
        }

        try
        {
            var data = await ParseDesignSheetAsync(fileStream, cancellationToken);
            if (data == null)
            {
                result.Message = "无法解析设计单文件，请确认文件格式正确。";
                return result;
            }

            // Check if style number already exists
            var styleExists = await _dbContext.ClothingItems
                .AnyAsync(c => c.StyleNumber == data.StyleNumber, cancellationToken);

            if (styleExists)
            {
                result.Message = $"款号 {data.StyleNumber} 已存在，请修改后再导入。";
                return result;
            }

            // Create new clothing item
            var currentUser = _currentUserService.GetCurrentUser();
            if (!currentUser.IsAuthenticated)
            {
                result.Message = "用户未登录，无法创建款式。";
                return result;
            }

            var item = new ClothingItem
            {
                Id = Guid.NewGuid(),
                StyleNumber = data.StyleNumber,
                Title = data.ProductName ?? data.StyleNumber,
                Year = data.Year > 0 ? data.Year : DateTime.Now.Year,
                Season = data.Season ?? "",
                Progress = ClothingProgressStatus.PatternMaking,
                ApprovalStatus = ApprovalStatus.Draft,
                OwnerId = currentUser.UserId,
                OwnerDisplayName = currentUser.DisplayName ?? "未知用户",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            // Add fabric entries
            foreach (var fabric in data.FabricEntries)
            {
                item.FabricEntries.Add(new FabricEntry
                {
                    ClothingItemId = item.Id,
                    MaterialName = fabric.MaterialName ?? "",
                    Specification = fabric.Specification ?? "",
                    Remark = fabric.Remark ?? "",
                    SortOrder = item.FabricEntries.Count
                });
            }

            // Build fabric information summary
            if (item.FabricEntries.Count > 0)
            {
                item.FabricInformation = string.Join("；",
                    item.FabricEntries.Select(f =>
                        string.Join(" / ", new[] { f.MaterialName, f.Specification, f.Remark }
                            .Where(s => !string.IsNullOrWhiteSpace(s)))));
            }

            _dbContext.ClothingItems.Add(item);
            await _dbContext.SaveChangesAsync(cancellationToken);

            result.Success = true;
            result.NewItemId = item.Id;
            result.StyleNumber = item.StyleNumber;
            result.Title = item.Title;
            result.Message = $"成功导入款式 {item.StyleNumber}！";

            if (result.Warnings.Count > 0)
            {
                result.Message += $"（{result.Warnings.Count} 个警告）";
            }
        }
        catch (Exception ex)
        {
            result.Message = $"导入失败：{ex.Message}";
        }

        return result;
    }

    private async Task<ClothingDesignSheetData?> ParseDesignSheetAsync(Stream fileStream, CancellationToken cancellationToken)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            using var spreadsheet = SpreadsheetDocument.Open(memoryStream, false);
            var workbookPart = spreadsheet.WorkbookPart;
            if (workbookPart == null) return null;

            var worksheetPart = GetWorksheetPart(workbookPart, _blankTemplateDefinition.WorksheetName);
            var worksheet = worksheetPart.Worksheet;

            // Get shared strings
            var sharedStrings = workbookPart.GetPartsOfType<SharedStringTablePart>()
                .FirstOrDefault();
            var strings = new List<string>();
            if (sharedStrings != null)
            {
                foreach (var item in sharedStrings.SharedStringTable.Elements<SharedStringItem>())
                {
                    strings.Add(item.Text?.Text ?? item.InnerText ?? "");
                }
            }

            var data = new ClothingDesignSheetData();

            // Parse header values
            data.StyleNumber = GetCellStringValue(worksheet, _blankTemplateDefinition.StyleNumberCellReference, strings) ?? "";
            data.Year = DateTime.Now.Year;

            var quarter = GetCellStringValue(worksheet, _blankTemplateDefinition.QuarterCellReference, strings);
            if (!string.IsNullOrWhiteSpace(quarter))
            {
                // Try to parse year and season from quarter (e.g., "2026SS", "2026AW")
                var yearMatch = System.Text.RegularExpressions.Regex.Match(quarter, @"^(\d{4})");
                if (yearMatch.Success)
                {
                    data.Year = int.Parse(yearMatch.Groups[1].Value);
                    data.Season = quarter.Substring(4);
                }
                else
                {
                    data.Season = quarter;
                }
            }

            data.Designer = GetCellStringValue(worksheet, _blankTemplateDefinition.DesignerCellReference, strings);

            var orderDateStr = GetCellStringValue(worksheet, _blankTemplateDefinition.OrderDateCellReference, strings);
            if (!string.IsNullOrWhiteSpace(orderDateStr) && DateTime.TryParse(orderDateStr, out var orderDate))
            {
                data.OrderDate = orderDate;
            }

            data.ProductName = GetCellStringValue(worksheet, _blankTemplateDefinition.ProductNameCellReference, strings);

            // Parse fabric entries from merged cells
            var fabricA = ParseFabricBlock(worksheet, _blankTemplateDefinition.FabricACellReference, strings);
            if (fabricA != null) data.FabricEntries.Add(fabricA);

            var fabricB = ParseFabricBlock(worksheet, _blankTemplateDefinition.FabricBCellReference, strings);
            if (fabricB != null) data.FabricEntries.Add(fabricB);

            var fabricC = ParseFabricBlock(worksheet, _blankTemplateDefinition.FabricCCellReference, strings);
            if (fabricC != null) data.FabricEntries.Add(fabricC);

            return data;
        }
        catch
        {
            return null;
        }
    }

    private string? GetCellStringValue(Worksheet worksheet, string cellReference, List<string> sharedStrings)
    {
        var cell = worksheet.Descendants<Cell>()
            .FirstOrDefault(c => c.CellReference?.Value == cellReference);

        if (cell == null || cell.CellValue == null) return null;

        var value = cell.CellValue.Text;
        if (cell.DataType?.Value == CellValues.SharedString && int.TryParse(value, out int index) && index < sharedStrings.Count)
        {
            return sharedStrings[index];
        }

        return value;
    }

    private FabricEntryEditDto? ParseFabricBlock(Worksheet worksheet, string startCellReference, List<string> sharedStrings)
    {
        // Get all text from the fabric block area
        var text = GetCellStringValue(worksheet, startCellReference, sharedStrings);
        if (string.IsNullOrWhiteSpace(text)) return null;

        // Split by newlines - first line is usually the label (面料A：, etc.)
        var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length == 0) return null;

        // First line might be the label, get the rest as material info
        var contentLines = lines.Skip(1).ToArray();
        if (contentLines.Length == 0 && lines.Length > 0)
        {
            // If there's no newline, the whole text might be material name
            contentLines = new[] { lines[0] };
        }

        var materialName = contentLines.Length > 0 ? contentLines[0].Trim() : "";
        var specification = contentLines.Length > 1 ? contentLines[1].Trim() : "";
        var remark = contentLines.Length > 2 ? contentLines[2].Trim() : "";

        if (string.IsNullOrWhiteSpace(materialName) && string.IsNullOrWhiteSpace(specification))
        {
            return null;
        }

        return new FabricEntryEditDto
        {
            MaterialName = materialName,
            Specification = specification,
            Remark = remark
        };
    }

    private void PopulateHeaderValues(Worksheet worksheet, ClothingItem item)
    {
        SetCellText(worksheet, _templateDefinition.StyleNumberCellReference, item.StyleNumber);
        SetCellText(worksheet, _templateDefinition.QuarterCellReference, BuildQuarterDisplay(item.Year, item.Season));
        SetCellText(worksheet, _templateDefinition.DesignerCellReference, item.OwnerDisplayName);
        SetCellText(
            worksheet,
            _templateDefinition.OrderDateCellReference,
            item.CreatedAtUtc.ToLocalTime().ToString("yyyy/MM/dd", CultureInfo.InvariantCulture));
        ClearCellValue(worksheet, _templateDefinition.PatternMakerCellReference);
        ClearCellValue(worksheet, _templateDefinition.SampleMakerCellReference);
    }

    private void PopulateStructuredSections(WorkbookPart workbookPart, Worksheet worksheet, ClothingItem item)
    {
        SetCellText(
            worksheet,
            _templateDefinition.ProductNameCellReference,
            NormalizeSingleLineText(item.Title, _templateDefinition.ProductNameMaxLength));

        SetCellText(
            worksheet,
            _templateDefinition.ModificationNotesCellReference,
            BuildModificationNotesText(item));

        ApplyWrappedTextStyle(workbookPart, worksheet, _templateDefinition.ModificationNotesCellReference);

        SetCellText(
            worksheet,
            _templateDefinition.SummaryCellReference,
            BuildSummaryText(item));

        ApplyWrappedTextStyle(
            workbookPart,
            worksheet,
            _templateDefinition.SummaryCellReference,
            HorizontalAlignmentValues.Left,
            VerticalAlignmentValues.Top);

        var fabricEntries = GetTemplateFabricEntries(item);
        for (var index = 0; index < _templateDefinition.FabricBlocks.Count; index++)
        {
            var block = _templateDefinition.FabricBlocks[index];
            var entry = index < fabricEntries.Count ? fabricEntries[index] : null;
            SetCellText(
                worksheet,
                block.CellReference,
                BuildFabricBlockText(block.Label, entry));

            ApplyWrappedTextStyle(workbookPart, worksheet, block.CellReference);
        }

        SetCellText(
            worksheet,
            _templateDefinition.ApprovalCellReference,
            NormalizeSingleLineText(
                BuildApprovalSummaryText(item),
                _templateDefinition.ApprovalMaxCharacters));
    }

    private async Task PopulateDesignSheetHeaderAsync(Worksheet worksheet, ClothingItem item)
    {
        // Header row 2
        SetCellText(worksheet, _blankTemplateDefinition.StyleNumberCellReference, item.StyleNumber);
        SetCellText(worksheet, _blankTemplateDefinition.QuarterCellReference, BuildQuarterDisplay(item.Year, item.Season));
        // Designer: use current user's display name from database
        var designerName = await GetCurrentUserDisplayName();
        SetCellText(worksheet, _blankTemplateDefinition.DesignerCellReference, designerName);

        // Header row 3
        SetCellText(
            worksheet,
            _blankTemplateDefinition.OrderDateCellReference,
            item.CreatedAtUtc.ToLocalTime().ToString("yyyy/MM/dd", CultureInfo.InvariantCulture));
        // 不清空版师和样衣师单元格 - 保留模板原有内容

        // Product name
        SetCellText(
            worksheet,
            _blankTemplateDefinition.ProductNameCellReference,
            NormalizeSingleLineText(item.Title, _blankTemplateDefinition.ProductNameMaxLength));
    }

    private async Task<string> GetCurrentUserDisplayName()
    {
        var currentUser = _currentUserService.GetCurrentUser();
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            return "未知用户";
        }

        var user = await _userManager.FindByIdAsync(currentUser.UserId);
        return user?.DisplayName ?? user?.UserName ?? currentUser.DisplayName ?? "未知用户";
    }

    private void PopulateDesignSheetFabricBlocks(WorkbookPart workbookPart, Worksheet worksheet, ClothingItem item)
    {
        var fabricEntries = GetTemplateFabricEntries(item);

        for (var index = 0; index < _blankTemplateDefinition.FabricBlocks.Count; index++)
        {
            var block = _blankTemplateDefinition.FabricBlocks[index];
            var entry = index < fabricEntries.Count ? fabricEntries[index] : null;

            SetCellText(
                worksheet,
                block.CellReference,
                BuildFabricBlockTextForDesignSheet(block.Label, entry));
            ApplyWrappedTextStyle(workbookPart, worksheet, block.CellReference);
        }

        // 审批区域
        SetCellText(
            worksheet,
            _blankTemplateDefinition.ApprovalCellReference,
            NormalizeSingleLineText(
                BuildApprovalSummaryText(item),
                _blankTemplateDefinition.ApprovalMaxCharacters));
    }

    private void PopulateDesignSheetNotes(WorkbookPart workbookPart, Worksheet worksheet, ClothingItem item)
    {
        var notesText = BuildModificationNotesText(item);
        if (!string.IsNullOrWhiteSpace(notesText))
        {
            SetCellText(worksheet, _blankTemplateDefinition.ModificationNotesCellReference, notesText);
            ApplyWrappedTextStyle(
                workbookPart,
                worksheet,
                _blankTemplateDefinition.ModificationNotesCellReference,
                HorizontalAlignmentValues.Left,
                VerticalAlignmentValues.Top);
        }
    }

    private void PopulateDesignSheetNotesBelowImage(WorkbookPart workbookPart, Worksheet worksheet, ClothingItem item)
    {
        // When second image is present, put modification notes in a table below the image (G19:J24)
        var notes = BuildModificationNotesForTable(item);
        var startRef = _blankTemplateDefinition.SecondImageNotesTableCellReference;
        var maxRows = _blankTemplateDefinition.SecondImageNotesTableMaxRows;

        if (notes.Count == 0)
        {
            return;
        }

        // Place notes in a vertical table format across columns G-J
        // Each note gets one row, spread across G, H, I, J for multi-column layout
        var currentRow = GetRowIndex(startRef);
        var currentCol = GetColumnIndex(startRef);
        var colLetter = GetColumnLetter(currentCol);

        for (var i = 0; i < Math.Min(notes.Count, maxRows); i++)
        {
            var cellRef = $"{colLetter}{currentRow + i}";
            SetCellText(worksheet, cellRef, notes[i]);
            ApplyWrappedTextStyle(
                workbookPart,
                worksheet,
                cellRef,
                HorizontalAlignmentValues.Left,
                VerticalAlignmentValues.Top);
        }
    }

    private List<string> BuildModificationNotesForTable(ClothingItem item)
    {
        var result = new List<string>();

        if (!string.IsNullOrWhiteSpace(item.Description))
        {
            result.Add($"款式说明：{item.Description.Trim()}");
        }

        foreach (var note in item.ModificationNotes
                     .OrderByDescending(note => note.CreatedAtUtc)
                     .Take(5))
        {
            result.Add($"{note.CreatedAtUtc.ToLocalTime():MM/dd} {note.CreatedByDisplayName}：{note.Content.Trim()}");
        }

        return result;
    }

    private static string BuildFabricBlockTextForDesignSheet(string label, FabricEntry? entry)
    {
        var lines = new List<string> { $"{label}：" };
        if (entry == null)
        {
            return lines[0];
        }

        if (!string.IsNullOrWhiteSpace(entry.MaterialName))
        {
            lines.Add(entry.MaterialName.Trim());
        }

        if (!string.IsNullOrWhiteSpace(entry.Specification))
        {
            lines.Add(entry.Specification.Trim());
        }

        if (!string.IsNullOrWhiteSpace(entry.Remark))
        {
            lines.Add($"备注：{entry.Remark.Trim()}");
        }

        return string.Join("\n", lines);
    }

    private async Task<WorkbookImagePayload?> ResolvePrimaryImageAsync(ClothingItem item, CancellationToken cancellationToken)
    {
        var attachment = item.ImageAttachments
            .OrderBy(image => image.SortOrder)
            .ThenBy(image => image.UploadedAtUtc)
            .FirstOrDefault();

        if (attachment == null)
        {
            return null;
        }

        return await ResolveImageAsync(attachment, cancellationToken);
    }

    private async Task<WorkbookImagePayload?> ResolveSecondaryImageAsync(ClothingItem item, CancellationToken cancellationToken)
    {
        var attachments = item.ImageAttachments
            .OrderBy(image => image.SortOrder)
            .ThenBy(image => image.UploadedAtUtc)
            .Skip(1)
            .FirstOrDefault();

        if (attachments == null)
        {
            return null;
        }

        return await ResolveImageAsync(attachments, cancellationToken);
    }

    private async Task<WorkbookImagePayload?> ResolveImageAsync(ClothingImageAttachment attachment, CancellationToken cancellationToken)
    {
        if (attachment.BinaryContent is { Length: > 0 })
        {
            var metadata = SpreadsheetImageMetadataReader.TryRead(attachment.BinaryContent, attachment.ContentType);
            return new WorkbookImagePayload(
                attachment.BinaryContent,
                NormalizeImageContentType(attachment.ContentType, metadata),
                metadata);
        }

        if (string.IsNullOrWhiteSpace(attachment.FilePath))
        {
            return null;
        }

        var legacyBinaryContent = await ReadLegacyImageAsync(attachment.FilePath, cancellationToken);
        if (legacyBinaryContent == null)
        {
            return null;
        }

        var legacyMetadata = SpreadsheetImageMetadataReader.TryRead(legacyBinaryContent, attachment.ContentType);
        return new WorkbookImagePayload(
            legacyBinaryContent,
            NormalizeImageContentType(attachment.ContentType, legacyMetadata),
            legacyMetadata);
    }

    private void InsertImage(WorksheetPart worksheetPart, WorkbookImagePayload imagePayload)
    {
        var placement = CalculateImagePlacement(worksheetPart.Worksheet, imagePayload.Metadata);
        InsertImageWithPlacement(worksheetPart, imagePayload, placement);
    }

    private void InsertImageWithPlacement(WorksheetPart worksheetPart, WorkbookImagePayload imagePayload, ImagePlacementResult placement)
    {
        var drawingsPart = worksheetPart.DrawingsPart ?? worksheetPart.AddNewPart<DrawingsPart>();
        drawingsPart.WorksheetDrawing ??= new Xdr.WorksheetDrawing();

        EnsureDrawingReference(worksheetPart, drawingsPart);

        var relationshipId = $"image-{Guid.NewGuid():N}";
        var imagePart = drawingsPart.AddNewPart<ImagePart>(imagePayload.ContentType, relationshipId);
        using (var imageStream = new MemoryStream(imagePayload.BinaryContent, writable: false))
        {
            imagePart.FeedData(imageStream);
        }

        var pictureId = GetNextPictureId(drawingsPart.WorksheetDrawing);

        var picture = new Xdr.Picture(
            new Xdr.NonVisualPictureProperties(
                new Xdr.NonVisualDrawingProperties
                {
                    Id = pictureId,
                    Name = $"StyleImage-{pictureId}"
                },
                new Xdr.NonVisualPictureDrawingProperties(
                    new A.PictureLocks
                    {
                        NoChangeAspect = true
                    })),
            new Xdr.BlipFill(
                new A.Blip
                {
                    Embed = relationshipId,
                    CompressionState = A.BlipCompressionValues.Print
                },
                new A.Stretch(new A.FillRectangle())),
            new Xdr.ShapeProperties(
                new A.Transform2D(
                    new A.Offset { X = 0L, Y = 0L },
                    new A.Extents { Cx = placement.WidthEmu, Cy = placement.HeightEmu }),
                new A.PresetGeometry(new A.AdjustValueList())
                {
                    Preset = A.ShapeTypeValues.Rectangle
                }));

        drawingsPart.WorksheetDrawing.Append(
            new Xdr.AbsoluteAnchor(
                new Xdr.Position { X = placement.XEmu, Y = placement.YEmu },
                new Xdr.Extent { Cx = placement.WidthEmu, Cy = placement.HeightEmu },
                picture,
                new Xdr.ClientData()));

        drawingsPart.WorksheetDrawing.Save();
    }

    private ImagePlacementResult CalculateImagePlacement(Worksheet worksheet, SpreadsheetImageMetadata? metadata)
    {
        return CalculateImagePlacement(worksheet, _templateDefinition.ImagePlacement, metadata);
    }

    private ImagePlacementResult CalculateImagePlacement(Worksheet worksheet, WorkbookImagePlacement imageArea, SpreadsheetImageMetadata? metadata)
    {
        var totalOffsetX = SumColumnWidths(worksheet, 1, imageArea.StartColumnIndex - 1);
        var totalOffsetY = SumRowHeights(worksheet, 1, imageArea.StartRowIndex - 1);
        var areaWidth = SumColumnWidths(worksheet, imageArea.StartColumnIndex, imageArea.EndColumnIndex);
        var areaHeight = SumRowHeights(worksheet, imageArea.StartRowIndex, imageArea.EndRowIndex);

        var padding = Math.Max(0, imageArea.PaddingPixels);
        var availableWidth = Math.Max(1, areaWidth - (padding * 2));
        var availableHeight = Math.Max(1, areaHeight - (padding * 2));

        var targetWidth = availableWidth;
        var targetHeight = availableHeight;

        if (metadata is { WidthPixels: > 0, HeightPixels: > 0 })
        {
            var widthRatio = availableWidth / (double)metadata.Value.WidthPixels;
            var heightRatio = availableHeight / (double)metadata.Value.HeightPixels;
            var scale = Math.Min(widthRatio, heightRatio);

            targetWidth = Math.Max(1, (int)Math.Round(metadata.Value.WidthPixels * scale));
            targetHeight = Math.Max(1, (int)Math.Round(metadata.Value.HeightPixels * scale));
        }

        var imageOffsetX = totalOffsetX + padding + Math.Max(0, (availableWidth - targetWidth) / 2);
        var imageOffsetY = totalOffsetY + padding + Math.Max(0, (availableHeight - targetHeight) / 2);

        return new ImagePlacementResult(
            imageOffsetX * EmusPerPixel,
            imageOffsetY * EmusPerPixel,
            targetWidth * EmusPerPixel,
            targetHeight * EmusPerPixel);
    }

    private static void EnsureDrawingReference(WorksheetPart worksheetPart, DrawingsPart drawingsPart)
    {
        var drawingReference = worksheetPart.Worksheet.Elements<DocumentFormat.OpenXml.Spreadsheet.Drawing>().FirstOrDefault();
        var drawingRelationshipId = worksheetPart.GetIdOfPart(drawingsPart);
        if (drawingReference == null)
        {
            worksheetPart.Worksheet.Append(new DocumentFormat.OpenXml.Spreadsheet.Drawing { Id = drawingRelationshipId });
        }
        else
        {
            drawingReference.Id = drawingRelationshipId;
        }
    }

    private static uint GetNextPictureId(Xdr.WorksheetDrawing worksheetDrawing) =>
        worksheetDrawing
            .Descendants<Xdr.NonVisualDrawingProperties>()
            .Select(property => property.Id?.Value ?? 0U)
            .DefaultIfEmpty(0U)
            .Max() + 1U;

    private static WorksheetPart GetWorksheetPart(WorkbookPart workbookPart, string sheetName)
    {
        var sheet = workbookPart.Workbook.Sheets?
            .Elements<Sheet>()
            .FirstOrDefault(candidate => string.Equals(candidate.Name?.Value, sheetName, StringComparison.OrdinalIgnoreCase))
            ?? workbookPart.Workbook.Sheets?.Elements<Sheet>().FirstOrDefault()
            ?? throw new InvalidOperationException("No worksheet was found in the workbook template.");

        return (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
    }

    private async Task<ClothingItem?> GetVisibleItemAsync(Guid clothingItemId, CancellationToken cancellationToken)
    {
        var currentUser = _currentUserService.GetCurrentUser();
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            return null;
        }

        var query = _dbContext.ClothingItems
            .AsNoTracking()
            .Include(item => item.ImageAttachments)
            .Include(item => item.FabricEntries)
            .Include(item => item.ModificationNotes)
            .Include(item => item.ApprovalRecords);
        IQueryable<ClothingItem> visibleQuery = query;

        if (!currentUser.IsAdmin)
        {
            visibleQuery = visibleQuery.Where(item => item.OwnerId == currentUser.UserId);
        }

        return await visibleQuery.FirstOrDefaultAsync(item => item.Id == clothingItemId, cancellationToken);
    }

    private async Task<byte[]?> ReadLegacyImageAsync(string relativePath, CancellationToken cancellationToken)
    {
        // Legacy image support for desktop - check if ImageStoragePath has the file
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var normalized = relativePath.TrimStart('/', '\\')
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        // Try ImageStoragePath first, then check if it exists as absolute path
        var imagePath = Path.Combine(_dataPathProvider.ImageStoragePath, normalized);
        if (File.Exists(imagePath))
        {
            return await File.ReadAllBytesAsync(imagePath, cancellationToken);
        }

        // Try as absolute path
        if (Path.IsPathRooted(normalized) && File.Exists(normalized))
        {
            return await File.ReadAllBytesAsync(normalized, cancellationToken);
        }

        return null;
    }

    private string GetTemplatePath() =>
        Path.Combine(_dataPathProvider.TemplatePath, _templateDefinition.RelativeTemplatePath);

    private static void SetCellText(Worksheet worksheet, string cellReference, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            ClearCellValue(worksheet, cellReference);
            return;
        }

        var cell = GetOrCreateCell(worksheet, cellReference);
        cell.CellFormula = null;
        cell.CellValue = null;
        cell.DataType = CellValues.InlineString;
        cell.InlineString = new InlineString(
            new DocumentFormat.OpenXml.Spreadsheet.Text(value)
            {
                Space = SpaceProcessingModeValues.Preserve
            });
    }

    private static void ApplyWrappedTextStyle(
        WorkbookPart workbookPart,
        Worksheet worksheet,
        string cellReference,
        HorizontalAlignmentValues? horizontalAlignment = null,
        VerticalAlignmentValues? verticalAlignment = null)
    {
        var cell = GetOrCreateCell(worksheet, cellReference);
        var stylesheet = workbookPart.WorkbookStylesPart?.Stylesheet;
        if (stylesheet?.CellFormats == null)
        {
            return;
        }

        var baseStyleIndex = cell.StyleIndex?.Value ?? 0U;
        if (baseStyleIndex >= stylesheet.CellFormats.Count())
        {
            return;
        }

        var wrappedStyleIndex = EnsureWrappedCellFormat(
            stylesheet,
            (int)baseStyleIndex,
            horizontalAlignment,
            verticalAlignment);

        cell.StyleIndex = (uint)wrappedStyleIndex;
        stylesheet.Save();
    }

    private static void ClearCellValue(Worksheet worksheet, string cellReference)
    {
        var cell = GetOrCreateCell(worksheet, cellReference);
        cell.CellFormula = null;
        cell.CellValue = null;
        cell.DataType = null;
        cell.InlineString = null;
    }

    private static Cell GetOrCreateCell(Worksheet worksheet, string cellReference)
    {
        var rowIndex = GetRowIndex(cellReference);
        var row = GetOrCreateRow(worksheet, rowIndex);

        var existingCell = row.Elements<Cell>()
            .FirstOrDefault(cell => string.Equals(cell.CellReference?.Value, cellReference, StringComparison.OrdinalIgnoreCase));
        if (existingCell != null)
        {
            return existingCell;
        }

        Cell? refCell = null;
        foreach (var cell in row.Elements<Cell>())
        {
            if (CompareCellReferences(cell.CellReference?.Value, cellReference) > 0)
            {
                refCell = cell;
                break;
            }
        }

        var newCell = new Cell
        {
            CellReference = cellReference
        };

        row.InsertBefore(newCell, refCell);
        return newCell;
    }

    private static Row GetOrCreateRow(Worksheet worksheet, uint rowIndex)
    {
        var sheetData = worksheet.GetFirstChild<SheetData>() ?? worksheet.AppendChild(new SheetData());
        var row = sheetData.Elements<Row>().FirstOrDefault(candidate => candidate.RowIndex?.Value == rowIndex);
        if (row != null)
        {
            return row;
        }

        var refRow = sheetData.Elements<Row>().FirstOrDefault(candidate => candidate.RowIndex?.Value > rowIndex);
        var newRow = new Row
        {
            RowIndex = rowIndex
        };

        sheetData.InsertBefore(newRow, refRow);
        return newRow;
    }

    private static int CompareCellReferences(string? leftReference, string? rightReference)
    {
        if (string.IsNullOrWhiteSpace(leftReference))
        {
            return -1;
        }

        if (string.IsNullOrWhiteSpace(rightReference))
        {
            return 1;
        }

        var leftColumn = GetColumnIndex(leftReference);
        var rightColumn = GetColumnIndex(rightReference);
        if (leftColumn != rightColumn)
        {
            return leftColumn.CompareTo(rightColumn);
        }

        return GetRowIndex(leftReference).CompareTo(GetRowIndex(rightReference));
    }

    private static uint GetRowIndex(string cellReference)
    {
        var digits = new string(cellReference.Where(char.IsDigit).ToArray());
        return uint.TryParse(digits, out var rowIndex) ? rowIndex : 1U;
    }

    private static int GetColumnIndex(string cellReference)
    {
        var letters = new string(cellReference.Where(char.IsLetter).ToArray()).ToUpperInvariant();
        var columnIndex = 0;
        foreach (var letter in letters)
        {
            columnIndex = (columnIndex * 26) + (letter - 'A' + 1);
        }

        return columnIndex;
    }

    private static string GetColumnLetter(int columnIndex)
    {
        var result = string.Empty;
        while (columnIndex > 0)
        {
            var mod = (columnIndex - 1) % 26;
            result = (char)('A' + mod) + result;
            columnIndex = (columnIndex - mod) / 26;
        }
        return result;
    }

    private static int SumColumnWidths(Worksheet worksheet, int startColumnIndex, int endColumnIndex)
    {
        if (endColumnIndex < startColumnIndex)
        {
            return 0;
        }

        var total = 0;
        for (var columnIndex = startColumnIndex; columnIndex <= endColumnIndex; columnIndex++)
        {
            total += ColumnWidthToPixels(GetColumnWidth(worksheet, columnIndex));
        }

        return total;
    }

    private static int SumRowHeights(Worksheet worksheet, int startRowIndex, int endRowIndex)
    {
        if (endRowIndex < startRowIndex)
        {
            return 0;
        }

        var total = 0;
        for (var rowIndex = startRowIndex; rowIndex <= endRowIndex; rowIndex++)
        {
            total += RowHeightToPixels(GetRowHeight(worksheet, rowIndex));
        }

        return total;
    }

    private static double GetColumnWidth(Worksheet worksheet, int columnIndex)
    {
        var columns = worksheet.Elements<Columns>().FirstOrDefault();
        if (columns != null)
        {
            foreach (var column in columns.Elements<Column>())
            {
                var min = column.Min?.Value ?? 1U;
                var max = column.Max?.Value ?? min;
                if (columnIndex >= min && columnIndex <= max && column.Width?.Value is double width)
                {
                    return width;
                }
            }
        }

        return worksheet.SheetFormatProperties?.DefaultColumnWidth?.Value ?? 8.43D;
    }

    private static double GetRowHeight(Worksheet worksheet, int rowIndex)
    {
        var row = worksheet.GetFirstChild<SheetData>()?
            .Elements<Row>()
            .FirstOrDefault(candidate => candidate.RowIndex?.Value == (uint)rowIndex);
        if (row?.Height?.Value is double height)
        {
            return height;
        }

        return worksheet.SheetFormatProperties?.DefaultRowHeight?.Value ?? 15D;
    }

    private static int ColumnWidthToPixels(double width)
    {
        if (width <= 0)
        {
            return 0;
        }

        return (int)Math.Truncate(((256D * width + Math.Truncate(128D / 7D)) / 256D) * 7D);
    }

    private static int RowHeightToPixels(double heightInPoints)
    {
        if (heightInPoints <= 0)
        {
            return 0;
        }

        return (int)Math.Round(heightInPoints * 96D / 72D);
    }

    private static string BuildQuarterDisplay(int year, string? season)
    {
        if (string.IsNullOrWhiteSpace(season))
        {
            return year.ToString(CultureInfo.InvariantCulture);
        }

        var trimmedSeason = season.Trim();
        if (LooksLikeQuarterCode(trimmedSeason))
        {
            return trimmedSeason;
        }

        if (trimmedSeason.All(char.IsDigit))
        {
            return year.ToString(CultureInfo.InvariantCulture);
        }

        var normalizedSeason = trimmedSeason.ToLowerInvariant();
        var code = trimmedSeason.Contains('春') ||
                   trimmedSeason.Contains('夏') ||
                   normalizedSeason.Contains("spring") ||
                   normalizedSeason.Contains("summer") ||
                   normalizedSeason == "ss"
            ? "SS"
            : trimmedSeason.Contains('秋') ||
              trimmedSeason.Contains('冬') ||
              normalizedSeason.Contains("autumn") ||
              normalizedSeason.Contains("fall") ||
              normalizedSeason.Contains("winter") ||
              normalizedSeason is "aw" or "fw"
                ? "AW"
                : null;

        return code == null
            ? trimmedSeason
            : $"{year}{code}";
    }

    private static bool LooksLikeQuarterCode(string season)
    {
        if (season.Length < 4)
        {
            return false;
        }

        var normalized = season.Replace(" ", string.Empty, StringComparison.Ordinal);
        var suffix = normalized.Length >= 2 ? normalized[^2..].ToUpperInvariant() : normalized.ToUpperInvariant();
        return int.TryParse(normalized[..Math.Min(4, normalized.Length)], out _) && (suffix == "SS" || suffix == "AW" || suffix == "FW");
    }

    private static string NormalizeImageContentType(string? declaredContentType, SpreadsheetImageMetadata? metadata)
    {
        if (!string.IsNullOrWhiteSpace(metadata?.ContentType))
        {
            return metadata.Value.ContentType;
        }

        if (string.IsNullOrWhiteSpace(declaredContentType))
        {
            return "image/png";
        }

        return string.Equals(declaredContentType, "image/jpg", StringComparison.OrdinalIgnoreCase)
            ? "image/jpeg"
            : declaredContentType.Trim();
    }

    private List<FabricEntry> GetTemplateFabricEntries(ClothingItem item)
    {
        if (item.FabricEntries.Count > 0)
        {
            return item.FabricEntries
                .OrderBy(entry => entry.SortOrder)
                .ThenBy(entry => entry.MaterialName)
                .Take(_templateDefinition.FabricBlocks.Count)
                .ToList();
        }

        if (string.IsNullOrWhiteSpace(item.FabricInformation))
        {
            return new List<FabricEntry>();
        }

        return new List<FabricEntry>
        {
            new()
            {
                MaterialName = item.FabricInformation.Trim()
            }
        };
    }

    private string BuildModificationNotesText(ClothingItem item)
    {
        List<string> sections = new();

        if (!string.IsNullOrWhiteSpace(item.Description))
        {
            sections.Add($"款式说明：{item.Description.Trim()}");
        }

        foreach (var note in item.ModificationNotes
                     .OrderByDescending(note => note.CreatedAtUtc)
                     .Take(5))
        {
            sections.Add(
                $"{note.CreatedAtUtc.ToLocalTime():MM/dd} {note.CreatedByDisplayName}：{note.Content.Trim()}");
        }

        if (sections.Count == 0)
        {
            sections.Add("暂无修改意见");
        }

        return NormalizeMultilineText(sections, _templateDefinition.ModificationNotesMaxCharacters);
    }

    private string BuildSummaryText(ClothingItem item)
    {
        var lines = new List<string>
        {
            $"品名：{item.Title}",
            $"季度：{BuildQuarterDisplay(item.Year, item.Season)}",
            $"进度：{item.Progress.ToDisplayName()}",
            $"审批：{item.ApprovalStatus.ToDisplayName()}",
            $"负责人：{item.OwnerDisplayName}"
        };

        if (!string.IsNullOrWhiteSpace(item.Description))
        {
            lines.Add($"补充说明：{item.Description.Trim()}");
        }

        if (item.FabricEntries.Count > 0)
        {
            lines.Add($"面料数：{item.FabricEntries.Count}");
        }

        return NormalizeMultilineText(lines, _templateDefinition.SummaryMaxCharacters);
    }

    private string BuildFabricBlockText(string label, FabricEntry? entry)
    {
        if (entry == null)
        {
            return $"{label}：";
        }

        var lines = new List<string> { $"{label}：" };

        if (!string.IsNullOrWhiteSpace(entry.MaterialName))
        {
            lines.Add(entry.MaterialName.Trim());
        }

        if (!string.IsNullOrWhiteSpace(entry.Specification))
        {
            lines.Add(entry.Specification.Trim());
        }

        if (!string.IsNullOrWhiteSpace(entry.Remark))
        {
            lines.Add(entry.Remark.Trim());
        }

        return NormalizeMultilineText(lines, _templateDefinition.FabricBlockMaxCharacters);
    }

    private static string BuildApprovalSummaryText(ClothingItem item)
    {
        var latestRecord = item.ApprovalRecords
            .OrderByDescending(record => record.CreatedAtUtc)
            .FirstOrDefault();

        if (latestRecord == null)
        {
            return $"设计总监审批：{item.ApprovalStatus.ToDisplayName()}";
        }

        var summary = $"设计总监审批：{item.ApprovalStatus.ToDisplayName()}";
        if (!string.IsNullOrWhiteSpace(latestRecord.Comment))
        {
            summary += $" / {latestRecord.Comment.Trim()}";
        }

        return summary;
    }

    private static string NormalizeSingleLineText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();

        return normalized.Length <= maxLength
            ? normalized
            : $"{normalized[..Math.Max(0, maxLength - 1)]}…";
    }

    private static string NormalizeMultilineText(IEnumerable<string> lines, int maxCharacters)
    {
        var builder = new List<string>();
        var remainingCharacters = maxCharacters;
        const string lineBreak = "\n";

        foreach (var line in lines
                     .Select(line => (line ?? string.Empty).Trim())
                     .Where(line => !string.IsNullOrWhiteSpace(line)))
        {
            if (remainingCharacters <= 0)
            {
                break;
            }

            var normalizedLine = line.Replace("\r", string.Empty, StringComparison.Ordinal)
                .Replace("\n", " / ", StringComparison.Ordinal);

            if (normalizedLine.Length > remainingCharacters)
            {
                normalizedLine = remainingCharacters > 1
                    ? $"{normalizedLine[..(remainingCharacters - 1)]}…"
                    : string.Empty;
            }

            if (string.IsNullOrWhiteSpace(normalizedLine))
            {
                break;
            }

            builder.Add(normalizedLine);
            remainingCharacters -= normalizedLine.Length + lineBreak.Length;
        }

        return builder.Count == 0
            ? string.Empty
            : string.Join(lineBreak, builder);
    }

    private static int EnsureWrappedCellFormat(
        Stylesheet stylesheet,
        int baseStyleIndex,
        HorizontalAlignmentValues? horizontalAlignment,
        VerticalAlignmentValues? verticalAlignment)
    {
        var existingFormats = stylesheet.CellFormats?.Elements<CellFormat>().ToList()
            ?? throw new InvalidOperationException("Workbook stylesheet does not contain cell formats.");
        var baseFormat = existingFormats[baseStyleIndex];

        var alignment = (Alignment?)baseFormat.Alignment?.CloneNode(true) ?? new Alignment();
        alignment.WrapText = true;
        if (horizontalAlignment.HasValue)
        {
            alignment.Horizontal = horizontalAlignment.Value;
        }

        if (verticalAlignment.HasValue)
        {
            alignment.Vertical = verticalAlignment.Value;
        }

        foreach (var candidate in existingFormats.Select((format, index) => new { format, index }))
        {
            if (AreCellFormatsEquivalent(candidate.format, baseFormat, alignment))
            {
                return candidate.index;
            }
        }

        var newFormat = (CellFormat)baseFormat.CloneNode(true);
        newFormat.Alignment = alignment;
        newFormat.ApplyAlignment = true;
        stylesheet.CellFormats!.AppendChild(newFormat);
        stylesheet.CellFormats.Count = (uint)stylesheet.CellFormats.Count();
        return existingFormats.Count;
    }

    private static bool AreCellFormatsEquivalent(CellFormat candidate, CellFormat baseFormat, Alignment targetAlignment)
    {
        if (candidate.NumberFormatId?.Value != baseFormat.NumberFormatId?.Value ||
            candidate.FontId?.Value != baseFormat.FontId?.Value ||
            candidate.FillId?.Value != baseFormat.FillId?.Value ||
            candidate.BorderId?.Value != baseFormat.BorderId?.Value ||
            candidate.FormatId?.Value != baseFormat.FormatId?.Value)
        {
            return false;
        }

        var candidateAlignment = candidate.Alignment;
        if (candidateAlignment == null)
        {
            return false;
        }

        return candidateAlignment.WrapText?.Value == true &&
               candidateAlignment.Horizontal?.Value == targetAlignment.Horizontal?.Value &&
               candidateAlignment.Vertical?.Value == targetAlignment.Vertical?.Value;
    }

    private readonly record struct WorkbookImagePayload(
        byte[] BinaryContent,
        string ContentType,
        SpreadsheetImageMetadata? Metadata);

    private readonly record struct ImagePlacementResult(
        long XEmu,
        long YEmu,
        long WidthEmu,
        long HeightEmu);
}
