using AccountingAPI.Data;
using AccountingAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

var jwtSecret = builder.Configuration["JWT_SECRET"]
    ?? throw new InvalidOperationException("JWT_SECRET is not configured");
var connectionString = builder.Configuration.GetConnectionString("AccountingConnection")
    ?? builder.Configuration["ACCOUNTING_CONNECTION"]
    ?? throw new InvalidOperationException("Database connection string not configured");

// Configure services
builder.Services.AddControllers();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddDbContext<AccountingContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddHostedService<DatabaseInitializer>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });
builder.Services.AddAuthorization();

// Add CORS so clients on different origins can call the API
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// Swagger for API exploration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure middleware
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Login endpoint that validates credentials against stored users in the database using a secure password hasher.
app.MapPost("/api/auth/login", async ([FromServices] AccountingContext db, [FromServices] IPasswordHasher<User> passwordHasher, LoginRequest login) =>
{
    if (string.IsNullOrWhiteSpace(login.Username) || string.IsNullOrWhiteSpace(login.Password))
    {
        return Results.BadRequest(new { message = "Username and password are required" });
    }

    // Find user by username
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == login.Username);
    if (user == null)
    {
        return Results.BadRequest(new { message = "Invalid username or password" });
    }

    var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, login.Password);
    if (verification == PasswordVerificationResult.Failed)
    {
        return Results.BadRequest(new { message = "Invalid username or password" });
    }
    if (verification == PasswordVerificationResult.SuccessRehashNeeded)
    {
        user.PasswordHash = passwordHasher.HashPassword(user, login.Password);
        await db.SaveChangesAsync();
    }

    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.UTF8.GetBytes(jwtSecret);
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        }),
        Expires = DateTime.UtcNow.AddHours(1),
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    };
    var token = tokenHandler.CreateToken(tokenDescriptor);
    var tokenString = tokenHandler.WriteToken(token);

    return Results.Ok(new { token = tokenString });
});

// Map controllers
app.MapControllers();

// Run the application
app.Run();

// Login request model
public record LoginRequest(string Username, string Password);
