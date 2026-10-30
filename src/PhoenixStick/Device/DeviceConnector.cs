using PhoenixStick.Adb;
using PhoenixStick.Parsing;

namespace PhoenixStick.Device;

public record ConnectionResult(DeviceState State, string ConnectMessage);

public class DeviceConnector(IAdbClient adb)
{
    private readonly IAdbClient _adb = adb;

    public async Task<ConnectionResult> ConnectAsync(CancellationToken ct = default)
    {
        var connect = await _adb.ConnectAsync(ct);
        var devices = await _adb.DevicesAsync(ct);
        var state = DeviceListParser.GetState(devices.StdOut, _adb.Serial);

        var message = (connect.StdOut + connect.StdErr).Trim();
        return new ConnectionResult(state, message);
    }

    public static string Explain(DeviceState state) => state switch
    {
        DeviceState.Device => "verbunden",
        DeviceState.Unauthorized =>
            "Nicht freigegeben. Am Fernseher die Abfrage \"USB-Debugging zulassen\" bestätigen "
            + "(Haken bei \"Immer von diesem Computer zulassen\") und den Befehl wiederholen.",
        DeviceState.Offline =>
            "Gerät meldet offline. Stick neu starten, dann nochmal verbinden.",
        DeviceState.NotFound =>
            "Nicht erreichbar. Ist der Stick an, stimmt die IP und ist ADB-Debugging eingeschaltet?",
        _ => "Unbekannter Status, siehe Ausgabe von adb devices.",
    };
}
