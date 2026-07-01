namespace HubClub.Helpers
{
    public static class BusinessHelper
    {
        /// <summary>
        /// Business day starts at 8:30 AM.
        /// Sessions before 8:30 AM belong to the previous business date.
        /// </summary>
        public static DateOnly GetBusinessDate(DateTime dt)
        {
            return dt.TimeOfDay < new TimeSpan(8, 30, 0)
                ? DateOnly.FromDateTime(dt.AddDays(-1))
                : DateOnly.FromDateTime(dt);
        }
    }
}