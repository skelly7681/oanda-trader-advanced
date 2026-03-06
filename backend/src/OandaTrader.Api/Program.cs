using OandaTrader.Application;
using OandaTrader.Infrastructure;
using OandaTrader.Infrastructure.Configuration;
using OandaTrader.Api.Hubs;
using OandaTrader.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(_ => true));
});

builder.Services.AddTradingInfrastructure(builder.Configuration);
builder.Services.AddSingleton<DashboardBroadcaster>();
builder.Services.AddHostedService<HostedMarketLoop>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("frontend");
app.MapControllers();
app.MapHub<TradingHub>("/hub/trading");

app.Run();