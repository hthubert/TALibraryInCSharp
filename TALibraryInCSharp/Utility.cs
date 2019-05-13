using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaLib
{
    public partial class Core
    {
        private static void SeriesCopy(SmartQuant.ISeries source, long sourceIndex, Array destinationArray, long destinationIndex, long length)
        {
            for (var i = 0; i < length; i++) {
                destinationArray.SetValue(source[i + (int)sourceIndex], i + destinationIndex);
            }
        }
    }
}
