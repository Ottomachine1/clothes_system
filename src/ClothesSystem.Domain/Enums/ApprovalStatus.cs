using System.ComponentModel.DataAnnotations;

namespace ClothesSystem.Domain.Enums;

public enum ApprovalStatus
{
    [Display(Name = "草稿")]
    Draft = 1,

    [Display(Name = "审批中")]
    Pending = 2,

    [Display(Name = "已通过")]
    Approved = 3,

    [Display(Name = "已驳回")]
    Rejected = 4,

    [Display(Name = "待修改")]
    ChangesRequested = 5
}

public static class ApprovalStatusExtensions
{
    public static string ToDisplayName(this ApprovalStatus status) =>
        status switch
        {
            ApprovalStatus.Draft => "草稿",
            ApprovalStatus.Pending => "审批中",
            ApprovalStatus.Approved => "已通过",
            ApprovalStatus.Rejected => "已驳回",
            ApprovalStatus.ChangesRequested => "待修改",
            _ => status.ToString()
        };
}
