using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;

namespace HappyKleene
{
    static class SerializationCompiler
    {
        private const MethodAttributes emitAttrs = MethodAttributes.Static | MethodAttributes.Private;

        public static void SerializeArray<T>(IEnumerable<T> i, StringBuilder result)
        {
            var a = i.ToArray();
            result.Append(a.Length);
            foreach(var e in a)
            {
                result.Append(' ');
                result.Append(e);
            }
        }

        public static void SerializeIntAA<T>(IEnumerable<T> i, StringBuilder result)
            where T : IEnumerable<int>
        {
            var a = i.ToArray();
            result.Append(a.Length);
            foreach(var e in a)
            {
                result.Append(' ');
                SerializeArray(e, result);
            }
        }

        public static void SerializeDict<TKey, TValue>(
            IEnumerable<KeyValuePair<TKey, TValue>> i, 
            StringBuilder result, 
            Action<TKey, StringBuilder> serializeKey,
            Action<TValue, StringBuilder> serializeValue)
        {
            var a = i.ToArray();
            result.Append(a.Length);
            foreach(var e in a)
            {
                result.Append(' ');
                serializeKey(e.Key, result);
                result.Append(' ');
                serializeValue(e.Value, result);
            }
        }

        private static readonly Type i = typeof(int);
        private static readonly Type ia = typeof(int[]);
        private static readonly Type iaa = typeof(int[][]);
        private static readonly Type set = typeof(SortedSet<int>);
        private static readonly Type se = typeof(IEnumerator<string>);
        private static readonly Type str = typeof(string);
        private static readonly Type str_a = typeof(string[]);

        public static readonly MethodInfo m_GetEnum = typeof(IEnumerable<string>).GetMethod("GetEnumerator");
        private static readonly MethodInfo m_MoveNext = typeof(System.Collections.IEnumerator).GetMethod("MoveNext");
        private static readonly MethodInfo m_Current = se.GetMethod("get_Current");
        private static readonly MethodInfo m_toint = i.GetMethod("Parse", new Type[] { str});
        private static readonly MethodInfo m_SetAdd = set.GetMethod("Add");
        private static readonly ConstructorInfo ctor_set = set.GetConstructor(Type.EmptyTypes);
        private static readonly MethodInfo m_Split = str.GetMethod("Split", new Type[] { typeof(char[]) });

        public static void EmitSerializationLoading(ILGenerator il, string s)
        {
            il.Emit(OpCodes.Ldstr, s); // s
            il.Emit(OpCodes.Ldc_I4_1); // s 1
            il.Emit(OpCodes.Newarr, typeof(char)); // s {0}
            il.Emit(OpCodes.Dup); // s {} {}
            il.Emit(OpCodes.Ldc_I4_0); // s {} {} 0
            il.Emit(OpCodes.Ldc_I4, (int)' '); // s {} {} 0 ' '
            il.Emit(OpCodes.Stelem, typeof(char)); // s {' '}
            il.Emit(OpCodes.Call, m_Split);
        }

        public static MethodBuilder EmitArrayDeserialization<T>(TypeBuilder t)
        {
            var TE = typeof(T);
            var m = t.DefineMethod("emit_a_" + TE.Name, emitAttrs, TE.MakeArrayType(), new Type[] { se });
            var il = m.GetILGenerator();

            il.DeclareLocal(i); // loc.0 : int (len)
            il.DeclareLocal(i); // loc.1 : int (counter)

            il.Emit(OpCodes.Ldarg_0); // se
            il.Emit(OpCodes.Callvirt, m_MoveNext); // bool
            il.Emit(OpCodes.Pop); // -
            il.Emit(OpCodes.Ldarg_0); // se
            il.Emit(OpCodes.Callvirt, m_Current); // len_s
            il.Emit(OpCodes.Call, m_toint); // len
            il.Emit(OpCodes.Dup); // len len
            il.Emit(OpCodes.Stloc_0); // len {loc.0 := len}
            il.Emit(OpCodes.Newarr, TE); // []

            var loopStart = il.DefineLabel();
            var loopEnd = il.DefineLabel();

            il.Emit(OpCodes.Ldc_I4_0); // [] 0
            il.Emit(OpCodes.Stloc_1); // [] {loc.1 := 0}
            
            // loop check
            il.MarkLabel(loopStart);
            il.Emit(OpCodes.Ldloc_1); // [] counter
            il.Emit(OpCodes.Ldloc_0); // [] counter len
            il.Emit(OpCodes.Bge, loopEnd); // []

            // loop body
            il.Emit(OpCodes.Dup); // [] []
            il.Emit(OpCodes.Ldloc_1); // [] [] counter
            il.Emit(OpCodes.Ldarg_0); // [] [] counter se
            il.Emit(OpCodes.Callvirt, m_MoveNext); // [] [] counter bool
            il.Emit(OpCodes.Pop); // [] [] counter
            il.Emit(OpCodes.Ldarg_0); // [] [] counter se
            il.Emit(OpCodes.Callvirt, m_Current); // [] [] counter number_s
            il.Emit(OpCodes.Call, m_toint); // [] [] counter number
            il.Emit(OpCodes.Stelem, TE); // [] {[counter] := number}

            // loop end
            il.Emit(OpCodes.Ldloc_1); // [] counter
            il.Emit(OpCodes.Ldc_I4_1); // [] counter 1
            il.Emit(OpCodes.Add); // [] new_counter
            il.Emit(OpCodes.Stloc_1); // []
            il.Emit(OpCodes.Br, loopStart);
            il.MarkLabel(loopEnd);

            // return
            il.Emit(OpCodes.Ret);
            return m;
        }

        public static MethodBuilder EmitStringArrayDeserialization(TypeBuilder t)
        {
            var m = t.DefineMethod("emit_sa", emitAttrs, str_a, new Type[] { se });
            var il = m.GetILGenerator();

            il.DeclareLocal(i); // loc.0 : int (len)
            il.DeclareLocal(i); // loc.1 : int (counter)

            il.Emit(OpCodes.Ldarg_0); // se
            il.Emit(OpCodes.Callvirt, m_MoveNext); // bool
            il.Emit(OpCodes.Pop); // -
            il.Emit(OpCodes.Ldarg_0); // se
            il.Emit(OpCodes.Callvirt, m_Current); // len_s
            il.Emit(OpCodes.Call, m_toint); // len
            il.Emit(OpCodes.Dup); // len len
            il.Emit(OpCodes.Stloc_0); // len {loc.0 := len}
            il.Emit(OpCodes.Newarr, str); // []

            var loopStart = il.DefineLabel();
            var loopEnd = il.DefineLabel();

            il.Emit(OpCodes.Ldc_I4_0); // [] 0
            il.Emit(OpCodes.Stloc_1); // [] {loc.1 := 0}
            
            // loop check
            il.MarkLabel(loopStart);
            il.Emit(OpCodes.Ldloc_1); // [] counter
            il.Emit(OpCodes.Ldloc_0); // [] counter len
            il.Emit(OpCodes.Bge, loopEnd); // []

            // loop body
            il.Emit(OpCodes.Dup); // [] []
            il.Emit(OpCodes.Ldloc_1); // [] [] counter
            il.Emit(OpCodes.Ldarg_0); // [] [] counter se
            il.Emit(OpCodes.Callvirt, m_MoveNext); // [] [] counter bool
            il.Emit(OpCodes.Pop); // [] [] counter
            il.Emit(OpCodes.Ldarg_0); // [] [] counter se
            il.Emit(OpCodes.Callvirt, m_Current); // [] [] counter str
            il.Emit(OpCodes.Stelem, str); // [] {[counter] := str}

            // loop end
            il.Emit(OpCodes.Ldloc_1); // [] counter
            il.Emit(OpCodes.Ldc_I4_1); // [] counter 1
            il.Emit(OpCodes.Add); // [] new_counter
            il.Emit(OpCodes.Stloc_1); // []
            il.Emit(OpCodes.Br, loopStart);
            il.MarkLabel(loopEnd);

            // return
            il.Emit(OpCodes.Ret);
            return m;
        }

        public static MethodBuilder EmitIntAADeserialization(TypeBuilder t, MethodInfo m_de_ia)
        {
            var m = t.DefineMethod("emit_iaa", emitAttrs, iaa, new Type[] { se });
            var il = m.GetILGenerator();

            il.DeclareLocal(i); // loc.0 : int (len)
            il.DeclareLocal(i); // loc.1 : int (counter)

            il.Emit(OpCodes.Ldarg_0); // se
            il.Emit(OpCodes.Callvirt, m_MoveNext); // bool
            il.Emit(OpCodes.Pop); // -
            il.Emit(OpCodes.Ldarg_0); // se
            il.Emit(OpCodes.Callvirt, m_Current); // len_s
            il.Emit(OpCodes.Call, m_toint); // len
            il.Emit(OpCodes.Dup); // len len
            il.Emit(OpCodes.Stloc_0); // len {loc.0 := len}
            il.Emit(OpCodes.Newarr, ia); // []

            var loopStart = il.DefineLabel();
            var loopEnd = il.DefineLabel();

            il.Emit(OpCodes.Ldc_I4_0); // [] 0
            il.Emit(OpCodes.Stloc_1); // [] {loc.1 := 0}
            
            // loop check
            il.MarkLabel(loopStart);
            il.Emit(OpCodes.Ldloc_1); // [] counter
            il.Emit(OpCodes.Ldloc_0); // [] counter len
            il.Emit(OpCodes.Bge, loopEnd); // []

            // loop body
            il.Emit(OpCodes.Dup); // [] []
            il.Emit(OpCodes.Ldloc_1); // [] [] counter
            il.Emit(OpCodes.Ldarg_0); // [] [] counter se
            il.Emit(OpCodes.Call, m_de_ia); // [] [] counter ia
            il.Emit(OpCodes.Stelem, ia); // [] {[counter] := ia}

            // loop end
            il.Emit(OpCodes.Ldloc_1); // [] counter
            il.Emit(OpCodes.Ldc_I4_1); // [] counter 1
            il.Emit(OpCodes.Add); // [] new_counter
            il.Emit(OpCodes.Stloc_1); // []
            il.Emit(OpCodes.Br, loopStart);
            il.MarkLabel(loopEnd);

            // return
            il.Emit(OpCodes.Ret);
            return m;
        }

        public static MethodBuilder EmitDictDeserialization<TKey, TValue>(TypeBuilder t, MethodInfo m_de_val, Action<ILGenerator> readKeyToken)
        {
            var tk = typeof(TKey);
            var tv = typeof(TValue);
            var tdict = typeof(SortedDictionary<TKey, TValue>);
            var m = t.DefineMethod("emit_dict_" + tk.Name + "_" + tv.Name, emitAttrs, tdict, new Type[] { se });
            var il = m.GetILGenerator();

            il.DeclareLocal(i); // loc.0 : int (len)
            il.DeclareLocal(i); // loc.1 : int (counter)

            il.Emit(OpCodes.Ldarg_0); // se
            il.Emit(OpCodes.Callvirt, m_MoveNext); // bool
            il.Emit(OpCodes.Pop); // -
            il.Emit(OpCodes.Ldarg_0); // se
            il.Emit(OpCodes.Callvirt, m_Current); // len_s
            il.Emit(OpCodes.Call, m_toint); // len
            il.Emit(OpCodes.Stloc_0); // - {loc.0 := len}

            var ctor_dict = tdict.GetConstructor(Type.EmptyTypes);
            il.Emit(OpCodes.Newobj, ctor_dict); // dict

            var loopStart = il.DefineLabel();
            var loopEnd = il.DefineLabel();

            il.Emit(OpCodes.Ldc_I4_0); // dict 0
            il.Emit(OpCodes.Stloc_1); // dict {loc.1 := 0}
            
            // loop check
            il.MarkLabel(loopStart);
            il.Emit(OpCodes.Ldloc_1); // dict counter
            il.Emit(OpCodes.Ldloc_0); // dict counter len
            il.Emit(OpCodes.Bge, loopEnd); // dict

            // loop body
            il.Emit(OpCodes.Dup); // dict dict
            il.Emit(OpCodes.Ldarg_0); // dict dict se
            il.Emit(OpCodes.Callvirt, m_MoveNext); // dict dict bool
            il.Emit(OpCodes.Pop); // dict dict
            il.Emit(OpCodes.Ldarg_0); // dict dict se
            il.Emit(OpCodes.Callvirt, m_Current); // dict dict char_s
            readKeyToken(il); // dict dict char
            il.Emit(OpCodes.Ldarg_0); // dict dict char se
            il.Emit(OpCodes.Call, m_de_val); // dict dict char iaa

            var m_add = tdict.GetMethod("Add");
            il.Emit(OpCodes.Callvirt, m_add); // dict

            // loop end
            il.Emit(OpCodes.Ldloc_1); // dict counter
            il.Emit(OpCodes.Ldc_I4_1); // dict counter 1
            il.Emit(OpCodes.Add); // dict new_counter
            il.Emit(OpCodes.Stloc_1); // dict
            il.Emit(OpCodes.Br, loopStart);
            il.MarkLabel(loopEnd);

            // return
            il.Emit(OpCodes.Ret);
            return m;
        }

        [Obsolete("Do not use it")]
        public static MethodBuilder EmitDictDeserialization<TKey>(TypeBuilder t, Action<ILGenerator> readKeyToken, Action<ILGenerator> readValueToken)
        {
            var tk = typeof(TKey);
            var tdict = typeof(SortedDictionary<TKey, int[][]>);
            var m = t.DefineMethod("emit_dict?", emitAttrs, tdict, new Type[] { se });
            var il = m.GetILGenerator();

            il.DeclareLocal(i); // loc.0 : int (len)
            il.DeclareLocal(i); // loc.1 : int (counter)

            il.Emit(OpCodes.Ldarg_0); // se
            il.Emit(OpCodes.Callvirt, m_MoveNext); // bool
            il.Emit(OpCodes.Pop); // -
            il.Emit(OpCodes.Ldarg_0); // se
            il.Emit(OpCodes.Callvirt, m_Current); // len_s
            il.Emit(OpCodes.Call, m_toint); // len
            il.Emit(OpCodes.Stloc_0); // - {loc.0 := len}

            var ctor_dict = tdict.GetConstructor(Type.EmptyTypes);
            il.Emit(OpCodes.Newobj, ctor_dict); // dict

            var loopStart = il.DefineLabel();
            var loopEnd = il.DefineLabel();

            il.Emit(OpCodes.Ldc_I4_0); // dict 0
            il.Emit(OpCodes.Stloc_1); // dict {loc.1 := 0}
            
            // loop check
            il.MarkLabel(loopStart);
            il.Emit(OpCodes.Ldloc_1); // dict counter
            il.Emit(OpCodes.Ldloc_0); // dict counter len
            il.Emit(OpCodes.Bge, loopEnd); // dict

            // loop body
            il.Emit(OpCodes.Dup); // dict dict
            il.Emit(OpCodes.Ldarg_0); // dict dict se
            il.Emit(OpCodes.Callvirt, m_MoveNext); // dict dict bool
            il.Emit(OpCodes.Pop); // dict dict
            il.Emit(OpCodes.Ldarg_0); // dict dict se
            il.Emit(OpCodes.Callvirt, m_Current); // dict dict char_s
            readKeyToken(il); // dict dict char
            il.Emit(OpCodes.Ldarg_0); // dict dict char se
            il.Emit(OpCodes.Callvirt, m_MoveNext); // dict dict char bool
            il.Emit(OpCodes.Pop); // dict dict char
            il.Emit(OpCodes.Ldarg_0); // dict dict char se
            il.Emit(OpCodes.Callvirt, m_Current); // dict dict char val_s
            readValueToken(il); // dict dict char val

            var m_add = tdict.GetMethod("Add");
            il.Emit(OpCodes.Callvirt, m_add); // dict

            // loop end
            il.Emit(OpCodes.Ldloc_1); // dict counter
            il.Emit(OpCodes.Ldc_I4_1); // dict counter 1
            il.Emit(OpCodes.Add); // dict new_counter
            il.Emit(OpCodes.Stloc_1); // dict
            il.Emit(OpCodes.Br, loopStart);
            il.MarkLabel(loopEnd);

            // return
            il.Emit(OpCodes.Ret);
            return m;
        }

        public static MethodBuilder EmitSetDeserialization(TypeBuilder t)
        {
            var m = t.DefineMethod("emit_set", emitAttrs, set, new Type[] { se });
            var il = m.GetILGenerator();

            il.DeclareLocal(i); // loc.0 : int (len)
            il.DeclareLocal(i); // loc.1 : int (counter)

            il.Emit(OpCodes.Ldarg_0); // se
            il.Emit(OpCodes.Callvirt, m_MoveNext); // bool
            il.Emit(OpCodes.Pop); // -
            il.Emit(OpCodes.Ldarg_0); // se
            il.Emit(OpCodes.Callvirt, m_Current); // len_s
            il.Emit(OpCodes.Call, m_toint); // len
            il.Emit(OpCodes.Stloc_0); // - {loc.0 := len}
            il.Emit(OpCodes.Newobj, ctor_set); // set

            var loopStart = il.DefineLabel();
            var loopEnd = il.DefineLabel();

            il.Emit(OpCodes.Ldc_I4_0); // set 0
            il.Emit(OpCodes.Stloc_1); // set {loc.1 := 0}

            // loop check
            il.MarkLabel(loopStart);
            il.Emit(OpCodes.Ldloc_1); // set counter
            il.Emit(OpCodes.Ldloc_0); // set counter len
            il.Emit(OpCodes.Bge, loopEnd); // set

            // loop body
            il.Emit(OpCodes.Dup); // set set
            il.Emit(OpCodes.Ldarg_0); // set set se
            il.Emit(OpCodes.Callvirt, m_MoveNext); // set set bool
            il.Emit(OpCodes.Pop); // set set
            il.Emit(OpCodes.Ldarg_0); // set set se
            il.Emit(OpCodes.Callvirt, m_Current); // set set number_s
            il.Emit(OpCodes.Call, m_toint); // set set number
            il.Emit(OpCodes.Callvirt, m_SetAdd); // set bool
            il.Emit(OpCodes.Pop); // set

            // loop end
            il.Emit(OpCodes.Ldloc_1); // set counter
            il.Emit(OpCodes.Ldc_I4_1); // set counter 1
            il.Emit(OpCodes.Add); // set new_counter
            il.Emit(OpCodes.Stloc_1); // set
            il.Emit(OpCodes.Br, loopStart);
            il.MarkLabel(loopEnd);

            // return
            il.Emit(OpCodes.Ret);
            return m;
        }
    }
}
