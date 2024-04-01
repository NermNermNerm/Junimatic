using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace NermNermNerm.Junimatic
{
    public abstract class GameInteractiveThing
    {
        protected GameInteractiveThing(Point accessPoint)
        {
            this.AccessPoint = accessPoint;
        }

        public Point AccessPoint { get; }
    }
}
