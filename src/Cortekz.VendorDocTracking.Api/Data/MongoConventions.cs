using MongoDB.Bson.Serialization.Conventions;

namespace Cortekz.VendorDocTracking.Api.Data;

public static class MongoConventions
{
    public static void Register()
    {
        var pack = new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new IgnoreExtraElementsConvention(true)
        };

        ConventionRegistry.Register("cortekz-camel-case", pack, t => t.Namespace == "Cortekz.VendorDocTracking.Api.Models");
    }
}
