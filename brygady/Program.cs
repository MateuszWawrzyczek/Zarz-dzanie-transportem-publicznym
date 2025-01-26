using Brygady.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Rejestracja usług
builder.Services.AddControllers(); // Dodaje kontrolery dla API
builder.Services.AddEndpointsApiExplorer(); // Dodaje wsparcie dla API Explorer (Swagger)
builder.Services.AddSwaggerGen(); // Rejestracja Swaggera (jeśli chcesz korzystać z interfejsu UI do testowania API)
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);


// Konfiguracja CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // Zezwala na połączenia z frontu działającego na porcie 3000
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Tworzenie aplikacji
var app = builder.Build();

// Konfiguracja aplikacji
if (app.Environment.IsDevelopment())
{
    // W trybie deweloperskim włączamy Swaggera i stronę testową
    app.UseSwagger(); 
    app.UseSwaggerUI(); // Interaktywny interfejs Swaggera do testowania API
}

app.UseCors("AllowReactApp"); // Aktywujemy politykę CORS, aby frontend mógł się łączyć z backendem

// Middleware do routingu
app.UseRouting();

// Mapowanie kontrolerów (do obsługi API)
app.MapControllers();

// Opcjonalne: Dodanie trasy do strony głównej (np. testowania backendu)
app.MapGet("/", () => "Witaj na stronie głównej!"); // Możesz również dodać konkretny widok, jeśli chcesz.

app.Run(); // Uruchomienie aplikacji
