using NexodusAPI.Models;

var builder = WebApplication.CreateBuilder(args);
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDb") ?? "mongodb://admin:admin@localhost:27017"; // Nuh uh, no way someone is thinking we are using this in production. Good luck with locating the dev server first.
var mongoDatabaseName = "nexodus";

builder.Services.AddSingleton(new UserContext(mongoConnectionString, mongoDatabaseName));

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
