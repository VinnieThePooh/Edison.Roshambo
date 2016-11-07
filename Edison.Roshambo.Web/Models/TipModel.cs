using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Edison.Roshambo.Web.Models
{
    public class TipModel
    {
        public int GameId { get; set; }
        public int RoundNumber { get; set; }
        public bool TipSucceeded { get; set;}
        public string ShapeOne { get; set; }
        public string ShapeTwo { get; set; }
    }
}