using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Trackr.Api.Data;
using Trackr.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IIssueService, IssueService>();

builder.Services.AddDbContext<TrackrDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("TrackrDatabase"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

public partial class Program { } // For testing purposes
