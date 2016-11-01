namespace Edison.Roshambo.Domain.Models
{
   public class Competitor
    {
       public int IdUser { get; set; }
       public int IdLobby { get; set; }
       public virtual  CustomUser User { get; set; }
       public virtual Lobby Lobby { get; set; }
    }
}
