var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/register", (UserRegister user) =>
{
    Console.WriteLine($"New user: {user.Email}");

    return Results.Ok(new
    {
        message = "Rejestracja udana"
    });
});

app.MapPost("/login", (UserLogin user) =>
{
    if (user.Email == "test@test.com" && user.Password == "1234")
    {
        return Results.Ok(new
        {
            message = "Zalogowano"
        });
    }

    return Results.BadRequest("Nieprawidłowe dane");
});

app.Run();

record UserRegister(string Name, string Email, string Password);
record UserLogin(string Email, string Password);