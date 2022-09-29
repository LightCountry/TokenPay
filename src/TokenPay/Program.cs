
using Exceptionless;
using FreeSql;
using FreeSql.DataAnnotations;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;
using Serilog.Events;
using System.Data.Common;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using TokenPay.BgServices;
using TokenPay.Domains;
using TokenPay.Helper;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);
var Services = builder.Services;
var Configuration = builder.Configuration;


builder.Host.UseSerilog((context, services, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .WriteTo.Exceptionless(b => b.AddTags("Serilog"))
                    );
builder.Services.AddControllersWithViews()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

var connectionString = Configuration.GetConnectionString("DB");

IFreeSql fsql = new FreeSqlBuilder()
    .UseConnectionString(FreeSql.DataType.Sqlite, connectionString)
    .UseAutoSyncStructure(true) //自动同步实体结构
    .Build();

Services.AddSingleton(fsql);
Services.AddScoped<UnitOfWorkManager>();
Services.AddFreeRepository();
Services.AddHostedService<OrderExpiredService>();
Services.AddHostedService<UpdateRateService>();
Services.AddHostedService<OrderNotifyService>();
Services.AddHostedService<OrderCheckTRC20Service>();
Services.AddHostedService<OrderCheckTRXService>();
Services.AddHostedService<OrderCheckETHService>();
Services.AddHostedService<OrderCheckERC20Service>();
Services.AddExceptionless(Configuration);
Services.AddHttpContextAccessor();
Services.AddEndpointsApiExplorer();
Services.AddSwaggerGen(c =>
{
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
});
Services.AddSingleton(s =>
{
    var bot = new TelegramBot(Configuration);
    try
    {
        var me = bot.GetMeAsync().GetAwaiter().GetResult();
    }
    catch (Exception e)
    {
        Log.Logger.Error(e, "机器人连接失败！");
        throw;
    }
    return bot;
});


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
}
else
{
    app.UseExceptionHandler("/error-development");
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseStaticFiles();
app.UseExceptionless();
app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
try
{
    Log.Information("Starting web host");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
