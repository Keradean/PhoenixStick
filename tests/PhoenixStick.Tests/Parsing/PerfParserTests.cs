using PhoenixStick.Parsing;

namespace PhoenixStick.Tests.Parsing;

// Ausschnitte aus echten Ausgaben vom Stick
public class MeminfoParserTests
{
    private const string Output = """
        Applications Memory Usage (in Kilobytes):
        Uptime: 292348 Realtime: 292348

        Total PSS by process:
            148,652K: com.amazon.tv.launcher (pid 1538 / activities)
            135,907K: com.netflix.ninja (pid 3512)
             10,363K: com.amazon.prism.android.service:remote (pid 6046)
                265K: debuggerd:signaller (pid 200)

        Total PSS by OOM adjustment:
            130,960K: Native
                265K: debuggerd:signaller (pid 200)

        Total RAM: 1,316,676K (status normal)
        Avail RAM: 308108 kB
         Free RAM:   440,398K (  218,846K cached pss +   128,180K cached kernel +    93,372K free)
         Used RAM: 1,050,185K (  901,533K used pss +   148,652K kernel)
         Lost RAM:   -48,317K
        """;

    [Fact]
    public void Parse_ReadsTotalsAndUptime()
    {
        var info = MeminfoParser.Parse(Output);

        Assert.Equal(292348, info.UptimeMs);
        Assert.Equal(1_316_676, info.TotalKb);
        Assert.Equal(440_398, info.FreeKb);
        Assert.Equal(1_050_185, info.UsedKb);
    }

    [Fact]
    public void Parse_ReadsOnlyFirstProcessList()
    {
        var info = MeminfoParser.Parse(Output);

        Assert.Equal(4, info.Processes.Count);
        Assert.Equal(new ProcessMemory("com.amazon.tv.launcher", 1538, 148_652), info.Processes[0]);
        Assert.Equal("com.amazon.prism.android.service:remote", info.Processes[2].Name);
    }

    [Fact]
    public void Parse_WithoutTotalRam_Throws() =>
        Assert.Throws<FormatException>(() => MeminfoParser.Parse("Error: nichts da"));
}

public class CpuinfoParserTests
{
    private const string Output = """
        Load: 5.11 / 1.17 / 0.38
        CPU usage from 242290ms to 237222ms ago (2026-10-30 11:05:40.279 to 2026-10-30 11:13:45.488):
          76% 617/system_server: 57% user + 18% kernel / faults: 17363 minor 38 major
          28% 1183/com.android.proxyhandler: 22% user + 5.8% kernel / faults: 6911 minor 1 major
          15% 152/mmcqd/0: 0% user + 15% kernel
          6.6% 197/logd: 4.7% user + 1.9% kernel / faults: 593 minor 1 major
         +0% 7072/dumpsys: 0% user + 0% kernel
        14% TOTAL: 10% user + 3.3% kernel + 0% iowait
        """;

    [Fact]
    public void Parse_ReadsLoadAndProcesses()
    {
        var info = CpuinfoParser.Parse(Output);

        Assert.Equal((5.11, 1.17, 0.38), (info.Load1, info.Load5, info.Load15));
        Assert.Equal(5, info.Processes.Count);
        Assert.Equal(new ProcessCpu("system_server", 617, 76), info.Processes[0]);
        Assert.Equal(new ProcessCpu("mmcqd/0", 152, 15), info.Processes[2]);
        Assert.Equal(6.6, info.Processes[3].Percent);
        Assert.DoesNotContain(info.Processes, p => p.Name == "TOTAL");
    }
}
