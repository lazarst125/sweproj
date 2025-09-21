using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SWETemplate.Models;
using SWETemplate.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<SweContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SWE"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("CORS", policy =>
    {
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .WithOrigins("http://localhost:5500", "https://localhost:5500", 
                         "http://127.0.0.1:5500", "https://127.0.0.1:5500",
                         "http://localhost:3000", "https://localhost:3000",
                         "http://127.0.0.1:3000", "https://127.0.0.1:3000",
                         "http://localhost:5173", "https://localhost:5173",
                         "http://127.0.0.1:5173", "https://127.0.0.1:5173");
    });
});

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JWT Configuration
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? "DefaultSuperSecretKeyForDevelopment12345");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    
    // Dodajte za debug svrhe
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("Token validated successfully");
            return Task.CompletedTask;
        }
    };
});

// Registracija servisa
builder.Services.AddScoped<IAdminService, AdminService>();

// Dodajte autorizaciju - moramo da ubacim JWT za ovo!!!!!!!!!!!! 
// builder.Services.AddAuthorization(options =>
// {
//     options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
//     options.AddPolicy("RequireDonorRole", policy => policy.RequireRole("Donor"));
//     options.AddPolicy("RequireOrganizerRole", policy => policy.RequireRole("Organizer"));
// });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Dodajte CORS za development
    app.UseCors("CORS");
    
    // Samo osigurajte da baza postoji, bez automatskog popunjavanja
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<SweContext>();
        context.Database.EnsureCreated();
    }
}
else
{
    app.UseCors("CORS");
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();