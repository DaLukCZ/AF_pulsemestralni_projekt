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
    var food1 = new Food { Name = "Chicken schnitzel", Description = "Served with potatoes", Price = 129m, IsActive = true };
    var food2 = new Food { Name = "Pasta carbonara", Description = "Classic Italian pasta", Price = 119m, IsActive = true };
    var food3 = new Food { Name = "Caesar salad", Description = "Chicken, croutons, parmesan", Price = 99m, IsActive = true };
    var food4 = new Food { Name = "Goulash", Description = "Beef goulash with bread", Price = 135m, IsActive = false }; // inactive example

    db.Foods.AddRange(food1, food2, food3, food4);
    await db.SaveChangesAsync();

    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var tomorrow = today.AddDays(1);

    var menu1 = new MenuItem { Date = today, FoodId = food1.Id, AvailablePortions = 10 };
    var menu2 = new MenuItem { Date = today, FoodId = food2.Id, AvailablePortions = 0 };  // sold out example
    var menu3 = new MenuItem { Date = today, FoodId = food3.Id, AvailablePortions = 5 };
    var menu4 = new MenuItem { Date = tomorrow, FoodId = food1.Id, AvailablePortions = 8 };

    db.MenuItems.AddRange(menu1, menu2, menu3, menu4);
    await db.SaveChangesAsync();

    var order1 = new Order { MenuItemId = menu1.Id, Status = OrderStatus.Preparing };
    var order2 = new Order { MenuItemId = menu3.Id, Status = OrderStatus.Ready };
    var order3 = new Order { MenuItemId = menu1.Id, Status = OrderStatus.Cancelled };
    var order4 = new Order { MenuItemId = menu3.Id, Status = OrderStatus.Completed };

    db.Orders.AddRange(order1, order2, order3, order4);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.UseHttpsRedirection();

app.Run();