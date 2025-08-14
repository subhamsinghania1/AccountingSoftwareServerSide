using AccountingAPI.Data;
using AccountingAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddControllers();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Add EF Core with SQL Server provider
builder.Services.AddDbContext<AccountingContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AccountingConnection")));

// Remove in-memory users; user credentials are stored in the database via AccountingContext.Users.
// A default admin user will be seeded through EF Core migrations or initialization scripts.


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

// Ensure the database is created and apply migrations; seed a default admin user if none exists
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AccountingContext>();
    db.Database.Migrate();
    // Seed default admin user if not present
    if (!db.Users.Any())
    {
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var admin = new User { Username = "admin", Role = "Admin" };
        admin.PasswordHash = passwordHasher.HashPassword(admin, "password");
        db.Users.Add(admin);
        db.SaveChanges();
    }
}

// Configure middleware
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

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

    return Results.Ok(new { username = user.Username, role = user.Role });
});

// Map controllers
app.MapControllers();

// Run the application
app.Run();

// Login request model
public record LoginRequest(string Username, string Password);