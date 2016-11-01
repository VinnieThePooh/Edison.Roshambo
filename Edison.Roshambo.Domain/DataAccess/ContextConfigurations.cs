using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration;
using Edison.Roshambo.Domain.Models;

namespace Edison.Roshambo.Domain.DataAccess
{
    public class CustomUserConfiguration : EntityTypeConfiguration<CustomUser>
    {
        public CustomUserConfiguration()
        {
            ToTable("Users");
            HasKey(x => x.Id);
            HasOptional(x => x.Competitor).WithRequired(x => x.User);
            HasMany(x => x.GameRounds).WithOptional(x => x.RoundWinner).HasForeignKey(x => x.IdRoundWinner).WillCascadeOnDelete(false);
        }
    }

    
    public class LobbyConfiguration : EntityTypeConfiguration<Lobby>
    {
        public LobbyConfiguration()
        {
            HasKey(x => x.LobbyId);
            Property(x => x.LobbyName)
                 .IsRequired()
                 .HasMaxLength(300)
                 .HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute { IsUnique = true }));
            HasRequired(x => x.LobbyOwner).WithOptional(x => x.OwnLobby);
            HasMany(x => x.Players).WithRequired(x => x.Lobby).HasForeignKey(x => x.IdLobby).WillCascadeOnDelete(true);
        }
    }

    public class LobbyStateConfiguration : EntityTypeConfiguration<LobbyState>
    {
        public LobbyStateConfiguration()
        {
            ToTable("LobbyStates", "handbooks");
            HasKey(x => x.LobbyStateId);
            Property(x => x.LobbyStateId).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(300)
                .HasColumnAnnotation(IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute { IsUnique = true }));

            HasMany(x => x.Lobbies)
                .WithRequired(x => x.LobbyState)
                .HasForeignKey(x => x.IdLobbyState)
                .WillCascadeOnDelete(false);
        }
    }

    public class CompetitorConfiguration : EntityTypeConfiguration<Competitor>
    {
        public CompetitorConfiguration()
        {
            ToTable("Players", "dbo");
            HasKey(x => x.IdUser);
            HasRequired(x => x.User).WithOptional(x => x.Competitor);
        }
    }


    public class GameConfiguration : EntityTypeConfiguration<Game>
    {
        public GameConfiguration()
        {
            ToTable("Games");
            HasRequired(x => x.Lobby).WithMany(x => x.Games).HasForeignKey(x => x.IdLobby).WillCascadeOnDelete(true);
            HasRequired(x => x.LobbyOwner).WithMany(x => x.Games).HasForeignKey(x => x.IdLobbyOwner).WillCascadeOnDelete(true);
            HasMany(x => x.Rounds).WithRequired(x => x.ParentGame).HasForeignKey(x => x.IdGame).WillCascadeOnDelete(true);
        }
    }


    public class GameShapeConfiguration : EntityTypeConfiguration<GameShape>
    {

        public GameShapeConfiguration()
        {
            ToTable("GameShapes", "handbooks");
            HasKey(x => x.ShapeId);
        }
    }


    public class GameRoundConfiguration : EntityTypeConfiguration<GameRound>
    {

        public GameRoundConfiguration()
        {
            ToTable("GameRounds");
            // two one-to-many relationships between the same tables
            HasOptional(x => x.RoundWinner).WithMany(x => x.GameRounds).HasForeignKey(x => x.IdRoundWinner).WillCascadeOnDelete(false);
        }
    }
}
