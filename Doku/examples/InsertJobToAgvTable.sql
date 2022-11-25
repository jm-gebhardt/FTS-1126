/**
Anzahl an Aufträgen, die erzeugt werden sollen
**/
DECLARE @AmountOfOrders INT = 50;
/** ENDE **/

DECLARE @cnt INT = 0;

WHILE @cnt < @AmountOfOrders
BEGIN
	INSERT INTO [master].[dbo].[ToAgv] (
		[RecordType]
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
		,[TimeStamp]
		)
	VALUES (
		'TransportOrder'
		,'Pending'
		,FLOOR(RAND() * 100000)
		,'Transport'
		,'-'
		,(
			SELECT str
			FROM (
				SELECT 0 AS id
					,'A' AS str
				
				UNION ALL
				
				SELECT 1
					,'B'
				
				UNION ALL
				
				SELECT 2
					,'C'
				) x
			WHERE id = floor(rand() * 3)
			)
		,/* Hier Source eintragen*/
		(
			SELECT str
			FROM (
				SELECT 0 AS id
					,'D' AS str
				
				UNION ALL
				
				SELECT 1
					,'E'
				
				UNION ALL
				
				SELECT 2
					,'F'
				
				UNION ALL
				
				SELECT 3
					,'G'
				) x
			WHERE id = floor(rand() * 4)
			)
		,/* Hier Destination eintragen*/
		NULL
		,'Initial'
		,NULL
		,'Simulation'
		,GETDATE()
		,1
		,GETDATE()
		)

	SET @cnt = @cnt + 1;

	/*WAITFOR DELAY '00:00:01' *//* Delay zwischen mehreren Aufträgen*/
END;
