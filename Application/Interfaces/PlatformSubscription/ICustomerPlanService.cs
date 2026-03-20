using System;
using System.Threading.Tasks;

public interface ICustomerPlanService
{
    Task<Guid> AssignAsync(Guid customerId, AssignPlanToCustomerDto dto);
    Task<AssignPlanToCustomerDto?> GetAssignedAsync(Guid customerId);
}