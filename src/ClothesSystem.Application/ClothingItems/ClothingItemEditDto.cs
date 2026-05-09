using System.ComponentModel.DataAnnotations;
using ClothesSystem.Domain.Enums;

namespace ClothesSystem.Application.ClothingItems;

public class ClothingItemEditDto
{
    [Required(ErrorMessage = "请填写款号")]
    [StringLength(50)]
    public string StyleNumber { get; set; } = string.Empty;

    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [Range(2000, 2100, ErrorMessage = "年份范围需在 2000 到 2100 之间")]
    public int Year { get; set; } = DateTime.UtcNow.Year;

    [Required(ErrorMessage = "请填写季节或时间标签")]
    [StringLength(50)]
    public string Season { get; set; } = string.Empty;

    [Required(ErrorMessage = "请选择当前进度")]
    public ClothingProgressStatus Progress { get; set; } = ClothingProgressStatus.PatternMaking;

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(450)]
    public string? OwnerId { get; set; }

    public List<FabricEntryEditDto> FabricEntries { get; set; } = new();

    public List<ClothingImageAttachmentDto> ExistingImageAttachments { get; set; } = new();

    public List<ClothingImageAttachmentInputDto> NewImageAttachments { get; set; } = new();

    public List<Guid> AttachmentIdsToDelete { get; set; } = new();
}
