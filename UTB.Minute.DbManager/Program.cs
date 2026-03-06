using Microsoft.EntityFrameworkCore;
using UTB.Minute.Db;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Register MinuteDbContext in DI container (Aspire provides connection string via "database").
builder.AddNpgsqlDbContext<MinuteDbContext>("database");

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapPost("/reset-db", async (MinuteDbContext db) =>
{
    // Delete database if it exists.
    await db.Database.EnsureDeletedAsync();

    // Create database if it does not exist.
    await db.Database.EnsureCreatedAsync();

    // Seed test data
    var foods = new[] {
    new Food {
      Name = "Dürüm Kebab",
        Description = "Beef kebab in tortilla with vegetables and herb sauce",
        Price = 129m,
        IsActive = true
    },
    new Food {
      Name = "Semolina porridge",
        Description = "Sweet semolina porridge with butter and cocoa",
        Price = 59m,
        IsActive = true
    },
    new Food {
      Name = "Pancakes with jam",
        Description = "Three pancakes served with strawberry jam and whipped cream",
        Price = 89m,
        IsActive = true
    },
    new Food {
      Name = "Belgian waffles",
        Description = "Waffles with whipped cream and chocolate topping",
        Price = 95m,
        IsActive = true
    }
    };

    db.Foods.AddRange(foods);
    await db.SaveChangesAsync();

    var kebab = foods[0];
    var semolinaPorridge = foods[1];
    var pancakes = foods[2];
    var waffles = foods[3];

    var today = DateOnly.FromDateTime(DateTime.Now);
    var tomorrow = today.AddDays(1);

    var menuItems = new[]
    {
        new MenuItem
        {
            Date = today,
            FoodId = kebab.Id,
            AvailablePortions = 10
        }, // kebab

        new MenuItem
        {
            Date = today,
            FoodId = pancakes.Id,
            AvailablePortions = 6
        }, // pancakes

        new MenuItem
        {
            Date = today,
            FoodId = waffles.Id,
            AvailablePortions = 0
        }, // waffles (sold out)

        new MenuItem
        {
            Date = tomorrow,
            FoodId = semolinaPorridge.Id,
            AvailablePortions = 8
        } // semolina porridge
    };

    db.MenuItems.AddRange(menuItems);
    await db.SaveChangesAsync();

    var order1 = new Order
    {
        MenuItemId = menuItems[0].Id,
        Status = OrderStatus.Preparing
    };

    var order2 = new Order
    {
        MenuItemId = menuItems[1].Id,
        Status = OrderStatus.Ready
    };

    var order3 = new Order
    {
        MenuItemId = menuItems[0].Id,
        Status = OrderStatus.Cancelled
    };

    var order4 = new Order
    {
        MenuItemId = menuItems[3].Id,
        Status = OrderStatus.Completed
    };

    db.Orders.AddRange(order1, order2, order3, order4);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.UseHttpsRedirection();

app.Run();