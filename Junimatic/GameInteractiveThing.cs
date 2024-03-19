using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace NermNermNerm.Junimatic
{
    public class GameInteractiveThing
    {
        public GameInteractiveThing(Point accessPoint)
        {
            this.AccessPoint = accessPoint;
        }

        public Point AccessPoint { get; }
    }
}
