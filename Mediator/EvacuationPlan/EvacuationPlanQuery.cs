using System.Text;
using aspnetcore_redis_cache;
using Evacuation.Constant;
using Evacuation.DTO.EvacuationPlan;
using Evacuation.DTO.EvacuationStatus;
using Evacuation.DTO.EvacuationZones;
using Evacuation.DTO.JsonDataDTO;
using Evacuation.DTO.Vehicle;
using Evacuation.Helpers;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace Evacuation.Mediator.EvacuationPlan;

public class EvacuationPlanQuery : IRequest<JsonDataDTO<List<EvacuationPlanDTO>>>
{
    public class EvacuationPlanQueryHandler : IRequestHandler<EvacuationPlanQuery, JsonDataDTO<List<EvacuationPlanDTO>>>
    {
        private readonly IDistributedCache _cache;
        private readonly Helper _helper;


        public EvacuationPlanQueryHandler(IDistributedCache cache, Helper helper)
        {
            _cache = cache;
            _helper = helper;
        }

        public async Task<JsonDataDTO<List<EvacuationPlanDTO>>> Handle(EvacuationPlanQuery request, CancellationToken cancellationToken)
        {

            var evacuationsZones = await _cache.GetObjectAsync<List<EvacuationZonesDTO>>(CacheKey.EVACUATION_ZONE);
            var vehicle = await _cache.GetObjectAsync<List<VehicleDTO>>(CacheKey.VEHICLE);
            if (evacuationsZones == null || vehicle == null)
                return ErrorResponse("Evacuation Zone or Vehicle Not Found Please Set it First");


            var (evacuationPlan, errMassage) = PlanToEvacuation(evacuationsZones, vehicle, _helper).Result;

            if (!string.IsNullOrEmpty(errMassage.ToString()))
                return ErrorResponse(errMassage.ToString());

            await SaveEvacuationPlan(evacuationPlan, _cache);

            await SaveEvacuationStatus(evacuationPlan, _cache);


            return new JsonDataDTO<List<EvacuationPlanDTO>>()
            {
                Data = evacuationPlan,
                Desc = "Evacuation Plan Successfully Retrieved",
            };
        }
        private static async Task SaveEvacuationPlan(List<EvacuationPlanDTO> plan, IDistributedCache _cache)
        {
            var newData = plan;
            var oldData = await _cache.GetObjectAsync<List<EvacuationPlanDTO>>(CacheKey.EVACUATION_PLAN);
            if (oldData != null)
            {
                var newDataId = newData.Select(x => x.ZoneId).ToList();

                var dataKeepToKeep = oldData.Where(x => !newDataId.Contains(x.ZoneId)).ToList();

                newData.AddRange(dataKeepToKeep);

                await _cache.RemoveAsync(CacheKey.EVACUATION_PLAN);
            }


            await _cache.SetObjectAsync(CacheKey.EVACUATION_PLAN, newData);
        }

        private static async Task SaveEvacuationStatus(List<EvacuationPlanDTO> plan, IDistributedCache _cache)
        {
            var zoneIds = plan.GroupBy(x => x.ZoneId).Select(x => x.Key).ToList();


            var evacuationStatus = new List<EvacuationStatusDTO>();

            await _cache.RemoveAsync(CacheKey.EVACUATION_STATUS);

            foreach (var zoneId in zoneIds)
            {
                var evacuationZones = await _cache.GetObjectAsync<List<EvacuationZonesDTO>>(CacheKey.EVACUATION_ZONE);

                var peopleToEvacuate = evacuationZones.FirstOrDefault(x => x.ZoneId == zoneId)?.NumberOfPeople ?? 0;

                evacuationStatus.Add(new EvacuationStatusDTO()
                {
                    ZoneId = zoneId,
                    LastVehicleId = null,
                    PeopleToEvacuate = peopleToEvacuate,
                    PeopleToEvacuated = 0,
                    LastTimeToCheck = null,
                    Status = EvacuationStatus.InProgress,
                });

            }
            await _cache.SetObjectAsync(CacheKey.EVACUATION_STATUS, evacuationStatus);
        }

        private static async Task<List<DraftEvacuationPlanDTO>> CreateDraftAllEvacuationScenarioPlanning(List<EvacuationZonesDTO> evacuationZones, List<VehicleDTO> vehicles, Helper _helper)
        {
            return await Task.Run(async () =>
            {
                var draftEvacuationPlans = new List<DraftEvacuationPlanDTO>();

                foreach (var zone in evacuationZones)
                {
                    foreach (var vehicle in vehicles)
                    {
                        var distance = await _helper.HaversineDistanceAsync(zone.LocationCoordinates.Latitude, zone.LocationCoordinates.Longitude, vehicle.LocationCoordinates.Latitude, vehicle.LocationCoordinates.Longitude);
                        var timeArriveInMinute = await _helper.CalculateTimeToReachDestinationAsync(distance, vehicle.Speed);

                        draftEvacuationPlans.Add(new DraftEvacuationPlanDTO()
                        {
                            VehicleId = vehicle.VehicleId,
                            ZoneId = zone.ZoneId,
                            ArriveInMinute = timeArriveInMinute,
                            PeopleToEvacuate = zone.NumberOfPeople,
                            VehicleCapacity = vehicle.Capacity,
                            UrgencyLevel = zone.UrgencyLevel
                        });
                    }
                }

                return draftEvacuationPlans;
            });
        }
        private static async Task<(List<EvacuationPlanDTO> evacuationPlan, StringBuilder errMassage)> PlanToEvacuation(List<EvacuationZonesDTO> evacuationZones, List<VehicleDTO> vehicles, Helper _helper)
        {
            var draftEvacuationPlans = await CreateDraftAllEvacuationScenarioPlanning(evacuationZones, vehicles, _helper);

            var (evacuationPlans, errorMessage) = await SelectBestPlan(draftEvacuationPlans, _helper);

            return (evacuationPlans.OrderBy(x => x.ZoneId).ToList(), errorMessage);
        }



        private static async Task<(List<EvacuationPlanDTO> evacuationPlans, StringBuilder errorEventHandler)> SelectBestPlan(List<DraftEvacuationPlanDTO> draftEvacuationPlans, Helper _helper)
        {
            return await Task.Run(async () =>
            {
                var dateNow = DateTime.Now;
                var evacuationPlans = new List<EvacuationPlanDTO>();

                StringBuilder errorEventHandler = new StringBuilder();

                var groupedZones = draftEvacuationPlans
                    .GroupBy(x => new { x.ZoneId, x.UrgencyLevel })
                    .OrderByDescending(x => x.Key.UrgencyLevel)
                    .Select(x => x.ToList())
                    .ToList();

                foreach (var zones in groupedZones)
                {
                    // var missingPersonsInPlans = new List<MissingPersoninPlanDTO>();
                    var vehicleInPlan = evacuationPlans.Select(x => x.VehicleId).ToList();
                    int peopleHaveVehicle = 0;

                    var (vehicleAvailable, errMassage) = await GetAvailableVehicle(zones, vehicleInPlan);

                    if (vehicleAvailable.Any() == false)
                        return (evacuationPlans, errMassage);
                    int vehicleAvailableCount = vehicleAvailable.Count;

                    for (int i = 0; i < vehicleAvailableCount; i++)
                    {
                        int peopleToEvacuate = zones.First().PeopleToEvacuate;
                        var item = vehicleAvailable[i];
                        if (peopleHaveVehicle >= item.PeopleToEvacuate)
                            break;

                        evacuationPlans.Add(new EvacuationPlanDTO()
                        {
                            VehicleId = item.VehicleId,
                            ZoneId = item.ZoneId,
                            EstimatedTimeOfArrival = dateNow.AddMinutes(item.ArriveInMinute),
                            PeopleToEvacuate = peopleToEvacuate >= item.VehicleCapacity
                            ? item.VehicleCapacity : peopleToEvacuate,
                        });

                        peopleHaveVehicle += item.VehicleCapacity;
                        peopleToEvacuate -= peopleHaveVehicle;

                        if (await IsLastItem(i, vehicleAvailableCount) && peopleToEvacuate > 0)
                            errorEventHandler.Append($"ZoneId: {item.ZoneId} does not have enough available vehicles for {peopleToEvacuate} people.<br>");
                    }

                    // AssignMissingPersonsToEvacuationPlans(missingPersonsInPlans, draftEvacuationPlans, evacuationPlans);
                }

                return (evacuationPlans, errorEventHandler);
            });
        }
        private static Task<bool> IsLastItem(int index, int vehicleAvailableCount)
        {
            return Task.FromResult(index == vehicleAvailableCount - 1);
        }
        private static async Task<(List<DraftEvacuationPlanDTO> draftEvacuationPlan, StringBuilder errorMessage)> GetAvailableVehicle(
         List<DraftEvacuationPlanDTO> draftEvacuationPlans, List<string?>? vehicleInPlan)
        {
            var vehicleAvailable = GetNotInPlanVehicles(draftEvacuationPlans, vehicleInPlan);

            if (vehicleAvailable.Count == 0)
            {
                return (vehicleAvailable, new StringBuilder($"Right now there are no vehicles available for ZoneId {vehicleAvailable.First().ZoneId}."));
            }

            int peopleToEvacuate = vehicleAvailable.First().PeopleToEvacuate;
            var urgencyLevel = vehicleAvailable.First().UrgencyLevel;
            var zoneId = vehicleAvailable.First().ZoneId;


            if (!urgencyLevelArriveTimeDdl.ContainsKey(urgencyLevel))
            {
                throw new ArgumentOutOfRangeException(nameof(urgencyLevel), $"Invalid urgency level for ZoneId: {zoneId} <br>");
            }

            vehicleAvailable = vehicleAvailable
               .Where(item => item.ArriveInMinute <= urgencyLevelArriveTimeDdl[urgencyLevel])
               .ToList();

            var possibleCombination = await CreateSubsetsAsync(vehicleAvailable.ToArray(), peopleToEvacuate);

            var possibleStatics = GeneratePossiblePlanStatics(possibleCombination, peopleToEvacuate);

            var bestVehiclesPlan = SelectBestPosiblePlan(possibleStatics, possibleCombination);



            var errorMessage = new StringBuilder();
            if (bestVehiclesPlan.Count == 0)
            {
                errorMessage.AppendLine(
                    $"No available vehicle for zoneId: {zoneId} with urgency level {urgencyLevel}. " +
                    $"Required capacity is not match (Missing people to evacuate: {peopleToEvacuate}).");
            }

            return (bestVehiclesPlan, errorMessage);
        }
        public static async Task<List<T[]>> CreateSubsetsAsync<T>(T[] originalArray, int peopleToEvacuate)
        {
            return await Task.Run(() =>
            {
                List<T[]> subsets = new List<T[]>();

                for (int i = 0; i < originalArray.Length; i++)
                {
                    int subsetCount = subsets.Count;
                    subsets.Add(new T[] { originalArray[i] });

                    for (int j = 0; j < subsetCount; j++)
                    {
                        T[] newSubset = new T[subsets[j].Length + 1];
                        subsets[j].CopyTo(newSubset, 0);
                        newSubset[newSubset.Length - 1] = originalArray[i];
                        subsets.Add(newSubset);
                    }
                }

                return subsets;
            });
        }


        private static List<DraftEvacuationPlanDTO> SelectBestPosiblePlan(List<PosiblePlanStaticDTO> posiblePlans, List<DraftEvacuationPlanDTO[]>? vehicleInPlan)
        {
            if (posiblePlans == null || !posiblePlans.Any() || vehicleInPlan == null || !vehicleInPlan.Any())
                return new List<DraftEvacuationPlanDTO>();

            var bestPlan = posiblePlans.OrderBy(x => x.ArriveInMinute).FirstOrDefault();

            if (bestPlan == null)
                return new List<DraftEvacuationPlanDTO>();

            if (bestPlan.DiffCapacityAndPeopleToEvacuate > 10)
            {
                var closestCapacity = posiblePlans
                    .Where(x => x.DiffCapacityAndPeopleToEvacuate <= 10)
                    .OrderBy(x => x.ArriveInMinute)
                    .FirstOrDefault();

                if (closestCapacity != null)
                {
                    bestPlan = closestCapacity;
                }
            }

            return vehicleInPlan[bestPlan.index].OrderByDescending(x=> x.VehicleCapacity).ToList();
        }

        private static Dictionary<int, int> urgencyLevelArriveTimeDdl = new Dictionary<int, int>
        {
            { 1, 520 },
            { 2, 360 },
            { 3, 300 },
            { 4, 180 },
            { 5, 120 },
        };

        private static List<DraftEvacuationPlanDTO> GetNotInPlanVehicles(
            List<DraftEvacuationPlanDTO> draftEvacuationPlans,
            List<string?>? vehicleInPlan)
        {
            return draftEvacuationPlans
                .Where(x => vehicleInPlan?.Contains(x.VehicleId) == false)
                .OrderBy(x => x.UrgencyLevel)
                .ThenBy(x => x.ArriveInMinute)
                .ToList();
        }

        private static List<PosiblePlanStaticDTO> GeneratePossiblePlanStatics(
            List<DraftEvacuationPlanDTO[]> possibleCombination,
            int peopleToEvacuate)
        {
            var possibleStatics = new List<PosiblePlanStaticDTO>();

            for (int i = 0; i < possibleCombination.Count; i++)
            {
                if (possibleCombination[i].Sum(x => x.VehicleCapacity) >= peopleToEvacuate)
                {
                    possibleStatics.Add(CreatePosiblePlanStatic(possibleCombination[i], peopleToEvacuate, i));
                }
            }

            return possibleStatics;
        }
        private static PosiblePlanStaticDTO CreatePosiblePlanStatic(DraftEvacuationPlanDTO[] posiblePlans, int peopleToEvacuate, int index)
        {
            var data = new PosiblePlanStaticDTO()
            {
                index = index,
                ArriveInMinute = posiblePlans.Average(x => x.ArriveInMinute),
                DiffCapacityAndPeopleToEvacuate = posiblePlans.Sum(x => x.VehicleCapacity) - peopleToEvacuate,
                TotalVehicle = posiblePlans.Count(),
                UrgencyLevel = posiblePlans.FirstOrDefault()?.UrgencyLevel
            };
            return data;
        }
        // // https://stackoverflow.com/questions/7802822/all-possible-combinations-of-a-list-of-values
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
        // private static IEnumerable<List<DraftEvacuationPlanDTO>> GetCombinations(List<DraftEvacuationPlanDTO> list)
        // {
        //     int n = list.Count;
        //     for (int i = 1; i < (1 << n); i++)
        //     {
        //         var combination = new List<DraftEvacuationPlanDTO>();
        //         for (int j = 0; j < n; j++)
        //         {
        //             if ((i & (1 << j)) != 0)
        //             {
        //                 combination.Add(list[j]);
        //             }
        //         }
        //         yield return combination;
        //     }
        // }
        //https://stackoverflow.com/questions/3319586/getting-all-possible-combinations-from-a-list-of-numbers/3319597
        // private static List<T[]> CreateSubsets<T>(T[] originalArray)
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



        // private async Task validate(List<MissingPersoninPlanDTO> missingPersonInPlans)
        // {
        //     StringBuilder errorEventHandler = new StringBuilder();
        //         foreach (var item in missingPersonInPlans)
        //         {
        //             errorEventHandler.Append($"ZoneId: {item.ZoneId}  <br>");
        //         }

        //     var jsonData = new JsonDataDTO<List<object>>()
        //     {
        //         IsError = true,
        //         StatusCode = 500,
        //         ErrorMessage = "Validation Error"
        //     };
        //     if (errorEventHandler.Length > 0)
        //         throw new Exception(JsonSerializer.Serialize(jsonData));

        // }
        // private static void AssignMissingPersonsToEvacuationPlans(
        // List<MissingPersoninPlanDTO> missingPersonInPlans,
        // List<DraftEvacuationPlanDTO> draftEvacuationPlans,
        // List<EvacuationPlanDTO> evacuationPlans)
        // {
        //     if (missingPersonInPlans.Count == 0 || draftEvacuationPlans.Count == 0)
        //         return;

        //     var vehicles = draftEvacuationPlans
        //                     .GroupBy(x => x.VehicleId)
        //                     .Select(x => x.First())
        //                     .ToList();

        //     List<EvacuationPlanDTO> newPlans = new List<EvacuationPlanDTO>();

        //     foreach (var missingPersonInPlan in missingPersonInPlans)
        //     {
        //         int totalMissingPerson = missingPersonInPlan.Total;
        //         var otherRound = evacuationPlans
        //                           .OrderBy(x => x.EstimatedTimeOfArrival)
        //                           .ToList();

        //         foreach (var item in otherRound)
        //         {
        //             if (totalMissingPerson <= 0)
        //                 break;

        //             var vehicle = vehicles.FirstOrDefault(x => x.VehicleId == item.VehicleId);
        //             if (vehicle == null) continue;

        //             int peopleToVehicle = Math.Min(totalMissingPerson, vehicle.VehicleCapacity);
        //             totalMissingPerson -= peopleToVehicle;

        //             newPlans.Add(new EvacuationPlanDTO()
        //             {
        //                 VehicleId = item.VehicleId,
        //                 ZoneId = missingPersonInPlan.ZoneId,
        //                 EstimatedTimeOfArrival = item.EstimatedTimeOfArrival.AddMinutes(vehicle.ArriveInMinute),
        //                 PeopleToEvacuate = peopleToVehicle,
        //             });
        //         }
        //     }

        //     evacuationPlans.AddRange(newPlans);
        // }


        // private static Task<double> HaversineDistanceAsync(double lat1, double lon1, double lat2, double lon2)
        // {
        //     return Task.Run(async () =>
        //     {
        //         const double earthRadiusKm = 6371.0;

        //         double dLat = await DegreesToRadiansAsync(lat2 - lat1);
        //         double dLon = await DegreesToRadiansAsync(lon2 - lon1);

        //         double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
        //                    Math.Cos(await DegreesToRadiansAsync(lat1)) * Math.Cos(await DegreesToRadiansAsync(lat2)) *
        //                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        //         double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        //         double distance = earthRadiusKm * c;

        //         return distance;
        //     });
        // }

        // public static Task<double> DegreesToRadiansAsync(double degrees)
        // {
        //     return Task.FromResult(degrees * Math.PI / 180.0);
        // }

        // private static async Task<double> CalculateTimeToReachDestinationAsync(double distance, double speed)
        // {
        //     return await Task.Run(() =>
        //     {
        //         return (distance / speed) * 60;
        //     });
        // }
        private static JsonDataDTO<List<EvacuationPlanDTO>> ErrorResponse(string errMassage)
        {
            return new JsonDataDTO<List<EvacuationPlanDTO>>()
            {
                Data = null,
                IsError = true,
                StatusCode = 500,
                ErrorMessage = errMassage,
            };
        }
    }
}