using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solver
{
    class PriorityQueue<T>
    {
        public PriorityQueue(Func<T, T, bool> lessFn)
        {
            this.lessFn = lessFn;
            this.items = new List<T>();
        }

        public bool IsEmpty()
        {
            return !this.items.Any();
        }

        public void Push(T obj)
        {
            items.Add(obj);
            var i = items.Count - 1;

            while (lessFn(items[i / 2], items[i]))
            {
                var j = i / 2;
                swap(i, j);
                i = j;
            }
        }

        public T Pop()
        {
            var i = 0;
            swap(i, items.Count - 1);

            T obj = items.Last();
            items.RemoveAt(items.Count - 1);

            while (true)
            {
                var largest = i;

                if (i * 2 < items.Count && lessFn(items[largest], items[i * 2]))
                {
                    largest = i * 2;
                }

                if (i * 2 + 1 < items.Count && lessFn(items[largest], items[i * 2 + 1]))
                {
                    largest = i * 2 + 1;
                }

                if (i == largest)
                {
                    return obj;
                }

                swap(i, largest);

                i = largest;
            }
        }

        private void swap(int i, int j)
        {
            T t = items[i];
            items[i] = items[j];
            items[j] = t;
        }

        private Func<T, T, bool> lessFn;
        private List<T> items;
    }
}
