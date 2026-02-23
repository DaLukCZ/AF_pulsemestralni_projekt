using System;
using System.Collections.Generic;
using System.Text;

namespace UTB.Minute.Contracts
{
    public record UpdateMenuItemDto(
        DateOnly Date,
        int FoodId,
        int AvailablePortions
    );
}
