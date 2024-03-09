using Gantt.Bot.Api;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddCors();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseHealthChecks("/health");
//app.UseHttpsRedirection();
app.UseCors(config => config.AllowAnyOrigin().WithMethods("GET", "POST").AllowAnyHeader());
app.MapPost("/simulation/run", SimulationApi.RunSimulation)
    .WithName("RunSimulation")
    .Produces<SimulationResult>()
    .Produces(StatusCodes.Status400BadRequest)
    .WithOpenApi();

app.Run();