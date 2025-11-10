using Microsoft.Xna.Framework;

namespace NermNermNerm.Junimatic
{
    public abstract class GameInteractiveThing
    {
        protected GameInteractiveThing(object gameObject, Point accessPoint)
        {
            this.AccessPoint = accessPoint;
            this.GameObject = gameObject;
        }

        public Point AccessPoint { get; }

        /// <summary>
        ///   This points to the object within the game code.  It exists because we need to be
        ///   able to tell if two instances of this class refer to the same thing.
        /// </summary>
        public object GameObject { get; }
    }
}
