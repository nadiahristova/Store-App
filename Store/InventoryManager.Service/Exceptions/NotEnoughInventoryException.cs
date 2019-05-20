using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManager.Service.Exceptions
{
    public class NotEnoughInventoryException : Exception
    {
        public NotEnoughInventoryException(string msg) : base(msg) { }
    }
}
