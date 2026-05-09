using ClothesSystem.Application.Common.Interfaces;

namespace ClothesSystem.Tests;

internal sealed class TestDataPathProvider : IDataPathProvider
{
    public TestDataPathProvider(string rootPath)
    {
        DatabasePath = Path.Combine(rootPath, "clothes-system.db");
        LogPath = Path.Combine(rootPath, "logs");
        ExportPath = Path.Combine(rootPath, "exports");
        ImageStoragePath = Path.Combine(rootPath, "images");
        TemplatePath = Path.Combine(rootPath, "templates");
    }

    public string DatabasePath { get; }
    public string LogPath { get; }
    public string ExportPath { get; }
    public string ImageStoragePath { get; }
    public string TemplatePath { get; }
}
