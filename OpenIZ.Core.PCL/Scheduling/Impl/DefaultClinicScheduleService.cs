using Newtonsoft.Json;
using OpenIZ.Core.Model;
using OpenIZ.Core.Model.Entities;
using OpenIZ.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Core.Scheduling.Impl
{
    /// <summary>
    /// Default clinic scheduling service
    /// </summary>
    public class DefaultClinicScheduleService : IClinicScheduleService
    {

        // Generic service guid
        private readonly Guid GenericServiceGuid = Guid.Parse("f5304ED0-6C9F-411B-B008-A1E1561B7963");

        /// <summary>
        /// Get the availability for the specified clinic at the specified time
        /// </summary>
        public ClinicScheduleInfo GetAvailability(Guid placeKey, Guid serviceConceptKey, DateTime atTime)
        {
            var schedule = this.GetSchedule(placeKey, serviceConceptKey);
            return schedule?.Schedule?.FirstOrDefault(o => o.Days.Any(d => d == atTime.DayOfWeek) && o.Start.GetValueOrDefault().TimeOfDay < atTime.TimeOfDay && o.Stop.GetValueOrDefault().TimeOfDay > atTime.TimeOfDay);
        }

        /// <summary>
        /// Get the schedule for the specified place and service
        /// </summary>
        public ClinicServiceScheduleInfo GetSchedule(Guid placeKey, Guid serviceConceptKey)
        {
            return this.GetSchedules(placeKey).FirstOrDefault(o => o.ServiceConceptKey == serviceConceptKey) ??
                this.GetSchedules(placeKey).FirstOrDefault(o => o.ServiceConceptKey == GenericServiceGuid);
        }

        /// <summary>
        /// Get all schedules for the specified place
        /// </summary>
        /// <param name="placeKey"></param>
        /// <returns></returns>
        public IEnumerable<ClinicServiceScheduleInfo> GetSchedules(Guid placeKey)
        {
            var placeRepository = ApplicationServiceContext.Current.GetService(typeof(IRepositoryService<Place>)) as IRepositoryService<Place>;
            if (placeRepository == null)
                throw new InvalidOperationException("Cannot find repository service for place");

            var place = placeRepository.Get(placeKey);
            if (place == null)
                throw new KeyNotFoundException($"Cannot find place {placeKey}");

            return place.LoadCollection<PlaceService>(nameof(Place.Services)).Select(o =>
            {
                var retVal = JsonConvert.DeserializeObject<ClinicServiceScheduleInfo>(o.ServiceSchedule);
                retVal.ServiceConceptKey = o.ServiceConceptKey.GetValueOrDefault();
                return retVal;  
            }).ToArray();
        }

        /// <summary>
        /// Save the specified schedule 
        /// </summary>
        public ClinicServiceScheduleInfo SaveSchedule(Guid placeKey, ClinicServiceScheduleInfo schedule)
        {
            var placeRepository = ApplicationServiceContext.Current.GetService(typeof(IRepositoryService<Place>)) as IRepositoryService<Place>;
            if (placeRepository == null)
                throw new InvalidOperationException("Cannot find repository service for place");

            var place = placeRepository.Get(placeKey);
            if (place == null)
                throw new KeyNotFoundException($"Cannot find place {placeKey}");

            // Update schedule
            var json = JsonConvert.SerializeObject(schedule);
            if (schedule.ServiceConceptKey == Guid.Empty) // Global schedule
            {
                var existing = place.LoadCollection<PlaceService>(nameof(Place.Services)).FirstOrDefault(o => o.ServiceConceptKey == GenericServiceGuid);
                if (existing == null)
                    place.Services.Add(new PlaceService()
                    {
                        ServiceConceptKey = GenericServiceGuid,
                        ServiceSchedule = json
                    });
                else
                    existing.ServiceSchedule = json;
            }
            else
            {
                var existing = place.LoadCollection<PlaceService>(nameof(Place.Services)).FirstOrDefault(o => o.ServiceConceptKey == schedule.ServiceConceptKey);
                if (existing == null)
                    place.Services.Add(new PlaceService()
                    {
                        ServiceConceptKey = schedule.ServiceConceptKey,
                        ServiceSchedule = json
                    });
                else
                    existing.ServiceSchedule = json;
            }

            // Now save
            placeRepository.Save(place);

            return schedule;
        }
    }
}
