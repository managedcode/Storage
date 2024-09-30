using TestAssigmentClient;
using TestAssigmentClient.Services;
using TestAssigmentClient.Services.Abstraction;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddScoped<IBlobClientService, BlobClientService>();
builder.Services.AddHttpClient<IBlobClientService>();

var app = builder.Build();
app.Run();
