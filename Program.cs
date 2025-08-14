using AccountingAPI.Data;
using AccountingAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddControllers();

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
        // Hash default password "password"
        string defaultHash;
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes("password"));
            var builder2 = new System.Text.StringBuilder();
            foreach (var b in bytes)
            {
                builder2.Append(b.ToString("x2"));
            }
            defaultHash = builder2.ToString();
        }
        db.Users.Add(new User { Username = "admin", PasswordHash = defaultHash, Role = "Admin" });
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

// Login endpoint that validates credentials against stored users in the database. Passwords are stored as SHA256 hashes.
app.MapPost("/api/auth/login", async ([FromServices] AccountingContext db, LoginRequest login) =>
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

    // Compute hash of the provided password for comparison
    string providedHash;
    using (var sha256 = System.Security.Cryptography.SHA256.Create())
    {
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(login.Password));
        var builder = new System.Text.StringBuilder();
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }
        providedHash = builder.ToString();
    }

    if (user.PasswordHash != providedHash)
    {
        return Results.BadRequest(new { message = "Invalid username or password" });
    }

    return Results.Ok(new { username = user.Username, role = user.Role });
});

// Map controllers
app.MapControllers();

// Run the application
app.Run();

// Login request model
public record LoginRequest(string Username, string Password);
