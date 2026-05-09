namespace ClothesSystem.Domain.Enums;

public enum ApprovalAction
{
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
    ReturnedForChanges = 4
}

public static class ApprovalActionExtensions
{
    public static string ToDisplayName(this ApprovalAction action) =>
        action switch
        {
            ApprovalAction.Submitted => "提交审批",
            ApprovalAction.Approved => "审批通过",
            ApprovalAction.Rejected => "审批驳回",
            ApprovalAction.ReturnedForChanges => "打回修改",
            _ => action.ToString()
        };
}
