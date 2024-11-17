using Evacuation.Helpers;
using Evacuation.Transformers;
using MediatR;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

var builder = WebApplication.CreateBuilder(args);

var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "testTtSoftware";
});

builder.Services.AddControllers(options =>
{
    options.Conventions.Add(new RouteTokenTransformerConvention(new LowerCaseParameterTransformer()));
});
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<Helper>();

var app = builder.Build();

// if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
// {
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API v1");
        c.RoutePrefix = string.Empty; 
    });
// }

app.UseHttpsRedirection();
app.UseAuthorization();

// app.MapGet("/", () => "Hello World");

app.MapControllers();

app.Run();
