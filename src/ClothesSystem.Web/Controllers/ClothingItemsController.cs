using ClothesSystem.Application.ClothingItems;
using ClothesSystem.Application.Common.Interfaces;
using ClothesSystem.Domain.Enums;
using ClothesSystem.Web.Authorization;
using ClothesSystem.Web.Models.ClothingItems;
using ClothesSystem.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.RateLimiting;

namespace ClothesSystem.Web.Controllers;

[Authorize]
[EnableRateLimiting("api")]
public class ClothingItemsController : Controller
{
    private readonly ILogger<ClothingItemsController> _logger;
    private readonly IClothingService _clothingService;
    private readonly IClothingWorkbookTemplateService _workbookTemplateService;
    private readonly IUserDirectoryService _userDirectoryService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILocalImageStorageService _imageStorageService;

    public ClothingItemsController(
        ILogger<ClothingItemsController> logger,
        IClothingService clothingService,
        IClothingWorkbookTemplateService workbookTemplateService,
        IUserDirectoryService userDirectoryService,
        ICurrentUserService currentUserService,
        ILocalImageStorageService imageStorageService)
    {
        _logger = logger;
        _clothingService = clothingService;
        _workbookTemplateService = workbookTemplateService;
        _userDirectoryService = userDirectoryService;
        _currentUserService = currentUserService;
        _imageStorageService = imageStorageService;
    }

    public async Task<IActionResult> Index([FromQuery] ClothingSearchRequest search, CancellationToken cancellationToken)
    {
        var isAdmin = User.IsInRole(ApplicationRoles.Admin);
        var viewModel = new ClothingItemSearchViewModel
        {
            Search = search,
            Result = await _clothingService.SearchAsync(search, cancellationToken),
            ProgressOptions = BuildProgressOptions(search.Progress),
            OwnerOptions = await BuildOwnerOptionsAsync(search.OwnerId, isAdmin, cancellationToken),
            IsAdmin = isAdmin
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var item = await _clothingService.GetDetailsAsync(id, cancellationToken);
        if (item == null)
        {
            return NotFound();
        }

        var currentUser = _currentUserService.GetCurrentUser();

        return View(new ClothingItemDetailsViewModel
        {
            Item = item,
            IsAdmin = currentUser.IsAdmin,
            CanSubmitApproval = (currentUser.IsAdmin || currentUser.UserId == item.OwnerId) && item.ApprovalStatus != ApprovalStatus.Pending,
            CanReviewApproval = currentUser.IsAdmin && item.ApprovalStatus == ApprovalStatus.Pending
        });
    }

    [HttpGet]
    public async Task<IActionResult> Image(Guid id, CancellationToken cancellationToken)
    {
        var imageFile = await _clothingService.GetImageFileAsync(id, cancellationToken);
        if (imageFile == null)
        {
            return NotFound();
        }

        if (imageFile.BinaryContent is { Length: > 0 } && !string.IsNullOrWhiteSpace(imageFile.ContentType))
        {
            return File(imageFile.BinaryContent, imageFile.ContentType);
        }

        if (!string.IsNullOrWhiteSpace(imageFile.LegacyFilePath))
        {
            return Redirect(imageFile.LegacyFilePath);
        }

        return NotFound();
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new ClothingItemFormViewModel
        {
            StyleNumber = await _clothingService.GenerateNextStyleNumberAsync(cancellationToken)
        };
        await PopulateFormOptionsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClothingItemFormViewModel model, CancellationToken cancellationToken)
    {
        NormalizeFormCollections(model);
        await EnsureGeneratedStyleNumberAsync(model, cancellationToken);
        await PopulateFormOptionsAsync(model, cancellationToken);
        ValidateCreateImages(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        List<ClothingImageAttachmentInputDto> uploadedAttachments = new();
        try
        {
            uploadedAttachments = await SaveAttachmentsAsync(model.NewImageFiles, cancellationToken);
            var id = await _clothingService.CreateAsync(ToEditDto(model, uploadedAttachments), cancellationToken);
            TempData["StatusMessage"] = "款式已创建。";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "创建款式时发生未处理异常。");
            ModelState.AddModelError(string.Empty, "创建款式时发生异常，请检查输入后重试。");
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var item = await _clothingService.GetEditAsync(id, cancellationToken);
        if (item == null)
        {
            return NotFound();
        }

        var model = new ClothingItemFormViewModel
        {
            Id = id,
            StyleNumber = item.StyleNumber,
            Title = item.Title,
            Year = item.Year,
            Season = item.Season,
            Progress = item.Progress,
            Description = item.Description,
            OwnerId = item.OwnerId,
            FabricEntries = item.FabricEntries
                .Select(entry => new FabricEntryRowViewModel
                {
                    Id = entry.Id,
                    MaterialName = entry.MaterialName,
                    Specification = entry.Specification,
                    Remark = entry.Remark
                })
                .ToList(),
            ExistingAttachments = item.ExistingImageAttachments
                .Select(attachment => new ExistingImageAttachmentViewModel
                {
                    Id = attachment.Id,
                    FilePath = attachment.FilePath,
                    OriginalFileName = attachment.OriginalFileName,
                    UploadedByDisplayName = attachment.UploadedByDisplayName,
                    UploadedAtUtc = attachment.UploadedAtUtc
                })
                .ToList()
        };

        if (model.FabricEntries.Count == 0)
        {
            model.FabricEntries.Add(new FabricEntryRowViewModel());
        }

        await PopulateFormOptionsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ClothingItemFormViewModel model, CancellationToken cancellationToken)
    {
        if (model.Id != id)
        {
            return BadRequest();
        }

        NormalizeFormCollections(model);

        var existingItem = await _clothingService.GetEditAsync(id, cancellationToken);
        if (existingItem == null)
        {
            return NotFound();
        }

        model.ExistingAttachments = existingItem.ExistingImageAttachments
            .Select(attachment => new ExistingImageAttachmentViewModel
            {
                Id = attachment.Id,
                FilePath = attachment.FilePath,
                OriginalFileName = attachment.OriginalFileName,
                UploadedByDisplayName = attachment.UploadedByDisplayName,
                UploadedAtUtc = attachment.UploadedAtUtc
            })
            .ToList();

        await PopulateFormOptionsAsync(model, cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        List<ClothingImageAttachmentInputDto> uploadedAttachments = new();
        try
        {
            uploadedAttachments = await SaveAttachmentsAsync(model.NewImageFiles, cancellationToken);
            var updated = await _clothingService.UpdateAsync(id, ToEditDto(model, uploadedAttachments), cancellationToken);

            if (!updated)
            {
                return NotFound();
            }

            TempData["StatusMessage"] = "款式信息已更新。";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "保存编辑款式时发生未处理异常，款式 Id: {ClothingItemId}", id);
            ModelState.AddModelError(string.Empty, "保存款式时发生异常，请稍后重试。");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddNote(Guid id, string content, CancellationToken cancellationToken)
    {
        var success = await _clothingService.AddNoteAsync(id, content, cancellationToken);
        TempData["StatusMessage"] = success ? "修改意见已记录。" : "请输入有效的修改意见。";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitApproval(Guid id, string? comment, CancellationToken cancellationToken)
    {
        var success = await _clothingService.SubmitForApprovalAsync(id, comment, cancellationToken);
        TempData["StatusMessage"] = success ? "该款式已提交审批。" : "当前状态下无法重复提交审批。";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewApproval(Guid id, ApprovalAction decision, string? comment, CancellationToken cancellationToken)
    {
        var success = await _clothingService.ReviewApprovalAsync(id, decision, comment, cancellationToken);
        TempData["StatusMessage"] = success ? $"审批操作已完成：{decision.ToDisplayName()}。" : "当前状态下无法执行该审批动作。";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> ExportExcelTemplate(Guid id, CancellationToken cancellationToken)
    {
        var file = await _workbookTemplateService.ExportPlaceholderAsync(id, cancellationToken);
        if (file == null)
        {
            return NotFound();
        }

        return File(file.Content, file.ContentType, file.FileName);
    }

    public async Task<IActionResult> ExportDesignSheet(Guid id, CancellationToken cancellationToken)
    {
        var file = await _workbookTemplateService.ExportDesignSheetAsync(id, cancellationToken);
        if (file == null)
        {
            return NotFound();
        }

        return File(file.Content, file.ContentType, file.FileName);
    }

    public async Task<IActionResult> PrintDesignSheet(Guid id, CancellationToken cancellationToken)
    {
        var item = await _clothingService.GetDetailsAsync(id, cancellationToken);
        if (item == null)
        {
            return NotFound();
        }

        var primaryImage = item.ImageAttachments
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.UploadedAtUtc)
            .FirstOrDefault();

        var secondaryImage = item.ImageAttachments
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.UploadedAtUtc)
            .Skip(1)
            .FirstOrDefault();

        return View(new ClothingItemPrintViewModel
        {
            Item = item,
            PrimaryImageBase64 = GetImageBase64(primaryImage),
            PrimaryImageContentType = InferContentType(primaryImage?.OriginalFileName),
            SecondaryImageBase64 = GetImageBase64(secondaryImage),
            SecondaryImageContentType = InferContentType(secondaryImage?.OriginalFileName)
        });
    }

    private static string? GetImageBase64(ClothingImageAttachmentDto? attachment)
    {
        if (attachment == null) return null;
        var bytes = attachment.BinaryContent;
        if (bytes == null || bytes.Length == 0) return null;
        return Convert.ToBase64String(bytes);
    }

    private static string InferContentType(string? fileName) =>
        string.IsNullOrWhiteSpace(fileName)
            ? "image/png"
            : Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                _ => "image/png"
            };

    public IActionResult ImportDesignSheet()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportDesignSheet(IFormFile? designSheetFile, CancellationToken cancellationToken)
    {
        if (designSheetFile == null || designSheetFile.Length == 0)
        {
            TempData["StatusMessage"] = "请先选择要导入的设计单文件。";
            return RedirectToAction(nameof(ImportDesignSheet));
        }

        await using var stream = designSheetFile.OpenReadStream();
        var result = await _workbookTemplateService.ImportDesignSheetAsync(designSheetFile.FileName, stream, cancellationToken);

        if (result.Success && result.NewItemId.HasValue)
        {
            TempData["StatusMessage"] = result.Message;
            return RedirectToAction(nameof(Details), new { id = result.NewItemId.Value });
        }

        TempData["StatusMessage"] = result.Message;
        return RedirectToAction(nameof(ImportDesignSheet));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportExcelTemplate(Guid id, IFormFile? templateFile, CancellationToken cancellationToken)
    {
        if (templateFile == null || templateFile.Length == 0)
        {
            TempData["StatusMessage"] = "请先选择要导入的 Excel 文件。";
            return RedirectToAction(nameof(Details), new { id });
        }

        await using var stream = templateFile.OpenReadStream();
        var result = await _workbookTemplateService.ImportPlaceholderAsync(id, templateFile.FileName, stream, cancellationToken);
        TempData["StatusMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var details = await _clothingService.GetDetailsAsync(id, cancellationToken);
        if (details == null)
        {
            return NotFound();
        }

        var deleted = await _clothingService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "款式已删除。";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateFormOptionsAsync(ClothingItemFormViewModel model, CancellationToken cancellationToken)
    {
        NormalizeFormCollections(model);

        if (model.IsCreateMode && string.IsNullOrWhiteSpace(model.StyleNumber))
        {
            model.StyleNumber = await _clothingService.GenerateNextStyleNumberAsync(cancellationToken);
            ModelState.Remove(nameof(model.StyleNumber));
        }

        var isAdmin = User.IsInRole(ApplicationRoles.Admin);
        model.IsAdmin = isAdmin;
        model.ProgressOptions = BuildProgressOptions(model.Progress);
        model.OwnerOptions = await BuildOwnerOptionsAsync(model.OwnerId, isAdmin, cancellationToken);
        model.YearOptions = BuildYearOptions(model.Year);
        model.SeasonOptions = BuildSeasonOptions(model.Season);

        if (model.FabricEntries.Count == 0)
        {
            model.FabricEntries.Add(new FabricEntryRowViewModel());
        }

        if (!isAdmin)
        {
            model.OwnerId = _currentUserService.GetCurrentUser().UserId;
        }
    }

    private static void NormalizeFormCollections(ClothingItemFormViewModel model)
    {
        model.FabricEntries ??= new List<FabricEntryRowViewModel>();
        model.ExistingAttachments ??= new List<ExistingImageAttachmentViewModel>();
        model.AttachmentIdsToDelete ??= new List<Guid>();
        model.NewImageFiles ??= new List<IFormFile>();
    }

    private static IReadOnlyCollection<SelectListItem> BuildProgressOptions(ClothingProgressStatus? selectedValue) =>
        Enum.GetValues<ClothingProgressStatus>()
            .Select(status => new SelectListItem
            {
                Value = status.ToString(),
                Text = status.ToDisplayName(),
                Selected = selectedValue.HasValue && selectedValue.Value == status
            })
            .ToList();

    private static IReadOnlyCollection<SelectListItem> BuildYearOptions(int selectedYear)
    {
        var currentYear = DateTime.Now.Year;
        var years = Enumerable.Range(currentYear - 10, 16).ToList();
        if (!years.Contains(selectedYear))
        {
            years.Add(selectedYear);
        }

        return years
            .OrderByDescending(year => year)
            .Select(year => new SelectListItem
            {
                Value = year.ToString(),
                Text = year.ToString(),
                Selected = year == selectedYear
            })
            .ToList();
    }

    private static IReadOnlyCollection<SelectListItem> BuildSeasonOptions(string? selectedSeason)
    {
        var seasons = new[]
        {
            "春季",
            "夏季",
            "秋季",
            "冬季",
            "春夏",
            "秋冬",
            "春季波段",
            "夏季波段",
            "秋冬季波段",
            "全年"
        };

        return seasons
            .Select(season => new SelectListItem
            {
                Value = season,
                Text = season,
                Selected = season == selectedSeason
            })
            .ToList();
    }

    private async Task<IReadOnlyCollection<SelectListItem>> BuildOwnerOptionsAsync(
        string? selectedOwnerId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        if (!isAdmin)
        {
            return Array.Empty<SelectListItem>();
        }

        var owners = await _userDirectoryService.GetOwnerOptionsAsync(cancellationToken);
        return owners
            .Select(owner => new SelectListItem
            {
                Value = owner.UserId,
                Text = $"{owner.DisplayName} ({owner.Email})",
                Selected = owner.UserId == selectedOwnerId
            })
            .ToList();
    }

    private async Task<List<ClothingImageAttachmentInputDto>> SaveAttachmentsAsync(
        IEnumerable<IFormFile>? files,
        CancellationToken cancellationToken)
    {
        List<ClothingImageAttachmentInputDto> savedAttachments = new();

        foreach (var file in (files ?? Array.Empty<IFormFile>()).Where(file => file != null && file.Length > 0))
        {
            var attachment = await _imageStorageService.SaveNewAsync(file, cancellationToken);
            if (attachment != null)
            {
                savedAttachments.Add(attachment);
            }
        }

        return savedAttachments;
    }

    private async Task EnsureGeneratedStyleNumberAsync(ClothingItemFormViewModel model, CancellationToken cancellationToken)
    {
        if (!model.IsCreateMode || !string.IsNullOrWhiteSpace(model.StyleNumber))
        {
            return;
        }

        model.StyleNumber = await _clothingService.GenerateNextStyleNumberAsync(cancellationToken);
        ModelState.Remove(nameof(model.StyleNumber));
    }

    private void ValidateCreateImages(ClothingItemFormViewModel model)
    {
        if (!model.IsCreateMode)
        {
            return;
        }

        var hasUploadedImages = model.NewImageFiles?.Any(file => file != null && file.Length > 0) == true;
        if (!hasUploadedImages)
        {
            ModelState.AddModelError(nameof(model.NewImageFiles), "首次创建款式时请至少上传一张图片。");
        }
    }

    private static ClothingItemEditDto ToEditDto(
        ClothingItemFormViewModel model,
        IReadOnlyCollection<ClothingImageAttachmentInputDto> uploadedAttachments) =>
        new()
        {
            StyleNumber = model.StyleNumber,
            Title = model.Title,
            Year = model.Year,
            Season = model.Season,
            Progress = model.Progress,
            Description = model.Description,
            OwnerId = model.OwnerId,
            FabricEntries = (model.FabricEntries ?? new List<FabricEntryRowViewModel>())
                .Select(entry => new FabricEntryEditDto
                {
                    Id = entry.Id,
                    MaterialName = entry.MaterialName,
                    Specification = entry.Specification,
                    Remark = entry.Remark
                })
                .ToList(),
            ExistingImageAttachments = (model.ExistingAttachments ?? new List<ExistingImageAttachmentViewModel>())
                .Select(attachment => new ClothingImageAttachmentDto
                {
                    Id = attachment.Id,
                    FilePath = attachment.FilePath,
                    OriginalFileName = attachment.OriginalFileName,
                    UploadedByDisplayName = attachment.UploadedByDisplayName,
                    UploadedAtUtc = attachment.UploadedAtUtc
                })
                .ToList(),
            NewImageAttachments = uploadedAttachments.ToList(),
            AttachmentIdsToDelete = (model.AttachmentIdsToDelete ?? new List<Guid>()).Distinct().ToList()
        };
}
