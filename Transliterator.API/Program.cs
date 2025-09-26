using Transliterator.Core.Repositories;
using Transliterator.Domain.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<Transliterator.Core.Models.StorageSettings>(
    builder.Configuration.GetSection("StorageSettings"));

// Register dependencies
builder.Services.AddScoped<IProfileRepository, JsonProfileRepository>();
builder.Services.AddScoped<ITransliterationService, TransliterationService>();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
