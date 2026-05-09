using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClothesSystem.Application.ClothingItems;
using ClothesSystem.Desktop.Services;
using System.Collections.ObjectModel;

namespace ClothesSystem.Desktop.ViewModels;

public partial class ClothingDetailsViewModel : ObservableObject
{
    private readonly IClothingService _clothingService;
    private readonly INavigationService _navigationService;
    private readonly IClothingWorkbookTemplateService _workbookService;

    [ObservableProperty]
    private ClothingItemDetailDto? _item;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isAdmin;

    [ObservableProperty]
    private bool _canSubmitApproval;

    [ObservableProperty]
    private bool _canReviewApproval;

    [ObservableProperty]
    private string _noteContent = string.Empty;

    [ObservableProperty]
    private string _reviewComment = string.Empty;

    [ObservableProperty]
    private int _selectedApprovalAction;

    public ObservableCollection<ClothingImageAttachmentDto> Images { get; } = new();

    public ObservableCollection<FabricEntryDto> Fabrics { get; } = new();

    public ObservableCollection<ModificationNoteDto> Notes { get; } = new();

    public ObservableCollection<ApprovalRecordDto> ApprovalRecords { get; } = new();

    public ClothingDetailsViewModel(
        IClothingService clothingService,
        INavigationService navigationService,
        IClothingWorkbookTemplateService workbookService)
    {
        _clothingService = clothingService;
        _navigationService = navigationService;
        _workbookService = workbookService;
    }

    public async Task LoadAsync(Guid id, CancellationToken ct = default)
    {
        IsLoading = true;
        try
        {
            Item = await _clothingService.GetDetailsAsync(id, ct);

            if (Item != null)
            {
                Images.Clear();
                foreach (var img in Item.ImageAttachments)
                {
                    Images.Add(img);
                }

                Fabrics.Clear();
                foreach (var fabric in Item.FabricEntries)
                {
                    Fabrics.Add(fabric);
                }

                Notes.Clear();
                foreach (var note in Item.ModificationNotes)
                {
                    Notes.Add(note);
                }

                ApprovalRecords.Clear();
                foreach (var record in Item.ApprovalRecords)
                {
                    ApprovalRecords.Add(record);
                }

                CanSubmitApproval = Item.ApprovalStatus == Domain.Enums.ApprovalStatus.Draft ||
                                    Item.ApprovalStatus == Domain.Enums.ApprovalStatus.ChangesRequested;

                CanReviewApproval = Item.ApprovalStatus == Domain.Enums.ApprovalStatus.Pending && IsAdmin;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Edit()
    {
        if (Item != null)
        {
            _navigationService.NavigateToEdit(Item.Id);
        }
    }

    [RelayCommand]
    private void Back()
    {
        _navigationService.NavigateToClothingList();
    }

    [RelayCommand]
    private async Task AddNoteAsync()
    {
        if (Item != null && !string.IsNullOrWhiteSpace(NoteContent))
        {
            await _clothingService.AddNoteAsync(Item.Id, NoteContent);
            NoteContent = string.Empty;
            await LoadAsync(Item.Id);
        }
    }

    [RelayCommand]
    private async Task SubmitForApprovalAsync()
    {
        if (Item != null)
        {
            await _clothingService.SubmitForApprovalAsync(Item.Id, null);
            await LoadAsync(Item.Id);
        }
    }

    [RelayCommand]
    private async Task ReviewApprovalAsync()
    {
        if (Item == null) return;

        var action = SelectedApprovalAction switch
        {
            0 => Domain.Enums.ApprovalAction.Approved,
            1 => Domain.Enums.ApprovalAction.Rejected,
            2 => Domain.Enums.ApprovalAction.ReturnedForChanges,
            _ => Domain.Enums.ApprovalAction.Approved
        };

        await _clothingService.ReviewApprovalAsync(Item.Id, action, ReviewComment);
        ReviewComment = string.Empty;
        await LoadAsync(Item.Id);
    }

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        if (Item == null) return;

        try
        {
            var file = await _workbookService.ExportPlaceholderAsync(Item.Id);
            if (file != null)
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel文件|*.xlsx",
                    FileName = file.FileName
                };

                if (dialog.ShowDialog() == true)
                {
                    await File.WriteAllBytesAsync(dialog.FileName, file.Content);
                    System.Windows.MessageBox.Show("导出成功！", "成功", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"导出失败: {ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}
