using System;
using System.Collections.Generic;
using System.Text;

namespace UTB.Minute.Contracts
{
    public record UpdateOrderStatusDto(
        OrderStatusDto Status
    );
}
