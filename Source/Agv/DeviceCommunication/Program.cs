using Microsoft.Extensions.Hosting;

try
{
    using var host = Gebhardt.StoreWare.Agv.Common.GebhardtStartup.BuildHost<Gebhardt.StoreWare.Agv.DeviceCommunication.Worker>(args);
    host.Run();
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

return 0;
