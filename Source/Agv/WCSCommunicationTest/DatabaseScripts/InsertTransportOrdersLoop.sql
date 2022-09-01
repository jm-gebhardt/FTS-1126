USE master;

DELETE FROM ToAgv;
DELETE FROM FromAgv;
DELETE FROM OrderSaga;
DELETE FROM OrderSaga_OrderDetails;
DELETE FROM OrderChangedEvent;

DECLARE @Loop INT;
SET @Loop = 1;

DECLARE @Sql AS NVARCHAR(MAX);
DECLARE @LoopNVARCHAR AS NVARCHAR(10);

WHILE (@Loop <= 30)
BEGIN
	SET @LoopNVARCHAR = CAST(@Loop AS NVARCHAR(20));

	SET @Sql = 'INSERT INTO ToAgv (RecordType, MessageStatus, Orderid, OrderType, LeNo, Source, Destination, Status, Creator, Created, CcuVersion, TimeStamp) 
	VALUES (''TransportOrder'', ''Pending'', ''ORD_'+@LoopNVARCHAR+''', ''Transport'', ''LE_'+@LoopNVARCHAR+''', ''SRC_'+@LoopNVARCHAR+''', ''DST_'+@LoopNVARCHAR+''', ''Initial'', ''IRS/Kn'', CURRENT_TIMESTAMP, 0, CURRENT_TIMESTAMP);';

	exec (@Sql)
    SET @Loop = @Loop + 1
END


SELECT COUNT(Id) AS NumberOfRows FROM ToAgv;