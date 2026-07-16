using DokanNet;
using DokanNet.Logging;
namespace DokanNetMirror;
internal static class Program
{
    static async Task<int> Main(string[] args)
    {
        if(args.Length<4||!long.TryParse(args[2],out var total)||!long.TryParse(args[3],out var free)){Console.Error.WriteLine("Usage: aggregate-proxy.exe <source> <drive:> <total> <free>");return 2;}
        try{using var log=new ConsoleLogger("[iPhone Drive] ");using var dokan=new DokanNet.Dokan(log);var mirror=new Mirror(log,Path.GetFullPath(args[0]),total,free);var builder=new DokanInstanceBuilder(dokan).ConfigureOptions(o=>{o.Options=DokanOptions.MountManager;o.MountPoint=args[1].TrimEnd('\\')+"\\";});using var instance=builder.Build(mirror);Console.CancelKeyPress+=(_,e)=>{e.Cancel=true;dokan.RemoveMountPoint(args[1]);};await instance.WaitForFileSystemClosedAsync(uint.MaxValue);return 0;}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
