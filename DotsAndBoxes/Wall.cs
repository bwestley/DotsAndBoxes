using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hopscotch
{
    public class Wall : IEquatable<Wall>
    {
        public readonly bool Set;
        public readonly bool IsColumn;
        public readonly int Column;
        public readonly int Row;

        public Wall(bool set, bool isColumn, int column, int row)
        {
            Set = set;
            IsColumn = isColumn;
            Column = column;
            Row = row;
        }

        public bool Equals(Wall? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.IsColumn == IsColumn && other.Column == Column && other.Row == Row;
        }

        public override int GetHashCode()
        {
            return IsColumn.GetHashCode() ^ Column.GetHashCode() ^ Row.GetHashCode();
        }
    }
}
