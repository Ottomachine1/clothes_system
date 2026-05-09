# 服装管理系统

基于 ASP.NET Core 8、WPF、SQLite、Entity Framework Core 和 Identity 的服装款式管理系统。

## 功能

- 款式档案管理：款号、名称、年份、季节、进度、负责人、描述。
- 多图片附件：支持 JPG、PNG、WEBP。
- 面料明细：材料、规格、备注三列结构化维护。
- 修改意见：按款式记录修改反馈。
- 审批流：提交、通过、驳回、打回修改，并保留时间线。
- 权限隔离：普通用户只能查看自己的款式，管理员可查看全部款式。
- Excel 接口：已预留设计单导入导出能力。
- Web 与桌面端：同一业务层同时服务 MVC Web 和 WPF 桌面应用。

## 项目结构

- `src/ClothesSystem.Domain`：领域实体与枚举。
- `src/ClothesSystem.Application`：应用服务、DTO、查询和业务接口。
- `src/ClothesSystem.Infrastructure`：EF Core、SQLite、Identity、迁移、模板服务。
- `src/ClothesSystem.Web`：ASP.NET Core MVC Web 应用。
- `src/ClothesSystem.Desktop`：WPF 桌面应用。
- `src/AnalyzeTemplate`：Excel 模板分析工具。

## 构建

当前 Windows 环境的并行 restore 会偶发无错误失败。建议使用单进程构建：

```powershell
dotnet build ClothesSystem.sln -m:1
```

## 运行 Web

```powershell
dotnet run --project .\src\ClothesSystem.Web\ClothesSystem.Web.csproj
```

开发环境会自动迁移数据库，并创建演示账号：

- 管理员：`admin@clothes.local` / `Admin123!`
- 设计师：`designer@clothes.local` / `Designer123!`

非开发环境默认不会创建演示账号。若确实需要启用，可在配置中设置：

```json
{
  "SeedDemoUsers": true
}
```

## 数据库迁移

```powershell
dotnet ef migrations add MigrationName `
  --project .\src\ClothesSystem.Infrastructure\ClothesSystem.Infrastructure.csproj `
  --startup-project .\src\ClothesSystem.Web\ClothesSystem.Web.csproj `
  --output-dir Persistence\Migrations

dotnet ef database update `
  --project .\src\ClothesSystem.Infrastructure\ClothesSystem.Infrastructure.csproj `
  --startup-project .\src\ClothesSystem.Web\ClothesSystem.Web.csproj
```

## 发布

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Publish-WindowsExe.ps1
```

发布产物位于 `publish/windows-exe`。
