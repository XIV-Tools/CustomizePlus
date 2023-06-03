// © Customize+.
// Licensed under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace CustomizePlus.Helpers
{
    /// <summary>
    ///     Implements a stack with a bounded number of elements. Elements are deleted in
    ///     FIFO order as stack size exceeds capacity.
    /// </summary>
    public sealed class DropoutStack<T> : IEnumerable<T>
    {
        private readonly int capacity;
        private readonly T[] memory;
        private int bottom;
        private int head;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DropoutStack{T}" /> class.
        /// </summary>
        /// <param name="maxCapacity">The maximum capacity of the stack.</param>
        public DropoutStack(int maxCapacity)
        {
            memory = new T[maxCapacity];
            capacity = maxCapacity;
            head = 0;
            bottom = 0;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return IterateValuesLIFO().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///     Push an element to the stack. If this causes the stack's size to exceed
        ///     <see cref="capacity" />, the stack's oldest item is deleted to make room.
        /// </summary>
        public void Push(T element)
        {
            memory[head] = element;
            head = IncrementIndex(head);

            if (head == bottom)
            {
                bottom = IncrementIndex(bottom);
            }
        }

        /// <summary>
        ///     Try to pop an element from the stack and return whether the operation was
        ///     successful. Sets <paramref name="value" /> is set to null if the stack is empty.
        /// </summary>
        public bool TryPop(out T? value)
        {
            if (head == bottom)
            {
                value = default;
                return false;
            }

            head = DecrementIndex(head);
            value = memory[head];
            return true;
        }

        /// <summary>
        ///     Try to peek the top element of the stack and return whether the operation was
        ///     successful. Sets <paramref name="value" /> is set to null if the stack is empty.
        /// </summary>
        public bool TryPeek(out T? value)
        {
            if (head == bottom)
            {
                value = default;
                return false;
            }

            value = memory[head - 1];
            return true;
        }

        /// <summary>
        ///     Empty the stack of all elements.
        /// </summary>
        public void Clear()
        {
            head = 0;
            bottom = 0;
        }

        private int IncrementIndex(int index)
        {
            return (index + 1) % capacity;
        }

        private int DecrementIndex(int index)
        {
            return index == 0
                ? capacity - 1
                : index - 1;
        }

        private IEnumerable<T> IterateValuesLIFO()
        {
            for (var iter = head; iter != bottom; iter = DecrementIndex(iter))
            {
                yield return memory[iter];
            }
        }
    }
}