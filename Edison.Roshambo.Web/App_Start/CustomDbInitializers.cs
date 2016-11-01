using System.Data.Entity;
using System.Diagnostics;
using Edison.Roshambo.Domain.DataAccess;
using Edison.Roshambo.Domain.Infrastructure;

namespace Edison.Roshambo.Web
{
    public class CustomCreateDbIfNotExist : CreateDatabaseIfNotExists<RoshamboContext>
    {
        protected override void Seed(RoshamboContext context)
        {   
            Debug.WriteLine("In CustomCreateDbIfNotExist.");
            DbSeeding.SeedContext(context);
            base.Seed(context);
        }
    }
}