using System;
using System.Collections.Generic;
using System.Text;

namespace UTB.Minute.Contracts
{
    public record MenuItemDto(
        int Id,
        DateOnly Date,
        FoodDto Food,
        int AvailablePortions
    );
}
