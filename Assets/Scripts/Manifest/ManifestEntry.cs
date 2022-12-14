﻿using BaseNcoding;
using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using static BSVReader;

public struct ManifestEntry 
{
    public byte[] name;
    public byte[] deps;
    public uint group;
    public uint priority;
    public ulong size;
    public ulong checksum;
    public string hname;
    public int kind;

    public enum Format
    {
        Simplified = 0,
        Full = 1
    }

    public static byte[] CalHame(ulong checksum, ulong size, byte[] name)
    {
        if (name == null) return null;
        SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();
        byte[] allbyte = new byte[name.Length + 16];
        byte[] checksumByte = BitConverter.GetBytes(checksum);
        byte[] sizeByte = BitConverter.GetBytes(size);
        for (int i = 0; i < 16; i++)
        {
            allbyte[i] = (i < 8 ? checksumByte[7 - i] : sizeByte[15 - i]);
        }
        Buffer.BlockCopy(name, 0, allbyte, 16, name.Length);
        return sha1.ComputeHash(allbyte);
    }

    public static string CalHameString(ulong checksum, ulong size, byte[] name)
    {
        var bytes = CalHame(checksum, size, name);
        Base32 base32 = new Base32();
        return base32.Encode(bytes);
    }

    public static string CalHameString(ulong checksum, ulong size, string name)
    {
        return CalHameString(checksum, size, Encoding.UTF8.GetBytes(name));
    }

    public override string ToString()
    {
        StringBuilder s = new StringBuilder();
        s.AppendFormat("Name : {0} ", Encoding.UTF8.GetString(name));
        s.AppendFormat("Deps : {0} \n", deps == null ? "NULL" : Encoding.UTF8.GetString(deps));
        s.AppendFormat("Size : {0} ", size);
        s.AppendFormat("Checksum : {0} ", (long)checksum);
        s.AppendFormat("Group : {0} ", group);
        s.AppendFormat("Priority : {0} ", priority);
        s.AppendFormat("Kind : {0} ", kind);
        s.AppendFormat("Hash : {0} ", CalHameString(checksum, size, name));
        return s.ToString();
    }

    public static ILineParser<ManifestEntry> GetBsvParser(Format format, IBSVReader bsvReader) 
    {
        if (format == Format.Simplified)
        {
            return new SimplifiedEntryLineReader();
        }
        else 
        {
            if (bsvReader is AnonymousSchemaBSVReader)
            {
                return new FullEntryLineReader();
            }
            else if (bsvReader is AprioriBSVReader)
            {
                return new LegacyFullEntryLineReader();
            }
            return null;
        }
    }

    public class SimplifiedEntryLineReader : ILineParser<ManifestEntry> 
    {
        public void Parse(byte[] buf, ref int offset, ref ManifestEntry dat) 
        {
            dat.name = GetArray(ReadText(buf, ref offset));
            dat.size = ReadVLQ(buf, ref offset);
            dat.checksum = ReadUNum(buf, ref offset, 8);
        }

    }

    public class FullEntryLineReader : ILineParser<ManifestEntry> 
    {

        public void Parse(byte[] buf, ref int offset, ref ManifestEntry dat) 
        {
            dat.name = GetArray(ReadText(buf, ref offset));
            dat.deps = GetArray(ReadText(buf, ref offset));
            dat.group = (uint)ReadVLQ(buf, ref offset, 4);
            dat.priority = (uint)ReadVLQ(buf, ref offset, 4);
            dat.size = (uint)ReadVLQ(buf, ref offset, 8);
            dat.checksum = ReadUNum(buf, ref offset, 8);
        }

    }

    public class LegacyFullEntryLineReader : ILineParser<ManifestEntry> 
    {
        public void Parse(byte[] buf, ref int offset, ref ManifestEntry dat) 
        {
            dat.name = GetArray(ReadText(buf, ref offset));
            dat.deps = GetArray(ReadText(buf, ref offset));
            dat.group = (uint)ReadVLQ(buf, ref offset, 4);
            dat.size = (uint)ReadVLQ(buf, ref offset, 8);
            dat.checksum = ReadUNum(buf, ref offset, 8);
            dat.priority = 0;
        }
    }
}
