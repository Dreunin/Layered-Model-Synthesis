using System.Collections.Generic;

/// <summary>
/// A stack that only allows unique items.
/// Based on https://stackoverflow.com/a/34323188
/// </summary>
public class UniqueStack<T> : IEnumerable<T>
{
    private HashSet<T> hashSet;
    private Stack<T> stack;
    
    public UniqueStack() {
        hashSet = new HashSet<T>();
        stack = new Stack<T>();
    }

    public UniqueStack(IEnumerable<T> collection)
    {
        hashSet = new HashSet<T>();
        stack = new Stack<T>();
        
        foreach (var item in collection)
        {
            Push(item);            
        }
    }

    public int Count => hashSet.Count;

    public void Clear() {
        hashSet.Clear();
        stack.Clear();
    }

    public bool Contains(T item) {
        return hashSet.Contains(item);
    }

    public void Push(T item) {
        if (hashSet.Add(item)) {
            stack.Push(item);
        }
    }

    public T Pop() {
        T item = stack.Pop();
        hashSet.Remove(item);
        return item;
    }

    public T Peek() {
        return stack.Peek();
    }

    public IEnumerator<T> GetEnumerator() {
        return stack.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
        return stack.GetEnumerator();
    }
}