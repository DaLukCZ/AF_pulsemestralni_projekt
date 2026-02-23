using System;
using System.Collections.Generic;
using System.Text;

namespace UTB.Minute.Contracts
{
    public record UpdateFoodDto(
        string Name,
        string? Description,
        decimal Price
    );
}
