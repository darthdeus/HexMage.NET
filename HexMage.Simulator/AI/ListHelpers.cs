using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HexMage.Simulator.PCG;

namespace HexMage.Simulator {
    public static class ListHelpers {
        public static void Shuffle<T>(this IList<T> list) {
            // TODO - rewrite this to be better
            int n = list.Count;
            while (n > 1) {
                n--;
                int k = Generator.Random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
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