using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace AMG.Utilities
{
    public static class RandomizerExtensions
    {
        private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();

        public static void ShuffleSecure<T>(this IList<T> list)
        {
            int n = list.Count;
            byte[] box = new byte[4];

            while (n > 1)
            {
                n--;

                // Gera bytes aleatórios e converte para um inteiro positivo
                _rng.GetBytes(box);
                int randomInt = Math.Abs(BitConverter.ToInt32(box, 0));

                // Garante que o valor está dentro do escopo restante da lista
                int k = randomInt % (n + 1);

                // Troca os elementos (Swap)
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static void ShuffleSecure<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
        {
            // Converte o dicionário para uma lista de pares chave-valor
            var pairs = new List<KeyValuePair<TKey, TValue>>(dictionary);

            // Embaralha a lista usando Fisher-Yates com RNG seguro
            int n = pairs.Count;
            byte[] box = new byte[4];

            while (n > 1)
            {
                n--;

                _rng.GetBytes(box);
                int randomInt = Math.Abs(BitConverter.ToInt32(box, 0));
                int k = randomInt % (n + 1);

                // Swap
                var temp = pairs[k];
                pairs[k] = pairs[n];
                pairs[n] = temp;
            }

            // Limpa o dicionário original e adiciona os itens embaralhados
            dictionary.Clear();
            foreach (var pair in pairs)
            {
                dictionary.Add(pair.Key, pair.Value);
            }
        }

        public static float GetSecureNextSingle()
        {
            byte[] box = new byte[4];
            _rng.GetBytes(box);
            uint randomInt = BitConverter.ToUInt32(box, 0);

            return randomInt / (float)uint.MaxValue;
        }

        public static int GetSecureRandomInt(int min, int max)
        {
            // Inverte se min for maior que max
            if (min > max)
            {
                int temp = min;
                min = max;
                max = temp;
            }

            if (min == max) return min;

            byte[] box = new byte[4];
            _rng.GetBytes(box);
            uint randomValue = BitConverter.ToUInt32(box, 0);

            long range = (long)max - min + 1;
            return (int)(min + (randomValue % range));
        }

        public static double GetSecureRandomDouble(double min, double max)
        {
            // Inverte se min for maior que max
            if (min > max)
            {
                double temp = min;
                min = max;
                max = temp;
            }

            if (Math.Abs(min - max) < double.Epsilon) return min;

            byte[] box = new byte[8];
            _rng.GetBytes(box);
            ulong randomValue = BitConverter.ToUInt64(box, 0);

            double normalized = randomValue / (double)ulong.MaxValue;
            return min + (normalized * (max - min));
        }


        public static float GetSecureRandomFloat(float min, float max)
        {
            // Inverte se min for maior que max
            if (min > max)
            {
                float temp = min;
                min = max;
                max = temp;
            }

            if (Math.Abs(min - max) < float.Epsilon) return min;

            byte[] box = new byte[4];
            _rng.GetBytes(box);
            uint randomValue = BitConverter.ToUInt32(box, 0);

            float normalized = randomValue / (float)uint.MaxValue;
            return min + (normalized * (max - min));
        }
    }
}