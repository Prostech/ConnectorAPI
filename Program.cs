using NLog;
using NLog.Web;
using Quartz;
using RozitekAPIConnector.Controllers;
using RozitekAPIConnector.Jobs;
using RozitekAPIConnector.Models;
using RozitekAPIConnector.Services;

// Early init of NLog to allow startup and exception logging, before host is built
var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("init main");
try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.

    builder.Host.ConfigureServices((hostContext, services) =>
    {
        services.AddControllers();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        var appConfigSection = hostContext.Configuration.GetSection("Logging:AppSettings");
        services.Configure<AppSettings>(appConfigSection);

        // CORS
        services.AddCors(o => o.AddPolicy("CorsPolicy", builder =>
        {
            builder.WithOrigins() // specify allowed origins
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .SetIsOriginAllowed(origin => true) // allow any origin
                   .AllowCredentials()
                   .Build();
        }));

        services.AddControllers().AddNewtonsoftJson();
        services.AddScoped<ConnectorController>();


        if (hostContext.Configuration.GetSection("Quartz:RunAPI:IsEnabled").Value.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            services.AddQuartz(q =>
            {
                q.UseMicrosoftDependencyInjectionScopedJobFactory();

                // Register the job, loading the schedule from configuration
                q.AddJobAndTrigger<RunAPI>(hostContext.Configuration);

            });

            services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
        }    
    });;


    // NLog: Setup NLog for Dependency injection
    //builder.Logging.ClearProviders();
    //builder.Host.UseNLog();

    var app = builder.Build();

    // Configure the HTTP request pipeline.

    if (app.Environment.IsDevelopment())
    {
        // Additional development-specific configuration can be added here
    }

    app.UseSwagger();
    app.UseSwaggerUI();

    //app.UseMiddleware<Middleware>();

    app.UseCors("CorsPolicy");

    app.UseAuthentication();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();

}
catch (Exception exception)
{
    // NLog: catch setup errors
    logger.Error(exception, "Stopped program because of exception");
    throw;
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
    NLog.LogManager.Shutdown();
}