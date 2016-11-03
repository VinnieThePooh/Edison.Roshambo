using Edison.Roshambo.Domain.Infrastructure;

namespace Edison.Roshambo.Web.Infrastructure
{
    public class PlayerMetadata
    {
        public string UserName { get; private set; }
        public string ShapeName { get; private set; }

        public PlayerMetadata(string userName, string shapeName = ShapeNames.Undefined)
        {
            UserName = userName;
            ShapeName = shapeName;
        }

        public PlayerMetadata():this(null) { }
    }
}