using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class CustomerPlanService : ICustomerPlanService
{
    private readonly IdentityDbContext _db;

    public CustomerPlanService(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> AssignAsync(Guid customerId, AssignPlanToCustomerDto dto)
    {
        var assignment = await _db.CustomerPlanAssignments
            .FirstOrDefaultAsync(x => x.CustomerId == customerId);

        if (assignment == null)
        {
            assignment = new CustomerPlanAssignment
            {
                CustomerId = customerId,
                PlanId = dto.PlanId,
                BillingCycle = dto.BillingCycle,
                StartDate = dto.StartDate,
                CustomPrice = dto.CustomPrice,
                CustomUserLimit = dto.CustomUserLimit,
                CustomVehicleLimit = dto.CustomVehicleLimit
            };

            _db.CustomerPlanAssignments.Add(assignment);
        }
        else
        {
            assignment.PlanId = dto.PlanId;
            assignment.BillingCycle = dto.BillingCycle;
            assignment.StartDate = dto.StartDate;
            assignment.CustomPrice = dto.CustomPrice;
            assignment.CustomUserLimit = dto.CustomUserLimit;
            assignment.CustomVehicleLimit = dto.CustomVehicleLimit;
        }

        await _db.SaveChangesAsync();
        return assignment.CustomerPlanAssignmentId;
    }

    public async Task<AssignPlanToCustomerDto?> GetAssignedAsync(Guid customerId)
    {
        var assignment = await _db.CustomerPlanAssignments
            .FirstOrDefaultAsync(x => x.CustomerId == customerId);

        if (assignment == null) return null;

        return new AssignPlanToCustomerDto
        {
            CustomerId = assignment.CustomerId,
            PlanId = assignment.PlanId,
            BillingCycle = assignment.BillingCycle,
            StartDate = assignment.StartDate,
            CustomPrice = assignment.CustomPrice,
            CustomUserLimit = assignment.CustomUserLimit,
            CustomVehicleLimit = assignment.CustomVehicleLimit
        };
    }
}