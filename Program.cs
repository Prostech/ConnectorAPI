using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using RozitekAPIConnector.Middleware;
using RozitekAPIConnector.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var appConfigSection = builder.Configuration.GetSection("Logging:AppSettings");
builder.Services.Configure<AppSettings>(appConfigSection);

// CORS
builder.Services.AddCors(o => o.AddPolicy("CorsPolicy", builder =>
{
    builder.WithOrigins() // specify allowed origins
           .AllowAnyMethod()
           .AllowAnyHeader()
           .SetIsOriginAllowed(origin => true) // allow any origin
           .AllowCredentials()
           .Build();
}));

builder.Services.AddControllers().AddNewtonsoftJson();

var app = builder.Build();

// Configure the HTTP request pipeline.

if (app.Environment.IsDevelopment())
{
    // Additional development-specific configuration can be added here
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<Middleware>();

app.UseCors("CorsPolicy");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
