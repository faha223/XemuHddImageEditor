namespace FatX.Net.Helpers;

internal static class DatePacker
{
    private const int Epoch = 2000;

    private static int UnpackSecond(ushort timeBytes) => (timeBytes & 0x1f) * 2;
    private static int UnpackMinute(ushort timeBytes) => (timeBytes >> 5) & 0x1f;
    private static int UnpackHour(ushort timeBytes) => (timeBytes >> 11) & 0xf;
    private static int UnpackDay(ushort dateBytes) => dateBytes & 0x1f;
    private static int UnpackMonth(ushort dateBytes) => (dateBytes >> 5) & 0x0f;
    private static int UnpackYear(ushort dateBytes) => ((dateBytes >> 9) & 0x7f) + Epoch;
    
    public static DateTime Unpack(ushort dateBytes, ushort timeBytes)
    {
        var year = UnpackYear(dateBytes);
        var month = UnpackMonth(dateBytes);
        var day = UnpackDay(dateBytes);

        var hour = UnpackHour(timeBytes);
        var minute = UnpackMinute(timeBytes);
        var second = UnpackSecond(timeBytes);

        return new DateTime(year, month, day, hour, minute, second);
    }

    public static void Pack(DateTime dateAndTime, out ushort dateBytes, out ushort timeBytes)
    {
        dateBytes = (ushort)((dateAndTime.Day & 0x1f) | ((dateAndTime.Month & 0xf) << 5) | (((dateAndTime.Year - Epoch) & 0x7f) << 9));
        timeBytes = (ushort)(((dateAndTime.Hour & 0xf) << 11) | ((dateAndTime.Minute & 0x1f) << 5) | ((dateAndTime.Second / 2) & 0x1f));
    }
}