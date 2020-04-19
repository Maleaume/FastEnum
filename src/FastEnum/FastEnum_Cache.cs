﻿using System;
using System.Linq;
using FastEnumUtility.Internals;



namespace FastEnumUtility
{
    /// <summary>
    /// Provides high performance utilitis for enum type.
    /// </summary>
    public static partial class FastEnum
    {
        private static class Cache_Type<T>
            where T : struct, Enum
        {
            public static readonly Type Type;
            public static readonly Type UnderlyingType;

            static Cache_Type()
            {
                Type = typeof(T);
                UnderlyingType = Enum.GetUnderlyingType(Type);
            }
        }



        private static class Cache_Values<T>
            where T : struct, Enum
        {
            public static readonly ReadOnlyArray<T> Values;
            public static readonly bool IsEmpty;

            static Cache_Values()
            {
                var type = Cache_Type<T>.Type;
                var values = Enum.GetValues(type) as T[];
                Values = values.AsReadOnly();
                IsEmpty = values.Length == 0;
            }
        }



        private static class Cache_Names<T>
            where T : struct, Enum
        {
            public static readonly ReadOnlyArray<string> Names;

            static Cache_Names()
            {
                var type = Cache_Type<T>.Type;
                Names = Enum.GetNames(type).ToReadOnlyArray();
            }
        }



        private static class Cache_Members<T>
            where T : struct, Enum
        {
            public static readonly ReadOnlyArray<Member<T>> Members;

            static Cache_Members()
            {
                Members
                    = Cache_Names<T>.Names
                    .Select(x => new Member<T>(x))
                    .ToReadOnlyArray();
            }
        }



        private static class Cache_MinMaxValues<T>
            where T : struct, Enum
        {
            public static readonly T MinValue;
            public static readonly T MaxValue;

            static Cache_MinMaxValues()
            {
                var values = Cache_Values<T>.Values;
                MinValue = values.DefaultIfEmpty().Min();
                MaxValue = values.DefaultIfEmpty().Max();
            }
        }



        private static class Cache_IsFlags<T>
            where T : struct, Enum
        {
            public static readonly bool IsFlags;

            static Cache_IsFlags()
            {
                var type = Cache_Type<T>.Type;
                IsFlags = Attribute.IsDefined(type, typeof(FlagsAttribute));
            }
        }



        private static class Cache_MembersByName<T>
            where T : struct, Enum
        {
            public static readonly FrozenDictionary<string,Member<T>> MemberByName;

            static Cache_MembersByName()
                => MemberByName = Cache_Members<T>.Members.ToFrozenDictionary(x => x.Name);
        }



        private static class Cache_UnderlyingOperation<T>
            where T : struct, Enum
        {
            public static readonly Type UnderlyingType;
            public static readonly IUnderlyingOperation<T> UnderlyingOperation;

            static Cache_UnderlyingOperation()
            {
                var type = Cache_Type<T>.Type;
                var min = Cache_MinMaxValues<T>.MinValue;
                var max = Cache_MinMaxValues<T>.MaxValue;
                var distincted = Cache_Members<T>.Members.OrderBy(x => x.Value).Distinct(new Member<T>.ValueComparer()).ToArray();
                UnderlyingType = Cache_Type<T>.UnderlyingType;
                UnderlyingOperation
                    = Type.GetTypeCode(type) switch
                    {
                        TypeCode.SByte => new SByteOperation<T>().Create(min, max, distincted),
                        TypeCode.Byte => new ByteOperation<T>().Create(min, max, distincted),
                        TypeCode.Int16 => new Int16Operation<T>().Create(min, max, distincted),
                        TypeCode.UInt16 => new UInt16Operation<T>().Create(min, max, distincted),
                        TypeCode.Int32 => new Int32Operation<T>().Create(min, max, distincted),
                        TypeCode.UInt32 => new UInt32Operation<T>().Create(min, max, distincted),
                        TypeCode.Int64 => new Int64Operation<T>().Create(min, max, distincted),
                        TypeCode.UInt64 => new UInt64Operation<T>().Create(min, max, distincted),
                        _ => throw new InvalidOperationException(),
                    };
            }
        }
    }
}
