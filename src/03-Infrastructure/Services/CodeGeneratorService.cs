using EfCore.Enterprise.Shared.DependencyInjection;
using System.Text;

namespace EfCore.Enterprise.Infrastructure.Services;

public interface ICodeGeneratorService
{
    string GenerateEntityCode(string entityName, List<PropertyInfo> properties);
    string GenerateDtoCode(string entityName, List<PropertyInfo> properties);
    string GenerateCreateDtoCode(string entityName, List<PropertyInfo> properties);
    string GenerateUpdateDtoCode(string entityName, List<PropertyInfo> properties);
    string GenerateProfileCode(string entityName);
    string GenerateValidatorCode(string entityName, List<PropertyInfo> properties);
    string GenerateServiceCode(string entityName);
    string GenerateControllerCode(string entityName);
    string GenerateVueCode(string entityName, List<PropertyInfo> properties);
    string GenerateAllFiles(string entityName, List<PropertyInfo> properties);
}

public class CodeGeneratorService : ICodeGeneratorService
{
    public string GenerateEntityCode(string entityName, List<PropertyInfo> properties)
    {
        var sb = new StringBuilder();
        var ctorParams = new List<string>();
        var ctorBody = new List<string>();
        var privateFields = new List<string>();

        foreach (var prop in properties)
        {
            var camelName = char.ToLower(prop.Name[0]) + prop.Name[1..];
            var csharpType = MapToCSharpType(prop.Type);
            var nullable = IsNullableType(prop.Type) ? "?" : "";

            ctorParams.Add($"{csharpType}{nullable} {camelName}");
            ctorBody.Add($"        {prop.Name} = {camelName};");
            privateFields.AddRange(GeneratePropertyCode(prop));
        }

        sb.AppendLine("using EfCore.Enterprise.Domain.Entities;");
        sb.AppendLine();
        sb.AppendLine($"namespace EfCore.Enterprise.Domain.Entities;");
        sb.AppendLine();
        sb.AppendLine($"public class {entityName} : BaseFullEntity");
        sb.AppendLine("{");
        sb.AppendLine($"    private {entityName}() {{ }}");
        sb.AppendLine();
        sb.AppendLine($"    public {entityName}({string.Join(", ", ctorParams)})");
        sb.AppendLine("    {");
        foreach (var body in ctorBody)
            sb.AppendLine(body);
        sb.AppendLine("    }");
        sb.AppendLine();

        foreach (var line in privateFields)
            sb.AppendLine(line);

        sb.AppendLine("}");
        return sb.ToString();
    }

    public string GenerateDtoCode(string entityName, List<PropertyInfo> properties)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"namespace EfCore.Enterprise.Application.DTOs.{entityName};");
        sb.AppendLine();
        sb.AppendLine($"public class {entityName}Dto");
        sb.AppendLine("{");
        sb.AppendLine("    public long Id { get; set; }");

        foreach (var prop in properties)
        {
            var csharpType = MapToCSharpType(prop.Type);
            var nullable = IsNullableType(prop.Type) ? "?" : "";
            sb.AppendLine($"    public {csharpType}{nullable} {prop.Name} {{ get; set; }}");
        }

        sb.AppendLine("    public DateTimeOffset CreatedTime { get; set; }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public string GenerateCreateDtoCode(string entityName, List<PropertyInfo> properties)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System.ComponentModel.DataAnnotations;");
        sb.AppendLine();
        sb.AppendLine($"namespace EfCore.Enterprise.Application.DTOs.{entityName};");
        sb.AppendLine();
        sb.AppendLine($"public class Create{entityName}Request");
        sb.AppendLine("{");

        foreach (var prop in properties)
        {
            var csharpType = MapToCSharpType(prop.Type);
            var nullable = IsNullableType(prop.Type) ? "?" : "";

            if (csharpType == "string" && nullable == "")
                sb.AppendLine("    [Required(ErrorMessage = \"{0}不能为空\")]");

            if (!string.IsNullOrEmpty(prop.Description))
                sb.AppendLine($"    [Display(Name = \"{prop.Description}\")]");

            sb.AppendLine($"    public {csharpType}{nullable} {prop.Name} {{ get; set; }}");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    public string GenerateUpdateDtoCode(string entityName, List<PropertyInfo> properties)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"namespace EfCore.Enterprise.Application.DTOs.{entityName};");
        sb.AppendLine();
        sb.AppendLine($"public class Update{entityName}Request");
        sb.AppendLine("{");

        foreach (var prop in properties)
        {
            var csharpType = MapToCSharpType(prop.Type);
            var nullable = IsNullableType(prop.Type) ? "?" : "";
            sb.AppendLine($"    public {csharpType}{nullable} {prop.Name} {{ get; set; }}");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    public string GenerateProfileCode(string entityName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using AutoMapper;");
        sb.AppendLine($"using EfCore.Enterprise.Application.DTOs.{entityName};");
        sb.AppendLine($"using EfCore.Enterprise.Domain.Entities;");
        sb.AppendLine();
        sb.AppendLine($"namespace EfCore.Enterprise.Application.Mapping;");
        sb.AppendLine();
        sb.AppendLine($"public class {entityName}Profile : Profile");
        sb.AppendLine("{");
        sb.AppendLine($"    public {entityName}Profile()");
        sb.AppendLine("    {");
        sb.AppendLine($"        CreateMap<{entityName}, {entityName}Dto>();");
        sb.AppendLine($"        CreateMap<Create{entityName}Request, {entityName}>();");
        sb.AppendLine($"        CreateMap<Update{entityName}Request, {entityName}>();");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public string GenerateValidatorCode(string entityName, List<PropertyInfo> properties)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using FluentValidation;");
        sb.AppendLine($"using EfCore.Enterprise.Application.DTOs.{entityName};");
        sb.AppendLine();
        sb.AppendLine($"namespace EfCore.Enterprise.Application.Validation;");
        sb.AppendLine();
        sb.AppendLine($"public class Create{entityName}Validator : AbstractValidator<Create{entityName}Request>");
        sb.AppendLine("{");
        sb.AppendLine($"    public Create{entityName}Validator()");
        sb.AppendLine("    {");

        foreach (var prop in properties)
        {
            var csharpType = MapToCSharpType(prop.Type);
            if (csharpType == "string")
            {
                sb.AppendLine($"        RuleFor(x => x.{prop.Name})");
                sb.AppendLine($"            .NotEmpty().WithMessage(\"{prop.Description ?? prop.Name}不能为空\")");
                sb.AppendLine($"            .MaximumLength(200).WithMessage(\"{prop.Description ?? prop.Name}不能超过200个字符\");");
            }
            else if (csharpType == "decimal" || csharpType == "double")
            {
                sb.AppendLine($"        RuleFor(x => x.{prop.Name})");
                sb.AppendLine($"            .GreaterThanOrEqualTo(0).WithMessage(\"{prop.Description ?? prop.Name}不能小于0\");");
            }
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public string GenerateServiceCode(string entityName)
    {
        var sb = new StringBuilder();
        var entityNameLower = char.ToLower(entityName[0]) + entityName[1..];

        sb.AppendLine("using AutoMapper;");
        sb.AppendLine("using EfCore.Enterprise.Application.Crud;");
        sb.AppendLine($"using EfCore.Enterprise.Application.DTOs.{entityName};");
        sb.AppendLine($"using EfCore.Enterprise.Domain.Entities;");
        sb.AppendLine("using EfCore.Enterprise.Domain.Interfaces;");

        sb.AppendLine();
        sb.AppendLine($"namespace EfCore.Enterprise.Application.Services;");
        sb.AppendLine();
        sb.AppendLine($"public class {entityName}Service : CrudAppService<{entityName}, {entityName}Dto, Create{entityName}Request, Update{entityName}Request>");
        sb.AppendLine("{");
        sb.AppendLine($"    public {entityName}Service(ISuperRepository<{entityName}> repository, IMapper mapper)");
        sb.AppendLine("        : base(repository, mapper)");
        sb.AppendLine("    {");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public string GenerateControllerCode(string entityName)
    {
        var sb = new StringBuilder();
        var entityNameLower = char.ToLower(entityName[0]) + entityName[1..];
        var entityNameCamel = entityNameLower;

        sb.AppendLine("using EfCore.Enterprise.Application.Crud;");
        sb.AppendLine($"using EfCore.Enterprise.Application.DTOs.{entityName};");
        sb.AppendLine($"using EfCore.Enterprise.Application.Services;");
        sb.AppendLine("using EfCore.Enterprise.Domain.Attributes;");
        sb.AppendLine("using EfCore.Enterprise.Domain.Entities;");
        sb.AppendLine("using EfCore.Enterprise.Shared.Models;");
        sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
        sb.AppendLine();
        sb.AppendLine($"namespace EfCore.Enterprise.Presentation.Controllers;");
        sb.AppendLine();
        sb.AppendLine($"[ApiController]");
        sb.AppendLine($"[Route(\"api/{entityNameLower}\")]");
        sb.AppendLine($"public class {entityName}Controller : ControllerBase");
        sb.AppendLine("{");
        sb.AppendLine($"    private readonly {entityName}Service _{entityNameCamel}Service;");
        sb.AppendLine();
        sb.AppendLine($"    public {entityName}Controller({entityName}Service {entityNameCamel}Service)");
        sb.AppendLine("    {");
        sb.AppendLine($"        _{entityNameCamel}Service = {entityNameCamel}Service;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    [OpLog(\"{entityName}管理\", \"查询{entityName}\")]");
        sb.AppendLine("    [HttpGet]");
        sb.AppendLine($"    public async Task<ApiResult<PagedResult<{entityName}Dto>>> GetPage([FromQuery] PagedRequest request)");
        sb.AppendLine($"        => ApiResult.Ok(await _{entityNameCamel}Service.GetPageAsync(request));");
        sb.AppendLine();
        sb.AppendLine($"    [OpLog(\"{entityName}管理\", \"获取{entityName}详情\", \"ID: {{id}}\")]");
        sb.AppendLine("    [HttpGet(\"{id}\")]");
        sb.AppendLine($"    public async Task<ApiResult<{entityName}Dto?>> GetById(long id)");
        sb.AppendLine($"        => ApiResult.Ok(await _{entityNameCamel}Service.GetByIdAsync(id));");
        sb.AppendLine();
        sb.AppendLine($"    [OpLog(\"{entityName}管理\", \"创建{entityName}\")]");
        sb.AppendLine("    [HttpPost]");
        sb.AppendLine($"    public async Task<ApiResult<{entityName}Dto>> Create(Create{entityName}Request request)");
        sb.AppendLine($"        => ApiResult.Ok(await _{entityNameCamel}Service.CreateAsync(request));");
        sb.AppendLine();
        sb.AppendLine($"    [OpLog(\"{entityName}管理\", \"更新{entityName}\", \"ID: {{id}}\")]");
        sb.AppendLine("    [HttpPut(\"{id}\")]");
        sb.AppendLine($"    public async Task<ApiResult<{entityName}Dto>> Update(long id, Update{entityName}Request request)");
        sb.AppendLine($"        => ApiResult.Ok(await _{entityNameCamel}Service.UpdateAsync(id, request));");
        sb.AppendLine();
        sb.AppendLine($"    [OpLog(\"{entityName}管理\", \"删除{entityName}\", \"ID: {{id}}\")]");
        sb.AppendLine("    [HttpDelete(\"{id}\")]");
        sb.AppendLine("    public async Task<ApiResult> Delete(long id)");
        sb.AppendLine("    {");
        sb.AppendLine($"        await _{entityNameCamel}Service.DeleteAsync(id);");
        sb.AppendLine("        return ApiResult.Ok();");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public string GenerateVueCode(string entityName, List<PropertyInfo> properties)
    {
        var sb = new StringBuilder();
        var entityNameLower = char.ToLower(entityName[0]) + entityName[1..];

        sb.AppendLine("<template>");
        sb.AppendLine("  <div class=\"page-container\">");
        sb.AppendLine("    <div class=\"page-header\">");
        sb.AppendLine($"      <h2>{entityName}管理</h2>");
        sb.AppendLine($"      <el-button type=\"primary\" @click=\"showCreateDialog\">新增</el-button>");
        sb.AppendLine("    </div>");
        sb.AppendLine();
        sb.AppendLine("    <div class=\"search-bar\">");
        sb.AppendLine("      <el-input v-model=\"searchForm.keyword\" placeholder=\"搜索...\" clearable style=\"width: 200px\" />");
        sb.AppendLine("      <el-button type=\"primary\" @click=\"fetchData\">查询</el-button>");
        sb.AppendLine("    </div>");
        sb.AppendLine();
        sb.AppendLine("    <el-table :data=\"tableData\" border stripe v-loading=\"loading\">");

        foreach (var prop in properties)
        {
            sb.AppendLine($"      <el-table-column prop=\"{prop.Name}\" label=\"{prop.Description ?? prop.Name}\" />");
        }

        sb.AppendLine("      <el-table-column label=\"操作\" width=\"200\">");
        sb.AppendLine("        <template #default=\"{ row }\">");
        sb.AppendLine("          <el-button size=\"small\" @click=\"showEditDialog(row)\">编辑</el-button>");
        sb.AppendLine("          <el-button size=\"small\" type=\"danger\" @click=\"handleDelete(row.id)\">删除</el-button>");
        sb.AppendLine("        </template>");
        sb.AppendLine("      </el-table-column>");
        sb.AppendLine("    </el-table>");
        sb.AppendLine();
        sb.AppendLine("    <el-pagination");
        sb.AppendLine("      v-model:current-page=\"pagination.pageIndex\"");
        sb.AppendLine("      :page-size=\"pagination.pageSize\"");
        sb.AppendLine("      :total=\"pagination.total\"");
        sb.AppendLine("      @current-change=\"fetchData\"");
        sb.AppendLine("    />");
        sb.AppendLine();
        sb.AppendLine($"    <el-dialog v-model=\"dialogVisible\" :title=\"isEdit ? '编辑' : '新增'\">");
        sb.AppendLine("      <el-form :model=\"formData\" label-width=\"100px\">");

        foreach (var prop in properties)
        {
            sb.AppendLine($"        <el-form-item label=\"{prop.Description ?? prop.Name}\">");
            sb.AppendLine($"          <el-input v-model=\"formData.{prop.Name}\" />");
            sb.AppendLine("        </el-form-item>");
        }

        sb.AppendLine("      </el-form>");
        sb.AppendLine("      <template #footer>");
        sb.AppendLine("        <el-button @click=\"dialogVisible = false\">取消</el-button>");
        sb.AppendLine("        <el-button type=\"primary\" @click=\"handleSave\">保存</el-button>");
        sb.AppendLine("      </template>");
        sb.AppendLine("    </el-dialog>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</template>");
        sb.AppendLine();
        sb.AppendLine("<script setup lang=\"ts\">");
        sb.AppendLine("import { reactive, ref } from 'vue';");
        sb.AppendLine($"import {{ getPaginated, create, update, remove }} from '@/api/{entityNameLower}';");
        sb.AppendLine();
        sb.AppendLine("const tableData = ref([]);");
        sb.AppendLine("const loading = ref(false);");
        sb.AppendLine("const dialogVisible = ref(false);");
        sb.AppendLine("const isEdit = ref(false);");
        sb.AppendLine("const currentId = ref<number | null>(null);");
        sb.AppendLine();
        sb.AppendLine("const searchForm = reactive({ keyword: '' });");
        sb.AppendLine("const pagination = reactive({ pageIndex: 1, pageSize: 10, total: 0 });");
        sb.AppendLine($"const formData = reactive({{ {string.Join(", ", properties.Select(p => p.Name + ": ''"))} }});");
        sb.AppendLine();
        sb.AppendLine("async function fetchData() {");
        sb.AppendLine("  loading.value = true;");
        sb.AppendLine("  const res = await getPaginated({ ...searchForm, ...pagination });");
        sb.AppendLine("  tableData.value = res.data.items;");
        sb.AppendLine("  pagination.total = res.data.totalCount;");
        sb.AppendLine("  loading.value = false;");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("function showCreateDialog() {");
        sb.AppendLine("  isEdit.value = false;");
        sb.AppendLine("  currentId.value = null;");
        sb.AppendLine($"  Object.assign(formData, {{ {string.Join(", ", properties.Select(p => p.Name + ": ''"))} }});");
        sb.AppendLine("  dialogVisible.value = true;");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("function showEditDialog(row: any) {");
        sb.AppendLine("  isEdit.value = true;");
        sb.AppendLine("  currentId.value = row.id;");
        sb.AppendLine("  Object.assign(formData, row);");
        sb.AppendLine("  dialogVisible.value = true;");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("async function handleSave() {");
        sb.AppendLine("  if (isEdit.value) {");
        sb.AppendLine("    await update(currentId.value!, formData);");
        sb.AppendLine("  } else {");
        sb.AppendLine("    await create(formData);");
        sb.AppendLine("  }");
        sb.AppendLine("  dialogVisible.value = false;");
        sb.AppendLine("  fetchData();");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("async function handleDelete(id: number) {");
        sb.AppendLine("  await ElMessageBox.confirm('确认删除？', '提示', { type: 'warning' });");
        sb.AppendLine("  await remove(id);");
        sb.AppendLine("  fetchData();");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("fetchData();");
        sb.AppendLine("</script>");
        return sb.ToString();
    }

    public string GenerateAllFiles(string entityName, List<PropertyInfo> properties)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// ========== Entity ==========");
        sb.AppendLine(GenerateEntityCode(entityName, properties));
        sb.AppendLine();
        sb.AppendLine("// ========== DTO ==========");
        sb.AppendLine(GenerateDtoCode(entityName, properties));
        sb.AppendLine();
        sb.AppendLine("// ========== CreateRequest ==========");
        sb.AppendLine(GenerateCreateDtoCode(entityName, properties));
        sb.AppendLine();
        sb.AppendLine("// ========== UpdateRequest ==========");
        sb.AppendLine(GenerateUpdateDtoCode(entityName, properties));
        sb.AppendLine();
        sb.AppendLine("// ========== Profile ==========");
        sb.AppendLine(GenerateProfileCode(entityName));
        sb.AppendLine();
        sb.AppendLine("// ========== Validator ==========");
        sb.AppendLine(GenerateValidatorCode(entityName, properties));
        sb.AppendLine();
        sb.AppendLine("// ========== Service ==========");
        sb.AppendLine(GenerateServiceCode(entityName));
        sb.AppendLine();
        sb.AppendLine("// ========== Controller ==========");
        sb.AppendLine(GenerateControllerCode(entityName));
        return sb.ToString();
    }

    private static string MapToCSharpType(string type)
    {
        return type.ToLower() switch
        {
            "string" => "string",
            "int" or "int32" or "number" => "int",
            "long" or "int64" => "long",
            "decimal" or "money" => "decimal",
            "double" or "float" => "double",
            "bool" or "boolean" => "bool",
            "datetime" or "datetimeoffset" => "DateTimeOffset",
            "guid" => "Guid",
            _ => "string"
        };
    }

    private static bool IsNullableType(string type)
    {
        return type.ToLower() switch
        {
            "string" => true,
            "datetime" or "datetimeoffset" => true,
            "guid" => true,
            _ => false
        };
    }

    private static List<string> GeneratePropertyCode(PropertyInfo prop)
    {
        var lines = new List<string>();
        var csharpType = MapToCSharpType(prop.Type);
        var nullable = IsNullableType(prop.Type) ? "?" : "";
        var defaultValue = GetDefaultValue(prop);

        lines.Add($"    public {csharpType}{nullable} {prop.Name} {{ get; private set; }}{defaultValue}");

        if (!string.IsNullOrEmpty(prop.Description))
            lines.Add($"    // {prop.Description}");

        return lines;
    }

    private static string GetDefaultValue(PropertyInfo prop)
    {
        if (!string.IsNullOrEmpty(prop.DefaultValue))
            return $" = {prop.DefaultValue};";

        return prop.Type.ToLower() switch
        {
            "string" => " = string.Empty;",
            "int" or "int32" or "number" => ";",
            "long" or "int64" => ";",
            "decimal" or "money" => ";",
            "double" or "float" => ";",
            "bool" or "boolean" => " = false;",
            "datetime" or "datetimeoffset" => " = DateTimeOffset.UtcNow;",
            "guid" => " = Guid.NewGuid();",
            _ => ";"
        };
    }
}

public class PropertyInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "string";
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }
}