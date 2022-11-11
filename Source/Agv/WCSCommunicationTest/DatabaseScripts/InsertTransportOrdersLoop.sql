USE master;

DELETE FROM ToAgv;
DELETE FROM FromAgv;
DELETE FROM OrderSaga;
DELETE FROM OrderSaga_OrderDetails;
DELETE FROM OrderChangedEvent;
DELETE FROM AgvSaga;
DELETE FROM Map;

DECLARE @Loop INT;
SET @Loop = 1;

DECLARE @DstNo NVARCHAR(20);
DECLARE @SrcNo NVARCHAR(20);

DECLARE @Sql AS NVARCHAR(MAX);
DECLARE @LoopNVARCHAR AS NVARCHAR(10);

WHILE (@Loop <= 100)
BEGIN
	-- Random number between max and min
	-- CAST( FLOOR( RAND() * max ) + min
	SET @DstNo = CAST( FLOOR( RAND() * 3 ) + 1 AS NVARCHAR(20) );
	SET @SrcNo = CAST( FLOOR( RAND() * 3 ) + 1 AS NVARCHAR(20) );

	SET @LoopNVARCHAR = CAST(@Loop AS NVARCHAR(20));

	SET @Sql = 'INSERT INTO ToAgv (RecordType, MessageStatus, Orderid, OrderType, LeNo, Source, Destination, Status, Creator, Created, CcuVersion, TimeStamp) 
	VALUES (''TransportOrder'', ''Pending'', ''ORD_'+@LoopNVARCHAR+''', ''Transport'', ''LE_'+@LoopNVARCHAR+''', ''SRC_'+@SrcNo+''', ''DST_'+@DstNo+''', ''Initial'', ''IRS/Kn'', GETUTCDATE(), 0, CURRENT_TIMESTAMP);';

	exec (@Sql)
    SET @Loop = @Loop + 1
END


SELECT COUNT(Id) AS NumberOfRows FROM ToAgv;