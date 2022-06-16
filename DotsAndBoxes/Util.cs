using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hopscotch
{
    public class Util
    {
        public static List<List<T>> Empty2DList<T>(int columns, int rows, T element)
        {
            List<List<T>> list = new();
            for (int column = 0; column < columns; column++)
            {
                list.Add(new List<T>());
                for (int row = 0; row < rows; row++) list[column].Add(element);
            }

            return list;
        }
    }
}
