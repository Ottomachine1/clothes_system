using ClothesSystem.Application.Common.Interfaces;
using ClothesSystem.Application.Common.Models;
using ClothesSystem.Application.Common.Services;
using ClothesSystem.Domain.Entities;
using ClothesSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClothesSystem.Application.ClothingItems;

public class ClothingService : IClothingService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserDirectoryService _userDirectoryService;
    private readonly IStyleNumberGenerator _styleNumberGenerator;
    private readonly IClothingImageStorageService _imageStorageService;

    public ClothingService(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IUserDirectoryService userDirectoryService,
        IStyleNumberGenerator styleNumberGenerator,
        IClothingImageStorageService imageStorageService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _userDirectoryService = userDirectoryService;
        _styleNumberGenerator = styleNumberGenerator;
        _imageStorageService = imageStorageService;
    }

    public async Task<string> GenerateNextStyleNumberAsync(CancellationToken cancellationToken = default) =>
        await _styleNumberGenerator.GenerateNextStyleNumberAsync(cancellationToken);

    public async Task<ClothingDashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var currentUser = EnsureAuthenticatedUser();
        var visibleQuery = ApplyVisibility(_dbContext.ClothingItems.AsNoTracking(), currentUser);
        var myItemsQuery = _dbContext.ClothingItems.AsNoTracking()
            .Where(item => item.OwnerId == currentUser.UserId);

        var visibleItems = await visibleQuery.CountAsync(cancellationToken);
        var myItems = await myItemsQuery.CountAsync(cancellationToken);
        var activeItems = await visibleQuery.CountAsync(item => item.Progress != ClothingProgressStatus.Pass, cancellationToken);
        var completedItems = await visibleQuery.CountAsync(item => item.Progress == ClothingProgressStatus.Pass, cancellationToken);

        return new ClothingDashboardSummaryDto
        {
            VisibleItems = visibleItems,
            MyItems = myItems,
            ActiveItems = activeItems,
            CompletedItems = completedItems
        };
    }

    public async Task<PaginatedResult<ClothingItemListItemDto>> SearchAsync(
        ClothingSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUser = EnsureAuthenticatedUser();
        var query = ApplyVisibility(_dbContext.ClothingItems.AsNoTracking(), currentUser);

        if (!string.IsNullOrWhiteSpace(request.StyleNumber))
        {
            var styleNumber = request.StyleNumber.Trim();
            query = query.Where(item => item.StyleNumber.Contains(styleNumber));
        }

        if (request.Year.HasValue)
        {
            query = query.Where(item => item.Year == request.Year.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Season))
        {
            var season = request.Season.Trim();
            query = query.Where(item => item.Season.Contains(season));
        }

        if (request.Progress.HasValue)
        {
            query = query.Where(item => item.Progress == request.Progress.Value);
        }

        if (currentUser.IsAdmin && !string.IsNullOrWhiteSpace(request.OwnerId))
        {
            query = query.Where(item => item.OwnerId == request.OwnerId);
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(item =>
                item.Title.Contains(keyword) ||
                item.StyleNumber.Contains(keyword) ||
                item.Season.Contains(keyword) ||
                item.FabricInformation.Contains(keyword) ||
                (item.Description != null && item.Description.Contains(keyword)) ||
                item.ModificationNotes.Any(note => note.Content.Contains(keyword)) ||
                item.FabricEntries.Any(entry =>
                    entry.MaterialName.Contains(keyword) ||
                    entry.Specification.Contains(keyword) ||
                    entry.Remark.Contains(keyword)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var visibleItems = await query
            .OrderByDescending(item => item.UpdatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(item => new
            {
                item.Id,
                item.OwnerId,
                item.StyleNumber,
                item.Title,
                item.Year,
                item.Season,
                item.Progress,
                item.ApprovalStatus,
                item.FabricInformation,
                item.ImagePath,
                item.OwnerDisplayName,
                item.UpdatedAtUtc,
                CoverImageAttachmentId = item.ImageAttachments
                    .OrderBy(attachment => attachment.SortOrder)
                    .ThenBy(attachment => attachment.UploadedAtUtc)
                    .Select(attachment => (Guid?)attachment.Id)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var items = visibleItems
            .Select(item => new ClothingItemListItemDto
            {
                Id = item.Id,
                OwnerId = item.OwnerId,
                StyleNumber = item.StyleNumber,
                Title = item.Title,
                Year = item.Year,
                Season = item.Season,
                Progress = item.Progress,
                ProgressDisplayName = item.Progress.ToDisplayName(),
                ApprovalStatus = item.ApprovalStatus,
                ApprovalStatusDisplayName = item.ApprovalStatus.ToDisplayName(),
                FabricInformation = item.FabricInformation,
                ImagePath = item.CoverImageAttachmentId.HasValue
                    ? BuildImageUrl(item.CoverImageAttachmentId.Value)
                    : item.ImagePath,
                OwnerDisplayName = item.OwnerDisplayName,
                UpdatedAtUtc = item.UpdatedAtUtc
            })
            .ToList();

        var result = new PaginatedResult<ClothingItemListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        return result;
    }

    public async Task<ClothingImageFileDto?> GetImageFileAsync(Guid attachmentId, CancellationToken cancellationToken = default)
    {
        var currentUser = EnsureAuthenticatedUser();
        var attachment = await _dbContext.ClothingImageAttachments
            .AsNoTracking()
            .Include(item => item.ClothingItem)
            .FirstOrDefaultAsync(item => item.Id == attachmentId, cancellationToken);

        if (attachment?.ClothingItem == null)
        {
            return null;
        }

        if (!currentUser.IsAdmin && attachment.ClothingItem.OwnerId != currentUser.UserId)
        {
            return null;
        }

        var storedBinaryContent = string.IsNullOrWhiteSpace(attachment.FilePath)
            ? null
            : await _imageStorageService.ReadAsync(attachment.FilePath, cancellationToken);

        return new ClothingImageFileDto
        {
            LegacyFilePath = attachment.FilePath,
            OriginalFileName = attachment.OriginalFileName,
            ContentType = attachment.ContentType,
            BinaryContent = attachment.BinaryContent ?? storedBinaryContent
        };
    }

    public async Task<ClothingItemDetailDto?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUser = EnsureAuthenticatedUser();
        var item = await ApplyVisibility(
                _dbContext.ClothingItems
                    .AsNoTracking()
                    .Include(clothingItem => clothingItem.ModificationNotes)
                    .Include(clothingItem => clothingItem.FabricEntries)
                    .Include(clothingItem => clothingItem.ImageAttachments)
                    .Include(clothingItem => clothingItem.ApprovalRecords),
                currentUser)
            .FirstOrDefaultAsync(clothingItem => clothingItem.Id == id, cancellationToken);

        return item == null ? null : await MapDetailsAsync(item, cancellationToken);
    }

    public async Task<ClothingItemEditDto?> GetEditAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUser = EnsureAuthenticatedUser();
        var item = await ApplyVisibility(
                _dbContext.ClothingItems
                    .AsNoTracking()
                    .Include(clothingItem => clothingItem.FabricEntries)
                    .Include(clothingItem => clothingItem.ImageAttachments),
                currentUser)
            .FirstOrDefaultAsync(clothingItem => clothingItem.Id == id, cancellationToken);

        if (item == null)
        {
            return null;
        }

        return new ClothingItemEditDto
        {
            StyleNumber = item.StyleNumber,
            Title = item.Title,
            Year = item.Year,
            Season = item.Season,
            Progress = item.Progress,
            Description = item.Description,
            OwnerId = item.OwnerId,
            FabricEntries = BuildEditFabricEntries(item).ToList(),
            ExistingImageAttachments = await BuildAttachmentDtosAsync(item.ImageAttachments, cancellationToken)
        };
    }

    public async Task<Guid> CreateAsync(ClothingItemEditDto input, CancellationToken cancellationToken = default)
    {
        var currentUser = EnsureAuthenticatedUser();
        var owner = await ResolveOwnerAsync(input.OwnerId, currentUser, cancellationToken);
        var styleNumber = input.StyleNumber.Trim();

        if (await _dbContext.ClothingItems.AnyAsync(item => item.StyleNumber == styleNumber, cancellationToken))
        {
            throw new InvalidOperationException("该款号已存在，请使用新的款号。");
        }

        var fabricEntries = NormalizeFabricEntries(input.FabricEntries);

        var item = new ClothingItem
        {
            StyleNumber = styleNumber,
            Title = input.Title.Trim(),
            Year = input.Year,
            Season = input.Season.Trim(),
            Progress = input.Progress,
            ApprovalStatus = ApprovalStatus.Draft,
            FabricInformation = BuildFabricInformationSummary(fabricEntries),
            Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
            OwnerId = owner.UserId,
            OwnerDisplayName = owner.DisplayName,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        foreach (var fabricEntry in fabricEntries)
        {
            item.FabricEntries.Add(new FabricEntry
            {
                MaterialName = fabricEntry.MaterialName ?? string.Empty,
                Specification = fabricEntry.Specification ?? string.Empty,
                Remark = fabricEntry.Remark ?? string.Empty,
                SortOrder = item.FabricEntries.Count
            });
        }

        foreach (var attachment in NormalizeAttachments(input.NewImageAttachments))
        {
            var storedFilePath = await _imageStorageService.SaveAsync(
                attachment.BinaryContent,
                attachment.OriginalFileName,
                attachment.ContentType,
                cancellationToken);

            item.ImageAttachments.Add(new ClothingImageAttachment
            {
                FilePath = storedFilePath,
                ContentType = attachment.ContentType,
                OriginalFileName = attachment.OriginalFileName,
                UploadedByUserId = currentUser.UserId,
                UploadedByDisplayName = currentUser.DisplayName,
                UploadedAtUtc = DateTime.UtcNow,
                SortOrder = item.ImageAttachments.Count
            });
        }

        RefreshCoverImage(item);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            _dbContext.ClothingItems.Add(item);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return item.Id;
    }

    public async Task<bool> UpdateAsync(Guid id, ClothingItemEditDto input, CancellationToken cancellationToken = default)
    {
        var currentUser = EnsureAuthenticatedUser();
        var item = await ApplyVisibility(
                _dbContext.ClothingItems
                    .Include(clothingItem => clothingItem.FabricEntries)
                    .Include(clothingItem => clothingItem.ImageAttachments),
                currentUser)
            .FirstOrDefaultAsync(clothingItem => clothingItem.Id == id, cancellationToken);

        if (item == null)
        {
            return false;
        }

        var styleNumber = input.StyleNumber.Trim();
        var hasDuplicate = await _dbContext.ClothingItems.AnyAsync(
            clothingItem => clothingItem.StyleNumber == styleNumber && clothingItem.Id != id,
            cancellationToken);

        if (hasDuplicate)
        {
            throw new InvalidOperationException("该款号已被其他款式使用。");
        }

        var fabricEntries = NormalizeFabricEntries(input.FabricEntries);
        var owner = await ResolveOwnerAsync(input.OwnerId, currentUser, cancellationToken);
        var now = DateTime.UtcNow;

        item.StyleNumber = styleNumber;
        item.Title = input.Title.Trim();
        item.Year = input.Year;
        item.Season = input.Season.Trim();
        item.Progress = input.Progress;
        item.FabricInformation = BuildFabricInformationSummary(fabricEntries);
        item.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        item.OwnerId = owner.UserId;
        item.OwnerDisplayName = owner.DisplayName;
        item.UpdatedAtUtc = now;

        var existingFabricEntries = item.FabricEntries.ToList();
        _dbContext.FabricEntries.RemoveRange(existingFabricEntries);
        item.FabricEntries.Clear();

        var newFabricEntries = fabricEntries
            .Select((fabricEntry, index) => new FabricEntry
            {
                ClothingItemId = item.Id,
                MaterialName = fabricEntry.MaterialName ?? string.Empty,
                Specification = fabricEntry.Specification ?? string.Empty,
                Remark = fabricEntry.Remark ?? string.Empty,
                SortOrder = index
            })
            .ToList();

        if (newFabricEntries.Count > 0)
        {
            _dbContext.FabricEntries.AddRange(newFabricEntries);
        }

        var attachmentIdsToDelete = input.AttachmentIdsToDelete.Distinct().ToHashSet();
        var attachmentsToDelete = item.ImageAttachments
            .Where(attachment => attachmentIdsToDelete.Contains(attachment.Id))
            .ToList();
        _dbContext.ClothingImageAttachments.RemoveRange(attachmentsToDelete);

        foreach (var attachment in attachmentsToDelete)
        {
            item.ImageAttachments.Remove(attachment);
        }

        var retainedAttachments = item.ImageAttachments
            .OrderBy(attachment => attachment.SortOrder)
            .ThenBy(attachment => attachment.UploadedAtUtc)
            .ToList();

        var newAttachments = new List<ClothingImageAttachment>();
        foreach (var attachment in NormalizeAttachments(input.NewImageAttachments))
        {
            var storedFilePath = await _imageStorageService.SaveAsync(
                attachment.BinaryContent,
                attachment.OriginalFileName,
                attachment.ContentType,
                cancellationToken);

            newAttachments.Add(new ClothingImageAttachment
            {
                ClothingItemId = item.Id,
                FilePath = storedFilePath,
                ContentType = attachment.ContentType,
                OriginalFileName = attachment.OriginalFileName,
                UploadedByUserId = currentUser.UserId,
                UploadedByDisplayName = currentUser.DisplayName,
                UploadedAtUtc = now
            });
        }

        ReorderAttachments(retainedAttachments.Concat(newAttachments));
        if (newAttachments.Count > 0)
        {
            _dbContext.ClothingImageAttachments.AddRange(newAttachments);
        }

        RefreshCoverImage(item, retainedAttachments.Concat(newAttachments));

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUser = EnsureAuthenticatedUser();
        var item = await ApplyVisibility(
                _dbContext.ClothingItems
                    .Include(clothingItem => clothingItem.ModificationNotes)
                    .Include(clothingItem => clothingItem.FabricEntries)
                    .Include(clothingItem => clothingItem.ImageAttachments)
                    .Include(clothingItem => clothingItem.ApprovalRecords),
                currentUser)
            .FirstOrDefaultAsync(clothingItem => clothingItem.Id == id, cancellationToken);

        if (item == null)
        {
            return false;
        }

        _dbContext.ClothingItems.Remove(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> AddNoteAsync(Guid id, string content, CancellationToken cancellationToken = default)
    {
        var currentUser = EnsureAuthenticatedUser();
        var trimmedContent = content.Trim();

        if (string.IsNullOrWhiteSpace(trimmedContent))
        {
            return false;
        }

        var item = await ApplyVisibility(_dbContext.ClothingItems, currentUser)
            .FirstOrDefaultAsync(clothingItem => clothingItem.Id == id, cancellationToken);

        if (item == null)
        {
            return false;
        }

        _dbContext.ModificationNotes.Add(new ModificationNote
        {
            ClothingItemId = id,
            Content = trimmedContent,
            CreatedByUserId = currentUser.UserId,
            CreatedByDisplayName = currentUser.DisplayName,
            CreatedAtUtc = DateTime.UtcNow
        });

        item.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SubmitForApprovalAsync(Guid id, string? comment, CancellationToken cancellationToken = default)
    {
        var currentUser = EnsureAuthenticatedUser();
        var item = await ApplyVisibility(
                _dbContext.ClothingItems.Include(clothingItem => clothingItem.ApprovalRecords),
                currentUser)
            .FirstOrDefaultAsync(clothingItem => clothingItem.Id == id, cancellationToken);

        if (item == null || item.ApprovalStatus == ApprovalStatus.Pending)
        {
            return false;
        }

        item.ApprovalStatus = ApprovalStatus.Pending;
        item.UpdatedAtUtc = DateTime.UtcNow;

        _dbContext.ApprovalRecords.Add(new ApprovalRecord
        {
            ClothingItemId = id,
            Action = ApprovalAction.Submitted,
            Comment = NormalizeOptionalText(comment),
            CreatedByUserId = currentUser.UserId,
            CreatedByDisplayName = currentUser.DisplayName,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ReviewApprovalAsync(
        Guid id,
        ApprovalAction action,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        var currentUser = EnsureAdminUser();
        if (action is not ApprovalAction.Approved and not ApprovalAction.Rejected and not ApprovalAction.ReturnedForChanges)
        {
            return false;
        }

        var item = await _dbContext.ClothingItems
            .Include(clothingItem => clothingItem.ApprovalRecords)
            .FirstOrDefaultAsync(clothingItem => clothingItem.Id == id, cancellationToken);

        if (item == null || item.ApprovalStatus != ApprovalStatus.Pending)
        {
            return false;
        }

        item.ApprovalStatus = action switch
        {
            ApprovalAction.Approved => ApprovalStatus.Approved,
            ApprovalAction.Rejected => ApprovalStatus.Rejected,
            ApprovalAction.ReturnedForChanges => ApprovalStatus.ChangesRequested,
            _ => item.ApprovalStatus
        };

        item.Progress = action switch
        {
            ApprovalAction.Approved when item.Progress != ClothingProgressStatus.Pass => ClothingProgressStatus.Confirmed,
            ApprovalAction.Rejected => ClothingProgressStatus.OnHold,
            ApprovalAction.ReturnedForChanges => ClothingProgressStatus.Revision,
            _ => item.Progress
        };

        item.UpdatedAtUtc = DateTime.UtcNow;
        _dbContext.ApprovalRecords.Add(new ApprovalRecord
        {
            ClothingItemId = id,
            Action = action,
            Comment = NormalizeOptionalText(comment),
            CreatedByUserId = currentUser.UserId,
            CreatedByDisplayName = currentUser.DisplayName,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<ClothingItemDetailDto> MapDetailsAsync(ClothingItem item, CancellationToken cancellationToken) =>
        new()
        {
            Id = item.Id,
            StyleNumber = item.StyleNumber,
            Title = item.Title,
            Year = item.Year,
            Season = item.Season,
            Progress = item.Progress,
            ProgressDisplayName = item.Progress.ToDisplayName(),
            ApprovalStatus = item.ApprovalStatus,
            ApprovalStatusDisplayName = item.ApprovalStatus.ToDisplayName(),
            FabricInformation = item.FabricInformation,
            Description = item.Description,
            ImagePath = item.ImageAttachments
                .OrderBy(attachment => attachment.SortOrder)
                .ThenBy(attachment => attachment.UploadedAtUtc)
                .Select(attachment => BuildImageUrl(attachment.Id))
                .FirstOrDefault() ?? item.ImagePath,
            OwnerId = item.OwnerId,
            OwnerDisplayName = item.OwnerDisplayName,
            CreatedAtUtc = item.CreatedAtUtc,
            UpdatedAtUtc = item.UpdatedAtUtc,
            FabricEntries = BuildDetailFabricEntries(item).ToList(),
            ImageAttachments = await BuildAttachmentDtosAsync(item.ImageAttachments, cancellationToken),
            ApprovalRecords = item.ApprovalRecords
                .OrderByDescending(record => record.CreatedAtUtc)
                .Select(record => new ApprovalRecordDto
                {
                    Id = record.Id,
                    Action = record.Action,
                    ActionDisplayName = record.Action.ToDisplayName(),
                    Comment = record.Comment,
                    CreatedByDisplayName = record.CreatedByDisplayName,
                    CreatedAtUtc = record.CreatedAtUtc
                })
                .ToList(),
            ModificationNotes = item.ModificationNotes
                .OrderByDescending(note => note.CreatedAtUtc)
                .Select(note => new ModificationNoteDto
                {
                    Id = note.Id,
                    Content = note.Content,
                    CreatedByDisplayName = note.CreatedByDisplayName,
                    CreatedAtUtc = note.CreatedAtUtc
                })
                .ToList()
        };

    private CurrentUserInfo EnsureAuthenticatedUser()
    {
        var currentUser = _currentUserService.GetCurrentUser();
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            throw new InvalidOperationException("当前用户未登录，无法访问服装数据。");
        }

        return currentUser;
    }

    private CurrentUserInfo EnsureAdminUser()
    {
        var currentUser = EnsureAuthenticatedUser();
        if (!currentUser.IsAdmin)
        {
            throw new InvalidOperationException("当前用户没有审批权限。");
        }

        return currentUser;
    }

    private IQueryable<ClothingItem> ApplyVisibility(IQueryable<ClothingItem> query, CurrentUserInfo currentUser) =>
        currentUser.IsAdmin
            ? query
            : query.Where(item => item.OwnerId == currentUser.UserId);

    private async Task<OwnerOption> ResolveOwnerAsync(
        string? requestedOwnerId,
        CurrentUserInfo currentUser,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAdmin)
        {
            return new OwnerOption
            {
                UserId = currentUser.UserId,
                DisplayName = currentUser.DisplayName,
                Email = currentUser.DisplayName
            };
        }

        var ownerId = string.IsNullOrWhiteSpace(requestedOwnerId) ? currentUser.UserId : requestedOwnerId.Trim();
        var owner = await _userDirectoryService.FindOwnerAsync(ownerId, cancellationToken);

        if (owner == null)
        {
            throw new InvalidOperationException("无法找到指定负责人，请重新选择。");
        }

        return owner;
    }

    private static List<FabricEntryEditDto> NormalizeFabricEntries(IEnumerable<FabricEntryEditDto>? entries) =>
        (entries ?? Array.Empty<FabricEntryEditDto>())
            .Select(entry => new FabricEntryEditDto
            {
                Id = entry.Id,
                MaterialName = entry.MaterialName?.Trim() ?? string.Empty,
                Specification = entry.Specification?.Trim() ?? string.Empty,
                Remark = entry.Remark?.Trim() ?? string.Empty
            })
            .Where(entry =>
                !string.IsNullOrWhiteSpace(entry.MaterialName) ||
                !string.IsNullOrWhiteSpace(entry.Specification) ||
                !string.IsNullOrWhiteSpace(entry.Remark))
            .ToList();

    private static List<ClothingImageAttachmentInputDto> NormalizeAttachments(IEnumerable<ClothingImageAttachmentInputDto>? attachments) =>
        (attachments ?? Array.Empty<ClothingImageAttachmentInputDto>())
            .Where(attachment =>
                attachment.BinaryContent is { Length: > 0 } &&
                !string.IsNullOrWhiteSpace(attachment.ContentType))
            .Select(attachment => new ClothingImageAttachmentInputDto
            {
                BinaryContent = attachment.BinaryContent,
                ContentType = attachment.ContentType.Trim(),
                OriginalFileName = string.IsNullOrWhiteSpace(attachment.OriginalFileName)
                    ? "image"
                    : attachment.OriginalFileName.Trim()
            })
            .ToList();

    private static string BuildFabricInformationSummary(IEnumerable<FabricEntryEditDto> entries) =>
        string.Join(
            "；",
            entries.Select(entry =>
            {
                var parts = new[] { entry.MaterialName, entry.Specification, entry.Remark }
                    .Where(part => !string.IsNullOrWhiteSpace(part));
                return string.Join(" / ", parts);
            }));

    private static IEnumerable<FabricEntryDto> BuildDetailFabricEntries(ClothingItem item)
    {
        if (item.FabricEntries.Count > 0)
        {
            return item.FabricEntries
                .OrderBy(entry => entry.SortOrder)
                .Select(entry => new FabricEntryDto
                {
                    Id = entry.Id,
                    MaterialName = entry.MaterialName,
                    Specification = entry.Specification,
                    Remark = entry.Remark,
                    SortOrder = entry.SortOrder
                });
        }

        if (string.IsNullOrWhiteSpace(item.FabricInformation))
        {
            return Array.Empty<FabricEntryDto>();
        }

        return new[]
        {
            new FabricEntryDto
            {
                Id = Guid.Empty,
                MaterialName = item.FabricInformation,
                Specification = string.Empty,
                Remark = string.Empty,
                SortOrder = 0
            }
        };
    }

    private static IEnumerable<FabricEntryEditDto> BuildEditFabricEntries(ClothingItem item)
    {
        if (item.FabricEntries.Count > 0)
        {
            return item.FabricEntries
                .OrderBy(entry => entry.SortOrder)
                .Select(entry => new FabricEntryEditDto
                {
                    Id = entry.Id,
                    MaterialName = entry.MaterialName,
                    Specification = entry.Specification,
                    Remark = entry.Remark
                });
        }

        return string.IsNullOrWhiteSpace(item.FabricInformation)
            ? new[] { new FabricEntryEditDto() }
            : new[]
            {
                new FabricEntryEditDto
                {
                    MaterialName = item.FabricInformation
                }
            };
    }

    private async Task<List<ClothingImageAttachmentDto>> BuildAttachmentDtosAsync(
        IEnumerable<ClothingImageAttachment> attachments,
        CancellationToken cancellationToken)
    {
        var orderedAttachments = attachments
            .OrderBy(attachment => attachment.SortOrder)
            .ThenBy(attachment => attachment.UploadedAtUtc)
            .ToList();
        var attachmentDtos = new List<ClothingImageAttachmentDto>(orderedAttachments.Count);

        foreach (var attachment in orderedAttachments)
        {
            var storedBinaryContent = string.IsNullOrWhiteSpace(attachment.FilePath)
                ? null
                : await _imageStorageService.ReadAsync(attachment.FilePath, cancellationToken);

            attachmentDtos.Add(new ClothingImageAttachmentDto
            {
                Id = attachment.Id,
                FilePath = BuildImageUrl(attachment.Id),
                OriginalFileName = attachment.OriginalFileName,
                UploadedByDisplayName = attachment.UploadedByDisplayName,
                UploadedAtUtc = attachment.UploadedAtUtc,
                SortOrder = attachment.SortOrder,
                BinaryContent = attachment.BinaryContent ?? storedBinaryContent
            });
        }

        return attachmentDtos;
    }

    private static void ReorderAttachments(IEnumerable<ClothingImageAttachment> attachments)
    {
        var orderedAttachments = attachments
            .OrderBy(attachment => attachment.SortOrder)
            .ThenBy(attachment => attachment.UploadedAtUtc)
            .ToList();

        for (var index = 0; index < orderedAttachments.Count; index++)
        {
            orderedAttachments[index].SortOrder = index;
        }
    }

    private static void RefreshCoverImage(ClothingItem item, IEnumerable<ClothingImageAttachment>? attachments = null)
    {
        var firstAttachmentId = (attachments ?? item.ImageAttachments)
            .OrderBy(attachment => attachment.SortOrder)
            .ThenBy(attachment => attachment.UploadedAtUtc)
            .Select(attachment => (Guid?)attachment.Id)
            .FirstOrDefault();

        item.ImagePath = firstAttachmentId.HasValue
            ? BuildImageUrl(firstAttachmentId.Value)
            : null;
    }

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string BuildImageUrl(Guid attachmentId) => $"/ClothingItems/Image/{attachmentId}";
}
