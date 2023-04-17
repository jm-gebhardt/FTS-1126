/**
Number of orders to create
**/
DECLARE @cnt INT = 0;


WHILE @cnt < 1
BEGIN
Insert into [master].[dbo].[ToAgv]
           ([RecordType]
           ,[MessageStatus]
           ,[OrderId]
           ,[OrderType]
           ,[LeNo]
           ,[Source]
           ,[Destination]
           ,[Vehicle]
           ,[Status]
           ,[Error]
           ,[Creator]
           ,[Created]
           ,[CcuVersion]
           ,[TimeStamp])
     VALUES
           ('TransportOrder',
            'Pending',
            FLOOR(RAND()*100000) ,
            'Transport',
            '-',
            'S1', /* Put source node*/
            'D2', /* Put destination node*/
            null,
            'Initial',
            null,
            'Simulation',
            GETDATE(),
            1,
            GETDATE())
SET @cnt = @cnt + 1;
WAITFOR DELAY '00:00:02' /* Delay between orders*/
END;
