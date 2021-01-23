using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenIZ.Core.Scheduling
{
    /// <summary>
    /// Represents a clinic scheduling service
    /// </summary>
    public interface IClinicScheduleService
    {

        /// <summary>
        /// Gets the total number of slots for the available service for the specified date range
        /// </summary>
        /// <param name="placeKey">The place to fetch availability for</param>
        /// <param name="atTime">The time of availability</param>
        /// <param name="serviceConceptKey">The service which is being requested (Example: Vaccination)</param>
        /// <returns>The schedule for the specified date/time</returns>
        ClinicScheduleInfo GetAvailability(Guid placeKey, Guid serviceConceptKey, DateTime atTime);


        /// <summary>
        /// Get the specified schedule settings for the specified service
        /// </summary>
        /// <param name="placeKey">The place to fetch the schedule for</param>
        /// <param name="serviceConceptKey">The service concept to fetch the schedule for</param>
        /// <returns>The schedule information</returns>
        ClinicServiceScheduleInfo GetSchedule(Guid placeKey, Guid serviceConceptKey);

        /// <summary>
        /// Gets the specified schedules
        /// </summary>
        /// <param name="placeKey">The place to fetch the schedule for</param>
        /// <returns>The schedule information for all services</returns>
        IEnumerable<ClinicServiceScheduleInfo> GetSchedules(Guid placeKey);

        /// <summary>
        /// Save the specified schedule
        /// </summary>
        ClinicServiceScheduleInfo SaveSchedule(Guid placeKey, ClinicServiceScheduleInfo schedule);
    }
}
