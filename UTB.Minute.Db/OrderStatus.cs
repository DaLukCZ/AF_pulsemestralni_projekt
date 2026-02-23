using System;
using System.Collections.Generic;
using System.Text;

namespace UTB.Minute.Db
{
    public enum OrderStatus
    {
        Preparing = 0,
        Ready = 1,
        Cancelled = 2,
        Completed = 3
    }
}
