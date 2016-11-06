using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Edison.Roshambo.Domain.Infrastructure;

namespace Edison.Roshambo.Web.Infrastructure
{
    internal static class FakeShapeGenerator
    {
        private static readonly Random _random = new Random();

        private static readonly List<string> Shapes = new List<string>(new[]
        {
            ShapeNames.Rock,
            ShapeNames.Paper,
            ShapeNames.Scissors,
            ShapeNames.Lizard,
            ShapeNames.Spock
        });

        
        public static string GenerateFakeShape(string genuineShape)
        {
            string result = genuineShape;
            while (result.Equals(genuineShape))
              result = Shapes[_random.Next(0, Shapes.Count)];
            return result;
        }

        public static int GenerateRandomIndex()
        {
            return _random.Next(0, 2);
        }

    }
}