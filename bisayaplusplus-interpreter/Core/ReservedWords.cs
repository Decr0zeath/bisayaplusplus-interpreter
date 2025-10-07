using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bisayaplusplus_interpreter.Core
{
    public static class ReservedWords
    {
        public static readonly HashSet<string> Words = new HashSet<string>()
        {
            "SUGOD", "KATAPUSAN", "MUGNA", "NUMERO", "LETRA", "TINUOD",
            "TIPIK", "IPAKITA", "DAWAT", "KUNG", "KUNG-KUNG", "PUNDOK", "ALANG SA", "WALA", "DILI", "UG", "OO", "O"
        };
    }
}
