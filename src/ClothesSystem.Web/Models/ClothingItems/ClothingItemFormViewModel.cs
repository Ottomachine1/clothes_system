using System.ComponentModel.DataAnnotations;
using ClothesSystem.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClothesSystem.Web.Models.ClothingItems;

public class ClothingItemFormViewModel
{
    public Guid? Id { get; set; }

    public bool IsCreateMode => !Id.HasValue;

    [Required(ErrorMessage = "请填写款号")]
    [StringLength(50)]
    public string StyleNumber { get; set; } = string.Empty;

    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [Range(2000, 2100, ErrorMessage = "年份范围需在 2000 到 2100 之间")]
    public int Year { get; set; } = DateTime.Now.Year;

    [Required(ErrorMessage = "请填写季节或时间标签")]
    [StringLength(50)]
    public string Season { get; set; } = string.Empty;

    [Required(ErrorMessage = "请选择当前进度")]
    public ClothingProgressStatus Progress { get; set; } = ClothingProgressStatus.PatternMaking;

    [StringLength(2000)]
    public string? Description { get; set; }

    public string? OwnerId { get; set; }

    public List<FabricEntryRowViewModel> FabricEntries { get; set; } = new() { new() };

    public List<ExistingImageAttachmentViewModel> ExistingAttachments { get; set; } = new();

    public List<Guid> AttachmentIdsToDelete { get; set; } = new();

    public List<IFormFile>? NewImageFiles { get; set; }

    public bool IsAdmin { get; set; }

    public IReadOnlyCollection<SelectListItem> ProgressOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyCollection<SelectListItem> OwnerOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyCollection<SelectListItem> YearOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyCollection<SelectListItem> SeasonOptions { get; set; } = Array.Empty<SelectListItem>();
}
