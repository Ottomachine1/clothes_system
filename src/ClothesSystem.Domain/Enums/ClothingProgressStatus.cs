using System.ComponentModel.DataAnnotations;

namespace ClothesSystem.Domain.Enums;

public enum ClothingProgressStatus
{
    [Display(Name = "打板")]
    PatternMaking = 1,

    [Display(Name = "样衣")]
    SampleMaking = 2,

    [Display(Name = "改版")]
    Revision = 3,

    [Display(Name = "确认")]
    Confirmed = 4,

    [Display(Name = "PASS")]
    Pass = 5,

    [Display(Name = "待定")]
    OnHold = 6
}

public static class ClothingProgressStatusExtensions
{
    public static string ToDisplayName(this ClothingProgressStatus status) =>
        status switch
        {
            ClothingProgressStatus.PatternMaking => "打板",
            ClothingProgressStatus.SampleMaking => "样衣",
            ClothingProgressStatus.Revision => "改版",
            ClothingProgressStatus.Confirmed => "确认",
            ClothingProgressStatus.Pass => "PASS",
            ClothingProgressStatus.OnHold => "待定",
            _ => status.ToString()
        };
}
