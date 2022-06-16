using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hopscotch
{
    public class SquareWalls
    {
        public readonly Wall Top;
        public readonly Wall Right;
        public readonly Wall Bottom;
        public readonly Wall Left;

        public SquareWalls(Wall top, Wall right, Wall bottom, Wall left)
        {
            Top = top;
            Right = right;
            Bottom = bottom;
            Left = left;
        }

        public List<Wall> GetWalls(bool set)
        {
            List<Wall> walls = new();
            if (Top.Set == set) walls.Add(Top);
            if (Right.Set == set) walls.Add(Right);
            if (Bottom.Set == set) walls.Add(Bottom);
            if (Left.Set == set) walls.Add(Left);
            return walls;
        }

        public Wall? GetFirstWall(bool set)
        {
            if (Top.Set == set) return Top;
            if (Right.Set == set) return Right;
            if (Bottom.Set == set) return Bottom;
            if (Left.Set == set) return Left;
            return null;
        }
    }
}
