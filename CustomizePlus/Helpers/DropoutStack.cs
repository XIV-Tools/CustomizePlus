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
        private readonly int _capacity;
        private readonly T[] _memory;
        private int _bottom;
        private int _head;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DropoutStack{T}" /> class.
        /// </summary>
        /// <param name="maxCapacity">The maximum capacity of the stack.</param>
        public DropoutStack(int maxCapacity)
        {
            _memory = new T[maxCapacity];
            _capacity = maxCapacity;
            _head = 0;
            _bottom = 0;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return IterateValuesLifo().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///     Push an element to the stack. If this causes the stack's size to exceed
        ///     <see cref="_capacity" />, the stack's oldest item is deleted to make room.
        /// </summary>
        public void Push(T element)
        {
            _memory[_head] = element;
            _head = IncrementIndex(_head);

            if (_head == _bottom)
            {
                _bottom = IncrementIndex(_bottom);
            }
        }

        /// <summary>
        ///     Try to pop an element from the stack and return whether the operation was
        ///     successful. Sets <paramref name="value" /> is set to null if the stack is empty.
        /// </summary>
        public bool TryPop(out T? value)
        {
            if (_head == _bottom)
            {
                value = default;
                return false;
            }

            _head = DecrementIndex(_head);
            value = _memory[_head];
            return true;
        }

        /// <summary>
        ///     Try to peek the top element of the stack and return whether the operation was
        ///     successful. Sets <paramref name="value" /> is set to null if the stack is empty.
        /// </summary>
        public bool TryPeek(out T? value)
        {
            if (_head == _bottom)
            {
                value = default;
                return false;
            }

            value = _memory[_head - 1];
            return true;
        }

        /// <summary>
        ///     Empty the stack of all elements.
        /// </summary>
        public void Clear()
        {
            _head = 0;
            _bottom = 0;
        }

        private int IncrementIndex(int index)
        {
            return (index + 1) % _capacity;
        }

        private int DecrementIndex(int index)
        {
            return index == 0
                ? _capacity - 1
                : index - 1;
        }

        private IEnumerable<T> IterateValuesLifo()
        {
            for (var iter = _head; iter != _bottom; iter = DecrementIndex(iter))
            {
                yield return _memory[iter];
            }
        }
    }
}