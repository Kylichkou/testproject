    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Text.Json.Serialization; // Добавьте эту строку
    using testproject;
    using Pomelo.EntityFrameworkCore.MySql;
    using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder(args);



    // Add services to the container.
    builder.Services.AddRazorPages();
    string connection = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});
builder.Services.AddDbContext<ApplicationContext>(options =>
        options.UseMySql(connection,
        new MySqlServerVersion(new Version(8, 0, 11)))
        .EnableSensitiveDataLogging()); // добавьте этот вызов метода

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
        });
    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();
app.UseCors("AllowAllOrigins"); // Добавьте эту строку

app.UseAuthorization();

    app.MapRazorPages();
    //app.Use(async (context, next) =>
    //{
    //    if (context.Request.Path == "/" || context.Request.Path == "/index.html")
    //    {
    //        context.Response.Redirect("/", permanent: false);
    //        return;
    //    }
    //    await next();
    //});
    app.MapGet("/", async (context) =>
    {
        context.Response.ContentType = "text/html;charset=utf-8";
        await context.Response.SendFileAsync("register.html");
    }); app.MapPost("/api/register", async (RegisteredUser user, ApplicationContext db, HttpContext context) =>
    {
        // проверка на существующего пользователя
        var existingUsername = await db.RegisteredUsers.FirstOrDefaultAsync(u => u.Username == user.Username);
        if (existingUsername != null)
        {
            return Results.BadRequest(new { message = "Имя пользователя уже занято" });
        }

        // проверка на существующий адрес электронной почты
        var existingEmail = await db.RegisteredUsers.FirstOrDefaultAsync(u => u.Email == user.Email);
        if (existingEmail != null)
        {
            return Results.BadRequest(new { message = "Электронная почта уже используется" });
        }

        // добавляем пользователя в базу данных
        await db.RegisteredUsers.AddAsync(user);
        await db.SaveChangesAsync();
        return Results.Json(user);

    });

    app.MapGet("/api/login", async (context) => {
        context.Response.ContentType = "text/html;charset=utf-8";
        await context.Response.SendFileAsync("login.html");

    });
    app.MapGet("/api/persons", async context =>
    {
        context.Response.ContentType = "text/html;charset=utf-8";
        await context.Response.SendFileAsync("index.html");
    });
    app.MapPost("/api/login", async (RegisteredUser userData, ApplicationContext db) =>
    {
        // проверка на существующего пользователя
        var existingUser = await db.RegisteredUsers.FirstOrDefaultAsync(u => u.Username == userData.Username && u.Password == userData.Password);
        if (existingUser == null) return Results.NotFound(new { message = "Неверный логин или пароль" });

        // возвращает идентификатор пользователя, который следует использовать для дальнейших операций связанных с хранением информации о людях
        return Results.Json(new { userId = existingUser.Id });
    });
    app.MapGet("/api/persons/{userId:int}", async (int userId, ApplicationContext db) =>
    {
        var user = await db.RegisteredUsers.FindAsync(userId);
        if (user == null) return Results.NotFound(new { message = "Пользователь не найден" });

        var persons = await db.Persons
                    .Include(p => p.Role)
                    .Include(p => p.Team)
                    .Where(p => p.RegisteredUserId == userId)
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Age,
                        Role = new
                        {
                            p.Role.Id,
                            p.Role.Name
                        },
                        Team = new
                        {
                            p.Team.Id,
                            p.Team.Name
                        }
                    })
                    .ToListAsync();

        return Results.Json(persons);
    });
    app.MapPost("/api/persons/{id:int}", async (int id, AddPersonRequest request, ApplicationContext dbContext) =>
    {
        var registeredUserId = id;

        var role = dbContext.Roles.FirstOrDefault(r => r.Name == request.Role.Name && r.RegisteredUserId == registeredUserId);
        if (role == null)
        {
            role = new Role
            {
                Name = request.Role.Name,
                RegisteredUserId = registeredUserId
            };
            dbContext.Roles.Add(role);
            await dbContext.SaveChangesAsync();
        }

        var team = dbContext.Teams.FirstOrDefault(t => t.Name == request.Team.Name && t.RegisteredUserId == registeredUserId);
        if (team == null)
        {
            team = new Team
            {
                Name = request.Team.Name,
                RegisteredUserId = registeredUserId
            };
            dbContext.Teams.Add(team);
            await dbContext.SaveChangesAsync();
        }

        var person = new Person
        {
            Name = request.Name,
            Age = request.Age,
            RoleId = role.Id,
            TeamId = team.Id,
            RegisteredUserId = registeredUserId
        };

        dbContext.Persons.Add(person);
        await dbContext.SaveChangesAsync();

        return Results.Created($"/api/persons/{person.Id}", person);
    });
app.MapPut("/api/persons/{userId:int}/{personId:int}", async (int userId, int personId, UpdatePersonRequest requestData, HttpContext httpContext, ApplicationContext db) =>
{
    var user = await db.RegisteredUsers.FindAsync(userId);
    if (user == null) return Results.NotFound(new { message = "Пользователь не найден" });

    var existingPerson = await db.Persons.FirstOrDefaultAsync(p => p.Id == personId && p.RegisteredUserId == userId);

    if (existingPerson == null) return Results.NotFound(new { message = "Человек не найден" });

    var existingRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == requestData.Role.Name && r.RegisteredUserId == userId);

    if (existingRole == null)
    {
        existingRole = new Role
        {
            Name = requestData.Role.Name,
            RegisteredUserId = userId
        };
        db.Roles.Add(existingRole);
        await db.SaveChangesAsync();
    }

    var existingTeam = await db.Teams.FirstOrDefaultAsync(t => t.Name == requestData.Team.Name && t.RegisteredUserId == userId);

    if (existingTeam == null)
    {
        existingTeam = new Team
        {
            Name = requestData.Team.Name,
            RegisteredUserId = userId
        };
        db.Teams.Add(existingTeam);
        await db.SaveChangesAsync();
    }

    existingPerson.Name = requestData.Name;
    existingPerson.Age = requestData.Age;
    existingPerson.Role = existingRole; // Обновляем роль
    existingPerson.Team = existingTeam; // Обновляем команду

    db.Persons.Update(existingPerson);
    await db.SaveChangesAsync();

    httpContext.Response.ContentType = "application/json";
    await JsonSerializer.SerializeAsync(httpContext.Response.Body, new
    {
        existingPerson.Id,
        existingPerson.Name,
        existingPerson.Age,
        Role = new
        {
            existingRole.Id,
            existingRole.Name
        },
        Team = new
        {
            existingTeam.Id,
            existingTeam.Name
        }
    });

    return Results.Ok();
});




app.MapGet("/api/persons/{userId:int}/{personId:int}", async (int userId, int personId, ApplicationContext dbContext) =>
{
    var user = await dbContext.RegisteredUsers.FindAsync(userId);
    if (user == null) return Results.NotFound(new { message = "Пользователь не найден" });

    var person = await dbContext.Persons
                    .Include(p => p.Role)
                    .Include(p => p.Team)
                    .Where(p => p.RegisteredUserId == userId && p.Id == personId)
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Age,
                        Role = new
                        {
                            p.Role.Id,
                            p.Role.Name
                        },
                        Team = new
                        {
                            p.Team.Id,
                            p.Team.Name
                        }
                    })
                    .FirstOrDefaultAsync();

    if (person == null) return Results.NotFound(new { message = "Человек не найден" });

    return Results.Json(person);
});


app.MapDelete("/api/persons/{userId:int}/{personId:int}", async (int userId, int personId, ApplicationContext db) =>
    {
        var user = await db.RegisteredUsers.FindAsync(userId);
        if (user == null) return Results.NotFound(new { message = "Пользователь не найден" });

        var existingPerson = await db.Persons.FirstOrDefaultAsync(p => p.Id == personId && p.RegisteredUserId == userId);
        if (existingPerson == null) return Results.NotFound(new { message = "Человек не найден" });

        db.Persons.Remove(existingPerson);
        await db.SaveChangesAsync();
        return Results.Ok(new { message = "Человек успешно удален" });
    });
    app.Run();

