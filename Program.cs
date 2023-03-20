using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using RozitekAPIConnector.Middleware;
using RozitekAPIConnector.Models;

var builder = WebApplication.CreateBuilder(args);


string[] apiAllowOrigin = builder.Configuration.GetSection("Logging:AppSettings:AllowOrigin").Value.Split(',');

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var appConfigSection = builder.Configuration.GetSection("Logging:AppSettings");
builder.Services.Configure<AppSettings>(appConfigSection);
//CORS
builder.Services.AddCors(o => o.AddPolicy("CorsPolicy", builder =>
{
    builder.AllowAnyMethod()
           .AllowAnyHeader()
           .AllowCredentials()
    .WithOrigins("http://127.0.0.1:5500");
}));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{

}

app.UseSwagger();
app.UseSwaggerUI();

//app.UseMiddleware<Middleware>();

app.UseCors("CorsPolicy");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
