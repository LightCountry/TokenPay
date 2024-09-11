using Flurl.Http;
using Flurl.Http.Newtonsoft;
using FreeSql;
using Microsoft.AspNetCore.Mvc.Razor;
using Serilog;
using Serilog.Events;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using TokenPay.BgServices;
using TokenPay.Domains;
using TokenPay.Helper;
using TokenPay.Models.EthModel;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.File("logs/log-.log", rollingInterval: RollingInterval.Day)
    .WriteTo.Console()
    .CreateBootstrapLogger();
Assembly assembly = Assembly.GetExecutingAssembly();
var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
Log.Information("-------------{value}-------------", "TokenPay Info");
Log.Information("File Version: {value}", fileVersionInfo.FileVersion);
Log.Information("Product Version: {value}", fileVersionInfo.ProductVersion);
Log.Information("-------------{value}-------------", "System Info");
Log.Information("Platform: {value}", (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux" :
                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "OSX" :
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" : "Unknown"));
Log.Information("Architecture: {value}", RuntimeInformation.OSArchitecture);
Log.Information("Description: {value}", RuntimeInformation.OSDescription);
Log.Information("ProcessArchitecture: {value}", RuntimeInformation.ProcessArchitecture);
Log.Information("X64: {value}", (Environment.Is64BitOperatingSystem ? "Yes" : "No"));
Log.Information("CPU CORE: {value}", Environment.ProcessorCount);
Log.Information("HostName: {value}", Environment.MachineName);
Log.Information("OSVersion: {value}", Environment.OSVersion);
Log.Information("IsServerGC: {value}", GCSettings.IsServerGC);
Log.Information("IsConcurrent: {value}", GC.GetGCMemoryInfo().Concurrent);
Log.Information("LatencyMode: {value}", GCSettings.LatencyMode);
Log.Information("-------------{value}-------------", "Info End");

var builder = WebApplication.CreateBuilder(args);
var Services = builder.Services;
var Configuration = builder.Configuration;
QueryTronAction.configuration = Configuration;
Configuration.AddJsonFile("EVMChains.json", optional: true, reloadOnChange: true);
if (!builder.Environment.IsProduction())
    Configuration.AddJsonFile($"EVMChains.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

builder.Host.UseSerilog((context, services, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .WriteTo.File("logs/log-.log", rollingInterval: RollingInterval.Day)
                    .WriteTo.Console()
                    );
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation()
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
var EVMChains = Configuration.GetSection("EVMChains").Get<List<EVMChain>>() ?? new List<EVMChain>();
Services.AddSingleton(EVMChains);

var connectionString = Configuration.GetConnectionString("DB");
IFreeSql fsql = new FreeSqlBuilder()
        .UseConnectionString(FreeSql.DataType.Sqlite, connectionString)
        .UseAutoSyncStructure(true) //自动同步实体结构
        .UseNoneCommandParameter(true)
        .Build();

Services.AddSingleton(fsql);
Services.AddScoped<UnitOfWorkManager>();
Services.AddFreeRepository();
Services.AddHostedService<OrderExpiredService>();
Services.AddHostedService<UpdateRateService>();
Services.AddHostedService<OrderNotifyService>();
Services.AddHostedService<OrderPaySuccessService>();
Services.AddHostedService<OrderCheckTRC20Service>();
Services.AddHostedService<OrderCheckTRXService>();
Services.AddHostedService<OrderCheckEVMBaseService>();
Services.AddHostedService<OrderCheckEVMERC20Service>();
Services.AddHostedService<CollectionTRONService>();
Services.AddHttpContextAccessor();
Services.AddEndpointsApiExplorer();
Services.AddSwaggerGen(c =>
{
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
});
Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new List<CultureInfo>
            {
                new CultureInfo("en"),
                new CultureInfo("zh"),
                new CultureInfo("ru")
            };

    options.SetDefaultCulture(supportedCultures[0].Name);
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
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
// 订单广播 
var channel = Channel.CreateUnbounded<TokenOrders>();
Services.AddSingleton(channel);

var WebProxy = Configuration.GetValue<string>("WebProxy");
FlurlHttp.Clients.UseNewtonsoft();
if (!string.IsNullOrEmpty(WebProxy))
{
    FlurlHttp.Clients.WithDefaults(c =>
    {
        c.AddMiddleware(() => new ProxyHttpClientFactory(WebProxy));
    });
}


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
app.UseRouting();

app.UseAuthorization();
app.UseRequestLocalization();
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