using System.Reflection;
using TestTask.Infrastructure.Utilities;
using TestTask.Infrastructure.Abstractions;
using TestTask.Infrastructure.Configuration;
using TestTask.Infrastructure.Fabrics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMediatR(x =>
{
    x.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
});

builder.Services.Configure<RoutesConfiguration>(builder.Configuration.GetSection("ApiRoutes"));

builder.Services.AddScoped<IRestClientFabric, RestClientFabric>();
builder.Services.AddScoped<IChunkedFileTransferUtility, ChunkedFileTransferUtility>();
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