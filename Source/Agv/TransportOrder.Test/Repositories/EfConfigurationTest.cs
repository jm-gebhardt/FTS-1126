using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class EfConfigurationTest
{
    [TestMethod]
    public void Model_Builds_Without_Throwing()
    {
        DbContextOptions<TransportOrderDbContext> options = new DbContextOptionsBuilder<TransportOrderDbContext>()
            .UseSqlServer("Server=.;Database=x;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        using var context = new TransportOrderDbContext(options);
        context.Model.ShouldNotBeNull();
    }
}