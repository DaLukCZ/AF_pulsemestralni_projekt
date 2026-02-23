using System;
using System.Collections.Generic;
using System.Text;

namespace UTB.Minute.Contracts
{
    public record FoodDto(
        int Id,
        string Name,
        string? Description,
        decimal Price,
        bool IsActive
    );
}
