using System;
using System.Collections.Generic;
using System.Text;

namespace UTB.Minute.Db;

public class MenuItem
{
    public int Id { get; set; }

    public DateOnly Date { get; set; }

    public int FoodId { get; set; }
    public Food Food { get; set; } = null!;

    public int AvailablePortions { get; set; }
}