using Gebhardt.Acc.IntegrationTest;

var test1 = new CompleteOrderTest();
await test1.StartServicesAsync();
await test1.SingleAgvAdd_OrderProcessedSuccessful();
await test1.TwoAgvAdd_FourOrderProcessedSuccessful();
await test1.StopServicesAsync();

var test2 = new CompleteOrderTestVirtualAgv();
await test2.SingleVirtualAgvAdd_OrderProcessedSuccessful();

await test2.StopServices();

Console.WriteLine("ComposeTest completed");