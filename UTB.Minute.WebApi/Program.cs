using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using UTB.Minute.Contracts;
using UTB.Minute.Db;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<MinuteDbContext>("database");

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseHttpsRedirection();

// Foods endpoints
app.MapPost("/foods", WebApiV1.CreateFood);
app.MapGet("/foods", WebApiV1.GetFoods);
app.MapGet("/foods/{id:int}", WebApiV1.GetFoodById);
app.MapPut("/foods/{id:int}", WebApiV1.UpdateFood);
app.MapPut("/foods/{id:int}/deactivate", WebApiV1.DeactivateFood);

// Menu endpoints
app.MapPost("/menu-items", WebApiV1.CreateMenuItem);
app.MapGet("/menu-items", WebApiV1.GetMenuItems);
app.MapGet("/menu-items/today", WebApiV1.GetTodayMenuItems);
app.MapPut("/menu-items/{id:int}", WebApiV1.UpdateMenuItem);
app.MapDelete("/menu-items/{id:int}", WebApiV1.DeleteMenuItem);

// Orders endpoints
app.MapPost("/orders", WebApiV1.CreateOrder);
app.MapGet("/orders", WebApiV1.GetOrders);
app.MapGet("/orders/open", WebApiV1.GetOpenOrders);
app.MapPut("/orders/{id:int}/status", WebApiV1.UpdateOrderStatus);

app.Run();

public static class WebApiV1
{
    // Foods
    public static async Task<Created<FoodDto>> CreateFood(CreateFoodDto dto, MinuteDbContext db)
    {
        var food = new Food
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            IsActive = true
        };

        db.Foods.Add(food);
        await db.SaveChangesAsync();

        var result = new FoodDto(food.Id, food.Name, food.Description, food.Price, food.IsActive);
        return TypedResults.Created($"/foods/{result.Id}", result);
    }

    public static async Task<Ok<FoodDto[]>> GetFoods(MinuteDbContext db)
    {
        var foods = await db.Foods
            .OrderBy(f => f.Name)
            .Select(f => new FoodDto(f.Id, f.Name, f.Description, f.Price, f.IsActive))
            .ToArrayAsync();

        return TypedResults.Ok(foods);
    }

    public static async Task<Results<NotFound, Ok<FoodDto>>> GetFoodById(int id, MinuteDbContext db)
    {
        var food = await db.Foods.FindAsync(id);
        if (food is null)
            return TypedResults.NotFound();

        var dto = new FoodDto(food.Id, food.Name, food.Description, food.Price, food.IsActive);
        return TypedResults.Ok(dto);
    }

    public static async Task<Results<NoContent, NotFound>> UpdateFood(int id, UpdateFoodDto dto, MinuteDbContext db)
    {
        var food = await db.Foods.FindAsync(id);
        if (food is null)
            return TypedResults.NotFound();

        food.Name = dto.Name;
        food.Description = dto.Description;
        food.Price = dto.Price;

        await db.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    public static async Task<Results<NoContent, NotFound>> DeactivateFood(int id, MinuteDbContext db)
    {
        var food = await db.Foods.FindAsync(id);
        if (food is null)
            return TypedResults.NotFound();

        food.IsActive = false;

        await db.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    // Menu items
    public static async Task<Results<Created<MenuItemDto>, BadRequest<string>>> CreateMenuItem(
        CreateMenuItemDto dto,
        MinuteDbContext db)
    {
        // Allow only active foods to appear in menu
        var food = await db.Foods.FindAsync(dto.FoodId);
        if (food is null)
            return TypedResults.BadRequest("Food does not exist.");

        if (!food.IsActive)
            return TypedResults.BadRequest("Food is inactive.");

        var menuItem = new MenuItem
        {
            Date = dto.Date,
            FoodId = dto.FoodId,
            AvailablePortions = dto.AvailablePortions
        };

        db.MenuItems.Add(menuItem);
        await db.SaveChangesAsync();

        // Load food for DTO mapping
        await db.Entry(menuItem).Reference(m => m.Food).LoadAsync();

        var result = MapMenuItem(menuItem);
        return TypedResults.Created($"/menu-items/{result.Id}", result);
    }

    public static async Task<Ok<MenuItemDto[]>> GetMenuItems(MinuteDbContext db)
    {
        var items = await db.MenuItems
            .Include(m => m.Food)
            .OrderBy(m => m.Date)
            .ThenBy(m => m.Food.Name)
            .Select(m => new MenuItemDto(
                m.Id,
                m.Date,
                new FoodDto(m.Food.Id, m.Food.Name, m.Food.Description, m.Food.Price, m.Food.IsActive),
                m.AvailablePortions))
            .ToArrayAsync();

        return TypedResults.Ok(items);
    }

    public static async Task<Ok<MenuItemDto[]>> GetTodayMenuItems(MinuteDbContext db)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var items = await db.MenuItems
            .Where(m => m.Date == today)
            .Include(m => m.Food)
            .OrderBy(m => m.Food.Name)
            .Select(m => new MenuItemDto(
                m.Id,
                m.Date,
                new FoodDto(m.Food.Id, m.Food.Name, m.Food.Description, m.Food.Price, m.Food.IsActive),
                m.AvailablePortions))
            .ToArrayAsync();

        return TypedResults.Ok(items);
    }

    public static async Task<Results<NoContent, NotFound, BadRequest<string>>> UpdateMenuItem(
        int id,
        UpdateMenuItemDto dto,
        MinuteDbContext db)
    {
        var menuItem = await db.MenuItems.FindAsync(id);
        if (menuItem is null)
            return TypedResults.NotFound();

        var food = await db.Foods.FindAsync(dto.FoodId);
        if (food is null)
            return TypedResults.BadRequest("Food does not exist.");

        if (!food.IsActive)
            return TypedResults.BadRequest("Food is inactive.");

        menuItem.Date = dto.Date;
        menuItem.FoodId = dto.FoodId;
        menuItem.AvailablePortions = dto.AvailablePortions;

        await db.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    public static async Task<Results<NoContent, NotFound>> DeleteMenuItem(int id, MinuteDbContext db)
    {
        var menuItem = await db.MenuItems.FindAsync(id);
        if (menuItem is null)
            return TypedResults.NotFound();

        db.MenuItems.Remove(menuItem);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    // Orders
    public static async Task<Results<Created<OrderDto>, BadRequest<string>>> CreateOrder(
        CreateOrderDto dto,
        MinuteDbContext db)
    {
        // Load menu item with food
        var menuItem = await db.MenuItems
            .Include(m => m.Food)
            .FirstOrDefaultAsync(m => m.Id == dto.MenuItemId);

        if (menuItem is null)
            return TypedResults.BadRequest("Menu item does not exist.");

        // Check availability
        if (menuItem.AvailablePortions <= 0)
            return TypedResults.BadRequest("Sold out.");

        // Decrease portion count
        menuItem.AvailablePortions -= 1;

        var order = new Order
        {
            MenuItemId = menuItem.Id,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Preparing
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        // Load navigation for mapping
        await db.Entry(order).Reference(o => o.MenuItem).LoadAsync();
        await db.Entry(order.MenuItem).Reference(m => m.Food).LoadAsync();

        var result = MapOrder(order);
        return TypedResults.Created($"/orders/{result.Id}", result);
    }

    public static async Task<Ok<OrderDto[]>> GetOrders(MinuteDbContext db)
    {
        var orders = await db.Orders
            .Include(o => o.MenuItem)
                .ThenInclude(m => m.Food)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderDto(
                o.Id,
                o.CreatedAt,
                MapOrderStatus(o.Status),
                new MenuItemDto(
                    o.MenuItem.Id,
                    o.MenuItem.Date,
                    new FoodDto(
                        o.MenuItem.Food.Id,
                        o.MenuItem.Food.Name,
                        o.MenuItem.Food.Description,
                        o.MenuItem.Food.Price,
                        o.MenuItem.Food.IsActive),
                    o.MenuItem.AvailablePortions)))
            .ToArrayAsync();

        return TypedResults.Ok(orders);
    }

    public static async Task<Ok<OrderDto[]>> GetOpenOrders(MinuteDbContext db)
    {
        var orders = await db.Orders
            .Where(o => o.Status != OrderStatus.Completed)
            .Include(o => o.MenuItem)
                .ThenInclude(m => m.Food)
            .OrderBy(o => o.CreatedAt)
            .Select(o => new OrderDto(
                o.Id,
                o.CreatedAt,
                MapOrderStatus(o.Status),
                new MenuItemDto(
                    o.MenuItem.Id,
                    o.MenuItem.Date,
                    new FoodDto(
                        o.MenuItem.Food.Id,
                        o.MenuItem.Food.Name,
                        o.MenuItem.Food.Description,
                        o.MenuItem.Food.Price,
                        o.MenuItem.Food.IsActive),
                    o.MenuItem.AvailablePortions)))
            .ToArrayAsync();

        return TypedResults.Ok(orders);
    }

    public static async Task<Results<NoContent, NotFound, BadRequest<string>>> UpdateOrderStatus(
        int id,
        UpdateOrderStatusDto dto,
        MinuteDbContext db)
    {
        var order = await db.Orders.FindAsync(id);
        if (order is null)
            return TypedResults.NotFound();

        var newStatus = MapOrderStatus(dto.Status);

        // Block invalid transitions:
        // - Completed is terminal
        // - Cancelled can go only to Completed
        // - Ready can go only to Completed
        // - Preparing can go to Ready, Cancelled or Completed (Completed used for "student informed")
        if (!IsValidTransition(order.Status, newStatus))
            return TypedResults.BadRequest("Invalid status transition.");

        order.Status = newStatus;
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    // Mapping helpers
    private static MenuItemDto MapMenuItem(MenuItem m)
        => new(
            m.Id,
            m.Date,
            new FoodDto(m.Food.Id, m.Food.Name, m.Food.Description, m.Food.Price, m.Food.IsActive),
            m.AvailablePortions);

    private static OrderDto MapOrder(Order o)
        => new(
            o.Id,
            o.CreatedAt,
            MapOrderStatus(o.Status),
            MapMenuItem(o.MenuItem));

    private static OrderStatusDto MapOrderStatus(OrderStatus status)
        => status switch
        {
            OrderStatus.Preparing => OrderStatusDto.Preparing,
            OrderStatus.Ready => OrderStatusDto.Ready,
            OrderStatus.Cancelled => OrderStatusDto.Cancelled,
            OrderStatus.Completed => OrderStatusDto.Completed,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown order status.")
        };

    private static OrderStatus MapOrderStatus(OrderStatusDto status)
        => status switch
        {
            OrderStatusDto.Preparing => OrderStatus.Preparing,
            OrderStatusDto.Ready => OrderStatus.Ready,
            OrderStatusDto.Cancelled => OrderStatus.Cancelled,
            OrderStatusDto.Completed => OrderStatus.Completed,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown order status.")
        };

    private static bool IsValidTransition(OrderStatus current, OrderStatus next)
    {
        if (current == next)
            return true;

        return current switch
        {
            OrderStatus.Preparing => next is OrderStatus.Ready or OrderStatus.Cancelled or OrderStatus.Completed,
            OrderStatus.Ready => next is OrderStatus.Completed,
            OrderStatus.Cancelled => next is OrderStatus.Completed,
            OrderStatus.Completed => false,
            _ => false
        };
    }
}