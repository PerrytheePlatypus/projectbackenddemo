using EduSync.Configurations;
using EduSync.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Globalization;
using System.Text;

// Set the default culture to en-US
var culture = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebApplication.CreateBuilder(args);

// Add configuration sources
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// CORS configuration
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
var frontendUrls = new[]
{
    "https://localhost:3000",
    "http://localhost:3000",
    "https://127.0.0.1:3000",
    "http://127.0.0.1:3000"
};

// Add CORS policy to services
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials()
                                .WithExposedHeaders("Content-Disposition", "X-Requested-With")
                                .WithOrigins(frontendUrls)
                                .SetIsOriginAllowedToAllowWildcardSubdomains(); // More specific origin configuration
                      });
});

// Add cookie policy
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.None;
    options.OnAppendCookie = cookieContext =>
        CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
    options.OnDeleteCookie = cookieContext =>
        CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
});

// Helper method to ensure cookies are secure
void CheckSameSite(HttpContext httpContext, CookieOptions options)
{
    if (options.SameSite == SameSiteMode.None)
    {
        options.Secure = true;
    }
}

builder.Services.AddControllers(options => {
    // Configure input formatters
    options.InputFormatters.Clear();
    var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Microsoft.AspNetCore.Mvc.Formatters.SystemTextJsonInputFormatter>>();
    var jsonOptions = new Microsoft.AspNetCore.Mvc.JsonOptions();
    jsonOptions.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
    jsonOptions.JsonSerializerOptions.MaxDepth = 64;
    options.InputFormatters.Add(new Microsoft.AspNetCore.Mvc.Formatters.SystemTextJsonInputFormatter(jsonOptions, logger));
    
    // Configure supported media types for input formatters
    options.InputFormatters.OfType<Microsoft.AspNetCore.Mvc.Formatters.SystemTextJsonInputFormatter>()
        .First()
        .SupportedMediaTypes.Add("application/json");
})
.AddJsonOptions(options => {
    // Configure output JSON serialization
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
    options.JsonSerializerOptions.MaxDepth = 64;
});

// Configure DbContext with SQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Always use Azure SQL connection
    var connectionString = builder.Configuration.GetConnectionString("AzureSqlConnection");
    
    Console.WriteLine($"Using connection string: {connectionString}");
    
    options.UseSqlServer(connectionString, sqlServerOptions => 
        sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 5, 
            maxRetryDelay: TimeSpan.FromSeconds(30), 
            errorNumbersToAdd: null));
});

// Configure JWT Authentication
builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("JwtConfig"));

// Configure Azure Blob Storage
builder.Services.Configure<AzureBlobStorageOptions>(builder.Configuration.GetSection("AzureBlobConfig"));

// Configure Azure Event Hub
builder.Services.Configure<EventHubOptions>(builder.Configuration.GetSection("AzureEventHub"));
builder.Services.AddSingleton<EduSync.EventHub.IEventPublisher, EduSync.EventHub.EventHubPublisher>();

// SignalR configuration is defined later in the file

// Add HTTP client factory for more robust connections
builder.Services.AddHttpClient();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var key = Encoding.ASCII.GetBytes(builder.Configuration["JwtConfig:Secret"]);
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        RequireExpirationTime = true
    };
    
    // Configure for SignalR integration
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            
            // If the request is for our hub...
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && 
                (path.StartsWithSegments("/hubs/assessment")))
            {
                // Read the token out of the query string
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        }
    };
});

// No repositories or services needed - using DbContext directly in controllers
// Configure AutoMapper
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));

// Configure SignalR with improved timeout settings
builder.Services.AddSignalR(options => {
    options.EnableDetailedErrors = true;
    options.KeepAliveInterval = TimeSpan.FromSeconds(15); // Reduce from default 30s
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60); // Increase from default 30s
    options.HandshakeTimeout = TimeSpan.FromSeconds(30); // More time for initial handshake
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
});

// Configure Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "EduSync API", Version = "v1" });
    
    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
    
    // Configure proper schema generation for complex types
    c.CustomSchemaIds(type => type.FullName);
    
    // Handle circular references
    c.UseAllOfToExtendReferenceSchemas();
    
    // Configure Swagger to use JWT Authentication
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
    
    // Handle operation ID conflicts
    c.CustomOperationIds(apiDesc =>
    {
        // Generate unique operation IDs using controller and action names
        return $"{apiDesc.ActionDescriptor.RouteValues["controller"]}_{apiDesc.ActionDescriptor.RouteValues["action"]}"; 
    });
});


var app = builder.Build();

// Ensure media directories exist
var mediaRootPath = Path.Combine(app.Environment.WebRootPath, "media");
var courseMediaPath = Path.Combine(mediaRootPath, "courses");

// Ensure uploads directories exist
var uploadsRootPath = Path.Combine(app.Environment.WebRootPath, "uploads");
var uploadsCoursesPath = Path.Combine(uploadsRootPath, "courses");

// Create all necessary directories
var dirsToCreate = new[] { mediaRootPath, courseMediaPath, uploadsRootPath, uploadsCoursesPath };
foreach (var dir in dirsToCreate)
{
    if (!Directory.Exists(dir))
    {
        Directory.CreateDirectory(dir);
        Console.WriteLine($"Created directory: {dir}");
    }
}

// Configure static file serving
app.UseStaticFiles();

// Serve files from media directory
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(app.Environment.WebRootPath, "media")),
    RequestPath = "/media"
});

// Serve files from uploads directory
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(app.Environment.WebRootPath, "uploads")),
    RequestPath = "/uploads",
    ServeUnknownFileTypes = true, // Allow serving all file types
    OnPrepareResponse = ctx =>
    {
        // Add CORS headers for upload files to ensure they can be accessed from frontend
        ctx.Context.Response.Headers.Add("Access-Control-Allow-Origin", string.Join(",", frontendUrls));
        ctx.Context.Response.Headers.Add("Access-Control-Allow-Methods", "GET");
        
        // Enable browser caching by setting Cache-Control header
        ctx.Context.Response.Headers.Add("Cache-Control", "public, max-age=3600"); // Cache for 1 hour
    }
});

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "EduSync API v1"));
}

app.UseHttpsRedirection();
app.UseRouting();

// CORS must be after Routing and before Authentication/Authorization
app.UseCors(MyAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Map SignalR hubs
app.MapHub<EduSync.Hubs.AssessmentHub>("/hubs/assessment");

// Handle database setup and migrations more safely
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        // Check if database exists first
        if (context.Database.CanConnect())
        {
            logger.LogInformation("Database exists, checking migration status...");
            
            // Check if __EFMigrationsHistory table exists
            var migrationHistoryExists = false;
            try {
                // Simple query to check if migration history table exists
                migrationHistoryExists = context.Database.ExecuteSqlRaw("SELECT COUNT(*) FROM [__EFMigrationsHistory]") >= 0;
            }
            catch {
                logger.LogWarning("__EFMigrationsHistory table does not exist. This may be a fresh database.");
            }
            
            if (migrationHistoryExists)
            {
                // Apply any pending migrations safely
                logger.LogInformation("Applying any pending migrations...");
                context.Database.Migrate();
            }
            else
            {
                // Migration table doesn't exist but database has tables
                // This means database was created without EF Core migrations
                logger.LogWarning("Database exists but no migration history. Skipping migrations to avoid conflicts.");
            }
        }
        else
        {
            // Database doesn't exist, create it with migrations
            logger.LogInformation("Database does not exist, creating and applying migrations...");
            context.Database.Migrate();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while setting up the database.");
    }
}

app.Run();
