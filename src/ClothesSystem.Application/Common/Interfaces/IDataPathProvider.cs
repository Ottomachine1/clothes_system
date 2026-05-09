namespace ClothesSystem.Application.Common.Interfaces;

public interface IDataPathProvider
{
    string DatabasePath { get; }
    string LogPath { get; }
    string ExportPath { get; }
    string ImageStoragePath { get; }
    string TemplatePath { get; }
}
