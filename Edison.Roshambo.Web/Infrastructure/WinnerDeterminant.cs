using System;
using Edison.Roshambo.Domain.Infrastructure;

namespace Edison.Roshambo.Web.Infrastructure
{
    public static class WinnerDeterminant
    {
        public static string DetermineWinner(PlayerMetadata first, PlayerMetadata second)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            if (first.ShapeName.Equals(ShapeNames.Undefined) && second.ShapeName.Equals(ShapeNames.Undefined)) return null;

            if (first.ShapeName.Equals(ShapeNames.Undefined))
                return second.UserName;
            if (second.ShapeName.Equals(ShapeNames.Undefined))
                return first.UserName;

            if (first.ShapeName.Equals(ShapeNames.Scissors))
            {
                if (second.ShapeName.Equals(ShapeNames.Scissors)) return null;
                if (second.ShapeName.Equals(ShapeNames.Lizard)) return first.UserName;
                if (second.ShapeName.Equals(ShapeNames.Paper)) return first.UserName;
                if (second.ShapeName.Equals(ShapeNames.Rock)) return second.UserName;
                if (second.ShapeName.Equals(ShapeNames.Spock)) return second.UserName;
            }

            if (first.ShapeName.Equals(ShapeNames.Lizard))
            {
                if (second.ShapeName.Equals(ShapeNames.Lizard)) return null;
                if (second.ShapeName.Equals(ShapeNames.Scissors)) return second.UserName;
                if (second.ShapeName.Equals(ShapeNames.Paper)) return first.UserName;
                if (second.ShapeName.Equals(ShapeNames.Rock)) return second.UserName;
                if (second.ShapeName.Equals(ShapeNames.Spock)) return first.UserName;
            }

            if (first.ShapeName.Equals(ShapeNames.Paper))
            {
                if (second.ShapeName.Equals(ShapeNames.Paper)) return null;
                if (second.ShapeName.Equals(ShapeNames.Scissors)) return second.UserName;
                if (second.ShapeName.Equals(ShapeNames.Lizard)) return second.UserName;
                if (second.ShapeName.Equals(ShapeNames.Rock)) return first.UserName;
                if (second.ShapeName.Equals(ShapeNames.Spock)) return first.UserName;
            }


            if (first.ShapeName.Equals(ShapeNames.Spock))
            {
                if (second.ShapeName.Equals(ShapeNames.Spock)) return null;
                if (second.ShapeName.Equals(ShapeNames.Scissors)) return first.UserName;
                if (second.ShapeName.Equals(ShapeNames.Lizard)) return second.UserName;
                if (second.ShapeName.Equals(ShapeNames.Rock)) return first.UserName;
                if (second.ShapeName.Equals(ShapeNames.Paper)) return second.UserName;
            }


            if (first.ShapeName.Equals(ShapeNames.Rock))
            {
                if (second.ShapeName.Equals(ShapeNames.Rock)) return null;
                if (second.ShapeName.Equals(ShapeNames.Scissors)) return first.UserName;
                if (second.ShapeName.Equals(ShapeNames.Lizard)) return first.UserName;
                if (second.ShapeName.Equals(ShapeNames.Spock)) return second.UserName;
                if (second.ShapeName.Equals(ShapeNames.Paper)) return second.UserName;
            }


            throw new ArgumentException(nameof(first));
        }
    }
}