using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClothesSystem.Application.ClothingItems;
using ClothesSystem.Desktop.Services;
using System.Collections.ObjectModel;

namespace ClothesSystem.Desktop.ViewModels;

public partial class ClothingEditViewModel : ObservableObject
{
    private readonly IClothingService _clothingService;
    private readonly INavigationService _navigationService;
    private readonly IDesktopImageService _imageService;

    [ObservableProperty]
    private Guid _id;

    [ObservableProperty]
    private bool _isNewItem;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _styleNumber = string.Empty;

    [ObservableProperty]
    private int _year = DateTime.Now.Year;

    [ObservableProperty]
    private string? _season;

    [ObservableProperty]
    private string? _fabricInformation;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    // Fabric entries
    public ObservableCollection<FabricEntryEditDto> FabricEntries { get; } = new();

    // Image attachments (for display)
    public ObservableCollection<ClothingImageAttachmentDto> ExistingImages { get; } = new();

    // New images to be added (stored as byte arrays temporarily)
    public ObservableCollection<NewImageInfo> NewImages { get; } = new();

    public ClothingEditViewModel(
        IClothingService clothingService,
        INavigationService navigationService,
        IDesktopImageService imageService)
    {
        _clothingService = clothingService;
        _navigationService = navigationService;
        _imageService = imageService;
    }

    public async Task LoadAsync(Guid id, CancellationToken ct = default)
    {
        IsLoading = true;
        IsNewItem = id == Guid.Empty;

        try
        {
            if (IsNewItem)
            {
                Title = string.Empty;
                StyleNumber = await _clothingService.GenerateNextStyleNumberAsync(ct);
                Year = DateTime.Now.Year;
                Season = null;
                FabricInformation = null;
                Description = null;
                FabricEntries.Clear();
                ExistingImages.Clear();
                NewImages.Clear();
            }
            else
            {
                var editDto = await _clothingService.GetEditAsync(id, ct);
                if (editDto == null)
                {
                    ErrorMessage = "未找到要编辑的款式";
                    return;
                }

                // Note: editDto doesn't have Id, Title etc directly - use the id parameter
                Id = id;
                Title = editDto.Title ?? string.Empty;
                StyleNumber = editDto.StyleNumber ?? string.Empty;
                Year = editDto.Year;
                Season = editDto.Season;
                Description = editDto.Description;

                FabricEntries.Clear();
                foreach (var fabric in editDto.FabricEntries)
                {
                    FabricEntries.Add(fabric);
                }

                ExistingImages.Clear();
                foreach (var img in editDto.ExistingImageAttachments)
                {
                    ExistingImages.Add(img);
                }

                NewImages.Clear();
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void AddFabric()
    {
        FabricEntries.Add(new FabricEntryEditDto
        {
            MaterialName = string.Empty,
            Specification = string.Empty
        });
    }

    [RelayCommand]
    private void RemoveFabric(FabricEntryEditDto fabric)
    {
        FabricEntries.Remove(fabric);
    }

    [RelayCommand]
    private async Task AddImagesAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Multiselect = true,
            Filter = "图片文件|*.jpg;*.jpeg;*.png;*.webp",
            Title = "选择图片"
        };

        if (dialog.ShowDialog() == true)
        {
            foreach (var filePath in dialog.FileNames)
            {
                try
                {
                    var attachment = await _imageService.LoadFromFileAsync(filePath);
                    if (attachment != null)
                    {
                        NewImages.Add(new NewImageInfo
                        {
                            FileName = attachment.OriginalFileName ?? Path.GetFileName(filePath),
                            ContentType = attachment.ContentType ?? "image/jpeg",
                            BinaryContent = attachment.BinaryContent ?? Array.Empty<byte>()
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"无法加载图片 {Path.GetFileName(filePath)}: {ex.Message}", "错误",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
            }
        }
    }

    [RelayCommand]
    private void RemoveNewImage(NewImageInfo image)
    {
        NewImages.Remove(image);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            ErrorMessage = "请输入款式名称";
            return;
        }

        if (string.IsNullOrWhiteSpace(StyleNumber))
        {
            ErrorMessage = "请输入款号";
            return;
        }

        if (ExistingImages.Count == 0 && NewImages.Count == 0)
        {
            ErrorMessage = "请至少上传一张图片";
            return;
        }

        IsSaving = true;
        ErrorMessage = string.Empty;

        try
        {
            var editDto = new ClothingItemEditDto
            {
                Title = Title,
                StyleNumber = StyleNumber,
                Year = Year,
                Season = Season ?? string.Empty,
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                FabricEntries = FabricEntries.ToList(),
                ExistingImageAttachments = ExistingImages.ToList(),
                NewImageAttachments = NewImages.Select(img => new ClothingImageAttachmentInputDto
                {
                    OriginalFileName = img.FileName,
                    ContentType = img.ContentType,
                    BinaryContent = img.BinaryContent
                }).ToList()
            };

            if (IsNewItem)
            {
                var newId = await _clothingService.CreateAsync(editDto);
                _navigationService.NavigateToDetails(newId);
            }
            else
            {
                await _clothingService.UpdateAsync(Id, editDto);
                _navigationService.NavigateToDetails(Id);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"保存失败: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        if (IsNewItem)
        {
            _navigationService.NavigateToClothingList();
        }
        else
        {
            _navigationService.NavigateToDetails(Id);
        }
    }
}

public class NewImageInfo
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] BinaryContent { get; set; } = Array.Empty<byte>();
}
