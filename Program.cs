using Microsoft.EntityFrameworkCore;
using Serilog;
using UserManagementAPI.Data;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/api-log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog(); // Usar Serilog en la aplicación

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddDbContext<UserContext>(options => options.UseInMemoryDatabase("UserDatabase"));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
//builder.Services.AddOpenApi();

var app = builder.Build();

# region Seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<UserContext>();
    context.Database.EnsureCreated();
}
# endregion

// Middleware para manejar excepciones y registrar errores
/*app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        if (exception != null)
        {
            Log.Error(exception, "Se ha producido un error no controlado.");
        }
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Ha ocurrido un error en el servidor.");
    });
});*/

app.UseSerilogRequestLogging(); // Habilitar logging de solicitudes

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "UserApi v1"));
    //app.MapOpenApi();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<JwtAuthenticationMiddleware>(builder.Configuration.GetSection("Jwt:Secret").Value!.ToString());
app.UseMiddleware<RequestResponseLoggingMiddleware>();
app.UseRouting();
app.UseAuthorization();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

//app.UseHttpsRedirection();

app.Run();
