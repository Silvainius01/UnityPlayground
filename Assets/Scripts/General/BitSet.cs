using UnityEngine;
using System.Collections.Generic;

public class BitSet {

    private const int DATA_SIZE = sizeof(uint) * 8;
    private List<uint> bits;

    public BitSet()
    {
        bits = new List<uint>(1);
    }

    public BitSet(HashSet<int> bits)
    {
        foreach(int bit in bits)
        {
            SetBit(bit);
        }
    }

    public void SetBit(int index)
    {
        int arrIndex = index / DATA_SIZE;
        int bitPos = index % DATA_SIZE;

        int num = bits.Count;
        for (int i = arrIndex; i >= num; --i)
        {
            bits.Add(new uint());
        }
        bits[arrIndex] |= 1u << bitPos;
    }

    public void ClearBit(int index)
    {
        int arrIndex = index / DATA_SIZE;
        int bitPos = index % DATA_SIZE;

        if (arrIndex >= bits.Count) return;

        bits[arrIndex] &= ~(1u << bitPos);
    }

    public bool IsSet(int index)
    {
        int arrIndex = index / DATA_SIZE;
        int bitPos = index % DATA_SIZE;

        if (arrIndex >= bits.Count) return false;

        return (bits[arrIndex] & (1 << bitPos)) != 0;
    }

    public void SetAll()
    {
        for (int i = 0; i < bits.Count; ++i)
        {
            bits[i] = 0xffffffff;
        }
    }

    public void ClearAll()
    {
        bits.Clear();
        bits.Add(new uint());
    }

    public bool IsSubsetOf(BitSet other)
    {
        if (other == null) return false;

        int count = Mathf.Min(bits.Count, other.bits.Count);
        for (int i = 0; i < count; ++i)
        {
            if ((bits[i] & other.bits[i]) != bits[i]) return false;
        }

        int extra = bits.Count - count;
        for (int i = count; i < extra; ++i)
        {
            if (bits[i] != 0) return false;
        }
        return true;
    }

    public List<int> GetListOfSetBits()
    {
        List<int> setBits = new List<int>();
        for (int i = bits.Count - 1; i >= 0; --i){
            uint n = bits[i];
            for(int b = 0; b < DATA_SIZE; b++)
            {
                if( (n & (1 << b)) != 0)
                {
                    setBits.Add(i * DATA_SIZE + b);
                }
            }
        }
        return setBits;
    }

    public override string ToString()
    {
        string str = "";
        for (int i = bits.Count - 1; i >= 0; --i)
        {
            str += GetIntBinaryString(bits[i]);
        }
        return str;
    }

    private string GetIntBinaryString(uint n)
    {
        char[] b = new char[DATA_SIZE];
        int pos = DATA_SIZE - 1;
        int i = 0;

        while (i < DATA_SIZE)
        {
            if ((n & (1 << i)) != 0)
            {
                b[pos] = '1';
            }
            else
            {
                b[pos] = '0';
            }
            pos--;
            i++;
        }
        return new string(b);
    }
}
