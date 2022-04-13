try
{
    Gebhardt.StoreWare.Agv.Common.GebhardtStartup.Run<Gebhardt.StoreWare.Agv.WCSCommunication.Worker>(args);
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

return 0;