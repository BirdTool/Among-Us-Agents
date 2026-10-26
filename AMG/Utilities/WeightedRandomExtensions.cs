using System;
using System.Collections.Generic;

namespace AMG.Utilities
{
    public static class WeightedRandomExtensions
    {
        public static T GetRandomWeighted<T>(this IEnumerable<T> items, Func<T, float> weightSelector)
        {
            if (items == null) return default;

            var list = new List<T>();
            var weights = new List<float>();
            float total = 0f;

            foreach (var item in items)
            {
                float w = weightSelector(item);
                if (float.IsNaN(w) || w < 0f) w = 0f;

                list.Add(item);
                weights.Add(w);
                total += w;
            }

            if (list.Count == 0) return default;
            if (total <= 0f) return list[RandomizerExtensions.GetSecureRandomInt(0, list.Count - 1)];

            float roll = RandomizerExtensions.GetSecureRandomFloat(0f, total);
            float acc = 0f;

            for (int i = 0; i < list.Count; i++)
            {
                if (weights[i] <= 0f) continue;

                acc += weights[i];
                if (roll < acc) return list[i];
            }

            for (int i = list.Count - 1; i >= 0; i--)
                if (weights[i] > 0f) return list[i];

            return list[^1];
        }
    }
}