using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FastEnumUtility.Internals
{
    abstract class UnderlyingOperation<T, S> : IUnderlyingOperation<T>
           where T : struct, Enum
        where S : struct, IComparable, IComparable<S>, IConvertible, IEquatable<S>, IFormattable

    {
        public abstract bool IsContinuous { get; }
        public abstract bool IsDefined(ref T value);
        public abstract bool IsDefined(ref S value);
        public abstract Member<T> GetMember(ref T value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryParse(string text, out T result)
        {
            result = default;
            ref var x = ref Unsafe.As<T, S>(ref result);
            return TryParse(text, out x);
        }

        public abstract bool TryParse(string text, out S x);
    }


    abstract class Continuous<T, S> : UnderlyingOperation<T, S>
      where T : struct, Enum
     where S : struct, IComparable, IComparable<S>, IConvertible, IEquatable<S>, IFormattable
    {
        protected readonly S minValue;
        protected readonly S maxValue;
        protected readonly Member<T>[] members;

        public Continuous(S min, S max, Member<T>[] members)
        {
            this.minValue = min;
            this.maxValue = max;
            this.members = members;
        }


        public override bool IsContinuous => true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool IsDefined(ref T value)
        {
            ref var val = ref Unsafe.As<T, S>(ref value);
            return (this.minValue.CompareTo(val) <= 0) && (val.CompareTo(this.maxValue) <= 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool IsDefined(ref S value)
            => (this.minValue.CompareTo(value) <= 0) && (value.CompareTo(this.maxValue) <= 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Member<T> GetMember(ref T value)
        {
            ref var val = ref Unsafe.As<T, S>(ref value);
            var index = ComputeIndex(val);  
            return members[index];
        }
        [MethodImpl(MethodImplOptions.ForwardRef)]
        public abstract ulong ComputeIndex(S val);
    }


    abstract class Discontinuous<T, S> : UnderlyingOperation<T, S>
     where T : struct, Enum
    where S : struct, IComparable, IComparable<S>, IConvertible, IEquatable<S>, IFormattable
    {
        private readonly FrozenDictionary<S, Member<T>> memberByValue;

        public Discontinuous(FrozenDictionary<S, Member<T>> memberByValue)
            => this.memberByValue = memberByValue;

        public override bool IsContinuous => false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool IsDefined(ref T value)
        {
            ref var val = ref Unsafe.As<T, S>(ref value);
            return this.memberByValue.ContainsKey(val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool IsDefined(ref S value)
            => this.memberByValue.ContainsKey(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Member<T> GetMember(ref T value)
        {
            ref var val = ref Unsafe.As<T, S>(ref value);
            return this.memberByValue[val];
        }
    }

    internal abstract class Operation<T, S>
       where T : struct, Enum
         where S : struct, IComparable, IComparable<S>, IConvertible, IEquatable<S>, IFormattable
    {

        static readonly Dictionary<Type, UnderlyingOperation<T, S>> operations = new Dictionary<Type, UnderlyingOperation<T, S>>();
        static readonly Type typeofT = typeof(T); 

        #region Fields
        private UnderlyingOperation<T, S> operation;
        #endregion


        #region Create
        public IUnderlyingOperation<T> Create(T min, T max, Member<T>[] members)
        { 
            
            var memberByValue
                = members.ToFrozenDictionary(x =>
                {
                    var value = x.Value;
                    return Unsafe.As<T, S>(ref value);
                });
            if (memberByValue.Count > 0)
            {
                var minValue = Unsafe.As<T, S>(ref min);
                var maxValue = Unsafe.As<T, S>(ref max);

                var length = GetLength(minValue, maxValue);
                var count = (ulong)(memberByValue.Count - 1);
                if (length == count)
                { 
                    var opC = operations[typeofT] = InstanciateContinuous(minValue, maxValue, members); 
                    return opC;
                }
            } 
            var opD = operations[typeofT] = InstanciateDiscontinuous(memberByValue);
            return opD;
        }
        #endregion

        private static async Task<int> GetTypeHashAsync()
            => await Task.Run(() => typeof(T).GetHashCode());

        private static int GetTypeHash()  => typeof(T).GetHashCode();

        #region IsDefined
    
        public static bool IsDefined(ref S value)
            => operations[typeofT].IsDefined(ref value);
        #endregion

        [MethodImpl(MethodImplOptions.ForwardRef)]
        protected abstract UnderlyingOperation<T, S> InstanciateContinuous(S minValue, S maxValue, Member<T>[] members);
        [MethodImpl(MethodImplOptions.ForwardRef)]
        protected abstract UnderlyingOperation<T, S> InstanciateDiscontinuous(FrozenDictionary<S, Member<T>> memberByValue);
        [MethodImpl(MethodImplOptions.ForwardRef)]
        protected abstract ulong GetLength(S minValue, S maxValue);
    } 
}
