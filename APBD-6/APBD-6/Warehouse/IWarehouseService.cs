namespace APBD_6.Warehouse;

public interface IWarehouseService
{
    Task<int> AddProductToWarehouse(ProductWarehouse productWarehouse);
}