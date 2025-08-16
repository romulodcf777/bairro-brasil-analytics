using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using BairroBrasilAnalytics.Data;
using BairroBrasilAnalytics.Models;
using BairroBrasilAnalytics.Dtos;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Configurando os serviços
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite("Data Source=app.db"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API do Sistema Bairro Brasil", Version = "v1.0" });
});

// CORS pra permitir requisições do frontend
builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(p =>
        p.AllowAnyOrigin()
         .AllowAnyHeader()
         .AllowAnyMethod());
});

var app = builder.Build();

// Configurando o pipeline
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseDefaultFiles(); // serve o index.html automaticamente
app.UseStaticFiles();

// Criando o banco e inserindo dados iniciais
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // Atualizar registros existentes com nomes antigos
    var registrosAntigos = db.Records.Where(r => 
        r.Source == "Academia Forma Brasil" || 
        r.Source == "Lanchonete do Seu José" || 
        r.Source == "Loja da Dona Maria").ToList();
    
    foreach (var registro in registrosAntigos)
    {
        if (registro.Source == "Academia Forma Brasil")
            registro.Source = "Academia Adrena";
        else if (registro.Source == "Lanchonete do Seu José")
            registro.Source = "Lanchonete da Cau";
        else if (registro.Source == "Loja da Dona Maria")
            registro.Source = "Armarinho Eliana";
    }
    
    if (registrosAntigos.Any())
    {
        db.SaveChanges();
    }

    // Se não tem categorias ainda, vamos criar algumas básicas
    if (!db.Categories.Any())
    {
        var mensalidade = new Category { Name = "Mensalidade" };
        var servico = new Category { Name = "Serviço" };
        var produto = new Category { Name = "Produto" };
        var alimentacao = new Category { Name = "Alimentação" };

        db.Categories.AddRange(mensalidade, servico, produto, alimentacao);
        db.SaveChanges();

        // Inserindo alguns registros de exemplo baseados em negócios reais do bairro
        var agora = DateTime.UtcNow.Date;
        db.Records.AddRange(
            // Academia Forma Brasil
            new Record { Timestamp = agora.AddDays(-10).AddHours(8), Source = "Academia Adrena", CategoryId = mensalidade.Id, Amount = 119.90m, Notes = "Mensalidade - João Silva" },
            new Record { Timestamp = agora.AddDays(-9).AddHours(19), Source = "Academia Adrena", CategoryId = servico.Id, Amount = 45.00m, Notes = "Personal trainer - 1h" },
            new Record { Timestamp = agora.AddDays(-8).AddHours(7), Source = "Academia Adrena", CategoryId = mensalidade.Id, Amount = 89.90m, Notes = "Plano estudante - Maria" },
            
            // Lanchonete do Seu José
            new Record { Timestamp = agora.AddDays(-7).AddHours(12), Source = "Lanchonete da Cau", CategoryId = alimentacao.Id, Amount = 28.50m, Notes = "Marmitex grande + refrigerante" },
            new Record { Timestamp = agora.AddDays(-6).AddHours(14), Source = "Lanchonete da Cau", CategoryId = alimentacao.Id, Amount = 15.00m, Notes = "Sanduíche natural" },
            new Record { Timestamp = agora.AddDays(-5).AddHours(11), Source = "Lanchonete da Cau", CategoryId = alimentacao.Id, Amount = 42.00m, Notes = "Almoço executivo para 2" },
            
            // Loja da Dona Maria
            new Record { Timestamp = agora.AddDays(-4).AddHours(16), Source = "Armarinho Eliana", CategoryId = produto.Id, Amount = 85.00m, Notes = "Camiseta + bermuda infantil" },
            new Record { Timestamp = agora.AddDays(-3).AddHours(10), Source = "Armarinho Eliana", CategoryId = produto.Id, Amount = 120.00m, Notes = "Calça jeans feminina" },
            new Record { Timestamp = agora.AddDays(-2).AddHours(15), Source = "Armarinho Eliana", CategoryId = produto.Id, Amount = 65.00m, Notes = "Kit 3 camisetas masculinas" },
            
            // Registros mais recentes
            new Record { Timestamp = agora.AddDays(-1).AddHours(9), Source = "Academia Adrena", CategoryId = servico.Id, Amount = 25.00m, Notes = "Aula avulsa de spinning" },
            new Record { Timestamp = agora.AddHours(-6), Source = "Lanchonete da Cau", CategoryId = alimentacao.Id, Amount = 32.50m, Notes = "Combo hambúrguer" }
        );
        db.SaveChanges();
    }
}

// Definindo as rotas da API
app.MapGet("/health", () => Results.Ok(new { status = "funcionando", timestamp = DateTime.Now }));

// Rotas para categorias
app.MapGet("/api/categories", async (AppDbContext db) =>
{
    var categorias = await db.Categories
        .OrderBy(c => c.Name)
        .Select(c => new { c.Id, c.Name })
        .ToListAsync();
    return Results.Ok(categorias);
});

app.MapPost("/api/categories", async (AppDbContext db, CategoryCreateDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest(new { erro = "Nome da categoria é obrigatório" });

    var jaExiste = await db.Categories.AnyAsync(c => c.Name.ToLower() == dto.Name.Trim().ToLower());
    if (jaExiste) 
        return Results.Conflict(new { erro = "Essa categoria já existe" });

    var novaCategoria = new Category { Name = dto.Name.Trim() };
    db.Categories.Add(novaCategoria);
    await db.SaveChangesAsync();
    
    return Results.Created($"/api/categories/{novaCategoria.Id}", new { novaCategoria.Id, novaCategoria.Name });
});

app.MapGet("/api/records", async (AppDbContext db, string? from, string? to, string? source, string? category) =>
{
    var q = db.Records
        .Include(r => r.Category)
        .AsQueryable();

    if (!string.IsNullOrWhiteSpace(from) && DateTime.TryParse(from, out var fromDt))
        q = q.Where(r => r.Timestamp >= fromDt);

    if (!string.IsNullOrWhiteSpace(to) && DateTime.TryParse(to, out var toDt))
        q = q.Where(r => r.Timestamp <= toDt);

    if (!string.IsNullOrWhiteSpace(source))
        q = q.Where(r => r.Source.ToLower().Contains(source.ToLower()));

    if (!string.IsNullOrWhiteSpace(category))
        q = q.Where(r => r.Category != null && r.Category.Name.ToLower().Contains(category.ToLower()));

    var list = await q
        .OrderByDescending(r => r.Timestamp)
        .Select(r => new {
            r.Id,
            Timestamp = r.Timestamp,
            Source = r.Source,
            Category = r.Category != null ? r.Category.Name : null,
            r.Amount,
            r.Notes
        })
        .ToListAsync();

    return Results.Ok(list);
});

app.MapPost("/api/records", async (AppDbContext db, RecordCreateDto dto) =>
{
    if (dto.Timestamp == default) dto.Timestamp = DateTime.UtcNow;
    if (string.IsNullOrWhiteSpace(dto.Source))
        return Results.BadRequest(new { erro = "Estabelecimento é obrigatório" });
    if (string.IsNullOrWhiteSpace(dto.CategoryName))
        return Results.BadRequest(new { erro = "Categoria é obrigatória" });

    var categoria = await db.Categories.FirstOrDefaultAsync(c => c.Name.ToLower() == dto.CategoryName.ToLower());
    if (categoria == null)
    {
        categoria = new Category { Name = dto.CategoryName.Trim() };
        db.Categories.Add(categoria);
        await db.SaveChangesAsync();
    }

    var novoRegistro = new Record
    {
        Timestamp = dto.Timestamp,
        Source = dto.Source.Trim(),
        CategoryId = categoria.Id,
        Amount = dto.Amount,
        Notes = dto.Notes?.Trim()
    };
    db.Records.Add(novoRegistro);
    await db.SaveChangesAsync();

    return Results.Created($"/api/records/{novoRegistro.Id}", new { novoRegistro.Id });
});

app.MapPut("/api/records/{id:int}", async (AppDbContext db, int id, RecordUpdateDto dto) =>
{
    var rec = await db.Records.FindAsync(id);
    if (rec == null) return Results.NotFound();

    if (dto.Timestamp.HasValue) rec.Timestamp = dto.Timestamp.Value;
    if (!string.IsNullOrWhiteSpace(dto.Source)) rec.Source = dto.Source.Trim();
    if (dto.Amount.HasValue) rec.Amount = dto.Amount.Value;
    if (dto.Notes != null) rec.Notes = dto.Notes.Trim();

    if (!string.IsNullOrWhiteSpace(dto.CategoryName))
    {
        var cat = await db.Categories.FirstOrDefaultAsync(c => c.Name.ToLower() == dto.CategoryName.ToLower());
        if (cat == null)
        {
            cat = new Category { Name = dto.CategoryName.Trim() };
            db.Categories.Add(cat);
            await db.SaveChangesAsync();
        }
        rec.CategoryId = cat.Id;
    }

    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/api/records/{id:int}", async (AppDbContext db, int id) =>
{
    var rec = await db.Records.FindAsync(id);
    if (rec == null) return Results.NotFound();
    db.Records.Remove(rec);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapGet("/api/records/export.csv", async (AppDbContext db, HttpResponse res, string? from, string? to, string? source, string? category) =>
{
    var q = db.Records.Include(r => r.Category).AsQueryable();

    if (!string.IsNullOrWhiteSpace(from) && DateTime.TryParse(from, out var fromDt))
        q = q.Where(r => r.Timestamp >= fromDt);

    if (!string.IsNullOrWhiteSpace(to) && DateTime.TryParse(to, out var toDt))
        q = q.Where(r => r.Timestamp <= toDt);

    if (!string.IsNullOrWhiteSpace(source))
        q = q.Where(r => r.Source.ToLower().Contains(source.ToLower()));

    if (!string.IsNullOrWhiteSpace(category))
        q = q.Where(r => r.Category != null && r.Category.Name.ToLower().Contains(category.ToLower()));

    res.Headers.ContentDisposition = "attachment; filename=registros.csv";
    res.ContentType = "text/csv; charset=utf-8";

    await res.WriteAsync("Id,Timestamp,Source,Category,Amount,Notes\n");
    await foreach (var r in q.AsAsyncEnumerable())
    {
        var line = string.Join(",", new string[] {
            r.Id.ToString(),
            r.Timestamp.ToString("s", CultureInfo.InvariantCulture),
            EscapeCsv(r.Source),
            EscapeCsv(r.Category?.Name ?? ""),
            r.Amount.ToString(CultureInfo.InvariantCulture),
            EscapeCsv(r.Notes ?? "")
        });
        await res.WriteAsync(line + "\n");
    }
});

app.Run();

// Helpers
static string EscapeCsv(string input)
{
    if (input == null) return "";
    var needsQuotes = input.Contains(",") || input.Contains("\"") || input.Contains("\n");
    var value = input.Replace("\"", "\"\"");
    return needsQuotes ? $"\"{value}\"" : value;
}

// Needed for testing
public partial class Program { }