using Cortekz.MockAiReviewService.Configuration;
using Cortekz.MockAiReviewService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<MockAiSettings>(builder.Configuration.GetSection("MockAiSettings"));
builder.Services.AddSingleton<ReviewJobStore>();
builder.Services.AddScoped<ReviewJobService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
