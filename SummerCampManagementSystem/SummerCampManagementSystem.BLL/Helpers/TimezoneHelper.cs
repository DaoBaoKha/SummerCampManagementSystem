using System;
using System.Runtime.InteropServices;

namespace SummerCampManagementSystem.BLL.Helpers
{
    public static class TimezoneHelper
    {
        // Vietnam timezone is UTC+7
        // Use different timezone IDs based on OS
        private static readonly TimeZoneInfo VietnamTimeZone = GetVietnamTimeZone();

        private static TimeZoneInfo GetVietnamTimeZone()
        {
            try
            {
                // Check if running on Windows or Linux
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                }
                else
                {
                    // Linux/Unix uses IANA timezone IDs
                    return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
                }
            }
            catch (TimeZoneNotFoundException)
            {
                // Fallback: Create custom timezone for UTC+7
                return TimeZoneInfo.CreateCustomTimeZone(
                    "Vietnam Standard Time",
                    TimeSpan.FromHours(7),
                    "Vietnam Standard Time",
                    "Vietnam Standard Time"
                );
            }
        }

        /// <summary>
        /// Gets current Vietnam time (UTC+7)
        /// </summary>
        public static DateTime GetVietnamNow()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
        }

        /// <summary>
        /// Converts a DateTime to Vietnam time (UTC+7)
        /// </summary>
        public static DateTime ToVietnamTime(this DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Unspecified)
            {
                // ✅ FIX: Database stores UTC, so treat Unspecified as UTC
                // and convert to Vietnam time (+7 hours)
                var utcDate = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                return TimeZoneInfo.ConvertTimeFromUtc(utcDate, VietnamTimeZone);
            }

            // If already marked as UTC or Local, convert normally
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime.ToUniversalTime(), VietnamTimeZone);
        }

        /// <summary>
        /// Converts Vietnam time to UTC for database storage
        /// </summary>
        public static DateTime ToUtcForStorage(this DateTime vietnamDateTime)
        {
            if (vietnamDateTime.Kind == DateTimeKind.Utc)
            {
                return vietnamDateTime;
            }

            // Treat as Vietnam time and convert to UTC
            return TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(vietnamDateTime, DateTimeKind.Unspecified), 
                VietnamTimeZone
            );
        }
    }
}