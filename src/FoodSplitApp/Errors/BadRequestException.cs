using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodSplitApp.Errors
{
    /// <summary>
    /// Exception with user readable message that should explain how user failed to provide expected input.
    /// </summary>
    public class BadRequestException : Exception
    {
        public BadRequestException(string message) : base(message)
        {
        }
    }
}