using ClothesSystem.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace ClothesSystem.Web.Services;

public class WebDataPathProvider : IDataPathProvider
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public WebDataPathProvider(IWebHostEnvironment environment, IConfiguration configuration)
    {
        _environment = environment;
        _configuration = configuration;
    }

    private string DataRootPath =>
        _configuration.GetValue<string>("DataRoot") ?? _environment.ContentRootPath;

    public string DatabasePath => Path.Combine(DataRootPath, "clothes-system.db");

    public string LogPath => Path.Combine(DataRootPath, "logs");

    public string ExportPath => Path.Combine(DataRootPath, "exports");

    public string ImageStoragePath => Path.Combine(DataRootPath, "images");

    public string TemplatePath => Path.Combine(_environment.ContentRootPath, "Templates");
}
