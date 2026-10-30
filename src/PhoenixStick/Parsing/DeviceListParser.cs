using PhoenixStick.Device;

namespace PhoenixStick.Parsing;

/// <summary>Liest den Status eines Geräts aus der Ausgabe von <c>adb devices</c>.</summary>
public static class DeviceListParser
{
    public static DeviceState GetState(string output, string serial)
    {
        foreach (var rawLine in output.Split('\n'))
        {
            var parts = rawLine.Trim().Split('\t');
            if (parts.Length < 2 || parts[0] != serial)
                continue;

            return parts[1].Trim() switch
            {
                "device" => DeviceState.Device,
                "unauthorized" => DeviceState.Unauthorized,
                "offline" => DeviceState.Offline,
                _ => DeviceState.Unknown,
            };
        }
        return DeviceState.NotFound;
    }
}
