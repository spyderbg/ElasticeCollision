using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MultiValueDictionary<TKey, TValue> : Dictionary<TKey, HashSet<TValue>>
{
    public MultiValueDictionary()
        : base()
    {
    }

    public void Add(TKey key, TValue value)
    {
        HashSet<TValue> container = null;
        if(!this.TryGetValue(key, out container))
        {
            container = new HashSet<TValue>();
            base.Add(key, container);
        }
        container.Add(value);
    }

    public bool ContainsValue(TKey key, TValue value)
    {
        bool toReturn = false;
        HashSet<TValue> values = null;
        if(this.TryGetValue(key, out values))
        {
            toReturn = values.Contains(value);
        }
        return toReturn;
    }


    public void Remove(TKey key, TValue value)
    {
        HashSet<TValue> container = null;
        if(this.TryGetValue(key, out container))
        {
            container.Remove(value);
            if(container.Count <= 0)
            {
                this.Remove(key);
            }
        }
    }

    public void Merge(MultiValueDictionary<TKey, TValue> toMergeWith)
    {
        if(toMergeWith == null)
        {
            return;
        }

        foreach(KeyValuePair<TKey, HashSet<TValue>> pair in toMergeWith)
        {
            foreach(TValue value in pair.Value)
            {
                this.Add(pair.Key, value);
            }
        }
    }

    public HashSet<TValue> GetValues(TKey key, bool returnEmptySet)
    {
        HashSet<TValue> toReturn = null;
        if(!base.TryGetValue(key, out toReturn) && returnEmptySet)
        {
            toReturn = new HashSet<TValue>();
        }
        return toReturn;
    }
}
