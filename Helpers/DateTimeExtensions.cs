namespace TicketSupport.Helpers;

/// <summary>
/// Tüm tarihler veritabanında UTC olarak saklanır. Bu yardımcılar görüntüleme sırasında
/// değerleri Türkiye yerel saatine (Europe/Istanbul, UTC+3) çevirir.
/// </summary>
public static class DateTimeExtensions
{
    private static readonly TimeZoneInfo TurkeyTimeZone = ResolveTurkeyTimeZone();

    private static TimeZoneInfo ResolveTurkeyTimeZone()
    {
        foreach (var id in new[] { "Europe/Istanbul", "Turkey Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                // Bir sonraki kimliği dene.
            }
            catch (InvalidTimeZoneException)
            {
                // Bir sonraki kimliği dene.
            }
        }

        // Hiçbir saat dilimi bulunamazsa UTC+3 sabitine düş.
        return TimeZoneInfo.CreateCustomTimeZone("TR", TimeSpan.FromHours(3), "Türkiye", "Türkiye");
    }

    /// <summary>
    /// UTC bir tarihi yerel saate çevirip biçimlendirir.
    /// </summary>
    public static string ToLocalString(this DateTime utc, string format = "dd.MM.yyyy HH:mm")
    {
        return utc.ToLocal().ToString(format);
    }

    /// <summary>
    /// Nullable UTC tarih için biçimlendirme. Değer yoksa <paramref name="fallback"/> döner.
    /// </summary>
    public static string ToLocalString(this DateTime? utc, string format = "dd.MM.yyyy HH:mm", string fallback = "-")
    {
        return utc.HasValue ? utc.Value.ToLocalString(format) : fallback;
    }

    /// <summary>
    /// UTC bir tarihi Türkiye yerel saatine çevirir (biçimlendirmeden).
    /// </summary>
    public static DateTime ToLocal(this DateTime utc)
    {
        var asUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(asUtc, TurkeyTimeZone);
    }
}
