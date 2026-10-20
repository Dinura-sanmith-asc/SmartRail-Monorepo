using SmartRail.Domain.RailwayNetwork.Entities;

namespace SmartRail.UnitTests.RailwayNetwork;

public sealed class TrainServiceIdentityTests
{
    [Fact]
    public void Optional_public_identity_is_normalized_and_distinct_from_internal_code()
    {
        var service = new TrainService(Guid.NewGuid(), "INTERNAL-42", Guid.NewGuid(),
            displayName: "  Test Service  ", publicServiceNumber: "  TEST-42  ");

        Assert.Equal("INTERNAL-42", service.ServiceCode);
        Assert.Equal("Test Service", service.DisplayName);
        Assert.Equal("TEST-42", service.PublicServiceNumber);
    }

    [Fact]
    public void Missing_public_identity_is_allowed_and_overlong_values_are_rejected()
    {
        var service = new TrainService(Guid.NewGuid(), "INTERNAL-43", Guid.NewGuid());
        Assert.Null(service.DisplayName);
        Assert.Null(service.PublicServiceNumber);

        Assert.Throws<ArgumentException>(() => service.SetPublicIdentity(new string('x', 201), "TEST-43"));
        Assert.Throws<ArgumentException>(() => service.SetPublicIdentity("Test Service", new string('x', 51)));
    }
}
