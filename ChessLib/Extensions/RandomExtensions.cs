using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessLib.Extensions
{
    public static class RandomExtensions
    {
        /// <summary>
        /// Return a random number based on Walker's alias method
        /// </summary>
        /// <param name="rnd"></param>
        /// <param name="probs">The probabilities</param>
        /// <returns></returns>
        public static int GetAlias(this Random rnd,  IEnumerable<int> probs)
        {
            int pick = rnd.Next(probs.Sum());
            int sum = 0;
            int idx = 0;
            foreach (var p in probs) {
                sum += p;
                if (sum >= pick)
                    break;
                idx++;
            }

            return idx;
        } // GetAlias
    }
}