USE CosmeticsOrdersDb;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

CREATE OR ALTER PROCEDURE dbo.usp_ConfirmOrder
    @OrderId   BIGINT,
    @ChangedBy NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @Status NVARCHAR(30) =
        (SELECT Status FROM dbo.Orders WITH (UPDLOCK) WHERE Id = @OrderId AND IsDeleted = 0);

    IF @Status IS NULL
        THROW 50001, 'Order not found', 1;

    IF @Status NOT IN ('Submitted', 'AwaitingValidation')
        THROW 50002, 'Stock can be confirmed only for a new order', 1;

    UPDATE dbo.Orders
    SET Status = 'StockConfirmed',
        UpdatedAt = SYSUTCDATETIME(),
        UpdatedBy = @ChangedBy
    WHERE Id = @OrderId;

    INSERT dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedBy)
    VALUES (@OrderId, @Status, 'StockConfirmed', @ChangedBy);

    COMMIT TRANSACTION;
END
GO
