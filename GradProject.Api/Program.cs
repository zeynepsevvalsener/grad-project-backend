using DotNetEnv;
using FluentValidation;
using GradProject.Api.Middlewares;
using GradProject.Application.Interfaces;
using GradProject.Application.Interfaces.Nutrition;
using GradProject.Application.Utilities;
using GradProject.Application.Validators.Auth;
using GradProject.Infrastructure.Persistence;
using GradProject.Infrastructure.Services;
using GradProject.Infrastructure.Services.Nutrition;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;



DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// DbContext (PostgreSQL)
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// JwtOptions (appsettings.json -> "Jwt")
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

// DI - Services
builder.Services.AddSingleton<GradProject.Infrastructure.Services.StravaApiService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFoodService, FoodService>();
builder.Services.AddScoped<IConsumedFoodService, ConsumedFoodService>();
builder.Services.AddSingleton<ITdeeCalculator, TdeeCalculator>();
builder.Services.AddScoped<INutritionCalculationService, NutritionCalculationService>();
builder.Services.AddSingleton<IBmiCalculator, BmiCalculator>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<INutritionTargetsService, NutritionTargetsService>();
builder.Services.AddScoped<IFoodSearchService, FoodSearchService>();
builder.Services.AddScoped<IRunActivityService, RunActivityService>();




//  Exception middleware DI
builder.Services.AddTransient<ExceptionHandlingMiddleware>();

// Controllers +  enums as strings
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// FluentValidation validators DI
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestDtoValidator>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GradProject API",
        Version = "v1"
    });

    // 🔐 JWT Bearer definition
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header. Example: \"Bearer {token}\""
    });

    // 🔐 Apply Bearer globally
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});


// JWT Auth
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
          ?? throw new InvalidOperationException("Jwt configuration is missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,

            ValidateAudience = true,
            ValidAudience = jwt.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCorsPolicy", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "http://localhost:19006",
                "http://localhost:8081"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}


//  Exception handling
app.UseMiddleware<ExceptionHandlingMiddleware>();

// order important
app.UseCors("DevCorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// DIŞ SERVİSE TEK SEFERLİK STRAVA İSTEK
using (var scope = app.Services.CreateScope())
{
    // var stravaService = scope.ServiceProvider.GetRequiredService<GradProject.Infrastructure.Services.StravaApiService>();
    // var result = await stravaService.GetAthleteInfo();
    // Console.WriteLine(result);
}

app.Run();
