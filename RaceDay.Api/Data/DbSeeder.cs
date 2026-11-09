using RaceDay.Api.Models;

namespace RaceDay.Api.Data
{
    // Fills a brand-new database with sample data so every page has something to show.
    // It only runs when the database has no organisers yet, so it never touches real data.
    //
    // Demo logins (password for all of them: Password123!)
    //   Organiser   organiser@raceday.test
    //   Participant runner@raceday.test
    public static class DbSeeder
    {
        public const string DemoPassword = "Password123!";

        public static void Seed(RaceDayContext db)
        {
            if (db.Organisers.Any())
            {
                return;
            }

            string hash = BCrypt.Net.BCrypt.HashPassword(DemoPassword);
            DateTime today = DateTime.Today;

            // ---------- people ----------
            var organiser = new Organiser
            {
                FullName = "Sam Mokoena",
                Email = "organiser@raceday.test",
                PasswordHash = hash,
                PhoneNumber = "0821234567"
            };
            db.Organisers.Add(organiser);

            var runner = new Participant
            {
                FullName = "Thandi Nkosi",
                Email = "runner@raceday.test",
                PasswordHash = hash,
                PhoneNumber = "0837654321"
            };
            db.Participants.Add(runner);

            string[] names =
            {
                "Lerato Dlamini", "Pieter van Wyk", "Aisha Patel", "Sipho Khumalo", "Naledi Mahlangu", "Johan Botha",
                "Zanele Zulu", "Ruan Pretorius", "Fatima Moosa", "Kabelo Sithole", "Megan Naidoo"
            };

            var others = new List<Participant>();
            for (int i = 0; i < names.Length; i++)
            {
                others.Add(new Participant
                {
                    FullName = names[i],
                    Email = "runner" + (i + 1).ToString("00") + "@raceday.test",
                    PasswordHash = hash
                });
            }
            db.Participants.AddRange(others);
            db.SaveChanges();

            // ---------- events ----------
            var marathon = new Event
            {
                OrganiserId = organiser.OrganiserId,
                EventName = "Joburg City Marathon",
                EventDate = today.AddDays(21),
                Location = "Johannesburg, Gauteng",
                Description = "A fast, flat route through the heart of Johannesburg with full road closures, water tables every 2.5 km and a big finish-line party.",
                Distance = 42.2m,
                EventType = "Run"
            };
            var walk = new Event
            {
                OrganiserId = organiser.OrganiserId,
                EventName = "Sunrise Park Walk",
                EventDate = today.AddDays(10),
                Location = "Pretoria, Gauteng",
                Description = "A relaxed family walk through the park at sunrise. Prams, dogs on leads and walking sticks are all welcome.",
                Distance = 5m,
                EventType = "Walk"
            };
            var cycle = new Event
            {
                OrganiserId = organiser.OrganiserId,
                EventName = "Magaliesburg Cycle Classic",
                EventDate = today.AddDays(45),
                Location = "Magaliesburg, Gauteng",
                Description = "Rolling country roads and one serious climb. Timing chips are included in the entry fee.",
                Distance = 60m,
                EventType = "Cycle"
            };
            var past = new Event
            {
                OrganiserId = organiser.OrganiserId,
                EventName = "Soweto 10K Fun Run",
                EventDate = today.AddDays(-14),
                Location = "Soweto, Gauteng",
                Description = "A friendly 10 km community run that finished at the Orlando Stadium.",
                Distance = 10m,
                EventType = "Run"
            };
            db.Events.AddRange(marathon, walk, cycle, past);
            db.SaveChanges();

            // ---------- categories ----------
            var full = new Category { EventId = marathon.EventId, CategoryName = "42km Full Marathon", DistanceKm = 42.2m, EntryFee = 450m, MaxParticipants = 500 };
            var half = new Category { EventId = marathon.EventId, CategoryName = "21km Half Marathon", DistanceKm = 21.1m, EntryFee = 300m, MaxParticipants = 800 };
            var tenK = new Category { EventId = marathon.EventId, CategoryName = "10km Fun Run", DistanceKm = 10m, EntryFee = 180m, MaxParticipants = 1000 };
            var family = new Category { EventId = walk.EventId, CategoryName = "5km Family Walk", DistanceKm = 5m, EntryFee = 50m, MaxParticipants = 300 };
            var kids = new Category { EventId = walk.EventId, CategoryName = "Under 12", DistanceKm = 2m, EntryFee = 0m, MaxParticipants = 150 };
            var sixty = new Category { EventId = cycle.EventId, CategoryName = "60km Classic", DistanceKm = 60m, EntryFee = 350m, MaxParticipants = 400 };
            var thirty = new Category { EventId = cycle.EventId, CategoryName = "30km Social Ride", DistanceKm = 30m, EntryFee = 220m, MaxParticipants = 400 };
            var open = new Category { EventId = past.EventId, CategoryName = "10km Open", DistanceKm = 10m, EntryFee = 120m, MaxParticipants = 200 };
            var u20 = new Category { EventId = past.EventId, CategoryName = "Under 20", DistanceKm = 10m, EntryFee = 80m, MaxParticipants = 100 };
            db.Categories.AddRange(full, half, tenK, family, kids, sixty, thirty, open, u20);
            db.SaveChanges();

            db.RaceRoutes.AddRange(
                new RaceRoute { CategoryId = full.CategoryId, StartPoint = "Mary Fitzgerald Square", EndPoint = "Mary Fitzgerald Square", ElevationGainM = 180 },
                new RaceRoute { CategoryId = half.CategoryId, StartPoint = "Mary Fitzgerald Square", EndPoint = "Ellis Park", ElevationGainM = 95 },
                new RaceRoute { CategoryId = sixty.CategoryId, StartPoint = "Magaliesburg Village Hall", EndPoint = "Magaliesburg Village Hall", ElevationGainM = 820 });
            db.SaveChanges();

            // ---------- enrolments (race numbers match the API's own RD-1001, RD-1002 ... pattern) ----------
            int number = 1001;
            Func<int, int, string, Enrolment> enrol = (participantId, categoryId, status) => new Enrolment
            {
                ParticipantId = participantId,
                CategoryId = categoryId,
                RaceNumber = "RD-" + (number++),
                Status = status,
                EnrolmentDate = DateTime.Now.AddDays(-20)
            };

            // the finished event: twelve runners, the demo runner finishes 7th
            var finishers = new List<Participant>();
            finishers.AddRange(others.Take(6));
            finishers.Add(runner);
            finishers.AddRange(others.Skip(6));

            var pastEnrolments = new List<Enrolment>();
            foreach (var p in finishers)
            {
                pastEnrolments.Add(enrol(p.ParticipantId, open.CategoryId, "Confirmed"));
            }
            db.Enrolments.AddRange(pastEnrolments);

            // upcoming events: one of each status, so the colour coding is visible
            db.Enrolments.Add(enrol(runner.ParticipantId, tenK.CategoryId, "Confirmed"));
            db.Enrolments.Add(enrol(runner.ParticipantId, family.CategoryId, "Pending"));
            db.Enrolments.Add(enrol(runner.ParticipantId, thirty.CategoryId, "Cancelled"));
            db.Enrolments.Add(enrol(others[0].ParticipantId, half.CategoryId, "Confirmed"));
            db.Enrolments.Add(enrol(others[1].ParticipantId, half.CategoryId, "Confirmed"));
            db.Enrolments.Add(enrol(others[2].ParticipantId, full.CategoryId, "Confirmed"));
            db.Enrolments.Add(enrol(others[3].ParticipantId, sixty.CategoryId, "Pending"));
            db.SaveChanges();

            // ---------- published results for the finished event ----------
            for (int i = 0; i < pastEnrolments.Count; i++)
            {
                TimeSpan finish = new TimeSpan(0, 38, 20) + TimeSpan.FromSeconds(i * 73);
                db.Results.Add(new Result
                {
                    EnrolmentId = pastEnrolments[i].EnrolmentId,
                    FinishTime = finish,
                    ChipTime = finish - TimeSpan.FromSeconds(4),
                    Position = i + 1
                });
            }
            db.SaveChanges();
        }
    }
}
