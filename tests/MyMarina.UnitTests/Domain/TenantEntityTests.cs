using FluentAssertions;
using MyMarina.Domain.Common;

namespace MyMarina.UnitTests.Domain;

/// <summary>
/// Smoke tests verifying the base entity behaviour.
/// </summary>
public class TenantEntityTests
{
    private class TestEntity : TenantEntity { }

    [Fact]
    public void Id_is_assigned_on_construction()
    {
        var entity = new TestEntity();
        entity.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Two_entities_have_different_ids()
    {
        var a = new TestEntity();
        var b = new TestEntity();
        a.Id.Should().NotBe(b.Id);
    }

    [Fact]
    public void CreatedAt_is_set_to_utc_now()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var entity = new TestEntity();
        entity.CreatedAt.Should().BeAfter(before).And.BeBefore(DateTimeOffset.UtcNow.AddSeconds(1));
    }
}
