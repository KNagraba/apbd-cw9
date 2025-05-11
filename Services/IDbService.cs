using Tutorial9.Model;

namespace Tutorial9.Services;

public interface IDbService
{
    Task DoSomethingAsync();
    Task ProcedureAsync();
    Task<int> AddProductToWarehouseAsync(WarehouseRequest request);
    Task<int> AddProductToWarehouseViaProcedureAsync(WarehouseRequest request);


}