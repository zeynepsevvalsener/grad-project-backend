using DotNetEnv;
using FluentValidation;
using GradProject.Api.Middlewares;
using GradProject.Application.Interfaces;
using GradProject.Application.Interfaces.Nutrition;
using GradProject.Application.Interfaces.Gamification;
using GradProject.Application.Interfaces.Running;
using GradProject.Application.Interfaces.Geometry;
using GradProject.Application.Services.Polyline;
using GradProject.Application.Services.Geometry;
using GradProject.Application.Utilities;
using GradProject.Application.Validators.Auth;
using GradProject.Infrastructure.Persistence;
using GradProject.Infrastructure.Services;
using GradProject.Infrastructure.Services.Geometry;
using GradProject.Infrastructure.Services.Nutrition;
using GradProject.Infrastructure.Services.Gamification;
using GradProject.Infrastructure.Services.Running;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using GradProject.Application.Interfaces.Nutrition.AI;
using GradProject.Infrastructure.Services.Nutrition.AI;




DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);
var aiUrl = builder.Configuration.GetValue<string>("AiServiceSettings:BaseUrl") ?? "http://127.0.0.1:8000";
// DbContext (PostgreSQL + PostGIS)
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default"), o => o.UseNetTopologySuite()));

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
builder.Services.AddScoped<IDailyIntakeAggregationService, DailyIntakeAggregationService>();
//builder.Services.AddScoped<IMealParsingService, MealParsingService>(); şimdilik alttakine çevirdim denemek için.
builder.Services.AddHttpClient<IMealParsingService, MealParsingService>(client =>{client.BaseAddress = new Uri(aiUrl);});
builder.Services.AddScoped<IMealService, MealService>();
builder.Services.AddScoped<IChallengeService, ChallengeService>();
builder.Services.AddScoped<IBadgeService, BadgeService>();
builder.Services.AddScoped<IRunningAnalyticsService, RunningAnalyticsService>();
builder.Services.AddSingleton<PolylineDecoder>();
builder.Services.AddSingleton<GeometryConverter>();
builder.Services.AddScoped<IBoundingBoxService, BoundingBoxService>();
builder.Services.AddScoped<IConvexHullService, ConvexHullService>();
builder.Services.AddScoped<IRouteService, RouteService>();




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

// Rate Limiting (prepared for future use)
// Uncomment when needed:
// builder.Services.AddRateLimiter(options =>
// {
//     options.AddFixedWindowLimiter("RoutePolicy", opt =>
//     {
//         opt.Window = TimeSpan.FromMinutes(1);
//         opt.PermitLimit = 60; // 60 requests per minute
//         opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
//         opt.QueueLimit = 10;
//     });
// });
// 
// Then add to endpoint:
// [EnableRateLimiting("RoutePolicy")]

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

// Static files (for route visualizer)
app.UseStaticFiles();

// order important
app.UseCors("DevCorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var mealParsingService = scope.ServiceProvider.GetRequiredService<IMealParsingService>();

        mealParsingService.SyncFoodsToAiAsync().Wait();

        Console.WriteLine("SUCCESS: Foods synced to AI service.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"WARNING: Could not sync foods to AI service. Is Python running? Error: {ex.Message}");
    }
}

app.Run();
