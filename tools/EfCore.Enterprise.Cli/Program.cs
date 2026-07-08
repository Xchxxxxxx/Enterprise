using System.Diagnostics;
using System.Text;
using EfCore.Enterprise.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EfCore.Enterprise.Cli;

internal partial class Program
{
    static void Main(string[] args)
    {
        try
        {
            Run(args);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ 错误: {ex.Message}");
            Console.ResetColor();
            Environment.Exit(1);
        }
    }

    static void Run(string[] args)
    {
        if (args.Length == 0 || args[0] == "help")
        {
            PrintHelp();
            return;
        }

        var command = args[0].ToLower();

        switch (command)
        {
            case "new":
                NewProject(args.Skip(1).ToArray());
                break;
            case "generate":
                Generate(args.Skip(1).ToArray());
                break;
            case "seed":
                Seed();
                break;
            case "dev":
                Dev();
                break;
            default:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ 未知命令: {command}");
                Console.ResetColor();
                PrintHelp();
                break;
        }
    }

    static void PrintHelp()
    {
        Console.WriteLine(@"
╔══════════════════════════════════════════════════════════════╗
║              EfCore.Enterprise CLI 工具                      ║
╚══════════════════════════════════════════════════════════════╝

可用命令:

  ef-cli new <项目名>                创建新的六层架构项目
  ef-cli new <项目名> --db sqlserver 指定数据库类型创建项目
  ef-cli generate module <实体名>    生成完整模块代码（后端 + 可选前端）
  ef-cli generate interactive        交互式生成模块（问答式）
  ef-cli generate migration          生成 EF Core 迁移
  ef-cli generate test <服务名>      生成单元测试文件
  ef-cli seed                       填充种子数据
  ef-cli dev                        启动开发模式（自动迁移 + 自动种子）
  ef-cli help                       显示帮助信息

示例:

  ef-cli new MyApp
  ef-cli new MyApp --db sqlserver
  ef-cli generate module Product --web
  ef-cli generate interactive
  ef-cli generate migration
  ef-cli generate test ProductService
  ef-cli seed
  ef-cli dev
");
    }

    static void Generate(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("❌ 缺少子命令: module/interactive/migration/test");
            return;
        }

        var subCommand = args[0].ToLower();

        switch (subCommand)
        {
            case "module":
                GenerateModule(args.Skip(1).ToArray());
                break;
            case "interactive":
                GenerateInteractive();
                break;
            case "migration":
                GenerateMigration();
                break;
            case "test":
                GenerateTest(args.Skip(1).ToArray());
                break;
            default:
                Console.WriteLine($"❌ 未知 generate 子命令: {subCommand}");
                break;
        }
    }

    static void GenerateModule(string[] args)
    {
        var entityName = args.FirstOrDefault();
        if (string.IsNullOrEmpty(entityName))
        {
            Console.WriteLine("❌ 请输入实体名: ef-cli generate module <实体名> [--web]");
            return;
        }

        var generateWeb = args.Contains("--web");

        Console.WriteLine($"🔧 正在生成模块 {entityName}...");

        var properties = PromptProperties();

        var services = BuildServices();
        var generator = services.GetRequiredService<ICodeGeneratorService>();

        GenerateFile(generator.GenerateEntityCode(entityName, properties),
            $"src/01-Domain/Entities/{entityName}.cs");

        GenerateFile(generator.GenerateDtoCode(entityName, properties),
            $"src/04-Application/DTOs/{entityName}/{entityName}Dto.cs");

        GenerateFile(generator.GenerateCreateDtoCode(entityName, properties),
            $"src/04-Application/DTOs/{entityName}/Create{entityName}Request.cs");

        GenerateFile(generator.GenerateUpdateDtoCode(entityName, properties),
            $"src/04-Application/DTOs/{entityName}/Update{entityName}Request.cs");

        GenerateFile(generator.GenerateProfileCode(entityName),
            $"src/04-Application/Mapping/Profiles/{entityName}Profile.cs");

        GenerateFile(generator.GenerateValidatorCode(entityName, properties),
            $"src/04-Application/Validation/{entityName}Validator.cs");

        GenerateFile(generator.GenerateServiceCode(entityName),
            $"src/04-Application/Services/{entityName}Service.cs");

        GenerateFile(generator.GenerateControllerCode(entityName),
            $"src/05-Presentation/Controllers/{entityName}Controller.cs");

        if (generateWeb)
        {
            var vueCode = generator.GenerateVueCode(entityName, properties);
            GenerateFile(vueCode, $"web/src/views/{entityName}/index.vue");
            GenerateFile(GenerateApiTs(entityName), $"web/src/api/{entityName.ToLower()}.ts");
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✅ 模块 {entityName} 生成完成!");
        if (generateWeb)
        {
            Console.WriteLine($"   后端: 8 个文件已生成");
            Console.WriteLine($"   前端: 2 个文件已生成（Vue3 页面 + API）");
        }
        else
        {
            Console.WriteLine($"   8 个文件已生成");
        }
        Console.ResetColor();
    }

    static void GenerateInteractive()
    {
        Console.WriteLine("🎯 交互式生成模块");
        Console.WriteLine("------------------");

        Console.Write("请输入模块名: ");
        var moduleName = Console.ReadLine()?.Trim() ?? string.Empty;

        Console.Write("请输入实体名: ");
        var entityName = Console.ReadLine()?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(entityName))
        {
            Console.WriteLine("❌ 实体名不能为空");
            return;
        }

        Console.WriteLine("请选择基类:");
        Console.WriteLine("  1. BaseEntity");
        Console.WriteLine("  2. BaseAuditEntity");
        Console.WriteLine("  3. BaseFullEntity (推荐)");
        Console.WriteLine("  4. BaseComplianceEntity");
        Console.Write("请选择 [默认 3]: ");
        var baseChoice = Console.ReadLine()?.Trim() ?? "3";

        Console.WriteLine();
        Console.WriteLine("请输入属性（格式: 类型 名称 注释），空行结束输入:");
        Console.WriteLine("示例: string Name 商品名称");
        Console.WriteLine("      decimal Price 价格");
        Console.WriteLine("      int Stock 库存");

        var properties = new List<PropertyInfo>();
        while (true)
        {
            Console.Write("> ");
            var line = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(line)) break;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                Console.WriteLine("⚠ 格式不对，跳过");
                continue;
            }

            var type = parts[0];
            var name = parts[1];
            var description = parts.Length >= 3 ? string.Join(" ", parts.Skip(2)) : null;

            properties.Add(new PropertyInfo { Name = name, Type = type, Description = description });
        }

        Console.Write("是否生成前端页面? (y/n) [默认 n]: ");
        var generateWebInput = Console.ReadLine()?.Trim().ToLower() ?? "n";
        var generateWeb = generateWebInput == "y" || generateWebInput == "yes";

        Console.WriteLine($"\n🔧 正在生成 {entityName}...");

        var services = BuildServices();
        var generator = services.GetRequiredService<ICodeGeneratorService>();

        GenerateFile(generator.GenerateEntityCode(entityName, properties),
            $"src/01-Domain/Entities/{entityName}.cs");

        GenerateFile(generator.GenerateDtoCode(entityName, properties),
            $"src/04-Application/DTOs/{entityName}/{entityName}Dto.cs");

        GenerateFile(generator.GenerateCreateDtoCode(entityName, properties),
            $"src/04-Application/DTOs/{entityName}/Create{entityName}Request.cs");

        GenerateFile(generator.GenerateUpdateDtoCode(entityName, properties),
            $"src/04-Application/DTOs/{entityName}/Update{entityName}Request.cs");

        GenerateFile(generator.GenerateProfileCode(entityName),
            $"src/04-Application/Mapping/Profiles/{entityName}Profile.cs");

        GenerateFile(generator.GenerateValidatorCode(entityName, properties),
            $"src/04-Application/Validation/{entityName}Validator.cs");

        GenerateFile(generator.GenerateServiceCode(entityName),
            $"src/04-Application/Services/{entityName}Service.cs");

        GenerateFile(generator.GenerateControllerCode(entityName),
            $"src/05-Presentation/Controllers/{entityName}Controller.cs");

        if (generateWeb)
        {
            var vueCode = generator.GenerateVueCode(entityName, properties);
            GenerateFile(vueCode, $"web/src/views/{entityName}/index.vue");
            GenerateFile(GenerateApiTs(entityName), $"web/src/api/{entityName.ToLower()}.ts");
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✅ 完成! 模块 {entityName} 已生成。");
        if (generateWeb)
        {
            Console.WriteLine($"   后端: 8 个文件");
            Console.WriteLine($"   前端: 2 个文件");
        }
        else
        {
            Console.WriteLine($"   总共 8 个文件");
        }
        Console.ResetColor();
    }

    static void GenerateMigration()
    {
        Console.WriteLine("🔧 生成 EF Core 迁移...");
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var name = $"Migration_{timestamp}";
        var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"ef migrations add {name} --project src/03-Infrastructure --startup-project src/05-Presentation",
            UseShellExecute = false
        });
        process?.WaitForExit();
        Console.WriteLine($"✅ 迁移 {name} 生成完成");
    }

    static void GenerateTest(string[] args)
    {
        var serviceName = args.FirstOrDefault();
        if (string.IsNullOrEmpty(serviceName))
        {
            Console.WriteLine("❌ 请输入服务名: ef-cli generate test <服务名>");
            return;
        }

        var code = GenerateTestCode(serviceName);
        GenerateFile(code, $"test/{serviceName}Tests.cs");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✅ 单元测试 {serviceName}Tests.cs 生成完成");
        Console.ResetColor();
    }

    static void Seed()
    {
        Console.WriteLine("🌱 正在填充种子数据...");
        var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --project src/05-Presentation -- seed",
            UseShellExecute = false
        });
        process?.WaitForExit();
        Console.WriteLine("✅ 种子数据填充完成");
    }

    static void Dev()
    {
        Console.WriteLine("🚀 启动开发模式...");
        Console.WriteLine("⏳ 自动迁移数据库...");
        var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "watch run --project src/05-Presentation",
            UseShellExecute = false
        });
        process?.WaitForExit();
    }

    static void NewProject(string[] args)
    {
        var projectName = args.FirstOrDefault(a => !a.StartsWith("--"));
        if (string.IsNullOrEmpty(projectName))
        {
            Console.WriteLine("❌ 请输入项目名称: ef-cli new <项目名> [--db mysql|sqlserver|postgresql]");
            return;
        }

        var dbArg = args.FirstOrDefault(a => a.StartsWith("--db"));
        var dbType = dbArg?.Split('=') is { Length: 2 } parts ? parts[1] : "mysql";

        var templateName = "EfCore.Enterprise.Template";
        var templateShortName = "ef-enterprise";

        Console.WriteLine($"🚀 正在创建项目: {projectName}");
        Console.WriteLine($"   数据库类型: {dbType}");
        Console.WriteLine();

        if (!IsTemplateInstalled(templateName))
        {
            Console.WriteLine("📦 模板未安装，正在从 NuGet 安装...");
            if (!InstallTemplate())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ 模板安装失败！请手动执行:");
                Console.WriteLine($"   dotnet new install EfCore.Enterprise.Templates");
                Console.ResetColor();
                return;
            }
            Console.WriteLine("✅ 模板安装成功");
            Console.WriteLine();
        }

        var argsList = new List<string>
        {
            "new", templateShortName,
            "-n", projectName,
            "-o", projectName
        };

        if (dbType != "mysql")
        {
            argsList.Add("--Database");
            argsList.Add(dbType);
        }

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = string.Join(" ", argsList),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(psi)!;
        process.WaitForExit();

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        if (process.ExitCode == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✅ 项目 {projectName} 创建成功！");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("📁 项目结构:");
            Console.WriteLine($"   {projectName}/");
            Console.WriteLine("   ├── 01-Domain/         领域层");
            Console.WriteLine("   ├── 02-Shared/         共享层");
            Console.WriteLine("   ├── 03-Contracts/      契约层(DTO)");
            Console.WriteLine("   ├── 04-Application/    应用层");
            Console.WriteLine("   ├── 05-Infrastructure/ 基础设施层");
            Console.WriteLine("   └── 06-Presentation/   展示层(WebApi)");
            Console.WriteLine();
            Console.WriteLine("🔧 下一步:");
            Console.WriteLine($"   cd {projectName}");
            Console.WriteLine("   修改 appsettings.json 中的数据库连接字符串");
            Console.WriteLine("   dotnet build");
            Console.WriteLine("   dotnet run --project 06-Presentation");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ 项目创建失败！");
            if (!string.IsNullOrWhiteSpace(error))
                Console.WriteLine(error);
            Console.ResetColor();
        }
    }

    static bool IsTemplateInstalled(string templateName)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "new list",
            UseShellExecute = false,
            RedirectStandardOutput = true
        };

        using var process = Process.Start(psi)!;
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return output.Contains(templateName);
    }

    static bool InstallTemplate()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "new install EfCore.Enterprise.Templates",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(psi)!;
        process.WaitForExit();

        return process.ExitCode == 0;
    }

    static List<PropertyInfo> PromptProperties()
    {
        var properties = new List<PropertyInfo>();

        Console.WriteLine("请输入属性（格式: 类型 名称 注释），空行结束输入:");
        Console.WriteLine("示例: string Name 商品名称");

        while (true)
        {
            Console.Write("> ");
            var line = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(line)) break;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                Console.WriteLine("⚠ 格式不对，跳过");
                continue;
            }

            var type = parts[0];
            var name = parts[1];
            var description = parts.Length >= 3 ? string.Join(" ", parts.Skip(2)) : null;

            properties.Add(new PropertyInfo { Name = name, Type = type, Description = description });
        }

        return properties;
    }

    static void GenerateFile(string content, string path)
    {
        var fullPath = Path.Combine(Environment.CurrentDirectory, path);
        var dir = Path.GetDirectoryName(fullPath)!;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        if (File.Exists(fullPath))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"⚠ 文件已存在，跳过: {path}");
            Console.ResetColor();
            return;
        }

        File.WriteAllText(fullPath, content, Encoding.UTF8);
        Console.WriteLine($"   ✓ {path}");
    }

    static string GenerateApiTs(string entityName)
    {
        var entityNameLower = char.ToLower(entityName[0]) + entityName[1..];
        return $@"import request from '@/utils/request';
import {{ PagedRequest, PagedResult, ApiResult }} from '@/types';

export interface {entityName}Dto {{
  id: number;
{string.Join("\n", GetTsProperties(entityName))}
  createdTime: string;
}}

export async function getPaginated(params: PagedRequest & {{ keyword?: string }}) {{
  return await request.get<ApiResult<PagedResult<{entityName}Dto>>>('/api/{entityNameLower}', {{ params }});
}}

export async function getById(id: number) {{
  return await request.get<ApiResult<{entityName}Dto>>(`/api/{entityNameLower}/${{id}}`);
}}

export async function create(data: Partial<{entityName}Dto>) {{
  return await request.post<ApiResult<{entityName}Dto>>('/api/{entityNameLower}', data);
}}

export async function update(id: number, data: Partial<{entityName}Dto>) {{
  return await request.put<ApiResult<{entityName}Dto>>(`/api/{entityNameLower}/${{id}}`, data);
}}

export async function remove(id: number) {{
  return await request.delete<ApiResult>(`/api/{entityNameLower}/${{id}}`);
}}
";
    }

    static IEnumerable<string> GetTsProperties(string entityName)
    {
        // 返回空，让用户补全
        return Enumerable.Empty<string>();
    }

    static string GenerateTestCode(string serviceName)
    {
        var template = @"using {SERVICE};
using EfCore.Enterprise.Test;
using Xunit;

namespace EfCore.Enterprise.Tests;

public class {SERVICE}Tests : UnitTestBase<{SERVICE}>
{
    {NEWLINE}
    [Fact]
    public async Task Should_Work_Correctly()
    {NEWLINE}
        // Arrange
        // MockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new ...);

        // Act
        // var result = await Service.SomeMethod(1);

        // Assert
        // Assert.True(result.Success);
    }
}
";
        return template
            .Replace("{SERVICE}", serviceName)
            .Replace("{NEWLINE}", Environment.NewLine);
    }

    static IServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICodeGeneratorService, CodeGeneratorService>();
        return services.BuildServiceProvider();
    }
}