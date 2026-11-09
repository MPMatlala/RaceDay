using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Helpers
{
    // turns database models into DTOs so we never send the password hash back
    public static class Mapper
    {
        public static EventDto ToEventDto(Event e)
        {
            return new EventDto
            {
                EventId = e.EventId,
                OrganiserId = e.OrganiserId,
                EventName = e.EventName,
                EventDate = e.EventDate,
                Location = e.Location,
                Description = e.Description,
                Distance = e.Distance,
                EventType = e.EventType,
                BannerUrl = e.BannerUrl
            };
        }

        public static RouteDto ToRouteDto(RaceRoute r)
        {
            if (r == null) return null;

            return new RouteDto
            {
                RouteId = r.RouteId,
                CategoryId = r.CategoryId,
                StartPoint = r.StartPoint,
                EndPoint = r.EndPoint,
                ElevationGainM = r.ElevationGainM,
                MapUrl = r.MapUrl
            };
        }

        public static CategoryDto ToCategoryDto(Category c)
        {
            return new CategoryDto
            {
                CategoryId = c.CategoryId,
                EventId = c.EventId,
                CategoryName = c.CategoryName,
                DistanceKm = c.DistanceKm,
                EntryFee = c.EntryFee,
                MaxParticipants = c.MaxParticipants,
                Route = ToRouteDto(c.RaceRoute)
            };
        }

        // the enrolment must be loaded with Participant, Category and Category.Event
        public static EnrolmentDto ToEnrolmentDto(Enrolment e)
        {
            return new EnrolmentDto
            {
                EnrolmentId = e.EnrolmentId,
                ParticipantId = e.ParticipantId,
                ParticipantName = e.Participant.FullName,
                CategoryId = e.CategoryId,
                CategoryName = e.Category.CategoryName,
                EventId = e.Category.EventId,
                EventName = e.Category.Event.EventName,
                EventDate = e.Category.Event.EventDate,
                EnrolmentDate = e.EnrolmentDate,
                RaceNumber = e.RaceNumber,
                Status = e.Status
            };
        }

        // the result must be loaded with Enrolment, Enrolment.Participant, Category and Category.Event
        public static ResultDto ToResultDto(Result r)
        {
            return new ResultDto
            {
                ResultId = r.ResultId,
                EnrolmentId = r.EnrolmentId,
                ParticipantName = r.Enrolment.Participant.FullName,
                RaceNumber = r.Enrolment.RaceNumber,
                EventId = r.Enrolment.Category.EventId,
                EventName = r.Enrolment.Category.Event.EventName,
                CategoryName = r.Enrolment.Category.CategoryName,
                EventDate = r.Enrolment.Category.Event.EventDate,
                FinishTime = r.FinishTime,
                ChipTime = r.ChipTime,
                Position = r.Position
            };
        }
    }
}
