using System;
using System.Collections.Generic;
using System.Text;

namespace UTB.Minute.Contracts
{
    public record CreateMenuItemDto(
        DateOnly Date,
        int FoodId,
        int AvailablePortions
    );
}
