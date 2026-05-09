using System.IO;
using ClothesSystem.Application.Common.Interfaces;

namespace ClothesSystem.Desktop.Services;

public class DesktopDataPathProvider : IDataPathProvider
{
    private readonly string _appDataFolder;

    public DesktopDataPathProvider()
    {
        _appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClothesSystem");

        Directory.CreateDirectory(_appDataFolder);
        Directory.CreateDirectory(LogPath);
        Directory.CreateDirectory(ExportPath);
        Directory.CreateDirectory(ImageStoragePath);
        Directory.CreateDirectory(TemplatePath);
    }

    public string DatabasePath => Path.Combine(_appDataFolder, "clothes-system.db");

    public string LogPath => Path.Combine(_appDataFolder, "logs");

    public string ExportPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "ClothesSystem", "Exports");

    public string ImageStoragePath => Path.Combine(_appDataFolder, "Images");

    public string TemplatePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");

    public string GetLegacyWebDbPath()
    {
        // Check for old web deployment database in exe folder
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        var webDbPath = Path.Combine(exeDir, "clothes-system.db");
        return File.Exists(webDbPath) ? webDbPath : string.Empty;
    }
}
