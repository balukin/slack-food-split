using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodSplitApp.Model
{
    public static class CommandTexts
    {
        /* Order handling */
        public const string OpenNewOrder = "host";
        public const string OpenNewOrderAlt1 = "new";
        public const string OpenNewOrderAlt2 = "create";
        public const string OpenNewOrderAlt3 = "start";
        public const string Cancel = "cancel";
        public const string Reopen = "reopen";
        public const string Finish = "finish";
        public const string EatAs = "eatas";
        public const string Eat = "eat";
        public const string EatAlt1 = "join";
        public const string Shared = "shared";
        public const string SharedAlt1 = "share";

        /* Balance operations */
        public const string Owe = "owe";
        public const string Forgive = "forgive";
        public const string Balance = "balance";
    }
}