using System;
using System.Collections.Generic;
using HexMage.Simulator.PCG;

namespace HexMage.Simulator.AI {
    public static class ListHelpers {
        /// <summary>
        /// Randomly shuffles a given list.
        /// </summary>
        public static void Shuffle<T>(this IList<T> list) {
            int n = list.Count;
            while (n > 1) {
                n--;
                int k = Generator.Random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        /// <summary>
        /// Returns a maximum of a given list while caching the intermediate values
        /// of the maximum calculation. Used when determining the value of an item
        /// is computationally expensive.
        /// </summary>
        public static T FastMax<T>(this List<T> t, Func<T, float> valFunc) {
            float maxVal = valFunc(t[0]);
            T max = t[0];

            for (int i = 1; i < t.Count; i++) {
                T curr = t[i];
                float currVal = valFunc(curr);

                if (currVal > maxVal) {
                    max = curr;
                }
            }

            return max;
        }
    }
}