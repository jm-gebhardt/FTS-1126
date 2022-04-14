using Microsoft.Extensions.Hosting;

try
{
    using IHost host = Gebhardt.StoreWare.Agv.Common.GebhardtStartup.BuildHost<Gebhardt.StoreWare.Agv.TransportOrder.Worker>(args);
    host.Run();
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

return 0;