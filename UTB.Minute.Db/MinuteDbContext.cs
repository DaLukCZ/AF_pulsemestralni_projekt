using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace UTB.Minute.Db;

public class MinuteDbContext(DbContextOptions<MinuteDbContext> options)
    : DbContext(options)
{
    public DbSet<Food> Foods => Set<Food>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MenuItem>()
            .HasOne(m => m.Food)
            .WithMany()
            .HasForeignKey(m => m.FoodId);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.MenuItem)
            .WithMany()
            .HasForeignKey(o => o.MenuItemId);
    }
}
