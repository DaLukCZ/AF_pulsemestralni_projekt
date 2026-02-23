using System;
using System.Collections.Generic;
using System.Text;

namespace UTB.Minute.Contracts
{
    public record OrderDto(
        int Id,
        DateTime CreatedAt,
        OrderStatusDto Status,
        MenuItemDto MenuItem
    );
}
