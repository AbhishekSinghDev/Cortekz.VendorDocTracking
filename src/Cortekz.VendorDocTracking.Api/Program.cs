using System.Text.Json.Serialization;
using Cortekz.VendorDocTracking.Api.Data;
using Cortekz.VendorDocTracking.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

MongoConventions.Register();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("MongoSettings"));
builder.Services.AddSingleton<IMongoClient>(_ =>
    new MongoClient(builder.Configuration.GetConnectionString("Mongo")));
builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var settings = sp.GetRequiredService<IOptions<MongoSettings>>().Value;
    return client.GetDatabase(settings.DatabaseName);
});
builder.Services.AddSingleton<SubmissionRepository>();
builder.Services.AddScoped<PurchaseOrderService>();
builder.Services.AddScoped<RequirementService>();
builder.Services.AddScoped<SubmissionService>();

builder.Services.Configure<AiReviewWorkerSettings>(builder.Configuration.GetSection("AiReviewWorker"));
builder.Services.AddHttpClient<IAiReviewClient, AiReviewClient>((sp, client) =>
{
    var baseUrl = builder.Configuration["AiReviewService:BaseUrl"]
        ?? throw new InvalidOperationException("AiReviewService:BaseUrl is not configured.");
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");

    var workerSettings = sp.GetRequiredService<IOptions<AiReviewWorkerSettings>>().Value;
    client.Timeout = TimeSpan.FromSeconds(workerSettings.RequestTimeoutSeconds);
});
builder.Services.AddHostedService<AiReviewJobWorker>();

var app = builder.Build();

using (var startupScope = app.Services.CreateScope())
{
    var submissionRepository = startupScope.ServiceProvider.GetRequiredService<SubmissionRepository>();
    await submissionRepository.EnsureIndexesAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
