namespace Evacuation.Helpers
{
    public class Helper
    {

        public Task<double> HaversineDistanceAsync(double lat1, double lon1, double lat2, double lon2)
        {
            return Task.Run(async () =>
            {
                const double earthRadiusKm = 6371.0;

                double dLat = await DegreesToRadiansAsync(lat2 - lat1);
                double dLon = await DegreesToRadiansAsync(lon2 - lon1);

                double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                           Math.Cos(await DegreesToRadiansAsync(lat1)) * Math.Cos(await DegreesToRadiansAsync(lat2)) *
                           Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

                double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

                double distance = earthRadiusKm * c;

                return distance;
            });
        }

        private Task<double> DegreesToRadiansAsync(double degrees)
        {
            return Task.FromResult(degrees * Math.PI / 180.0);
        }

        public async Task<double> CalculateTimeToReachDestinationAsync(double distance, double speed)
        {
            return await Task.Run(() =>
            {
                return (distance / speed) * 60;
            });
        }
        //https://stackoverflow.com/questions/3319586/getting-all-possible-combinations-from-a-list-of-numbers/3319597

        // public List<T[]> CreateSubsets<T>(T[] originalArray)
        // {
        //     List<T[]> subsets = new List<T[]>();

        //     for (int i = 0; i < originalArray.Length; i++)
        //     {
        //         int subsetCount = subsets.Count;
        //         subsets.Add(new T[] { originalArray[i] });

        //         for (int j = 0; j < subsetCount; j++)
        //         {
        //             T[] newSubset = new T[subsets[j].Length + 1];
        //             subsets[j].CopyTo(newSubset, 0);
        //             newSubset[newSubset.Length - 1] = originalArray[i];
        //             subsets.Add(newSubset);
        //         }
        //     }

        //     return subsets;
        // }

        // https://stackoverflow.com/questions/7802822/all-possible-combinations-of-a-list-of-values
        // private static List<DraftEvacuationPlanDTO> GetCombination(List<DraftEvacuationPlanDTO> list)
        // {
        //     double count = Math.Pow(2, list.Count);
        //     for (int i = 1; i <= count - 1; i++)
        //     {
        //         string str = Convert.ToString(i, 2).PadLeft(list.Count, '0');
        //         for (int j = 0; j < str.Length; j++)
        //         {
        //             if (str[j] == '1')
        //             {
        //                 Console.Write(list[j]);
        //             }
        //         }
        //         Console.WriteLine();
        //     }
        //     return list;
        // }
        // public  IEnumerable<List<T>> GetCombinations<T>(List<T> list)
        // {
        //     int n = list.Count;
        //     for (int i = 1; i < (1 << n); i++)
        //     {
        //     var combination = new List<T>();
        //     for (int j = 0; j < n; j++)
        //     {
        //         if ((i & (1 << j)) != 0)
        //         {
        //         combination.Add(list[j]);
        //         }
        //     }
        //     yield return combination;
        //     }
        // }
    }
}


