using System;
using System.IO;
using AdvancedMod.Basic.Calculate.Diagnostics;

if (args.Length != 1)
{
    Console.Error.WriteLine("用法：dotnet run --project Tools/CalculateReplay -- <Trace.jsonl>");
    return 2;
}
int count = 0, mismatches = 0;
try
{
    foreach (string line in File.ReadLines(args[0]))
    {
        count++;
        try
        {
            var trace = CombatTraceJson.Deserialize(line);
            var comparison = CombatTraceReplay.Replay(trace);
            if (!comparison.Matches)
            {
                mismatches++;
                Console.Error.WriteLine($"第 {count} 行，命中 {trace.HitId}：{string.Join("；", comparison.Differences)}");
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"第 {count} 行不能重放：{exception.Message}");
            return 2;
        }
    }
    if (count == 0) throw new InvalidDataException("Trace 文件没有命中记录。");
    Console.WriteLine($"重放 {count} 条记录，差异 {mismatches} 条。");
    return mismatches == 0 ? 0 : 1;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 2;
}
