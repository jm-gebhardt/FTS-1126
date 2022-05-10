-- This is an test example of a new order with random orderID
INSERT INTO ToAgv
    (RecordType, MessageStatus, OrderId, OrderType, LeNo, "Source", Destination, "Status", Creator, Created, CcuVersion, TimeStamp)
    VALUES ('TransportOrder', 'Pending', CONCAT('Order#', NEWID()), 'Transport', 'LeNo123', 'Source_1', 'Destination_1', 'Initial','ManualInsert', GETDATE(), 0, GETDATE());


select * from sys.schemas
select * from sys.tables