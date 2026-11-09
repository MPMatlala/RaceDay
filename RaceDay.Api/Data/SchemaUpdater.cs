using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data
{
    // Adds the two Part 3 image columns to an existing database when they are missing,
    // so nobody has to run migrations or SQL by hand. Safe to run every time the API starts.
    public static class SchemaUpdater
    {
        public static void EnsureImageColumns(RaceDayContext context)
        {
            // the tests use an in-memory database, which has no columns to add
            if (!context.Database.IsSqlServer())
            {
                return;
            }

            AddColumnIfMissing(context, typeof(Event), "BannerUrl");
            AddColumnIfMissing(context, typeof(Participant), "ProfilePictureUrl");
        }

        private static void AddColumnIfMissing(RaceDayContext context, Type entity, string column)
        {
            var entityType = context.Model.FindEntityType(entity);
            string table = entityType.GetTableName();
            string schema = entityType.GetSchema() ?? "dbo";

            string sql = "IF COL_LENGTH('[" + schema + "].[" + table + "]', '" + column + "') IS NULL " +
                         "ALTER TABLE [" + schema + "].[" + table + "] ADD [" + column + "] nvarchar(500) NULL;";

            context.Database.ExecuteSqlRaw(sql);
        }
    }
}