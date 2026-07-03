using EfCore.Enterprise.Shared.DependencyInjection;
using System.Text;

namespace EfCore.Enterprise.Infrastructure.Services;

public interface ICodeGeneratorService
{
    string GenerateEntityCode(string entityName, List<PropertyInfo> properties);
    string GenerateRepositoryCode(string entityName);
    string GenerateServiceCode(string entityName);
    string GenerateControllerCode(string entityName);
    string GenerateVueComponentCode(string entityName, List<PropertyInfo> properties);
    string GenerateReactComponentCode(string entityName, List<PropertyInfo> properties);
}

[Injectable(ServiceLifetime.Singleton)]
public class CodeGeneratorService : ICodeGeneratorService
{
    public string GenerateEntityCode(string entityName, List<PropertyInfo> properties)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using EfCore.Enterprise.Domain.Entities;");
        sb.AppendLine();
        sb.AppendLine($"namespace EfCore.Enterprise.Domain.Entities;");
        sb.AppendLine();
        sb.AppendLine($"public class {entityName} : BaseFullEntity");
        sb.AppendLine("{");

        foreach (var prop in properties)
        {
            sb.AppendLine($"    public {prop.Type} {prop.Name} {{ get; set; }}");
            if (!string.IsNullOrEmpty(prop.DefaultValue))
            {
                sb.AppendLine($" = {prop.DefaultValue};");
            }
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    public string GenerateRepositoryCode(string entityName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using EfCore.Enterprise.Domain.Entities;");
        sb.AppendLine("using EfCore.Enterprise.Infrastructure.Data;");
        sb.AppendLine("using EfCore.Enterprise.Infrastructure.Data;");
        sb.AppendLine();
        sb.AppendLine($"namespace EfCore.Enterprise.Infrastructure.Data;");
        sb.AppendLine();
        sb.AppendLine($"public class {entityName}Repository : SuperRepository<{entityName}>");
        sb.AppendLine("{");
        sb.AppendLine($"    public {entityName}Repository(AppDbContext context) : base(context)");
        sb.AppendLine("    {");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public string GenerateServiceCode(string entityName)
    {
        var sb = new StringBuilder();
        var entityNameLower = char.ToLower(entityName[0]) + entityName[1..];

        sb.AppendLine("using AutoMapper;");
        sb.AppendLine("using EfCore.Enterprise.Application.Services;");
        sb.AppendLine("using EfCore.Enterprise.Domain.Entities;");
        sb.AppendLine("using EfCore.Enterprise.Domain.Interfaces;");
        sb.AppendLine("using EfCore.Enterprise.Infrastructure.Data;");
        sb.AppendLine("using EfCore.Enterprise.Shared.Models;");
        sb.AppendLine();
        sb.AppendLine($"namespace EfCore.Enterprise.Application.Services;");
        sb.AppendLine();
        sb.AppendLine($"public class {entityName}Service : BaseService");
        sb.AppendLine("{");
        sb.AppendLine($"    private readonly {entityName}Repository _{entityNameLower}Repository;");
        sb.AppendLine();
        sb.AppendLine($"    public {entityName}Service(");
        sb.AppendLine($"        {entityName}Repository {entityNameLower}Repository,");
        sb.AppendLine("        IUnitOfWork unitOfWork,");
        sb.AppendLine("        IMapper mapper) : base(unitOfWork, mapper)");
        sb.AppendLine("    {");
        sb.AppendLine($"        _{entityNameLower}Repository = {entityNameLower}Repository;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public async Task<ApiResult<PagedResult<{entityName}>>> GetPagedAsync(PagedRequest request)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var result = await _{entityNameLower}Repository.GetPagedAsync(e => true, request);");
        sb.AppendLine("        return ApiResult<PagedResult<{entityName}>>.Success(result);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public async Task<ApiResult<{entityName}>> GetByIdAsync(long id)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var entity = await _{entityNameLower}Repository.GetByIdAsync(id);");
        sb.AppendLine("        return entity == null");
        sb.AppendLine("            ? ApiResult<{entityName}>.Fail(Shared.Enums.ErrorCode.NotFound)");
        sb.AppendLine($"            : ApiResult<{entityName}>.Ok(entity);");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public string GenerateControllerCode(string entityName)
    {
        var sb = new StringBuilder();
        var entityNameLower = char.ToLower(entityName[0]) + entityName[1..];

        sb.AppendLine("using EfCore.Enterprise.Application.Services;");
        sb.AppendLine("using EfCore.Enterprise.Presentation.Controllers;");
        sb.AppendLine("using EfCore.Enterprise.Shared.Models;");
        sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
        sb.AppendLine();
        sb.AppendLine($"namespace EfCore.Enterprise.Presentation.Controllers;");
        sb.AppendLine();
        sb.AppendLine($"[Route(\"api/{entityNameLower}\")]");
        sb.AppendLine($"public class {entityName}Controller : BaseApiController");
        sb.AppendLine("{");
        sb.AppendLine($"    private readonly {entityName}Service _{entityNameLower}Service;");
        sb.AppendLine();
        sb.AppendLine($"    public {entityName}Controller({entityName}Service {entityNameLower}Service)");
        sb.AppendLine("    {");
        sb.AppendLine($"        _{entityNameLower}Service = {entityNameLower}Service;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    [HttpGet]");
        sb.AppendLine("    public async Task<IActionResult> GetPaged([FromQuery] PagedRequest request)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var result = await _{entityNameLower}Service.GetPagedAsync(request);");
        sb.AppendLine("        return Ok(result);");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public string GenerateVueComponentCode(string entityName, List<PropertyInfo> properties)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<template>");
        sb.AppendLine("  <div class=\"page-container\">");
        sb.AppendLine($"    <h2>{entityName}管理</h2>");
        sb.AppendLine("    <el-table :data=\"tableData\" border stripe>");

        foreach (var prop in properties)
        {
            sb.AppendLine($"      <el-table-column prop=\"{prop.Name}\" label=\"{prop.Name}\" />");
        }

        sb.AppendLine("    </el-table>");
        sb.AppendLine("    <el-pagination layout=\"prev, pager, next\" :total=\"total\" />");
        sb.AppendLine("  </div>");
        sb.AppendLine("</template>");
        return sb.ToString();
    }

    public string GenerateReactComponentCode(string entityName, List<PropertyInfo> properties)
    {
        var sb = new StringBuilder();
        sb.AppendLine("import React, { useState, useEffect } from 'react';");
        sb.AppendLine("import { Table, Pagination } from 'antd';");
        sb.AppendLine();
        sb.AppendLine($"const {entityName}Page: React.FC = () => {{");
        sb.AppendLine("  const [data, setData] = useState([]);");
        sb.AppendLine("  const [total, setTotal] = useState(0);");
        sb.AppendLine();
        sb.AppendLine("  const columns = [");

        foreach (var prop in properties)
        {
            sb.AppendLine($"    {{ title: '{prop.Name}', dataIndex: '{prop.Name}', key: '{prop.Name}' }},");
        }

        sb.AppendLine("  ];");
        sb.AppendLine();
        sb.AppendLine("  return (");
        sb.AppendLine("    <div>");
        sb.AppendLine($"      <h2>{entityName}管理</h2>");
        sb.AppendLine("      <Table columns={columns} dataSource={data} />");
        sb.AppendLine("      <Pagination total={total} />");
        sb.AppendLine("    </div>");
        sb.AppendLine("  );");
        sb.AppendLine("};");
        sb.AppendLine();
        sb.AppendLine($"export default {entityName}Page;");
        return sb.ToString();
    }
}

public class PropertyInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "string";
    public string? DefaultValue { get; set; }
}
