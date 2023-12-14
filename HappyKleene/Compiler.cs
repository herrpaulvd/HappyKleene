using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.IO;

namespace HappyKleene
{
    // главный класс компиляции
    // в этом классе собран в т.ч. весь IL-код проекта
    // почти каждый класс в IL-представлении оформлен в виде отдельного метода
    // логика в IL-коде почти идентична логике в классах, относящихся к интерпретатору
    // почти каждая IL-инструкция сопровождается диаграммой стека (вершина справа)
    class Compiler
    {
        // отдельные константы
        // некоторые атрибуты
        private static readonly TypeAttributes attr_class = TypeAttributes.Class;
        private static readonly TypeAttributes attr_internal = TypeAttributes.NotPublic;
        private static readonly TypeAttributes attr_public = TypeAttributes.Public;
        private static readonly FieldAttributes attr_fld_private = FieldAttributes.Private;
        private static readonly FieldAttributes attr_fld_public = FieldAttributes.Public;
        private static readonly FieldAttributes attr_fld_readonly = FieldAttributes.InitOnly;
        private static readonly MethodAttributes attr_method_private = MethodAttributes.Private;
        private static readonly MethodAttributes attr_method_public = MethodAttributes.Public;
        private static readonly MethodAttributes attr_method_internal = MethodAttributes.Assembly;
        private static readonly MethodAttributes attr_method_virtual = MethodAttributes.Virtual;
        private static readonly CallingConventions convention_class = CallingConventions.Standard;
        // типы CLR
        private static readonly Type t_set_int = typeof(SortedSet<int>);
        private static readonly Type t_fsm_transition = typeof(SortedDictionary<char, int[][]>);
        private static readonly Type t_List_int = typeof(List<int>);
        private static readonly Type t_int = typeof(int);
        private static readonly Type t_array_int = typeof(int[]);
        private static readonly Type t_object = typeof(object);
        private static readonly Type t_void = typeof(void);
        private static readonly Type t_char = typeof(char);
        private static readonly Type t_bool = typeof(bool);
        private static readonly Type t_IEnumerable_int = typeof(IEnumerable<int>);
        private static readonly Type t_Func_int_bool = typeof(Func<int, bool>);
        private static readonly Type t_native_int = typeof(IntPtr);
        private static readonly Type t_array_bool = typeof(bool[]);
        private static readonly Type t_array_array_int = typeof(int[][]);
        private static readonly Type t_sortedRuleIds = typeof(SortedDictionary<int, int[]>);
        private static readonly Type t_array_string = typeof(string[]);
        private static readonly Type t_situation = typeof(ValueTuple<int, int, int>);
        private static readonly Type t_historyRecord = typeof(ValueTuple<int, int, int, int, int>);
        private static readonly Type t_array_List_situation = typeof(List<(int, int, int)>[]);
        private static readonly Type t_array_List_historyRecord = typeof(List<(int, int, int, int, int)>[]);
        private static readonly Type t_endSituations = typeof(SortedDictionary<(int, int), int>[]);
        private static readonly Type t_middleSituations = typeof(SortedDictionary<int, List<(int, int, int)>>[]);
        private static readonly Type t_array_set_int = typeof(SortedSet<int>[]);
        private static readonly Type t_tuple_int_int = typeof(ValueTuple<int, int>);
        private static readonly Type t_List_situation = typeof(List<(int, int, int)>);
        private static readonly Type t_List_historyRecord = typeof(List<(int, int, int, int, int)>);
        private static readonly Type t_endSituations_item = typeof(SortedDictionary<(int, int), int>);
        private static readonly Type t_middleSituations_item = typeof(SortedDictionary<int, List<(int, int, int)>>);
        private static readonly Type t_string = typeof(string);
        private static readonly Type t_StringBuilder = typeof(StringBuilder);
        private static readonly Type t_Queue_int = typeof(Queue<int>);
        private static readonly Type t_Stack_string = typeof(Stack<string>);
        private static readonly Type t_Exception = typeof(Exception);
        private static readonly Type t_set_situation = typeof(SortedSet<(int, int, int)>);
        private static readonly Type t_array_set_situation = t_set_situation.MakeArrayType();
        // поля CLR
        private static readonly FieldInfo fld_situation_ruleId = t_situation.GetField("Item1");
        private static readonly FieldInfo fld_situation_pointLocation = t_situation.GetField("Item2");
        private static readonly FieldInfo fld_situation_second = t_situation.GetField("Item3");
        private static readonly FieldInfo fld_historyRecord_Item4 = t_historyRecord.GetField("Item4");
        private static readonly FieldInfo fld_historyRecord_Item5 = t_historyRecord.GetField("Item5");
        private static readonly FieldInfo fld_tuple_int_int_Item1 = t_tuple_int_int.GetField("Item1");
        private static readonly FieldInfo fld_tuple_int_int_Item2 = t_tuple_int_int.GetField("Item2");
        // методы/конструкторы CLR
        private static readonly ConstructorInfo ctor_object = t_object.GetConstructor(Type.EmptyTypes);
        private static readonly ConstructorInfo ctor_set_int = t_set_int.GetConstructor(Type.EmptyTypes);
        private static readonly MethodInfo method_set_int_Add = t_set_int.GetMethod("Add", new Type[] { t_int });
        private static readonly ConstructorInfo ctor_List_int = t_List_int.GetConstructor(Type.EmptyTypes);
        private static readonly MethodInfo method_set_int_Clear = t_set_int.GetMethod("Clear", Type.EmptyTypes);
        private static readonly MethodInfo property_set_int_Count = t_set_int.GetMethod("get_Count", Type.EmptyTypes);
        private static readonly MethodInfo method_transition_ContainsKey = t_fsm_transition.GetMethod("ContainsKey", new Type[] { t_char });
        private static readonly MethodInfo method_List_int_Clear = t_List_int.GetMethod("Clear", Type.EmptyTypes);
        private static readonly MethodInfo indexer_transition = t_fsm_transition.GetMethod("get_Item", new Type[] { t_char });
        private static readonly MethodInfo property_set_int_Min = t_set_int.GetMethod("get_Min", Type.EmptyTypes);
        private static readonly MethodInfo method_set_int_Remove = t_set_int.GetMethod("Remove", new Type[] { t_int });
        private static readonly MethodInfo method_List_int_AddRange = t_List_int.GetMethod("AddRange", new Type[] { t_IEnumerable_int });
        private static readonly MethodInfo method_set_int_UnionWith = t_set_int.GetMethod("UnionWith", new Type[] { t_IEnumerable_int });
        private static readonly MethodInfo method_set_int_Contains = t_set_int.GetMethod("Contains", new Type[] { t_int });
        private static readonly ConstructorInfo ctor_Func_int_bool = t_Func_int_bool.GetConstructor(new Type[] { t_object, t_native_int });
        private static readonly ConstructorInfo ctor_situation = t_situation.GetConstructor(new Type[] { t_int, t_int, t_int });
        private static readonly ConstructorInfo ctor_historyRecord = t_historyRecord.GetConstructor(new Type[] { t_int, t_int, t_int, t_int, t_int });
        private static readonly ConstructorInfo ctor_List_situation = t_List_situation.GetConstructor(Type.EmptyTypes);
        private static readonly ConstructorInfo ctor_List_historyRecord = t_List_historyRecord.GetConstructor(Type.EmptyTypes);
        private static readonly ConstructorInfo ctor_endSituations_item = t_endSituations_item.GetConstructor(Type.EmptyTypes);
        private static readonly ConstructorInfo ctor_middleSituations_item = t_middleSituations_item.GetConstructor(Type.EmptyTypes);
        private static readonly MethodInfo property_List_situation_Count = t_List_situation.GetMethod("get_Count", Type.EmptyTypes);
        private static readonly MethodInfo indexer_List_situation = t_List_situation.GetMethod("get_Item", new Type[] { t_int });
        private static readonly ConstructorInfo ctor_tuple_int_int = t_tuple_int_int.GetConstructor(new Type[] { t_int, t_int });
        private static readonly MethodInfo method_endSituations_item_ContainsKey = t_endSituations_item.GetMethod("ContainsKey", new Type[] { t_tuple_int_int });
        private static readonly MethodInfo method_endSituations_item_Add = t_endSituations_item.GetMethod("Add", new Type[] { t_tuple_int_int, t_int });
        private static readonly MethodInfo indexer_endSituations_item = t_endSituations_item.GetMethod("get_Item", new Type[] { t_tuple_int_int });
        private static readonly MethodInfo method_List_situation_Sort = t_List_situation.GetMethod("Sort", Type.EmptyTypes);
        private static readonly MethodInfo method_List_historyRecord_Sort = t_List_historyRecord.GetMethod("Sort", Type.EmptyTypes);
        private static readonly MethodInfo method_middleSituations_item_ContainsKey = t_middleSituations_item.GetMethod("ContainsKey", new Type[] { t_int });
        private static readonly MethodInfo method_middleSituations_item_Add = t_middleSituations_item.GetMethod("Add", new Type[] { t_int, t_List_situation });
        private static readonly MethodInfo method_List_situation_Add = t_List_situation.GetMethod("Add", new Type[] { t_situation });
        private static readonly MethodInfo indexer_middleSituations_item = t_middleSituations_item.GetMethod("get_Item", new Type[] { t_int });
        private static readonly MethodInfo indexer_sortedRuleIds = t_sortedRuleIds.GetMethod("get_Item", new Type[] { t_int });
        private static readonly MethodInfo method_sortedRuleIds_ContainsKey = t_sortedRuleIds.GetMethod("ContainsKey", new Type[] { t_int });
        private static readonly MethodInfo method_List_historyRecord_Add = t_List_historyRecord.GetMethod("Add", new Type[] { t_historyRecord });
        private static readonly MethodInfo method_List_situation_BinarySearch = t_List_situation.GetMethod("BinarySearch", new Type[] { t_situation });
        private static readonly MethodInfo indexer_List_historyRecord = t_List_historyRecord.GetMethod("get_Item", new Type[] { t_int });
        private static readonly MethodInfo method_List_int_Add = t_List_int.GetMethod("Add", new Type[] { t_int });
        private static readonly ConstructorInfo ctor_StringBuilder = t_StringBuilder.GetConstructor(Type.EmptyTypes);
        private static readonly MethodInfo method_StringBuilder_Append = t_StringBuilder.GetMethod("Append", new Type[] { t_string });
        private static readonly ConstructorInfo ctor_string = t_string.GetConstructor(new Type[] { t_char, t_int });
        private static readonly MethodInfo method_string_Format3 = t_string.GetMethod("Format", new Type[] { t_string, t_object, t_object, t_object });
        private static readonly MethodInfo method_string_Format2 = t_string.GetMethod("Format", new Type[] { t_string, t_object, t_object });
        private static readonly MethodInfo method_string_Format1 = t_string.GetMethod("Format", new Type[] { t_string, t_object });
        private static readonly MethodInfo method_string_Concat = t_string.GetMethod("Concat", new Type[] { t_string, t_string });
        private static readonly MethodInfo method_StringBuilder_ToString = t_StringBuilder.GetMethod("ToString", Type.EmptyTypes);
        private static readonly ConstructorInfo ctor_Queue_int = t_Queue_int.GetConstructor(Type.EmptyTypes);
        private static readonly ConstructorInfo ctor_Stack_string = t_Stack_string.GetConstructor(Type.EmptyTypes);
        private static readonly MethodInfo method_Queue_int_Enqueue = t_Queue_int.GetMethod("Enqueue", new Type[] { t_int });
        private static readonly MethodInfo method_Queue_int_Dequeue = t_Queue_int.GetMethod("Dequeue", Type.EmptyTypes);
        private static readonly MethodInfo method_Stack_string_Push = t_Stack_string.GetMethod("Push", new Type[] { t_string });
        private static readonly MethodInfo method_Stack_string_Pop = t_Stack_string.GetMethod("Pop", Type.EmptyTypes);
        private static readonly MethodInfo method_List_int_ToArray = t_List_int.GetMethod("ToArray", Type.EmptyTypes);
        private static readonly MethodInfo method_Queue_int_Clear = t_Queue_int.GetMethod("Clear", Type.EmptyTypes);
        private static readonly MethodInfo property_string_Length = t_string.GetMethod("get_Length", Type.EmptyTypes);
        private static readonly MethodInfo property_Queue_int_Count = t_Queue_int.GetMethod("get_Count", Type.EmptyTypes);
        private static readonly MethodInfo indexer_string = t_string.GetMethod("get_Chars", new Type[] { t_int });
        private static readonly MethodInfo method_Queue_int_Peek = t_Queue_int.GetMethod("Peek", Type.EmptyTypes);
        private static readonly MethodInfo op_string_eq = t_string.GetMethod("op_Equality", new Type[] { t_string, t_string });
        private static readonly MethodInfo method_string_Substring = t_string.GetMethod("Substring", new Type[] { t_int, t_int });
        private static readonly ConstructorInfo ctor_Exception = t_Exception.GetConstructor(new Type[] { t_string });
        private static readonly MethodInfo method_StringBuilder_AppendInt = t_StringBuilder.GetMethod("Append", new Type[] { t_int });
        private static readonly ConstructorInfo ctor_set_situation = t_set_situation.GetConstructor(Type.EmptyTypes);
        private static readonly MethodInfo method_set_situation_Contains = t_set_situation.GetMethod("Contains", new Type[] { t_situation });
        private static readonly MethodInfo method_set_situation_Add = t_set_situation.GetMethod("Add", new Type[] { t_situation });

        // linq
        private static readonly Func<IEnumerable<int>, Func<int, bool>, bool> linq_delegate = Enumerable.Any;
        private static readonly MethodInfo linq_Any_int = linq_delegate.Method;

        // некоторые типы генерируемой сборки
        private static Type t_FSM;
        private static Type t_array_FSM;
        private static Type t_Earley;
        private static Type t_ParsingTree;

        // конструкторы классов генерируемой сборки
        private static ConstructorInfo ctor_UnexpSymEx;
        private static ConstructorInfo ctor_TkExpEx;
        private static ConstructorInfo ctor_EOFExpEx;
        private static ConstructorInfo ctor_fsm;
        private static ConstructorInfo ctor_Earley;
        private static ConstructorInfo ctor_ParsingTree_Leaf;
        private static ConstructorInfo ctor_ParsingTree_NotLeaf;

        // методы классов генерируемой сборки
        private static MethodInfo method_fsm_reset;
        private static MethodInfo method_fsm_tact;
        private static MethodInfo method_fsm_final;
        private static MethodInfo method_earley_GetRightAnalysis;

        // поля генерируемых классов
        private static FieldInfo fld_earley_rulesources;
        private static FieldInfo fld_earley_rulebodies;
        private static FieldInfo fld_earley_symbolNames;
        private static FieldInfo fld_earley_Start;

        // методы сборки классов

        // сборка классов исключений
        private static void MakeExceptions(ModuleBuilder module)
        {
            // UnexpectedSymbolException
            TypeBuilder t = module.DefineType("Parsing.UnexpectedSymbolException", attr_public, t_Exception);
            // поля
            FieldBuilder where = t.DefineField("Where", t_int, attr_fld_public | attr_fld_readonly);
            // конструктор
            ConstructorBuilder ctor = t.DefineConstructor(attr_method_internal, convention_class, new Type[] { t_int });
            ILGenerator il = ctor.GetILGenerator();
            // call base()
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldstr, "Unexpected symbol at position {0} in file"); // #0, format
            il.Emit(OpCodes.Ldarg_1); // #0, format, where_val
            il.Emit(OpCodes.Box, t_int); // #0, format, where_val_boxed
            il.Emit(OpCodes.Call, method_string_Format1); // #0, msg
            il.Emit(OpCodes.Call, ctor_Exception); // -
            // Where := where
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldarg_1); // #0, where
            il.Emit(OpCodes.Stfld, where); // -
            // that's all
            il.Emit(OpCodes.Ret); // -
            // создание класса
            t.CreateType();
            ctor_UnexpSymEx = ctor;

            // ExpectedEOFException
            t = module.DefineType("Parsing.ExpectedEOFException", attr_public, t_Exception);
            // поля
            where = t.DefineField("Where", t_int, attr_fld_public | attr_fld_readonly);
            // конструктор
            ctor = t.DefineConstructor(attr_method_internal, convention_class, new Type[] { t_int });
            il = ctor.GetILGenerator();
            // call base()
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldstr, "EOF expected at position {0} in file"); // #0, format
            il.Emit(OpCodes.Ldarg_1); // #0, format, where_val
            il.Emit(OpCodes.Box, t_int); // #0, format, where_val_boxed
            il.Emit(OpCodes.Call, method_string_Format1); // #0, msg
            il.Emit(OpCodes.Call, ctor_Exception); // -
            // Where := where
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldarg_1); // #0, where
            il.Emit(OpCodes.Stfld, where); // -
            // that's all
            il.Emit(OpCodes.Ret); // -
            // создание класса
            t.CreateType();
            ctor_EOFExpEx = ctor;

            // ExpectedTokensException
            t = module.DefineType("Parsing.ExpectedTokensException", attr_public, t_Exception);
            // поля
            FieldBuilder expectations = t.DefineField("Expectations", t_array_string, attr_fld_public | attr_fld_readonly);
            where = t.DefineField("Where", t_int, attr_fld_public | attr_fld_readonly);
            // конструктор
            ctor = t.DefineConstructor(attr_method_internal, convention_class, new Type[] { t_array_string, t_int });
            il = ctor.GetILGenerator();
            // call base()
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldstr, "{0} expected at position {1} in file"); // #0, fs
            // свёртка expectations
            il.Emit(OpCodes.Newobj, ctor_StringBuilder); // #0, fs, sb
            il.Emit(OpCodes.Ldarg_1); // #0, fs, sb, exp
            il.Emit(OpCodes.Ldc_I4_0); // #0, fs, sb, exp, 0
            il.Emit(OpCodes.Ldelem, t_string); // #0, fs, sb, exp[0]
            il.Emit(OpCodes.Call, method_StringBuilder_Append); // #0, fs, sb
            // цикл for (1 < exp.Length)
            // для этого заводим i <-> loc.0
            il.DeclareLocal(t_int);
            il.Emit(OpCodes.Ldc_I4_1); // #0, fs, sb, 1
            il.Emit(OpCodes.Stloc_0); // #0, fs, sb
            // проверка
            Label startLoop = il.DefineLabel();
            Label endLoop = il.DefineLabel();
            il.MarkLabel(startLoop);
            il.Emit(OpCodes.Ldloc_0); // #0, fs, sb, i
            il.Emit(OpCodes.Ldarg_1); // #0, fs, sb, i, exp
            il.Emit(OpCodes.Ldlen); // #0, fs, sb, i, exp.Length
            il.Emit(OpCodes.Clt); // #0, fs, sb, (i < len)
            il.Emit(OpCodes.Brfalse, endLoop); // #0, fs, sb
            // тело
            il.Emit(OpCodes.Ldstr, " or "); // #0, fs, sb, conststr
            il.Emit(OpCodes.Call, method_StringBuilder_Append); // #0, fs, sb
            il.Emit(OpCodes.Ldarg_1); // #0, fs, sb, exp
            il.Emit(OpCodes.Ldloc_0); // #0, fs, sb, exp, i
            il.Emit(OpCodes.Ldelem, t_string); // #0, fs, sb, exp[i]
            il.Emit(OpCodes.Call, method_StringBuilder_Append); // #0, fs, sb
            // i++, goto проверка
            il.Emit(OpCodes.Ldloc_0); // #0, fs, sb, i
            il.Emit(OpCodes.Ldc_I4_1); // #0, fs, sb, i, 1
            il.Emit(OpCodes.Add); // #0, fs, sb, (i + 1)
            il.Emit(OpCodes.Stloc_0); // #0, fs, sb
            il.Emit(OpCodes.Br, startLoop); // #0, fs, sb
            il.MarkLabel(endLoop);
            // продолжаем вызывать
            il.Emit(OpCodes.Ldarg_2); // #0, fs, sb, where_val
            il.Emit(OpCodes.Box, t_int); // #0, fs, sb, where_val_boxed
            il.Emit(OpCodes.Call, method_string_Format2); // #0, msg
            il.Emit(OpCodes.Call, ctor_Exception); // -
            // Expectations := expectations
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldarg_1); // #0, expectations
            il.Emit(OpCodes.Stfld, expectations); // -
            // Where := where
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldarg_2); // #0, where
            il.Emit(OpCodes.Stfld, where); // -
            // that's all
            il.Emit(OpCodes.Ret); // -
            // создание класса
            t.CreateType();
            ctor_TkExpEx = ctor;
        }

        // сборка класса автомата
        // является урезанной версией FSMFactory (т.к. предназначен только для работы)
        private static void MakeFSMClass(ModuleBuilder module)
        {
            // internal class FSM
            TypeBuilder t = module.DefineType("Parsing.C1", attr_class | attr_internal);
            // поля класса
            FieldBuilder states = t.DefineField("f1", t_set_int, attr_fld_private);
            FieldBuilder finalStates = t.DefineField("f2", t_set_int, attr_fld_private);
            FieldBuilder transition = t.DefineField("f3", t_fsm_transition, attr_fld_private);
            FieldBuilder helper = t.DefineField("f4", t_List_int, attr_fld_private);
            // конструктор класса (transition, finals)
            ConstructorBuilder constructor = t.DefineConstructor(attr_method_public, convention_class, new Type[] { t_fsm_transition, t_set_int });
            ILGenerator il = constructor.GetILGenerator();
            // 1) вызов base() // <диаграмма стека (вершина справа)>
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Call, ctor_object); // - [вызов base()]
            // 2) states = {0}
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Newobj, ctor_set_int); // #0, set
            il.Emit(OpCodes.Dup); // #0, set, set
            il.Emit(OpCodes.Ldc_I4_0); // #0, set, set, 0
            il.Emit(OpCodes.Call, method_set_int_Add); // #0, set({0}), Add_result
            il.Emit(OpCodes.Pop); // #0, set({0})
            il.Emit(OpCodes.Stfld, states); // -
            // 3) finalStates = arg.2
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldarg_2); // #0 #2
            il.Emit(OpCodes.Stfld, finalStates); // -
            // 4) transition = arg.1
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldarg_1); // #0 #1
            il.Emit(OpCodes.Stfld, transition); // -
            // 5) helper = new List<int>()
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Newobj, ctor_List_int); // #0, List<int>
            il.Emit(OpCodes.Stfld, helper); // -
            // that's all
            il.Emit(OpCodes.Ret);
            //------------------------------
            // метод void Reset()
            MethodBuilder reset = t.DefineMethod("m1", attr_method_public, t_void, Type.EmptyTypes);
            il = reset.GetILGenerator();
            // 1) states.Clear()
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, states); // states
            il.Emit(OpCodes.Callvirt, method_set_int_Clear); // -
            // 2) states.Add(0)
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, states); // states
            il.Emit(OpCodes.Ldc_I4_0); // states, 0
            il.Emit(OpCodes.Call, method_set_int_Add); // Add_result
            il.Emit(OpCodes.Pop); // -
            // end
            il.Emit(OpCodes.Ret);
            //-----------------------------
            // метод bool Tact(char c)
            MethodBuilder tact = t.DefineMethod("m2", attr_method_public, t_bool, new Type[] { t_char });
            il = tact.GetILGenerator();
            // if(!transition.ContainsKey(c)) {states.Clear(); return false;}
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, transition); // transition
            il.Emit(OpCodes.Ldarg_1); // transition, c
            il.Emit(OpCodes.Call, method_transition_ContainsKey); // CK
            Label label = il.DefineLabel();
            il.Emit(OpCodes.Brtrue, label); // -
            il.Emit(OpCodes.Ldc_I4_0); // false
            il.Emit(OpCodes.Ret); // return false
            il.MarkLabel(label); // - [transition really contains key c]
            // helper.Clear()
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, helper); // helper
            il.Emit(OpCodes.Call, method_List_int_Clear); // -
            // var func = transition[c] : int[][]
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, transition); //transition
            il.Emit(OpCodes.Ldarg_1); // transition, c
            il.Emit(OpCodes.Call, indexer_transition); // func
            il.DeclareLocal(t_array_array_int); // [define loc.0]
            il.Emit(OpCodes.Stloc_0); // -
            // foreach(var s in states) helper.AddRange(func[s]); | states.Clear();
            // подойдём к этому по-другому
            // while(states.Count > 0) {helper.AddRange(func[states.Min]); states.Remove(states.Min)}
            // а т.к. states.Count точно > 0 (иначе не вызовем) то цикл удобнее записать как do...while
            label = il.DefineLabel();
            il.MarkLabel(label);
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, helper); // helper
            il.Emit(OpCodes.Ldloc_0); // helper, func
            il.Emit(OpCodes.Ldarg_0); // helper, func, #0
            il.Emit(OpCodes.Ldfld, states); // helper, func, states
            il.Emit(OpCodes.Call, property_set_int_Min); // helper, func, s
            il.Emit(OpCodes.Ldelem, t_array_int); // helper, func[s]
            il.Emit(OpCodes.Call, method_List_int_AddRange); // -
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, states); // states
            il.Emit(OpCodes.Dup); // states, states
            il.Emit(OpCodes.Dup); // states, states, states
            il.Emit(OpCodes.Call, property_set_int_Min); // states, states, states.Min
            il.Emit(OpCodes.Call, method_set_int_Remove); // states, Remove_result
            il.Emit(OpCodes.Pop); // states
            il.Emit(OpCodes.Call, property_set_int_Count); // states.Count
            il.Emit(OpCodes.Brtrue, label); // -
            //[here SortedSet.Count == 0]
            // states.UnionWith(helper);
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, states); // states
            il.Emit(OpCodes.Dup); // states, states
            il.Emit(OpCodes.Ldarg_0); // states, states, #0
            il.Emit(OpCodes.Ldfld, helper); // states, states, helper
            il.Emit(OpCodes.Call, method_set_int_UnionWith); // states
            il.Emit(OpCodes.Call, property_set_int_Count); // states.Count
            il.Emit(OpCodes.Ldc_I4_0); // states.Count, 0
            il.Emit(OpCodes.Cgt); // (>)
            il.Emit(OpCodes.Ret); // [это был результат]
            //------------------------------------------------
            // bool Final()
            MethodBuilder final = t.DefineMethod("m3", attr_method_public, t_bool, Type.EmptyTypes);
            il = final.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, states); // states
            il.Emit(OpCodes.Ldarg_0); // states, #0
            il.Emit(OpCodes.Ldfld, finalStates); // states, finalStates
            il.Emit(OpCodes.Dup); // states, finalStates, finalStates (второй раз для инстр. ldvirtftn)
            il.Emit(OpCodes.Ldvirtftn, method_set_int_Contains); // states, finalStates, *SortedSet<int>::Contains
            il.Emit(OpCodes.Newobj, ctor_Func_int_bool); //states, Func
            il.Emit(OpCodes.Call, linq_Any_int); // any_result
            il.Emit(OpCodes.Ret);
            //---------------------------------------
            //класс готов
            t_FSM = t.CreateType();
            t_array_FSM = t_FSM.MakeArrayType();
            ctor_fsm = constructor;
            method_fsm_reset = reset;
            method_fsm_tact = tact;
            method_fsm_final = final;
        }

        // класс сессии алгоритма Эрли
        // класс является объединением классов Grammar, EarleyListSystem и EarleyListSystem.EarleyList
        // без методов конструкции
        // от класса Grammar достаётся:
        // terminal, который теперь будет bool[]
        // rules, который разделяется на
        //   # rulesources : int[]
        //   # rulebodies  : int[][]
        // sortedRuleIds, который теперь будет SortedDictionary<int, int[]>
        // Start
        // все методы и остальные свойства класса Grammar не войдут сюда
        // symbolNames (string[]) и symbolIds (тот же тип) теперь будут находиться
        // отдельно и инциализироваться вне Earley-класса
        // от класса EarleyListSystem получаем:
        // метод List<int> GetRightAnalysis(int[], int[]), он будет принимать только строку a
        // в терминалах и массив truepositions
        // методы BuildLists и Analyze будут прописаны непосредственно внутри него
        // класс EarleyList
        // все поля класса EarleyList будут преобразованы в массивы исходных типов
        // situations : List<(int, int, int)>[]
        // history: List<(int, int, int, int, int)>[]
        // endSituations : SortedDictionary<(int, int), int>[]
        // middleSituations : SortedDictionary<int, List<(int, int, int)>>[]
        // startSources : Sorted<int>[]
        // void AddEndSituation(int A, int i, int ruleId, int listId)
        // bool ExistsEndSituation(int A, int i, int listId)
        // void AddMiddleSituation(int ruleId, int pointLocation, int second, int listId)
        // List<int> GetMiddleSituations(int A, int listId)
        //      причём emptyarr заменяется на null и в месте его вызова следует делать проверку на null
        // void AddAllStartRules(int src, int listId)
        // void Add(int ruleId, int pointLocation, int second, int hA, int hi, int listId)
        // (int, int) GetHistory(int ruleId, int pointLocation, int second, int listId)
        // void R(List<int> result, int rule, int i, int j)
        // Exception MakeError(int aLength, int[] truepos)
        // BuildI0, BuildIj и PrepareToAnalyze будут интегрированы в GetRightAnalysis
        // из всего этого публичными будут только конструктор и GetRightAnalysis
        private static void MakeEarleyClass(ModuleBuilder module)
        {
            TypeBuilder t = module.DefineType("Parsing.C2", attr_class | attr_internal);
            // поля класса
            // public для использования в Parser
            FieldBuilder terminal = t.DefineField("f1", t_array_bool, attr_fld_public);
            FieldBuilder rulesources = t.DefineField("f2", t_array_int, attr_fld_public);
            FieldBuilder rulebodies = t.DefineField("f3", t_array_array_int, attr_fld_public);
            FieldBuilder symbolNames = t.DefineField("f4", t_array_string, attr_fld_public);
            // end
            FieldBuilder sortedRuleIds = t.DefineField("f5", t_sortedRuleIds, attr_fld_private);
            FieldBuilder start = t.DefineField("f6", t_int, attr_fld_public);
            FieldBuilder situations = t.DefineField("f7", t_array_List_situation, attr_fld_private);
            FieldBuilder history = t.DefineField("f8", t_array_List_historyRecord, attr_fld_private);
            FieldBuilder endSituations = t.DefineField("f9", t_endSituations, attr_fld_private);
            FieldBuilder middleSituations = t.DefineField("f10", t_middleSituations, attr_fld_private);
            FieldBuilder startSources = t.DefineField("f11", t_array_set_int, attr_fld_private);
            FieldBuilder situationsSet = t.DefineField("f12", t_array_set_situation, attr_fld_private);
            // конструктор класса
            // его параметры будут инициализировать
            // terminal, rulesources, rulebodies, symbolNames, sortedRuleIds, start (т.е. задавать грамматику)
            ConstructorBuilder constructor = t.DefineConstructor(attr_method_public, convention_class, new Type[] { t_array_bool, t_array_int, t_array_array_int, t_array_string, t_sortedRuleIds, t_int });
            ILGenerator il = constructor.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Call, ctor_object); // - [вызов base()]
            // заданные параметры инициализируем с конструктора, остальные мы здесь не трогаем
            // terminal
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldarg_1); // #0, terminal
            il.Emit(OpCodes.Stfld, terminal); //-
            // и дальше полностью по аналогии с похожими диаграммами стека
            // rulesources
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Stfld, rulesources);
            // rulebodies
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_3);
            il.Emit(OpCodes.Stfld, rulebodies);
            // symbolNames
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg, (short)4);
            il.Emit(OpCodes.Stfld, symbolNames);
            // sortedRuleIds
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg, (short)5); // NB! Int16
            il.Emit(OpCodes.Stfld, sortedRuleIds);
            // Start
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg, (short)6);
            il.Emit(OpCodes.Stfld, start);
            // end
            il.Emit(OpCodes.Ret);
            //---------------------------------------------------
            // инициализируем сначала все методы, поскольку они используют перекрёстную рекурсию
            MethodBuilder getRightAnalysis = t.DefineMethod("m1", attr_method_public, t_Queue_int, new Type[] { t_array_int, t_array_int });
            MethodBuilder addEndSituation = t.DefineMethod("m2", attr_method_private, t_void, new Type[] { t_int, t_int, t_int, t_int });
            MethodBuilder existsEndSituation = t.DefineMethod("m3", attr_method_private, t_bool, new Type[] { t_int, t_int, t_int });
            MethodBuilder addMiddleSituations = t.DefineMethod("m4", attr_method_private, t_void, new Type[] { t_int, t_int, t_int, t_int });
            MethodBuilder getMiddleSituations = t.DefineMethod("m5", attr_method_private, t_List_situation, new Type[] { t_int, t_int });
            MethodBuilder addAllStartRules = t.DefineMethod("m6", attr_method_private, t_void, new Type[] { t_int, t_int });
            MethodBuilder add = t.DefineMethod("m7", attr_method_private, t_void, new Type[] { t_int, t_int, t_int, t_int, t_int, t_int });
            MethodBuilder getHistory = t.DefineMethod("m8", attr_method_private, t_tuple_int_int, new Type[] { t_int, t_int, t_int, t_int });
            MethodBuilder r = t.DefineMethod("m9", attr_method_private, t_void, new Type[] { t_Queue_int, t_int, t_int, t_int, t_array_int });
            MethodBuilder makeError = t.DefineMethod("m10", attr_method_private, t_Exception, new Type[] { t_int, t_array_int });

            // реализация getRightAnalysis
            il = getRightAnalysis.GetILGenerator();
            // 1) присваивание грамматики
            // в нашем случае - инициализация параметров грамматики, которая уже прошла в конструкторе
            // 2) получение массива a
            // здесь он будет передаваться и использоваться как параметр
            // 3) часть от BuildLists
            // 3.1) инициализация списков Эрли
            // вместо неё проводим инициализацию бывших полей EarleyList
            // сохраним в n(loc.0) a.Length + 1
            il.DeclareLocal(t_int); // [loc.0 is n]
            il.Emit(OpCodes.Ldarg_1); // a
            il.Emit(OpCodes.Ldlen); // a.Length
            il.Emit(OpCodes.Ldc_I4_1); // a, 1 
            il.Emit(OpCodes.Add); // (a.Length + 1)
            il.Emit(OpCodes.Stloc_0); // - [n(loc.0) = a.Length + 1]
            // инициализация situations
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldloc_0); // #0, n
            il.Emit(OpCodes.Newarr, t_List_situation); // #0, new List<situation>[]
            il.Emit(OpCodes.Stfld, situations); // -
            // инициализация всех элементов situations
            // i = 0
            // do {situations[i] = new List<situation>(); i++;} while(i < n)
            // заводим счётчик: loc.1
            il.DeclareLocal(t_int); // [loc.1 is i]
            il.Emit(OpCodes.Ldc_I4_0); // 0
            il.Emit(OpCodes.Stloc_1); // -
            // нужна метка на начало цикла
            Label label = il.DefineLabel();
            il.MarkLabel(label);
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, situations); // situations
            il.Emit(OpCodes.Ldloc_1); // situations, i
            il.Emit(OpCodes.Newobj, ctor_List_situation); // situations, i, List<situation>
            il.Emit(OpCodes.Stelem, t_List_situation); // -
            il.Emit(OpCodes.Ldloc_1); // i
            il.Emit(OpCodes.Ldc_I4_1); // i, 1
            il.Emit(OpCodes.Add); // (i + 1)
            il.Emit(OpCodes.Dup); // (i + 1), (i + 1)
            il.Emit(OpCodes.Stloc_1); // (i + 1)
            il.Emit(OpCodes.Ldloc_0); // new i, n
            il.Emit(OpCodes.Clt); // (i < n)
            il.Emit(OpCodes.Brtrue, label); // -
            // то же самое с history
            // инициализация history
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldloc_0); // #0, n
            il.Emit(OpCodes.Newarr, t_List_historyRecord); // #0, new List<historyRecord>
            il.Emit(OpCodes.Stfld, history); // -
            // инициализация всех элементов history
            // i = 0
            // do {history[i] = new List<historyRecord>(); i++;} while(i < n)
            // заводим счётчик: loc.1
            il.Emit(OpCodes.Ldc_I4_0); // 0
            il.Emit(OpCodes.Stloc_1); // -
            // нужна метка на начало цикла
            label = il.DefineLabel();
            il.MarkLabel(label);
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, history); // history
            il.Emit(OpCodes.Ldloc_1); // history, i
            il.Emit(OpCodes.Newobj, ctor_List_historyRecord); // history, i, List<historyRecord>
            il.Emit(OpCodes.Stelem, t_List_historyRecord); // -
            il.Emit(OpCodes.Ldloc_1); // i
            il.Emit(OpCodes.Ldc_I4_1); // i, 1
            il.Emit(OpCodes.Add); // (i + 1)
            il.Emit(OpCodes.Dup); // (i + 1), (i + 1)
            il.Emit(OpCodes.Stloc_1); // (i + 1)
            il.Emit(OpCodes.Ldloc_0); // new i, n
            il.Emit(OpCodes.Clt); // (i < n)
            il.Emit(OpCodes.Brtrue, label); // -
            // endSituations
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldloc_0); // #0, n
            il.Emit(OpCodes.Newarr, t_endSituations_item); // #0, newvalueof(endSituations)
            il.Emit(OpCodes.Stfld, endSituations); // -
            // инициализация всех элементов endSituations
            // i = 0
            // do {endSituations[i] = new ...; i++;} while(i < n)
            // заводим счётчик: loc.1
            il.Emit(OpCodes.Ldc_I4_0); // 0
            il.Emit(OpCodes.Stloc_1); // -
            // нужна метка на начало цикла
            label = il.DefineLabel();
            il.MarkLabel(label);
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, endSituations); // endSituations
            il.Emit(OpCodes.Ldloc_1); // endSituations, i
            il.Emit(OpCodes.Newobj, ctor_endSituations_item); // endSituations, i, endSituationsItem
            il.Emit(OpCodes.Stelem, t_endSituations_item); // -
            il.Emit(OpCodes.Ldloc_1); // i
            il.Emit(OpCodes.Ldc_I4_1); // i, 1
            il.Emit(OpCodes.Add); // (i + 1)
            il.Emit(OpCodes.Dup); // (i + 1), (i + 1)
            il.Emit(OpCodes.Stloc_1); // (i + 1)
            il.Emit(OpCodes.Ldloc_0); // new i, n
            il.Emit(OpCodes.Clt); // (i < n)
            il.Emit(OpCodes.Brtrue, label); // -
            // middleSituations
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldloc_0); // #0, n
            il.Emit(OpCodes.Newarr, t_middleSituations_item); // #0, valueof(middleSituations)
            il.Emit(OpCodes.Stfld, middleSituations); // -
            // инициализация всех элементов middleSituations
            // i = 0
            // do {middleSituations[i] = new ...(); i++;} while(i < n)
            // заводим счётчик: loc.1
            il.Emit(OpCodes.Ldc_I4_0); // 0
            il.Emit(OpCodes.Stloc_1); // -
            // нужна метка на начало цикла
            label = il.DefineLabel();
            il.MarkLabel(label);
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, middleSituations); // middleSituations
            il.Emit(OpCodes.Ldloc_1); // middleSituations, i
            il.Emit(OpCodes.Newobj, ctor_middleSituations_item); // middleSituations, i, middleSituationsItem
            il.Emit(OpCodes.Stelem, t_middleSituations_item); // -
            il.Emit(OpCodes.Ldloc_1); // i
            il.Emit(OpCodes.Ldc_I4_1); // i, 1
            il.Emit(OpCodes.Add); // (i + 1)
            il.Emit(OpCodes.Dup); // (i + 1), (i + 1)
            il.Emit(OpCodes.Stloc_1); // (i + 1)
            il.Emit(OpCodes.Ldloc_0); // new i, n
            il.Emit(OpCodes.Clt); // (i < n)
            il.Emit(OpCodes.Brtrue, label); // -
            // startSources
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldloc_0); // #0, n
            il.Emit(OpCodes.Newarr, t_set_int); // #0, valueof(startSources)
            il.Emit(OpCodes.Stfld, startSources); // -
            // инициализация всех элементов startSources
            // i = 0
            // do {startSources[i] = new SortedSet<int>(); i++;} while(i < n)
            // заводим счётчик: loc.1
            il.Emit(OpCodes.Ldc_I4_0); // 0
            il.Emit(OpCodes.Stloc_1); // -
            // нужна метка на начало цикла
            label = il.DefineLabel();
            il.MarkLabel(label);
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, startSources); // startSources
            il.Emit(OpCodes.Ldloc_1); // startSources, i
            il.Emit(OpCodes.Newobj, ctor_set_int); // startSources, i, SortedSet<int>
            il.Emit(OpCodes.Stelem, t_set_int); // -
            il.Emit(OpCodes.Ldloc_1); // i
            il.Emit(OpCodes.Ldc_I4_1); // i, 1
            il.Emit(OpCodes.Add); // (i + 1)
            il.Emit(OpCodes.Dup); // (i + 1), (i + 1)
            il.Emit(OpCodes.Stloc_1); // (i + 1)
            il.Emit(OpCodes.Ldloc_0); // new i, n
            il.Emit(OpCodes.Clt); // (i < n)
            il.Emit(OpCodes.Brtrue, label); // -
            // situationsSet
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldloc_0); // #0, n
            il.Emit(OpCodes.Newarr, t_set_situation); // #0, valueof(situationsSet)
            il.Emit(OpCodes.Stfld, situationsSet); // -
            // инициализация всех элементов startSources
            // i = 0
            // do {startSources[i] = new SortedSet<int>(); i++;} while(i < n)
            // заводим счётчик: loc.1
            il.Emit(OpCodes.Ldc_I4_0); // 0
            il.Emit(OpCodes.Stloc_1); // -
            // нужна метка на начало цикла
            label = il.DefineLabel();
            il.MarkLabel(label);
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, situationsSet); // situationsSet
            il.Emit(OpCodes.Ldloc_1); // situationsSet, i
            il.Emit(OpCodes.Newobj, ctor_set_situation); // situationsSet, i, SortedSet<situation>
            il.Emit(OpCodes.Stelem, t_set_situation); // -
            il.Emit(OpCodes.Ldloc_1); // i
            il.Emit(OpCodes.Ldc_I4_1); // i, 1
            il.Emit(OpCodes.Add); // (i + 1)
            il.Emit(OpCodes.Dup); // (i + 1), (i + 1)
            il.Emit(OpCodes.Stloc_1); // (i + 1)
            il.Emit(OpCodes.Ldloc_0); // new i, n
            il.Emit(OpCodes.Clt); // (i < n)
            il.Emit(OpCodes.Brtrue, label); // -
            // часть BuildI0
            // здесь просто будет происходить вызов this.AddAllStartRules(start, 0)
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldarg_0); // #0, #0
            il.Emit(OpCodes.Ldfld, start); // #0, start
            il.Emit(OpCodes.Ldc_I4_0); // #0, start, 0
            il.Emit(OpCodes.Call, addAllStartRules);
            // часть BuildIj
            // i := 1
            // while(i < n) { do something with i-th list }
            // заводим две метки: loopStart, loopEnd
            Label loopStart = il.DefineLabel();
            Label loopEnd = il.DefineLabel();
            il.Emit(OpCodes.Ldc_I4_1); // 1
            il.Emit(OpCodes.Stloc_1); // - [i := 1]
            // начало цикла отмечается loopStart
            il.MarkLabel(loopStart);
            // проверка условия
            // false => goto loopEnd
            il.Emit(OpCodes.Ldloc_1); // i
            il.Emit(OpCodes.Ldloc_0); // i, n
            il.Emit(OpCodes.Clt); // (i < n)
            il.Emit(OpCodes.Brfalse, loopEnd); // -
            // основное тело цикла
            // пройтись по всем правилам в списке i-1
            // отобрать необходимые и добавить себе с историей (-1, -1)
            // заведём ещё один внутренний счётчик j [loc.2]
            il.DeclareLocal(t_int);
            // организуем внутренний цикл по j от 0 до situations[i - 1].Count
            il.Emit(OpCodes.Ldc_I4_0); // 0
            il.Emit(OpCodes.Stloc_2);  // - [j := 0]
            // здесь тоже организуем метки начала и конца выхода
            Label startInnerLoop = il.DefineLabel();
            Label endInnerLoop = il.DefineLabel();
            // + метка для continue
            Label continueLabel = il.DefineLabel();
            // начало
            il.MarkLabel(startInnerLoop);
            // проверка условия
            il.Emit(OpCodes.Ldloc_2); // j
            il.Emit(OpCodes.Ldarg_0); // j, #0
            il.Emit(OpCodes.Ldfld, situations); // j, situations
            il.Emit(OpCodes.Ldloc_1); // j, situations, i
            il.Emit(OpCodes.Ldc_I4_M1); // j, situations, i, -1
            il.Emit(OpCodes.Add); // j, situations, (i - 1)
            il.Emit(OpCodes.Ldelem, t_List_situation); // j, situations[i-1]
            il.Emit(OpCodes.Call, property_List_situation_Count); // j, situations[i-1].Count
            il.Emit(OpCodes.Clt); // (j < situations[i-1].Count)
            il.Emit(OpCodes.Brfalse, endInnerLoop);
            // основное тело
            // [loc.3] - situation
            il.DeclareLocal(t_situation);
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, situations); // situations
            il.Emit(OpCodes.Ldloc_1); // situations, i
            il.Emit(OpCodes.Ldc_I4_M1); // situations, i, -1
            il.Emit(OpCodes.Add); // situations, (i - 1)
            il.Emit(OpCodes.Ldelem, t_List_situation); // situations[i-1]
            il.Emit(OpCodes.Ldloc_2); // situations[i-1], j
            il.Emit(OpCodes.Call, indexer_List_situation); // situations[i-1][j]
            il.Emit(OpCodes.Stloc_3); // - [loc.3 := situations[i-1][j]]
            // if(rulebodies[loc.3.ruleId].Length == loc.3.pointLocation) continue;
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, rulebodies); // rulebodies
            il.Emit(OpCodes.Ldloca, (short)3); // rulebodies, &loc.3
            il.Emit(OpCodes.Ldfld, fld_situation_ruleId); // rulebodies, ruleId
            il.Emit(OpCodes.Ldelem, t_array_int); // rulebodies[ruleId]
            il.Emit(OpCodes.Ldlen); // rulebodies[ruleId].Length
            il.Emit(OpCodes.Ldloca, (short)3); // length, &loc.3
            il.Emit(OpCodes.Ldfld, fld_situation_pointLocation); // length, pointLocation
            il.Emit(OpCodes.Ceq); // (length == pointLocation)
            il.Emit(OpCodes.Brtrue, continueLabel); // -
            // if(a[i - 1] != rulebodies[ruleId][pointLocation]) continue;
            il.Emit(OpCodes.Ldarg_1); // a
            il.Emit(OpCodes.Ldloc_1); // a, i
            il.Emit(OpCodes.Ldc_I4_M1); // a, i, -1
            il.Emit(OpCodes.Add); // a, (i - 1)
            il.Emit(OpCodes.Ldelem, t_int); // a[i-1]
            il.Emit(OpCodes.Ldarg_0); // a[i-1], #0
            il.Emit(OpCodes.Ldfld, rulebodies); // a[i-1], rulebodies
            il.Emit(OpCodes.Ldloca, (short)3); // a[i-1], rulebodies, &loc.3
            il.Emit(OpCodes.Ldfld, fld_situation_ruleId); // a[i-1], rulebodies, ruleId
            il.Emit(OpCodes.Ldelem, t_array_int); // a[i-1], rulebodies[ruleId]
            il.Emit(OpCodes.Ldloca, (short)3); // a[i-1], rulebodies[ruleId], &loc.3
            il.Emit(OpCodes.Ldfld, fld_situation_pointLocation); // a[i-1], rulebodies[ruleId], pointLocation
            il.Emit(OpCodes.Ldelem, t_int); // a[i-1], rulebodies[ruleId][pointLocation]
            il.Emit(OpCodes.Ceq); // (a[i-1] == rulebodies[ruleId][pointLocation])
            il.Emit(OpCodes.Brfalse, continueLabel); // -
            // this.Add(ruleId, pointLocation + 1, second, -1, -1, i as listId)
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldloca, (short)3); // #0, &loc.3
            il.Emit(OpCodes.Ldfld, fld_situation_ruleId); // #0, ruleId
            il.Emit(OpCodes.Ldloca, (short)3); // #0, ruleId, &loc.3
            il.Emit(OpCodes.Ldfld, fld_situation_pointLocation); // #0, ruleId, pointLocation
            il.Emit(OpCodes.Ldc_I4_1); // #0, ruleId, pointLocation, 1
            il.Emit(OpCodes.Add); // #0, ruleId, (pointLocation + 1)
            il.Emit(OpCodes.Ldloca, (short)3); // #0, ruleId, (pointLocation + 1), &loc.3
            il.Emit(OpCodes.Ldfld, fld_situation_second); // #0, ruleId, (pointLocation + 1), second
            il.Emit(OpCodes.Ldc_I4_M1); // #0, ruleId, (pointLocation + 1), second, -1
            il.Emit(OpCodes.Ldc_I4_M1); // #0, ruleId, (pointLocation + 1), second, -1, -1
            il.Emit(OpCodes.Ldloc_1); // #0, ruleId, (pointLocation + 1), second, -1, -1, i
            il.Emit(OpCodes.Call, add); // -
            // увеличение j на 1
            il.MarkLabel(continueLabel);
            il.Emit(OpCodes.Ldloc_2); // j
            il.Emit(OpCodes.Ldc_I4_1); // j, 1
            il.Emit(OpCodes.Add); // (j + 1)
            il.Emit(OpCodes.Stloc_2); // - [j := j + 1]
            // перейти к проверке условия внутреннего цикла
            il.Emit(OpCodes.Br, startInnerLoop); // -
            // конец внутреннего цикла
            il.MarkLabel(endInnerLoop);
            // увеличение i на 1
            il.Emit(OpCodes.Ldloc_1); // i
            il.Emit(OpCodes.Ldc_I4_1); // i, 1
            il.Emit(OpCodes.Add); // (i + 1)
            il.Emit(OpCodes.Stloc_1); // - [i := i + 1]
            // перейти к проверке условия: goto loopStart
            il.Emit(OpCodes.Br, loopStart); // -
            // конец цикла  отмечается loopEnd
            il.MarkLabel(loopEnd);
            // часть Analyze (неполная - без R)
            // int startRuleId = -1
            // это значение будем держать в loc.2
            // итак, сейчас обозначаем startRuleId := loc.2
            // -1 записывать нет смысла
            // просто вместо break в следующем цикле будем сразу делать goto на часть кода
            // где используется валидное значение startRuleId
            // foreach (var (ruleId, pointLocation, second) in situations[a.Length])
            // используем ту же конструкцию цикла (по i : loc.1)
            loopStart = il.DefineLabel();
            loopEnd = il.DefineLabel();
            continueLabel = il.DefineLabel();
            label = il.DefineLabel(); // а это будет метка на инициализацию result и вызов R
            // инициализация: i := 0
            il.Emit(OpCodes.Ldc_I4_0); // 0
            il.Emit(OpCodes.Stloc_1); // - [i := 0]
            // начало цикла
            il.MarkLabel(loopStart);
            // проверка условия: i < situations[a.Length].Count
            il.Emit(OpCodes.Ldloc_1); // i
            il.Emit(OpCodes.Ldarg_0); // i, #0
            il.Emit(OpCodes.Ldfld, situations); // i, situations
            il.Emit(OpCodes.Ldarg_1); // i, situations, a
            il.Emit(OpCodes.Ldlen); // i, situations, a.Length
            il.Emit(OpCodes.Ldelem, t_List_situation); // i, situations[a.Length]
            il.Emit(OpCodes.Call, property_List_situation_Count); // i, situations[a.Length].Count
            il.Emit(OpCodes.Clt); // (i < situations[a.Length].Count)
            il.Emit(OpCodes.Brfalse, loopEnd); // -
            // основное тело цикла
            // снова запишем в loc.3 текущую ситуацию - situations[a.Length][i]
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, situations); // situations
            il.Emit(OpCodes.Ldarg_1); // situations, a
            il.Emit(OpCodes.Ldlen); // situations, a.Length
            il.Emit(OpCodes.Ldelem, t_List_situation); // situations[a.Length]
            il.Emit(OpCodes.Ldloc_1); // situations[a.Length], i
            il.Emit(OpCodes.Call, indexer_List_situation); // situations[a.Length][i]
            il.Emit(OpCodes.Stloc_3); // - [loc.3 := current situation]
            // требования к ситуации для её принятия
            // rulesources[ruleId] == start
            // second == 0
            // rulebodies[ruleId].Length == pointLocation
            // при невыполнении этих условий переход на continueLabel
            // if(rulesources[ruleId] != start) continue;
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, rulesources); // rulesources
            il.Emit(OpCodes.Ldloca, (short)3); // rulesources, &loc.3
            il.Emit(OpCodes.Ldfld, fld_situation_ruleId); // rulesources, ruleId
            il.Emit(OpCodes.Ldelem, t_int); // rulesources[ruleId]
            il.Emit(OpCodes.Ldarg_0); // rulesources[ruleId], #0
            il.Emit(OpCodes.Ldfld, start); // rulesources[ruleId], start
            il.Emit(OpCodes.Ceq); // (rulesources[ruleId] == start)
            il.Emit(OpCodes.Brfalse, continueLabel); // -
            // if(second != 0) continue;
            il.Emit(OpCodes.Ldloca, (short)3); // &loc.3
            il.Emit(OpCodes.Ldfld, fld_situation_second); // second
            il.Emit(OpCodes.Brtrue, continueLabel); // -
            // if(rulebodies[ruleId].Length != pointLocation) continue;
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, rulebodies); // rulebodies
            il.Emit(OpCodes.Ldloca, (short)3); // rulebodies, &loc.3
            il.Emit(OpCodes.Ldfld, fld_situation_ruleId); // rulebodies, ruleId
            il.Emit(OpCodes.Ldelem, t_array_int); // rulebodies[ruleId]
            il.Emit(OpCodes.Ldlen); // rulebodies[ruleId].Length
            il.Emit(OpCodes.Ldloca, (short)3); // rulebodies[ruleId].Length, &loc.3
            il.Emit(OpCodes.Ldfld, fld_situation_pointLocation); // rulebodies[ruleId].Length, pointLocation
            il.Emit(OpCodes.Ceq); // (rulebodies[ruleId].Length == pointLocation)
            il.Emit(OpCodes.Brfalse, continueLabel); // -
            // в этом случае нужно сохранить ruleId в startRuleId(j | loc.2) и goto label
            // где находится переход к инициализации result и вызов R
            il.Emit(OpCodes.Ldloca, (short)3); // &loc.3
            il.Emit(OpCodes.Ldfld, fld_situation_ruleId); // ruleId
            il.Emit(OpCodes.Stloc_2); // - [startRuleId := ruleId]
            il.Emit(OpCodes.Br, label);
            // увеличение i на 1
            il.MarkLabel(continueLabel);
            il.Emit(OpCodes.Ldloc_1); // i
            il.Emit(OpCodes.Ldc_I4_1); // i, 1
            il.Emit(OpCodes.Add); // (i + 1)
            il.Emit(OpCodes.Stloc_1); // - [i := i + 1]
            // переход к проверке условия
            il.Emit(OpCodes.Br, loopStart);
            // конец цикла
            il.MarkLabel(loopEnd);
            // если мы добрались до сюда, то строка не является синтаксически корректной
            // здесь нужно запустить MakeError
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldarg_1); // #0, a
            il.Emit(OpCodes.Ldlen); // #0, aLen
            il.Emit(OpCodes.Ldarg_2); // #0, aLen, truepos
            il.Emit(OpCodes.Call, makeError); // exception
            il.Emit(OpCodes.Throw);
            // теперь часть, где строка признана корректной
            il.MarkLabel(label);
            // часть PrepareToAnalyze
            // цикл
            // i = 0;
            // do { ...; i++; } while(i < n);
            // инициализация
            il.Emit(OpCodes.Ldc_I4_0); // 0
            il.Emit(OpCodes.Stloc_1); // - [i := 0]
            // начало цикла
            label = il.DefineLabel();
            il.MarkLabel(label);
            // situations[i].Sort()
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, situations); // situations
            il.Emit(OpCodes.Ldloc_1); // situations, i
            il.Emit(OpCodes.Ldelem, t_List_situation); // situations[i]
            il.Emit(OpCodes.Call, method_List_situation_Sort); // -
            // history[i].Sort()
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, history); // history
            il.Emit(OpCodes.Ldloc_1); // history, i
            il.Emit(OpCodes.Ldelem, t_List_historyRecord); // history[i]
            il.Emit(OpCodes.Call, method_List_historyRecord_Sort); // -
            // i++;
            il.Emit(OpCodes.Ldloc_1); // i
            il.Emit(OpCodes.Ldc_I4_1); // i, 1
            il.Emit(OpCodes.Add); // (i + 1)
            il.Emit(OpCodes.Dup); // (i + 1), (i + 1)
            il.Emit(OpCodes.Stloc_1); // new i [i := i + 1]
            // check i < n, brtrue label
            il.Emit(OpCodes.Ldloc_0); // i, n
            il.Emit(OpCodes.Clt); // (i < n)
            il.Emit(OpCodes.Brtrue, label);
            // конец do ... while ...
            // call this.r(result, startId, 0, a.Length, truepos)
            il.Emit(OpCodes.Ldarg_0); // #0
            // result [loc.4] = new Queue<int>()
            il.DeclareLocal(t_Queue_int);
            il.Emit(OpCodes.Newobj, ctor_Queue_int); // #0, result
            il.Emit(OpCodes.Dup); // #0, result, result
            il.Emit(OpCodes.Stloc, (short)4); // #0, result
            il.Emit(OpCodes.Ldloc_2); // #0, result, startId
            il.Emit(OpCodes.Ldc_I4_0); // #0, result, startId, 0
            il.Emit(OpCodes.Ldarg_1); // #0, result, startId, 0, a
            il.Emit(OpCodes.Ldlen); // #0, result, startId, 0, a.Length
            il.Emit(OpCodes.Ldarg_2); // #0, result, startId, 0, a.Length, truepos
            il.Emit(OpCodes.Call, r); // -
            // return result
            il.Emit(OpCodes.Ldloc, (short)4); // result
            il.Emit(OpCodes.Ret); // return result;
            //----------------------------------------------------------------------
            // addEndSituation implementation
            // #0 как всегда this, #1 - A, #2 - i, #3 - endRuleId, #4 - listId
            il = addEndSituation.GetILGenerator();
            // заведём локальную переменную для хранения кортежа (A, i) : (int, int)
            il.DeclareLocal(t_tuple_int_int);
            // и запишем туда это значение
            il.Emit(OpCodes.Ldloca, (short)0); // &loc.0
            il.Emit(OpCodes.Ldarg_1); // &loc.0, A
            il.Emit(OpCodes.Ldarg_2); // &loc.0, A, i
            il.Emit(OpCodes.Call, ctor_tuple_int_int); // - [loc.0 := (A, i)]
            // if(endSituations[listId].ContainsKey((A, i))) return
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, endSituations); // endSituations
            il.Emit(OpCodes.Ldarg, (short)4); // endSituations, listId
            il.Emit(OpCodes.Ldelem, t_endSituations_item); // endSituations[listId]
            il.Emit(OpCodes.Ldloc_0); // endSituations[listId], (A, i)
            il.Emit(OpCodes.Call, method_endSituations_item_ContainsKey); // endSituations[listId].CK((A, i))
            // здесь нужно завести метку для перепрыгивания ret
            label = il.DefineLabel();
            il.Emit(OpCodes.Brfalse, label); // -
            il.Emit(OpCodes.Ret); // - [if CK return]
            il.MarkLabel(label);
            // endSituations[listId].Add((A, i), endRuleId)
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, endSituations); // endSituations
            il.Emit(OpCodes.Ldarg, (short)4); // endSituations, listId
            il.Emit(OpCodes.Ldelem, t_endSituations_item); // endSituations[listId]
            il.Emit(OpCodes.Ldloc_0); // endSituations[listId], (A, i)
            il.Emit(OpCodes.Ldarg_3); // endSituations[listId], (A, i), endRuleId
            il.Emit(OpCodes.Call, method_endSituations_item_Add); // -
            // foreach (var (ruleId, pointLocation, second) in GetMiddleSituations(A, i))
            // разворачиваем в стандартный for (по i)
            // помним, что system[i].GetMiddleSituations(A) м.б. null
            // это сохраним в loc.1
            il.DeclareLocal(t_List_situation);
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldarg_1); // #0, A
            il.Emit(OpCodes.Ldarg_2); // #0, A, i
            il.Emit(OpCodes.Call, getMiddleSituations); // middleSituations[A][i]?
            il.Emit(OpCodes.Dup); // middleSituations[A][i]?, middleSituations[A][i]?
            il.Emit(OpCodes.Stloc_1); // middleSituations[A][i]?
            // if null return
            label = il.DefineLabel();
            il.Emit(OpCodes.Brtrue, label); // -
            il.Emit(OpCodes.Ret); // -
            il.MarkLabel(label);
            // Здесь loc.1 != null
            // итак, сам цикл
            // i будет loc.2
            il.DeclareLocal(t_int);
            // инициализация
            il.Emit(OpCodes.Ldc_I4_0); // 0
            il.Emit(OpCodes.Stloc_2); // - [i := 0]
            // проверка
            loopStart = il.DefineLabel();
            loopEnd = il.DefineLabel();
            il.MarkLabel(loopStart);
            il.Emit(OpCodes.Ldloc_2); // i
            il.Emit(OpCodes.Ldloc_1); // i, loc.1(middleSituations)
            il.Emit(OpCodes.Call, property_List_situation_Count); // i, Count
            il.Emit(OpCodes.Clt); // (i < Count)
            il.Emit(OpCodes.Brfalse, loopEnd); // -
            // основное тело
            // this.Add(loc.1[i].ruleId, loc.1[i].pointLocation + 1, loc.1[i].second, endRuleId, i, listId);
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldloc_1); // #0, loc.1
            il.Emit(OpCodes.Ldloc_2); // #0, loc.1, i
            il.Emit(OpCodes.Call, indexer_List_situation); // #0, loc.1[i]
            // значение loc.1[i] сохраним в loc.3
            il.DeclareLocal(t_situation);
            il.Emit(OpCodes.Stloc_3); // #0 [loc.3 = some situation]
            il.Emit(OpCodes.Ldloca, (short)3); // #0, &loc.3
            il.Emit(OpCodes.Ldfld, fld_situation_ruleId); // #0, ruleId
            il.Emit(OpCodes.Ldloca, (short)3); // #0, ruleId, &loc.3
            il.Emit(OpCodes.Ldfld, fld_situation_pointLocation); // #0, ruleId, pointLocation
            il.Emit(OpCodes.Ldc_I4_1); // #0, ruleId, pointLocation, 1
            il.Emit(OpCodes.Add); // #0, ruleId, (pointLocation + 1)
            il.Emit(OpCodes.Ldloca, (short)3); // #0, ruleId, (pointLocation + 1), &loc.3
            il.Emit(OpCodes.Ldfld, fld_situation_second); // #0, ruleId, (pointLocation + 1), second
            il.Emit(OpCodes.Ldarg_3); // #0, ruleId, (pointLocation + 1), second, endRuleId
            il.Emit(OpCodes.Ldarg_2); // #0, ruleId, (pointLocation + 1), second, endRuleId, i
            il.Emit(OpCodes.Ldarg, (short)4); // #0, ruleId, (pointLocation + 1), second, endRuleId, i, listId
            il.Emit(OpCodes.Call, add); // -
            // i++
            il.Emit(OpCodes.Ldloc_2); // i
            il.Emit(OpCodes.Ldc_I4_1); // i, 1
            il.Emit(OpCodes.Add); // (i + 1)
            il.Emit(OpCodes.Stloc_2); // - [i := i + 1]
            il.Emit(OpCodes.Br, loopStart); // -
            // конец цикла
            il.MarkLabel(loopEnd);
            // конец процедуры
            il.Emit(OpCodes.Ret);
            //----------------------------------------------------------
            // bool ExistsEndSituation(int A, int i, int listId)
            il = existsEndSituation.GetILGenerator();
            // => endSituations[listId].ContainsKey((A, i))
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, endSituations); // endSituations
            il.Emit(OpCodes.Ldarg_3); // endSituations, listId
            il.Emit(OpCodes.Ldelem, t_endSituations_item); // curr endSituations ($)
            // для инициализации кортежа нужно создать переменную
            // loc.0 - кортеж
            il.DeclareLocal(t_tuple_int_int);
            il.Emit(OpCodes.Ldloca, (short)0); // $, &loc.0
            il.Emit(OpCodes.Ldarg_1); // $, &loc.0, A
            il.Emit(OpCodes.Ldarg_2); // $, &loc.0, A, i
            il.Emit(OpCodes.Call, ctor_tuple_int_int); // $
            il.Emit(OpCodes.Ldloc_0); // $, (A, i)
            il.Emit(OpCodes.Call, method_endSituations_item_ContainsKey); // CK
            il.Emit(OpCodes.Ret); // return CK
            //----------------------------------------------------------------------------
            // void AddMiddleSituation(int ruleId(#1), int pointLocation(#2), int second(#3), int listId(#4))
            il = addMiddleSituations.GetILGenerator();
            // let loc.0 be A
            // int A = rulebodies[ruleId][pointLocation]
            il.DeclareLocal(t_int);
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, rulebodies); // rulebodies
            il.Emit(OpCodes.Ldarg_1); // rulebodies, ruleId
            il.Emit(OpCodes.Ldelem, t_array_int); // rulebodies[ruleId]
            il.Emit(OpCodes.Ldarg_2); // rulebodies[ruleId], pointLocation
            il.Emit(OpCodes.Ldelem, t_int); // rulebodies[ruleId][pointLocation]
            il.Emit(OpCodes.Stloc_0); // - [A := rule...]
            // if (terminal[A]) return;
            label = il.DefineLabel();
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, terminal); // terminal
            il.Emit(OpCodes.Ldloc_0); // terminal, A
            il.Emit(OpCodes.Ldelem, t_bool); // terminal[A]
            il.Emit(OpCodes.Brfalse, label); // -
            il.Emit(OpCodes.Ret); // - [when terminal[A]]
            il.MarkLabel(label);
            // if (!middleSituations[listId].ContainsKey(A)) middleSituations.Add(A, new List<(int, int, int)>());
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, middleSituations); // middleSituations
            il.Emit(OpCodes.Ldarg, (short)4); // middleSituations, listId
            il.Emit(OpCodes.Ldelem, t_middleSituations_item); // middleSituations[listId]
            il.Emit(OpCodes.Ldloc_0); // middleSituations[listId], A
            il.Emit(OpCodes.Call, method_middleSituations_item_ContainsKey); // CK
            label = il.DefineLabel();
            il.Emit(OpCodes.Brtrue, label); // -
            // middleSituations[listId].Add(A, new List<situation>);
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, middleSituations); // middleSituations
            il.Emit(OpCodes.Ldarg, (short)4); // ms, lid
            il.Emit(OpCodes.Ldelem, t_middleSituations_item); // ms[lid]
            il.Emit(OpCodes.Ldloc_0); // ms[lid], A
            il.Emit(OpCodes.Newobj, ctor_List_situation); // ms[lid], A, new List<situation>
            il.Emit(OpCodes.Call, method_middleSituations_item_Add); // -
            il.MarkLabel(label);
            // middleSituations[listId][A].Add((ruleId, pointLocation, second));
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, middleSituations); // middleSituations
            il.Emit(OpCodes.Ldarg, (short)4); // middleSituations, listId
            il.Emit(OpCodes.Ldelem, t_middleSituations_item); // ms[listId]
            il.Emit(OpCodes.Ldloc_0); // ms[lId], A
            il.Emit(OpCodes.Call, indexer_middleSituations_item); // ms[lId][A]
            // для постройки кортежа situation придётся завести новую локальную переменную loc.1
            il.DeclareLocal(t_situation);
            il.Emit(OpCodes.Ldloca, (short)1); // ms[lId][A], &loc.1
            il.Emit(OpCodes.Ldarg_1); // ms[lId][A], &loc.1, ruleId
            il.Emit(OpCodes.Ldarg_2); // ms[lId][A], &loc.1, ruleId, pointLocation
            il.Emit(OpCodes.Ldarg_3); // ms[lId][A], &loc.1, ruleId, pointLocation, second
            il.Emit(OpCodes.Call, ctor_situation); // ms[lId][A]
            il.Emit(OpCodes.Ldloc_1); // ms[lId][A], (ruleId, pointLocation, second)
            il.Emit(OpCodes.Call, method_List_situation_Add); // -
            //if (this.ExistsEndSituation(A, second, listId))
            //  this.Add(ruleId, pointLocation + 1, second, endSituations[listId][(A, second)], second, listId);
            label = il.DefineLabel();
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldloc_0); // #0, A
            il.Emit(OpCodes.Ldarg_3); // #0, A, second
            il.Emit(OpCodes.Ldarg, (short)4); // #0, A, second, listId
            il.Emit(OpCodes.Call, existsEndSituation); // <bool value>
            il.Emit(OpCodes.Brfalse, label);
            // this.Add ...
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldarg_1); // #0, ruleId
            il.Emit(OpCodes.Ldarg_2); // #0, ruleId, pointLocation
            il.Emit(OpCodes.Ldc_I4_1); // #0, ruleId, pointLocation, 1
            il.Emit(OpCodes.Add); // #0, ruleId, (pointLocation + 1)
            il.Emit(OpCodes.Ldarg_3); // #0, ruleId, (pointLocation + 1), second
            il.Emit(OpCodes.Ldarg_0); // #0, ruleId, (pl + 1), second, #0
            il.Emit(OpCodes.Ldfld, endSituations); // ..., endSituations
            il.Emit(OpCodes.Ldarg, (short)4); // ..., ends, listId
            il.Emit(OpCodes.Ldelem, t_endSituations_item); // ..., ends[listId]
            // для кортежа endSituation loc.2
            il.DeclareLocal(t_tuple_int_int);
            il.Emit(OpCodes.Ldloca, (short)2); // ..., ends[lid], &loc.2
            il.Emit(OpCodes.Ldloc_0); // ..., ends[lid], &loc.2, A
            il.Emit(OpCodes.Ldarg_3); // ..., ends[lid], &loc.2, A, second
            il.Emit(OpCodes.Call, ctor_tuple_int_int); // ..., ends[lid]
            il.Emit(OpCodes.Ldloc_2); // ..., ends[lid], (A, second)
            il.Emit(OpCodes.Call, indexer_endSituations_item); // #0, ruleId, (pointLocation + 1), second, ends-result, 
            il.Emit(OpCodes.Ldarg_3); // #0, ruleId, (pointLocation + 1), second, ends-result, second
            il.Emit(OpCodes.Ldarg, (short)4); // #0, ruleId, (pl + 1), second, ends-result, second, listId
            il.Emit(OpCodes.Call, add);
            // endif
            il.MarkLabel(label);
            // this.AddAllStartRules(A, listId)
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldloc_0); // #0, A
            il.Emit(OpCodes.Ldarg, (short)4); // #0, A, listId
            il.Emit(OpCodes.Call, addAllStartRules); // -
            // end of procedure
            il.Emit(OpCodes.Ret);
            //------------------------------------------------------
            // List<int> GetMiddleSituations(int A, int listId)
            il = getMiddleSituations.GetILGenerator();
            // if middleSituations[listId].CK(A) => middleSituations[listId][A]
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, middleSituations); // middleSituations
            il.Emit(OpCodes.Ldarg_2); // ms, lid
            il.Emit(OpCodes.Ldelem, t_middleSituations_item); // ms[lid]
            il.Emit(OpCodes.Ldarg_1); // ms[lid], A
            il.Emit(OpCodes.Call, method_middleSituations_item_ContainsKey); // CK
            label = il.DefineLabel();
            il.Emit(OpCodes.Brfalse, label); // -
            // here: CK == true
            // return ms[lid][A]
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, middleSituations); // middleSituations
            il.Emit(OpCodes.Ldarg_2); // ms, listId
            il.Emit(OpCodes.Ldelem, t_middleSituations_item); // ms[lid]
            il.Emit(OpCodes.Ldarg_1); // ms[lid], A
            il.Emit(OpCodes.Call, indexer_middleSituations_item); // result
            il.Emit(OpCodes.Ret); // return result
            il.MarkLabel(label);
            // here: CK == false
            // return null
            il.Emit(OpCodes.Ldnull); // null
            il.Emit(OpCodes.Ret); // return null
            //---------------------------------------------------------------------
            // void AddAllStartRules(int src, int listId)
            il = addAllStartRules.GetILGenerator();
            // if (startSources[listId].Contains(src)) return;
            label = il.DefineLabel();
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, startSources); // startSources
            il.Emit(OpCodes.Ldarg_2); // startSources, listId
            il.Emit(OpCodes.Ldelem, t_set_int); // ss[lid]
            il.Emit(OpCodes.Ldarg_1); // ss[lid], src
            il.Emit(OpCodes.Callvirt, method_set_int_Contains); // contains?
            // when false, continue
            il.Emit(OpCodes.Brfalse, label); // -
            il.Emit(OpCodes.Ret); // -
            il.MarkLabel(label);
            // here: not contains
            // startSources[listId].Add(src);
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, startSources); // startSources
            il.Emit(OpCodes.Ldarg_2); // ss, lid
            il.Emit(OpCodes.Ldelem, t_set_int); // ss[lid]
            il.Emit(OpCodes.Ldarg_1); // s[lid], src
            il.Emit(OpCodes.Call, method_set_int_Add); // true
            il.Emit(OpCodes.Pop); // -
            // foreach (var rId in sortedRuleIds[src])
            // сначала проверим существование ключа
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, sortedRuleIds); // sortedRuleIds
            il.Emit(OpCodes.Ldarg_1); // sri, src
            il.Emit(OpCodes.Call, method_sortedRuleIds_ContainsKey); // CK
            label = il.DefineLabel();
            il.Emit(OpCodes.Brtrue, label); // -
            // if not
            il.Emit(OpCodes.Ret); // -
            il.MarkLabel(label);
            // else:
            // сначала сохраним этот список-массив в loc.0
            il.DeclareLocal(t_array_int);
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, sortedRuleIds); // sortedRuleIds
            il.Emit(OpCodes.Ldarg_1); // sri, src
            il.Emit(OpCodes.Call, indexer_sortedRuleIds); // sri[src]
            il.Emit(OpCodes.Stloc_0); // - [loc.0 - rulelist]
            // организуем цикл типа for
            // i - loc.1
            il.DeclareLocal(t_int);
            il.Emit(OpCodes.Ldc_I4_0); // 0
            il.Emit(OpCodes.Stloc_1); // - [i := 0]
            loopStart = il.DefineLabel();
            loopEnd = il.DefineLabel();
            // проверка условия
            il.MarkLabel(loopStart);
            il.Emit(OpCodes.Ldloc_1); // i
            il.Emit(OpCodes.Ldloc_0); // i, list
            il.Emit(OpCodes.Ldlen); // i, list.Length
            il.Emit(OpCodes.Clt); // (i < list.Count)
            il.Emit(OpCodes.Brfalse, loopEnd); // -
            // основное тело цикла
            // this.Add(list[i], 0, listId, -1, -1, listId)
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldloc_0); // #0, list
            il.Emit(OpCodes.Ldloc_1); // #0, list, i
            il.Emit(OpCodes.Ldelem, t_int); // #0, list[i]
            il.Emit(OpCodes.Ldc_I4_0); // #0, list[i], 0
            il.Emit(OpCodes.Ldarg_2); // #0, list[i], 0, listId
            il.Emit(OpCodes.Ldc_I4_M1); // #0, list[i], 0, listId, -1
            il.Emit(OpCodes.Ldc_I4_M1); // #0, list[i], 0, listId, -1, -1
            il.Emit(OpCodes.Ldarg_2); // #0, list[i], 0, listId, -1, -1, listId
            il.Emit(OpCodes.Call, add); // -
            // i++
            il.Emit(OpCodes.Ldloc_1); // i
            il.Emit(OpCodes.Ldc_I4_1); // i, 1
            il.Emit(OpCodes.Add); // (i + 1)
            il.Emit(OpCodes.Stloc_1); // - [i := i + 1]
            il.Emit(OpCodes.Br, loopStart); // -
            // end of loop
            il.MarkLabel(loopEnd);
            // end of procedure
            il.Emit(OpCodes.Ret);
            //--------------------------------------------------------
            // void Add(int ruleId, int pointLocation, int second, int hA, int hi, int listId)
            il = add.GetILGenerator();
            // сразу заводим две переменные под ситуацию и историю
            il.DeclareLocal(t_situation);
            il.DeclareLocal(t_historyRecord);
            // create current situation: loc.0
            il.Emit(OpCodes.Ldloca, (short)0); // &loc.0
            il.Emit(OpCodes.Ldarg_1); // &loc.0, ruleId
            il.Emit(OpCodes.Ldarg_2); // &loc.0, ruleId, pointLocation
            il.Emit(OpCodes.Ldarg_3); // &loc.0, ruleId, pointLocation, second
            il.Emit(OpCodes.Call, ctor_situation); // -
            // check: return if situationsSet contains the element
            label = il.DefineLabel();
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, situationsSet); // sit-set
            il.Emit(OpCodes.Ldarg, (short)6); // sit-set, lid
            il.Emit(OpCodes.Ldelem, t_set_situation); // curr sit-set
            il.Emit(OpCodes.Ldloc_0); // curr sit-set, curr situation
            il.Emit(OpCodes.Callvirt, method_set_situation_Contains); // contains?
            il.Emit(OpCodes.Brfalse, label); // - [if not contains, continue]
            il.Emit(OpCodes.Ret); // -
            // ok, not contains: sit-set[lid].Add(loc.0)
            il.MarkLabel(label);
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, situationsSet); // sit-set
            il.Emit(OpCodes.Ldarg, (short)6); // sit-set, lid
            il.Emit(OpCodes.Ldelem, t_set_situation); // curr sit-set
            il.Emit(OpCodes.Ldloc_0); // curr sit-set, curr situation
            il.Emit(OpCodes.Call, method_set_situation_Add); // true
            il.Emit(OpCodes.Pop); // -
            // situations[listId].Add((ruleId, pointLocation, second));
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, situations); // situations
            il.Emit(OpCodes.Ldarg, (short)6); // situations, listId
            il.Emit(OpCodes.Ldelem, t_List_situation); // s[lid]
            il.Emit(OpCodes.Ldloc_0); // s[lid], loc.0
            il.Emit(OpCodes.Call, method_List_situation_Add); // -
            // history[listId].Add((ruleId, pointLocation, second, hA, hi));
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, history); // history
            il.Emit(OpCodes.Ldarg, (short)6); // history, listId
            il.Emit(OpCodes.Ldelem, t_List_historyRecord); // h[lid]
            il.Emit(OpCodes.Ldloca, (short)1); // h[lid], &loc.1
            il.Emit(OpCodes.Ldarg_1); // h[lid], &loc.1, ruleId
            il.Emit(OpCodes.Ldarg_2); // h[lid], &loc.1, ruleId, pointLocation
            il.Emit(OpCodes.Ldarg_3); // h[lid], &loc.1, ruleId, pointLocation, second
            il.Emit(OpCodes.Ldarg, (short)4); // h[lid], &loc.1, ruleId, pointLocation, second, hA
            il.Emit(OpCodes.Ldarg, (short)5); // h[lid], &loc.1, ruleId, pointLocation, second, hA, hi
            il.Emit(OpCodes.Call, ctor_historyRecord); // h[lid]
            il.Emit(OpCodes.Ldloc_1); // h[lid], loc.1
            il.Emit(OpCodes.Call, method_List_historyRecord_Add); // -
            // if rulebodies[ruleId].Length == pointLocation -> AddEndSituation
            // else AddMiddleSituation
            // для этого заведём метку
            label = il.DefineLabel();
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, rulebodies); // rulebodies
            il.Emit(OpCodes.Ldarg_1); //rulebodies, ruleId
            il.Emit(OpCodes.Ldelem, t_array_int); // rulebody
            il.Emit(OpCodes.Ldlen); // length
            il.Emit(OpCodes.Ldarg_2); // length, pointLocation
            il.Emit(OpCodes.Ceq); // (length == pointLocation)
            il.Emit(OpCodes.Brfalse, label); // -
            // when true
            // this.AddEndSituation(rulesources[ruleId], second, ruleId, listId);
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Dup); // #0, #0
            il.Emit(OpCodes.Ldfld, rulesources); // #0, rulesources
            il.Emit(OpCodes.Ldarg_1); // #0, rulesources, ruleId
            il.Emit(OpCodes.Ldelem, t_int); // #0, A
            il.Emit(OpCodes.Ldarg_3); // #0, A, second
            il.Emit(OpCodes.Ldarg_1); // #0, A, second, ruleId
            il.Emit(OpCodes.Ldarg, (short)6); // #0, A, second, ruleId, listId
            il.Emit(OpCodes.Call, addEndSituation); // -
            il.Emit(OpCodes.Ret); // - [дальше действий не будет]
            // when false
            il.MarkLabel(label);
            // this.AddMiddleSituation(ruleId, pointLocation, second, listId);
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldarg_1); // #0, ruleId
            il.Emit(OpCodes.Ldarg_2); // #0, ruleId, pointLocation
            il.Emit(OpCodes.Ldarg_3); // #0, ruleId, pointLocation, second
            il.Emit(OpCodes.Ldarg, (short)6); // #0, ruleId, pointLocation, second, listId
            il.Emit(OpCodes.Call, addMiddleSituations); // -
            il.Emit(OpCodes.Ret); // -
            //------------------------------------------------------------------------------------
            // (int, int) GetHistory(int ruleId, int pointLocation, int second, int listId)
            il = getHistory.GetILGenerator();
            // loc.0 := history[listId][situations[listId].BinarySearch((ruleId, pointLocation, second))]
            // type=t_historyRecord
            // loc.1 будет (ruleId, pointLocation, second)
            il.DeclareLocal(t_historyRecord);
            il.DeclareLocal(t_situation);
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, history); // history
            il.Emit(OpCodes.Ldarg, (short)4); // h, lid
            il.Emit(OpCodes.Ldelem, t_List_historyRecord); // h[lid]
            il.Emit(OpCodes.Ldarg_0); // h[lid], #0
            il.Emit(OpCodes.Ldfld, situations); // h[lid], s
            il.Emit(OpCodes.Ldarg, (short)4); // h[lid], s, lid
            il.Emit(OpCodes.Ldelem, t_List_situation); // h[lid], s[lid]
            il.Emit(OpCodes.Ldloca, (short)1); // h[lid], s[lid], &loc.1
            il.Emit(OpCodes.Ldarg_1); // h[lid], s[lid], &loc.1, ruleId
            il.Emit(OpCodes.Ldarg_2); // h[lid], s[lid], &loc.1, ruleId, pointLocation
            il.Emit(OpCodes.Ldarg_3); // h[lid], s[lid], &loc.1, ruleId, pointLocation, second
            il.Emit(OpCodes.Call, ctor_situation); // h[lid], s[lid]
            il.Emit(OpCodes.Ldloc_1); // h[lid], s[lid], loc.1
            il.Emit(OpCodes.Call, method_List_situation_BinarySearch); // h[lid], bs-index
            il.Emit(OpCodes.Call, indexer_List_historyRecord); // historyRecord
            il.Emit(OpCodes.Stloc_0); // - [loc.0 := hrecord]
            // return (item4, item5)
            il.DeclareLocal(t_tuple_int_int);
            il.Emit(OpCodes.Ldloca, (short)2); // &loc.2
            il.Emit(OpCodes.Ldloca, (short)0); // &loc.2, &loc.0
            il.Emit(OpCodes.Ldfld, fld_historyRecord_Item4); // &loc.2, item4
            il.Emit(OpCodes.Ldloca, (short)0); // &loc.2, item4, &loc.0
            il.Emit(OpCodes.Ldfld, fld_historyRecord_Item5); // &loc.2, item4, item5
            il.Emit(OpCodes.Call, ctor_tuple_int_int); // -
            il.Emit(OpCodes.Ldloc_2); // loc.2
            il.Emit(OpCodes.Ret); // return loc.2
            //----------------------------------------------------------------------------
            // void R(Queue<int> result, int rule, int i, int j, int[] truepos)
            il = r.GetILGenerator();
            // result.Enqueue(rule);
            il.Emit(OpCodes.Ldarg_1); // result
            il.Emit(OpCodes.Ldarg_2); // result, rule
            il.Emit(OpCodes.Call, method_Queue_int_Enqueue); // -
            // result.Enqueue(truepos[i])
            il.Emit(OpCodes.Ldarg_1); // result
            il.Emit(OpCodes.Ldarg, (short)5); // result, truepos
            il.Emit(OpCodes.Ldarg_3); // result, truepos, i
            il.Emit(OpCodes.Ldelem, t_int); // result, truepos[i]
            il.Emit(OpCodes.Call, method_Queue_int_Enqueue); // -
            // j будет выступать вместо l
            // for(int k = rulebodies[rule].Length - 1; k >= 0; k--) ...
            // k = loc.0
            il.DeclareLocal(t_int);
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, rulebodies); // rulebodies
            il.Emit(OpCodes.Ldarg_2); // rulebodies, rule
            il.Emit(OpCodes.Ldelem, t_array_int); // rb[rule]
            il.Emit(OpCodes.Ldlen); // rb[rule].Length
            il.Emit(OpCodes.Ldc_I4_M1); // length, -1
            il.Emit(OpCodes.Add); // (length - 1)
            il.Emit(OpCodes.Stloc_0); // - [k := length - 1]
            // цикл
            loopStart = il.DefineLabel();
            loopEnd = il.DefineLabel();
            continueLabel = il.DefineLabel(); // понадобится для организации ветвления
            label = il.DefineLabel(); // тоже для ветвления
            // проверка условия
            il.MarkLabel(loopStart);
            il.Emit(OpCodes.Ldloc_0); // k
            il.Emit(OpCodes.Ldc_I4_M1); // k, -1
            il.Emit(OpCodes.Cgt); // (k > -1)
            il.Emit(OpCodes.Brfalse, loopEnd); // -
            // тело цикла
            // if(terminal[rulebodies[rule][k]]) j--; else ...
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, terminal); // terminal
            il.Emit(OpCodes.Ldarg_0); // terminal, #0
            il.Emit(OpCodes.Ldfld, rulebodies); // terminal, rulebodies
            il.Emit(OpCodes.Ldarg_2); // terminal, rulebodies, rule
            il.Emit(OpCodes.Ldelem, t_array_int); // terminal, rb[rule]
            il.Emit(OpCodes.Ldloc_0); // terminal, rb[rule], k
            il.Emit(OpCodes.Ldelem, t_int); // terminal, rb[rule][k]
            il.Emit(OpCodes.Ldelem, t_bool); // terminal[rb[rule][k]]
            il.Emit(OpCodes.Brfalse, label); // - [false -> goto else]
            // here: is terminal
            // j--
            il.Emit(OpCodes.Ldarg, (short)4); // j
            il.Emit(OpCodes.Ldc_I4_M1); // j, -1
            il.Emit(OpCodes.Add); // (j - 1)
            il.Emit(OpCodes.Starg, (short)4); // - [j := j - 1]
            // result.Enqueue(-1);
            il.Emit(OpCodes.Ldarg_1); // result
            il.Emit(OpCodes.Ldc_I4_M1); // result, -1
            il.Emit(OpCodes.Call, method_Queue_int_Enqueue); // -
            // result.Enqueue(truepositions[j]);
            il.Emit(OpCodes.Ldarg_1); // result
            il.Emit(OpCodes.Ldarg, (short)5); // result, truepos
            il.Emit(OpCodes.Ldarg, (short)4); // result, truepos, j
            il.Emit(OpCodes.Ldelem, t_int); // result, truepos[j]
            il.Emit(OpCodes.Call, method_Queue_int_Enqueue); // -
            il.Emit(OpCodes.Br, continueLabel); // -
            // else {(endRuleId, r) = GetHistory(rule, k + 1, i, j); R(result, endRuleId, r, j); j = r;}
            il.MarkLabel(label);
            // для кортежа понадобиться завести отдельную переменную
            il.DeclareLocal(t_tuple_int_int); // + loc.1
            // loc.1 := this.GetHistory(rule, k + 1, i, j)
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldarg_2); // #0, rule
            il.Emit(OpCodes.Ldloc_0); // #0, rule, k
            il.Emit(OpCodes.Ldc_I4_1); // #0, rule, k, 1
            il.Emit(OpCodes.Add); // #0, rule, (k + 1)
            il.Emit(OpCodes.Ldarg_3); // #0, rule, (k + 1), i
            il.Emit(OpCodes.Ldarg, (short)4); // #0, rule, (k + 1), i, j
            il.Emit(OpCodes.Call, getHistory); // (endRuleId, r)
            il.Emit(OpCodes.Stloc_1); // - [loc.1 := (endRuleId, r)]
            // this.R(result, endRuleId, r, j, truepos);
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldarg_1); // #0, result
            il.Emit(OpCodes.Ldloca, (short)1); // #0, result, &loc.1
            il.Emit(OpCodes.Ldfld, fld_tuple_int_int_Item1); // #0, result, endRuleId
            il.Emit(OpCodes.Ldloca, (short)1); // #0, result, endRuleId, &loc.1
            il.Emit(OpCodes.Ldfld, fld_tuple_int_int_Item2); // #0, result, endRuleId, r
            il.Emit(OpCodes.Ldarg, (short)4); // #0, result, endRuleId, r, j
            il.Emit(OpCodes.Ldarg, (short)5);
            il.Emit(OpCodes.Call, r); // -
            // j = r
            il.Emit(OpCodes.Ldloca, (short)1); // &loc.1
            il.Emit(OpCodes.Ldfld, fld_tuple_int_int_Item2); // r
            il.Emit(OpCodes.Starg, (short)4); // - [j := r]
            // цикл: k--
            il.MarkLabel(continueLabel);
            il.Emit(OpCodes.Ldloc_0); // k
            il.Emit(OpCodes.Ldc_I4_M1); // k, -1
            il.Emit(OpCodes.Add); // (k - 1)
            il.Emit(OpCodes.Stloc_0); // - [k := k - 1]
            il.Emit(OpCodes.Br, loopStart); // -
            // конец цикла
            il.MarkLabel(loopEnd);
            // конец процедуры
            il.Emit(OpCodes.Ret);
            //---------------------------------------------------------------------
            // Exception MakeError(int aLen, int[] truepos)
            il = makeError.GetILGenerator();
            // loc.0 - SortedSet<int> used
            il.DeclareLocal(t_set_int);
            il.Emit(OpCodes.Newobj, ctor_set_int); // set
            il.Emit(OpCodes.Stloc_0); // -
            // loc.1 - bool eofexpected := false
            il.DeclareLocal(t_bool);
            il.Emit(OpCodes.Ldc_I4_0); // false
            il.Emit(OpCodes.Stloc_1); // -
            // loc.2 - int j := a.Length
            il.DeclareLocal(t_int);
            il.Emit(OpCodes.Ldarg_1); // aLen
            il.Emit(OpCodes.Stloc_2); // -
            // loc.3 - int i - переменная внутреннего цикла
            il.DeclareLocal(t_int);
            // loc.4 - curr situation
            il.DeclareLocal(t_situation);
            // цикл for(int j = aLen; j >= 0; j--)
            // инициализацию провели
            // проверка
            loopStart = il.DefineLabel();
            loopEnd = il.DefineLabel();
            il.MarkLabel(loopStart);
            il.Emit(OpCodes.Ldloc_2); // j
            il.Emit(OpCodes.Ldc_I4_0); // 0
            il.Emit(OpCodes.Clt); // (j < 0)
            il.Emit(OpCodes.Brtrue, loopEnd);
            // тело
            // внутренний цикл
            // foreach(var curr situation in situations[j])
            // инициализация
            il.Emit(OpCodes.Ldc_I4_0); // 0
            il.Emit(OpCodes.Stloc_3); // - [i := 0]
            // проверка
            startInnerLoop = il.DefineLabel();
            endInnerLoop = il.DefineLabel();
            il.MarkLabel(startInnerLoop);
            il.Emit(OpCodes.Ldloc_3); // i
            il.Emit(OpCodes.Ldarg_0); // i, #0
            il.Emit(OpCodes.Ldfld, situations); // i, situations
            il.Emit(OpCodes.Ldloc_2); // i, situations, j
            il.Emit(OpCodes.Ldelem, t_List_situation); // i, situations[j]
            il.Emit(OpCodes.Call, property_List_situation_Count); // i, count
            il.Emit(OpCodes.Clt); // (i < count)
            il.Emit(OpCodes.Brfalse, endInnerLoop); // -
            // тело
            label = il.DefineLabel();
            // if (pointLocation < rb[ruleId].Length && terminal[rb[ruleId][pointLocation]])
            // как всегда разбиваем на две проверки
            // но сначала в loc.4 надо сохранить текущую ситуацию
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, situations); // situations
            il.Emit(OpCodes.Ldloc_2); // situations, j
            il.Emit(OpCodes.Ldelem, t_List_situation); // sit[j]
            il.Emit(OpCodes.Ldloc_3); // sit[j], i
            il.Emit(OpCodes.Call, indexer_List_situation); // sit[j][i]
            il.Emit(OpCodes.Stloc, (short)4); // -
            // проверка 1
            il.Emit(OpCodes.Ldloca, (short)4); // &loc.4
            il.Emit(OpCodes.Ldfld, fld_situation_pointLocation); // pl
            il.Emit(OpCodes.Ldarg_0); // pl, #0
            il.Emit(OpCodes.Ldfld, rulebodies); // pl, rb
            il.Emit(OpCodes.Ldloca, (short)4); // pl, rb, &loc.4
            il.Emit(OpCodes.Ldfld, fld_situation_ruleId); // pl, rb, rid
            il.Emit(OpCodes.Ldelem, t_array_int); // pl, rb[rid]
            il.Emit(OpCodes.Ldlen); // pl, rb[rid].Len
            il.Emit(OpCodes.Clt); // (pl < rb[rid].len)
            il.Emit(OpCodes.Brfalse, label);
            // проверка 2
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, terminal); // term
            il.Emit(OpCodes.Ldarg_0); // term, #0
            il.Emit(OpCodes.Ldfld, rulebodies); // term, rb
            il.Emit(OpCodes.Ldloca, (short)4); // term, rb, &loc.4
            il.Emit(OpCodes.Ldfld, fld_situation_ruleId); // term, rb, rid
            il.Emit(OpCodes.Ldelem, t_array_int); // term, rb[rid]
            il.Emit(OpCodes.Ldloca, (short)4); // term, rb[rid], &loc.4
            il.Emit(OpCodes.Ldfld, fld_situation_pointLocation); // term, rb[rid], pl
            il.Emit(OpCodes.Ldelem, t_int); // term, rb[rid][pl]
            il.Emit(OpCodes.Ldelem, t_bool); // term[rb[rid][pl]]
            il.Emit(OpCodes.Brfalse, label); // -
            // here: проверки пройдены
            // problems.Add(rb[ruleId][pointLocation]);
            il.Emit(OpCodes.Ldloc_0); // problems
            il.Emit(OpCodes.Ldarg_0); // problems, #0
            il.Emit(OpCodes.Ldfld, rulebodies); // problems, rb
            il.Emit(OpCodes.Ldloca, (short)4); // problems, rb, &loc.4
            il.Emit(OpCodes.Ldfld, fld_situation_ruleId); // problems, rb, rid
            il.Emit(OpCodes.Ldelem, t_array_int); // problems, rb[rid]
            il.Emit(OpCodes.Ldloca, (short)4); // problems, rb[rid], &loc.4
            il.Emit(OpCodes.Ldfld, fld_situation_pointLocation); // problems, rb[rid], pl
            il.Emit(OpCodes.Ldelem, t_int); // problems, rb[rid][pl]
            il.Emit(OpCodes.Call, method_set_int_Add); // Add_result
            il.Emit(OpCodes.Pop); // -
            // end
            il.MarkLabel(label);
            // if (rulesources[rid] == start && pointLocation == rb[rid].len && second == 0)
            label = il.DefineLabel();
            // три проверки
            // проверка 1
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, rulesources); // rs
            il.Emit(OpCodes.Ldloca, (short)4); // rs, &loc.4
            il.Emit(OpCodes.Ldfld, fld_situation_ruleId); // rs, ruleId
            il.Emit(OpCodes.Ldelem, t_int); // rs[rid]
            il.Emit(OpCodes.Ldarg_0); // rs[rid], #0
            il.Emit(OpCodes.Ldfld, start); // rs[rid], start
            il.Emit(OpCodes.Ceq); // (rs[rid] == start)
            il.Emit(OpCodes.Brfalse, label); // -
            // проверка 2
            il.Emit(OpCodes.Ldloca, (short)4); // &loc.4
            il.Emit(OpCodes.Ldfld, fld_situation_pointLocation); // pl
            il.Emit(OpCodes.Ldarg_0); // pl, #0
            il.Emit(OpCodes.Ldfld, rulebodies); // pl, rb
            il.Emit(OpCodes.Ldloca, (short)4); // pl, rb, &loc.4
            il.Emit(OpCodes.Ldfld, fld_situation_ruleId); // pl, rb, rid
            il.Emit(OpCodes.Ldelem, t_array_int); // pl, rb[rid]
            il.Emit(OpCodes.Ldlen); // pl, len
            il.Emit(OpCodes.Ceq); // (pl == len)
            il.Emit(OpCodes.Brfalse, label); // -
            // проверка 3
            il.Emit(OpCodes.Ldloca, (short)4); // &loc.4
            il.Emit(OpCodes.Ldfld, fld_situation_second); // second
            il.Emit(OpCodes.Brtrue, label); // -
            // here: проверки пройдены
            // eofexpected = true;
            il.Emit(OpCodes.Ldc_I4_1); // true
            il.Emit(OpCodes.Stloc_1); // - [eofexp := true]
            // end
            il.MarkLabel(label);
            // i++
            il.Emit(OpCodes.Ldloc_3); // i
            il.Emit(OpCodes.Ldc_I4_1); // i, 1
            il.Emit(OpCodes.Add); // (i + 1)
            il.Emit(OpCodes.Stloc_3); // - [i := i + 1]
            il.Emit(OpCodes.Br, startInnerLoop); // -
            // end of inner loop
            il.MarkLabel(endInnerLoop);
            // if (problems.Count > 0)
            label = il.DefineLabel();
            il.Emit(OpCodes.Ldloc_0); // problems
            il.Emit(OpCodes.Call, property_set_int_Count); // count
            il.Emit(OpCodes.Ldc_I4_0); // count, 0
            il.Emit(OpCodes.Cgt); // (count > 0)
            il.Emit(OpCodes.Brfalse, label);
            // here: ok
            // создадим массив string[] для исключения без локальной переменной
            il.Emit(OpCodes.Ldloc_0); // problems
            il.Emit(OpCodes.Call, property_set_int_Count); // count
            il.Emit(OpCodes.Newarr, t_string); // new string[count]
            // устроим цикл do ... while problems.Count > 0; при этом с i++
            // инициализация
            il.Emit(OpCodes.Ldc_I4_0); // arr, 0
            il.Emit(OpCodes.Stloc_3); // arr [i := 0]
            continueLabel = il.DefineLabel();
            il.MarkLabel(continueLabel);
            // тело цикла - arr[i] := symbolNames[problems.Min]; problems.Remove(Min)
            il.Emit(OpCodes.Dup); // arr, arr
            il.Emit(OpCodes.Ldloc_3); // arr, arr, i
            il.Emit(OpCodes.Ldarg_0); // arr, arr, i, #0
            il.Emit(OpCodes.Ldfld, symbolNames); // arr, arr, i, sn
            il.Emit(OpCodes.Ldloc_0); // arr, arr, i, sn, problems
            il.Emit(OpCodes.Call, property_set_int_Min); // arr, arr, i, sn, min
            il.Emit(OpCodes.Ldelem, t_string); // arr, arr, i, minstr
            il.Emit(OpCodes.Stelem, t_string); // arr
            il.Emit(OpCodes.Ldloc_0); // arr, problems
            il.Emit(OpCodes.Dup); // arr, problems, problems
            il.Emit(OpCodes.Call, property_set_int_Min); // arr, problems, Min
            il.Emit(OpCodes.Call, method_set_int_Remove); // arr, Remove_res
            il.Emit(OpCodes.Pop); // arr
            // i++, goto? label
            il.Emit(OpCodes.Ldloc_3); // arr, i
            il.Emit(OpCodes.Ldc_I4_1); // arr, i, 1
            il.Emit(OpCodes.Add); // arr, (i + 1)
            il.Emit(OpCodes.Stloc_3); // arr [i := i + 1]
            il.Emit(OpCodes.Ldloc_0); // arr, problems
            il.Emit(OpCodes.Call, property_set_int_Count); // arr, count
            il.Emit(OpCodes.Ldc_I4_0); // arr, count, 0
            il.Emit(OpCodes.Cgt); // arr, (count > 0)
            il.Emit(OpCodes.Brtrue, continueLabel); // arr
            // теперь осталось truepos[j]
            il.Emit(OpCodes.Ldarg_2); // arr, truepos
            il.Emit(OpCodes.Ldloc_2); // arr, truepos, j
            il.Emit(OpCodes.Ldelem, t_int); // arr, pos
            il.Emit(OpCodes.Newobj, ctor_TkExpEx); // ex
            il.Emit(OpCodes.Ret);
            // here: no problems
            il.MarkLabel(label);
            // if (eofexpected)
            label = il.DefineLabel();
            il.Emit(OpCodes.Ldloc_1); // eofexp
            il.Emit(OpCodes.Brfalse, label); // -
            // here: eofexp == true
            // return new ExpectedEOFException(truepositions[j]);
            il.Emit(OpCodes.Ldarg_2); // truepos
            il.Emit(OpCodes.Ldloc_2); // truepos, j
            il.Emit(OpCodes.Ldelem, t_int); // truepos[j]
            il.Emit(OpCodes.Newobj, ctor_EOFExpEx); // ex
            il.Emit(OpCodes.Ret);
            // here: not
            il.MarkLabel(label);
            // j--; continue
            il.Emit(OpCodes.Ldloc_2); // j
            il.Emit(OpCodes.Ldc_I4_M1); // j, -1
            il.Emit(OpCodes.Add); // (j - 1)
            il.Emit(OpCodes.Stloc_2); // - [j := j - 1]
            il.Emit(OpCodes.Br, loopStart);
            // end of loop
            il.MarkLabel(loopEnd);
            // если дошло до сюда, есть внутренняя ошибка
            // throw new Exception("Internal error in MakeError() method");
            il.Emit(OpCodes.Ldstr, "Internal error in MakeError() method"); // str
            il.Emit(OpCodes.Newobj, ctor_Exception); // ex
            il.Emit(OpCodes.Throw);
            //---------------------------------------------------------------------
            // формирование процедур класса завершено, осталось задать публичные члены
            t_Earley = t.CreateType();
            ctor_Earley = constructor;
            method_earley_GetRightAnalysis = getRightAnalysis;
            fld_earley_rulebodies = rulebodies;
            fld_earley_rulesources = rulesources;
            fld_earley_symbolNames = symbolNames;
            fld_earley_Start = start;
        }

        // класс ParsingTree
        // на сей раз публичный для сборки
        // от оригинального класса ParsingTree мы получаем:
        // Kind : string (ПОЛЕ!!!)
        // TokenValue : string (ПОЛЕ!!!)
        // Children : ParsingTree[] (ПОЛЕ!!!)
        // метод bool IsLeaf() => !(Children is object)
        // два конструктора
        // 1) kind, tokenValue
        // 2) kind, children
        // private (единственный) View(int)
        // перегрузка ToString()
        private static void MakeParsingTreeClass(ModuleBuilder module)
        {
            TypeBuilder t = module.DefineType("Parsing.ParsingTree", attr_class | attr_public);
            // тип массива
            Type array_t = t.MakeArrayType();
            // поля
            FieldBuilder kind = t.DefineField("Kind", t_string, attr_fld_public | attr_fld_readonly);
            FieldBuilder tokenValue = t.DefineField("TokenValue", t_string, attr_fld_public | attr_fld_readonly);
            FieldBuilder children = t.DefineField("Children", array_t, attr_fld_public | attr_fld_readonly);
            FieldBuilder position = t.DefineField("Position", t_int, attr_fld_public | attr_fld_readonly);
            // конструкторы
            // 1) (kind, tokenValue)
            ConstructorBuilder ctor_leaf = t.DefineConstructor(attr_method_public, convention_class, new Type[] { t_string, t_string, t_int });
            ctor_leaf.DefineParameter(1, ParameterAttributes.None, "kind");
            ctor_leaf.DefineParameter(2, ParameterAttributes.None, "tokenValue");
            ctor_leaf.DefineParameter(3, ParameterAttributes.None, "position");
            ILGenerator il = ctor_leaf.GetILGenerator();
            // call base()
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Call, ctor_object); // -
            // Kind := kind
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldarg_1); // #0, kind
            il.Emit(OpCodes.Stfld, kind); // -
            // TokenValue := tokenValue
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldarg_2); // #0, tokenValue
            il.Emit(OpCodes.Stfld, tokenValue); // -
            // Children := null
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldnull); // #0, null
            il.Emit(OpCodes.Stfld, children); // -
            // Position := position
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldarg_3); // #0, pos
            il.Emit(OpCodes.Stfld, position); // -
            // end of procedure
            il.Emit(OpCodes.Ret);
            //--------------------------------------------------
            // 2) (kind, children)
            ConstructorBuilder ctor_notleaf = t.DefineConstructor(attr_method_public, convention_class, new Type[] { t_string, array_t, t_int });
            ctor_notleaf.DefineParameter(1, ParameterAttributes.None, "kind");
            ctor_notleaf.DefineParameter(2, ParameterAttributes.None, "children");
            ctor_notleaf.DefineParameter(3, ParameterAttributes.None, "position");
            il = ctor_notleaf.GetILGenerator();
            // call base()
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Call, ctor_object); // -
            // Kind := kind
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldarg_1); // #0, kind
            il.Emit(OpCodes.Stfld, kind); // -
            // TokenValue := null
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldnull); // #0, null
            il.Emit(OpCodes.Stfld, tokenValue); // -
            // Children := children
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldarg_2); // #0, children
            il.Emit(OpCodes.Stfld, children); // -
            // Position := position
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldarg_3); // #0, pos
            il.Emit(OpCodes.Stfld, position); // -
            // end of procedure
            il.Emit(OpCodes.Ret);
            //-------------------------------------------
            MethodBuilder isLeaf = t.DefineMethod("IsLeaf", attr_method_public, t_bool, Type.EmptyTypes);
            il = isLeaf.GetILGenerator();
            // => Children is object => true; otherwise => false
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, children); // children
            Label label = il.DefineLabel();
            il.Emit(OpCodes.Brfalse, label); // -
            // here : is object -> not leaf
            il.Emit(OpCodes.Ldc_I4_0); // false
            il.Emit(OpCodes.Ret); // return false
            il.MarkLabel(label);
            // here : isn't object -> leaf
            il.Emit(OpCodes.Ldc_I4_1); // true
            il.Emit(OpCodes.Ret); // return true
            //------------------------------------------------------
            // string View(int tabs)
            MethodBuilder view = t.DefineMethod("f", attr_method_private, t_string, new Type[] { t_int });
            il = view.GetILGenerator();
            // loc.0 - tabs
            il.DeclareLocal(t_string);
            il.Emit(OpCodes.Ldc_I4, (int)'\t'); // \t
            il.Emit(OpCodes.Ldarg_1); // \t, tabs
            il.Emit(OpCodes.Newobj, ctor_string); // tab
            il.Emit(OpCodes.Stloc_0); // -
            // if (isLeaf()) return tab + string.Format("leaf {0} value={1}", Kind, TokenValue)
            // для этого метка:
            label = il.DefineLabel();
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Call, isLeaf); // <is leaf>
            il.Emit(OpCodes.Brfalse, label); // -
            // here: is leaf
            il.Emit(OpCodes.Ldloc_0); // tab
            il.Emit(OpCodes.Ldstr, "leaf {0} position={1} value={2}"); // tab, formatstr
            il.Emit(OpCodes.Ldarg_0); // tab, formatstr, #0
            il.Emit(OpCodes.Ldfld, kind); // tab, formatstr, kind
            il.Emit(OpCodes.Ldarg_0); // tab, fs, kind, #0
            il.Emit(OpCodes.Ldfld, position); // tab, fs, kind, pos
            il.Emit(OpCodes.Box, t_int); // tab, fs, kind, pos_boxed
            il.Emit(OpCodes.Ldarg_0); // tab, formatstr, kind, pos_boxed, #0
            il.Emit(OpCodes.Ldfld, tokenValue); // tab, formatstr, kind, pos_boxed, tokenValue
            il.Emit(OpCodes.Call, method_string_Format3); // tab, formatres
            il.Emit(OpCodes.Call, method_string_Concat); // result
            il.Emit(OpCodes.Ret); // return result
            il.MarkLabel(label);
            // here : not leaf
            // create StringBuilder (no loc.)
            il.Emit(OpCodes.Newobj, ctor_StringBuilder); // sb
            // append tab
            il.Emit(OpCodes.Ldloc_0); // sb, tabs
            // NB! Append возвращает this : StringBuilder
            il.Emit(OpCodes.Call, method_StringBuilder_Append); // sb
            // append "node "
            il.Emit(OpCodes.Ldstr, "node "); // sb, conststr
            il.Emit(OpCodes.Call, method_StringBuilder_Append); // sb
            // append Kind
            il.Emit(OpCodes.Ldarg_0); // sb, #0
            il.Emit(OpCodes.Ldfld, kind); // sb, Kind
            il.Emit(OpCodes.Call, method_StringBuilder_Append); // sb
            // append " position= "
            il.Emit(OpCodes.Ldstr, " position= "); // sb, conststr
            il.Emit(OpCodes.Call, method_StringBuilder_Append); // sb
            // append position
            il.Emit(OpCodes.Ldarg_0); // sb, #0
            il.Emit(OpCodes.Ldfld, position); // sb, pos
            il.Emit(OpCodes.Call, method_StringBuilder_AppendInt); // sb
            // append " children={\n"
            il.Emit(OpCodes.Ldstr, " children={\n"); // sb, conststr
            il.Emit(OpCodes.Call, method_StringBuilder_Append); // sb
            // foreach (var c in Children) ...
            // convert to while with i
            // i - loc.1
            il.DeclareLocal(t_int);
            il.Emit(OpCodes.Ldc_I4_0); // sb, 0
            il.Emit(OpCodes.Stloc_1); // sb [i := 0]
            // проверка
            Label loopStart = il.DefineLabel();
            Label loopEnd = il.DefineLabel();
            il.MarkLabel(loopStart);
            il.Emit(OpCodes.Ldloc_1); // sb, i
            il.Emit(OpCodes.Ldarg_0); // sb, i, #0
            il.Emit(OpCodes.Ldfld, children); // sb, i, children
            il.Emit(OpCodes.Ldlen); // sb, i, length
            il.Emit(OpCodes.Clt); // sb, (i < length)
            il.Emit(OpCodes.Brfalse, loopEnd); // sb
            // основное тело
            // sb.Append(children[i].View(tabs(#1) + 1))
            il.Emit(OpCodes.Ldarg_0); // sb, #0
            il.Emit(OpCodes.Ldfld, children); // sb, children
            il.Emit(OpCodes.Ldloc_1); // sb, children, i
            il.Emit(OpCodes.Ldelem, t); // sb, child
            il.Emit(OpCodes.Ldarg_1); // sb, child, #1
            il.Emit(OpCodes.Ldc_I4_1); // sb, child, #1, 1
            il.Emit(OpCodes.Add); // sb, child, (#1 + 1)
            il.Emit(OpCodes.Call, view); // sb, view_result
            il.Emit(OpCodes.Call, method_StringBuilder_Append); // sb
            // append "\n"
            il.Emit(OpCodes.Ldstr, "\n"); // sb, "\n"
            il.Emit(OpCodes.Call, method_StringBuilder_Append); // sb
            // i++
            il.Emit(OpCodes.Ldloc_1); // sb, i
            il.Emit(OpCodes.Ldc_I4_1); // sb, i, 1
            il.Emit(OpCodes.Add); // sb, (i + 1)
            il.Emit(OpCodes.Stloc_1); // sb [i := i + 1]
            il.Emit(OpCodes.Br, loopStart); // sb
            // end of loop
            il.MarkLabel(loopEnd);
            // append tab
            il.Emit(OpCodes.Ldloc_0); // sb, tab
            il.Emit(OpCodes.Call, method_StringBuilder_Append); // sb
            // append "}"
            il.Emit(OpCodes.Ldstr, "}"); // sb, conststr
            il.Emit(OpCodes.Call, method_StringBuilder_Append); // sb
            // return sb.ToStrig()
            il.Emit(OpCodes.Call, method_StringBuilder_ToString); // sb.str
            il.Emit(OpCodes.Ret); // return sb.str
            //---------------------------------------------------------------
            MethodBuilder toString = t.DefineMethod("ToString", attr_method_public | attr_method_virtual, t_string, Type.EmptyTypes);
            il = toString.GetILGenerator();
            // return this.View(0)
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldc_I4_0); // #0, 0
            il.Emit(OpCodes.Call, view); // result
            il.Emit(OpCodes.Ret); // return result
            //-------------------------------------------
            t_ParsingTree = t.CreateType();
            // из всех публичных членов этого класса нам здесь понадобятся только конструкторы
            ctor_ParsingTree_Leaf = ctor_leaf;
            ctor_ParsingTree_NotLeaf = ctor_notleaf;
        }

        // генератор основного класса, которого нет в интепретаторе: Parser
        // он является классом-менеджером всего процесса парсинга
        // члены:
        // private FSM[] fsms - автоматы-лексические анализаторы
        // private int[] fsmTokens - номера символов, соответствующих автоматам fsms
        // private Earley earley - синтаксический анализатор
        // private symbolNames : string[] - имена символов из класса Grammar
        // public конструктор без параметров, где загружаются все данные автомата и грамматики
        // приватный вспомогательный метод buildTree(stack tokens, queue ruleIds)
        // public ParsingTree GetTree(string s) - весь процесс построения дерева разбора
        private static void MakeParserClass(ModuleBuilder module, FSMFactory[] fsmfactories, int[] fsmtokenIds, Grammar g)
        {
            TypeBuilder t = module.DefineType("Parsing.Parser", attr_class | attr_public);

            void readChar(ILGenerator gen)
            {
                //gen.Emit(OpCodes.Ldc_I4_0); // s 0
                //gen.Emit(OpCodes.Call, typeof(string).GetMethod("get_Chars")); // c
                gen.Emit(OpCodes.Call, typeof(int).GetMethod("Parse", new Type[] { typeof(string) })); // i
            }

            void readInt(ILGenerator gen)
            {
                gen.Emit(OpCodes.Call, typeof(int).GetMethod("Parse", new Type[] { typeof(string)})); // i
            }

            // serialization methods
            var m_de_ia = SerializationCompiler.EmitArrayDeserialization<int>(t);
            var m_de_iaa = SerializationCompiler.EmitIntAADeserialization(t, m_de_ia);
            var m_de_dict_c = SerializationCompiler.EmitDictDeserialization<char, int[][]>(t, m_de_iaa, readChar);
            var m_de_dict_i = SerializationCompiler.EmitDictDeserialization<int, int[]>(t, m_de_ia, readInt);
            var m_de_set = SerializationCompiler.EmitSetDeserialization(t);
            var m_de_ib = SerializationCompiler.EmitArrayDeserialization<bool>(t);
            var m_de_sa = SerializationCompiler.EmitStringArrayDeserialization(t);

            // поля
            FieldBuilder fsms = t.DefineField("f1", t_array_FSM, attr_fld_private);
            FieldBuilder fsmTokens = t.DefineField("f2", t_array_int, attr_fld_private);
            FieldBuilder earley = t.DefineField("f3", t_Earley, attr_fld_private);
            // конструктор
            ConstructorBuilder ctor = t.DefineConstructor(attr_method_public, convention_class, Type.EmptyTypes);
            ILGenerator il = ctor.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, ctor_object);
            // здесь будет происходить только инициализация полей и загрузка данных
            // для этого будут также использоваться методы классов FSMFactory и Grammar
            // 1) fsms
            // конструктор FSM требует transition и finals
            // оба параметра загружаются с класса FSMFactory
            il.Emit(OpCodes.Ldarg_0); // #0
            // создание массива
            il.Emit(OpCodes.Ldc_I4, fsmfactories.Length); // #0, len
            il.Emit(OpCodes.Newarr, t_FSM); // #0, fsms
            for(int i = 0; i < fsmfactories.Length; i++)
            {
                il.Emit(OpCodes.Dup); // #0, fsms, fsms
                il.Emit(OpCodes.Ldc_I4, i); // #0, fsms, fsms, i
                fsmfactories[i].EmitTransition(il, m_de_dict_c); // #0, fsms, fsms, i, transition
                fsmfactories[i].EmitFinals(il, m_de_set); // #0, fsms, fsms, i, transition, finals
                il.Emit(OpCodes.Newobj, ctor_fsm); // #0, fsms, fsms, i, fsm
                il.Emit(OpCodes.Stelem, t_FSM); // #0, fsms
            }
            il.Emit(OpCodes.Stfld, fsms); // -
            // 2) fsmTokens
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldc_I4, fsmtokenIds.Length); // #0, len
            il.Emit(OpCodes.Newarr, t_int); // #0, fsmTokens
            for (int i = 0; i < fsmtokenIds.Length; i++)
            {
                il.Emit(OpCodes.Dup); // #0, fsmTokens, fsmTokens
                il.Emit(OpCodes.Ldc_I4, i); // #0, fsmTokens, fsmTokens, i
                il.Emit(OpCodes.Ldc_I4, fsmtokenIds[i]); // #0, fsmTokens, fsmTokens, i, e
                il.Emit(OpCodes.Stelem, t_int); // #0, fsmTokens
            }
            il.Emit(OpCodes.Stfld, fsmTokens); // -
            // 3) earley
            // конструктор требует terminal, rulesources, rulebodies, symbolNames, sortedRuleIds, start
            il.Emit(OpCodes.Ldarg_0); // #0
            g.EmitTerminal(il, m_de_ib);
            g.EmitRuleSources(il, m_de_ia);
            g.EmitRuleBodies(il, m_de_iaa);
            g.EmitSymbolNames(il, m_de_sa);
            g.EmitSortedRuleIds(il, m_de_dict_i);
            g.EmitStart(il);
            il.Emit(OpCodes.Newobj, ctor_Earley); // #0, earley
            il.Emit(OpCodes.Stfld, earley); // -
            // все поля инициализированы
            il.Emit(OpCodes.Ret);
            //-----------------------------------------------
            // вспомогательный приватный метод ParsingTree buildTree(Stack<string> tokens, Queue<int> ruleIds, int sym)
            MethodBuilder buildTree = t.DefineMethod("h", attr_method_private, t_ParsingTree, new Type[] { t_Stack_string, t_Queue_int, t_int });
            il = buildTree.GetILGenerator();
            // сохраним ruleId из ruleIds в loc.0
            il.DeclareLocal(t_int);
            il.Emit(OpCodes.Ldarg_2); // ruleIds
            il.Emit(OpCodes.Call, method_Queue_int_Dequeue); // ruleId
            il.Emit(OpCodes.Stloc_0); // - [loc.0 := ruleId]
            // сохраним pos в loc.1
            il.DeclareLocal(t_int);
            il.Emit(OpCodes.Ldarg_2); // ruleIds
            il.Emit(OpCodes.Call, method_Queue_int_Dequeue); // pos
            il.Emit(OpCodes.Stloc_1); // -
            // if ruleId == -1 -> new leaf(symbolNames[sym], tokens.Pop(), pos)
            Label label = il.DefineLabel();
            il.Emit(OpCodes.Ldloc_0); // ruleId
            il.Emit(OpCodes.Ldc_I4_M1); // ruleId, -1
            il.Emit(OpCodes.Ceq); // (ruleId == -1)
            il.Emit(OpCodes.Brfalse, label); // -
            // here: terminal
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, earley); // earley
            il.Emit(OpCodes.Ldfld, fld_earley_symbolNames); // sn
            il.Emit(OpCodes.Ldarg_3); // sn, sym
            il.Emit(OpCodes.Ldelem, t_string); // name
            il.Emit(OpCodes.Ldarg_1); // name, tokens
            il.Emit(OpCodes.Call, method_Stack_string_Pop); // name, kind
            il.Emit(OpCodes.Ldloc_1); // name, kind, pos
            il.Emit(OpCodes.Newobj, ctor_ParsingTree_Leaf); // leaf
            il.Emit(OpCodes.Ret); // return leaf
            // here: not terminal
            il.MarkLabel(label);
            // начнём создавать текущий ParsingTree
            // оставим в т.ч. children на стеке
            // с ним будем работать в цикле
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, earley); // earley
            il.Emit(OpCodes.Ldfld, fld_earley_symbolNames); // symbolNames
            il.Emit(OpCodes.Ldarg_0); // symbolNames, #0
            il.Emit(OpCodes.Ldfld, earley); // sn, earley
            il.Emit(OpCodes.Ldfld, fld_earley_rulesources); // sn, rulesrcs
            il.Emit(OpCodes.Ldloc_0); // sn, rulesrcs, ruleId
            il.Emit(OpCodes.Ldelem, t_int); // sn, curr rule source
            il.Emit(OpCodes.Ldelem, t_string); // node name (nn)
            // создадим children - без локальной переменной, он будет всё время лежать на стеке
            // children.Length == rb[ruleId].Length
            il.Emit(OpCodes.Ldarg_0); // nn, #0
            il.Emit(OpCodes.Ldfld, earley); // nn, earley
            il.Emit(OpCodes.Ldfld, fld_earley_rulebodies); // nn, rb
            il.Emit(OpCodes.Ldloc_0); // nn, rb, ruleId
            il.Emit(OpCodes.Ldelem, t_array_int); // nn, rb[rid]
            il.Emit(OpCodes.Ldlen); // nn, rb[rid].Length
            il.Emit(OpCodes.Newarr, t_ParsingTree); // nn, children
            // запустим цикл инициализации
            // let i be loc.2
            il.DeclareLocal(t_int);
            // i := children.Length - 1
            il.Emit(OpCodes.Dup); // nn, c, c
            il.Emit(OpCodes.Ldlen); // nn, c, c.len
            il.Emit(OpCodes.Ldc_I4_M1); // nn, c, c.len, -1
            il.Emit(OpCodes.Add); // n, c, (c.len - 1)
            il.Emit(OpCodes.Stloc_2); // n, c [i := c.len - 1]
            // начало цикла
            // проверка
            Label loopStart = il.DefineLabel();
            Label loopEnd = il.DefineLabel();
            il.MarkLabel(loopStart);
            il.Emit(OpCodes.Ldloc_2); // n, c, i
            il.Emit(OpCodes.Ldc_I4_M1); // n, c, i, -1
            il.Emit(OpCodes.Cgt); // n, c, (i > -1)
            il.Emit(OpCodes.Brfalse, loopEnd); // n, c
            // присваиваем c[i] := this.buildTree(tokens, ruleIds, rulebodies[ruleId][i])
            il.Emit(OpCodes.Dup); // n, c, c
            il.Emit(OpCodes.Ldloc_2); // n, c, c, i
            il.Emit(OpCodes.Ldarg_0); // n, c, c, i, #0
            il.Emit(OpCodes.Ldarg_1); // n, c, c, i, #0, tokens
            il.Emit(OpCodes.Ldarg_2); // n, c, c, i, #0, tokens, ruleIds
            il.Emit(OpCodes.Ldarg_0); // n, c, c, i, #0, tokens, ruleIds, #0
            il.Emit(OpCodes.Ldfld, earley); // n, c, c, i, #0, tokens, ruleIds, earley
            il.Emit(OpCodes.Ldfld, fld_earley_rulebodies); // n, c, c, i, #0, tokens, ruleIds, rb
            il.Emit(OpCodes.Ldloc_0); // n, c, c, i, #0, tokens, ruleIds, rb, ruleId
            il.Emit(OpCodes.Ldelem, t_array_int); // n, c, c, i, #0, tokens, ruleIds, rbody
            il.Emit(OpCodes.Ldloc_2); // n, c, c, i, #0, tokens, ruleIds, rbody, i
            il.Emit(OpCodes.Ldelem, t_int); // n, c, c, i, #0, tokens, ruleIds, rbody[i]
            il.Emit(OpCodes.Call, buildTree); // n, c, c, i, subtree
            il.Emit(OpCodes.Stelem, t_ParsingTree); // n, c
            // i--
            il.Emit(OpCodes.Ldloc_2); // n, c, i
            il.Emit(OpCodes.Ldc_I4_M1); // n, c, i, -1
            il.Emit(OpCodes.Add); // n, c, (i - 1)
            il.Emit(OpCodes.Stloc_2); // n, c
            il.Emit(OpCodes.Br, loopStart); // n, c
            // end of loop
            il.MarkLabel(loopEnd);
            // return new ParsingTree(n, children, pos);
            il.Emit(OpCodes.Ldloc_1); // n, c, pos
            il.Emit(OpCodes.Newobj, ctor_ParsingTree_NotLeaf); // not leaf
            il.Emit(OpCodes.Ret); // return tree
            //------------------------------------------------------------------
            MethodBuilder getTree = t.DefineMethod("GetTree", attr_method_public, t_ParsingTree, new Type[] { t_string });
            getTree.DefineParameter(1, ParameterAttributes.None, "s");
            il = getTree.GetILGenerator();
            // часть LexAnalyzer::Analyze
            // будет строить стек tokens для buildTree и массив a для earley
            // а также массив truepositions
            // let end be loc.0; end := 0
            il.DeclareLocal(t_int);
            il.Emit(OpCodes.Ldc_I4_0); // 0
            il.Emit(OpCodes.Stloc_0); // - [end := 0]
            // Stack<string> tokens (loc.1)
            il.DeclareLocal(t_Stack_string);
            il.Emit(OpCodes.Newobj, ctor_Stack_string); // new stack
            il.Emit(OpCodes.Stloc_1); // - [tokens := new]
            // Queue<int> autoq (loc.2)
            il.DeclareLocal(t_Queue_int);
            il.Emit(OpCodes.Newobj, ctor_Queue_int); // new queue
            il.Emit(OpCodes.Stloc_2); // - [autoq := new]
            // счётчик i (loc.3)
            il.DeclareLocal(t_int);
            // a : List<int> с последующей конвертацией в массив (loc.4)
            il.DeclareLocal(t_List_int);
            il.Emit(OpCodes.Newobj, ctor_List_int); // new list
            il.Emit(OpCodes.Stloc, (short)4); // - [a := new]
            // lastAccepted : loc.5, lastAcceptorId : loc.6
            il.DeclareLocal(t_int);
            il.DeclareLocal(t_int);
            // truepositions(сейчас как List<int>) : loc.7
            il.DeclareLocal(t_List_int);
            il.Emit(OpCodes.Newobj, ctor_List_int);
            il.Emit(OpCodes.Stloc, (short)7);
            // loop : while end < s.Length
            // проверка условия
            loopStart = il.DefineLabel();
            loopEnd = il.DefineLabel();
            il.MarkLabel(loopStart);
            il.Emit(OpCodes.Ldloc_0); // end
            il.Emit(OpCodes.Ldarg_1); // end, s
            il.Emit(OpCodes.Call, property_string_Length); // end, s.Length
            il.Emit(OpCodes.Clt); // (end < s.Length)
            il.Emit(OpCodes.Brfalse, loopEnd); // -
            // основное тело
            // lastAccepted = -1;
            il.Emit(OpCodes.Ldc_I4_M1); // -1
            il.Emit(OpCodes.Stloc, (short)5); // -
            // acceptorId = -1;
            il.Emit(OpCodes.Ldc_I4_M1); // -1
            il.Emit(OpCodes.Stloc, (short)6); // -
            // autoq.Clear();
            il.Emit(OpCodes.Ldloc_2); // autoq
            il.Emit(OpCodes.Call, method_Queue_int_Clear); // -
            // первый внутренний цикл
            Label innerLoopStart = il.DefineLabel();
            Label innerLoopEnd = il.DefineLabel();
            // инициализация
            il.Emit(OpCodes.Ldc_I4_0); // 0
            il.Emit(OpCodes.Stloc_3); // - [i := 0]
            // проверка i < fsms.Length
            il.MarkLabel(innerLoopStart);
            il.Emit(OpCodes.Ldloc_3); // i
            il.Emit(OpCodes.Ldarg_0); // i, #0
            il.Emit(OpCodes.Ldfld, fsms); // i, fsms
            il.Emit(OpCodes.Ldlen); // i, len
            il.Emit(OpCodes.Clt); // (i < len)
            il.Emit(OpCodes.Brfalse, innerLoopEnd); // -
            // основное тело
            // autoq.Enqueue(i)
            il.Emit(OpCodes.Ldloc_2); // autoq
            il.Emit(OpCodes.Ldloc_3); // i
            il.Emit(OpCodes.Call, method_Queue_int_Enqueue); // -
            // machines[i].Reset();
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, fsms); // fsms
            il.Emit(OpCodes.Ldloc_3); // fsms, i
            il.Emit(OpCodes.Ldelem, t_FSM); // fsm
            il.Emit(OpCodes.Call, method_fsm_reset); // -
            // i++
            il.Emit(OpCodes.Ldloc_3); // i
            il.Emit(OpCodes.Ldc_I4_1); // i, 1
            il.Emit(OpCodes.Add); // (i + 1)
            il.Emit(OpCodes.Stloc_3); // - [i := i + 1]
            il.Emit(OpCodes.Br, innerLoopStart);
            // end of inner loop
            il.MarkLabel(innerLoopEnd);
            // autoq.Enqueue(-1);
            il.Emit(OpCodes.Ldloc_2); // autoq
            il.Emit(OpCodes.Ldc_I4_M1); // autoq, -1
            il.Emit(OpCodes.Call, method_Queue_int_Enqueue); // -
            // и... новый внутренний цикл! for (int i = end; i < s.Length && autoq.Count > 1; i++)
            innerLoopStart = il.DefineLabel();
            innerLoopEnd = il.DefineLabel();
            // инициализация
            il.Emit(OpCodes.Ldloc_0); // end
            il.Emit(OpCodes.Stloc_3); // - [i := end]
            // проверка :: 1 этап
            il.MarkLabel(innerLoopStart);
            il.Emit(OpCodes.Ldloc_3); // i
            il.Emit(OpCodes.Ldarg_1); // i, s
            il.Emit(OpCodes.Call, property_string_Length); // i, slen
            il.Emit(OpCodes.Clt); // (i < slen)
            il.Emit(OpCodes.Brfalse, innerLoopEnd); // -
            // проверка :: 2 этап
            il.Emit(OpCodes.Ldloc_2); // autoq
            il.Emit(OpCodes.Call, property_Queue_int_Count); // autoq.Count
            il.Emit(OpCodes.Ldc_I4_1); // count, 1
            il.Emit(OpCodes.Cgt); // (count > 1)
            il.Emit(OpCodes.Brfalse, innerLoopEnd); // -
            // основное тело
            // в котором ещё и цикл while (autoq.Peek() != -1)
            // который в принципе можно переписать в do ... while
            // т.к. при count > 1 -1 не стоит на первой позиции
            label = il.DefineLabel();
            Label continueLabel = il.DefineLabel(); // в этом while понадобится continue
            il.MarkLabel(label);
            // основное тело
            // loc.8 : machineId
            il.DeclareLocal(t_int);
            // machineId = autoq.Dequeue();
            il.Emit(OpCodes.Ldloc_2); // autoq
            il.Emit(OpCodes.Call, method_Queue_int_Dequeue); // mid
            il.Emit(OpCodes.Stloc, (short)8); // -
            // if (fsms[machineId].Tact(s[i])) [else -> continue label]
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, fsms); // fsms
            il.Emit(OpCodes.Ldloc, (short)8); // fsms, machineId
            il.Emit(OpCodes.Ldelem, t_FSM); // fsm
            il.Emit(OpCodes.Ldarg_1); // fsm, s
            il.Emit(OpCodes.Ldloc_3); // fsm, s, i
            il.Emit(OpCodes.Call, indexer_string); // fsm, s[i]
            il.Emit(OpCodes.Call, method_fsm_tact); // <was tact>
            il.Emit(OpCodes.Brfalse, continueLabel); // -
            // here : tact was
            // autoq.Enqueue(machineId);
            il.Emit(OpCodes.Ldloc_2); // autoq
            il.Emit(OpCodes.Ldloc, (short)8); // autoq, mid
            il.Emit(OpCodes.Call, method_Queue_int_Enqueue); // -
            // if (i > lastAccepted && fsms[machineId].Final())
            // рассматриваем как двухэтапный if
            il.Emit(OpCodes.Ldloc_3); // i
            il.Emit(OpCodes.Ldloc, (short)5); // i, lastAccepted
            il.Emit(OpCodes.Cgt); // (i > lastAccepted)
            il.Emit(OpCodes.Brfalse, continueLabel); // -
            // 2 этап
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, fsms); // fsms
            il.Emit(OpCodes.Ldloc, (short)8); // fsms, machineId
            il.Emit(OpCodes.Ldelem, t_FSM); // fsm
            il.Emit(OpCodes.Call, method_fsm_final); // <final?>
            il.Emit(OpCodes.Brfalse, continueLabel);
            // here : ok
            // lastAccepted = i;
            il.Emit(OpCodes.Ldloc_3); // i
            il.Emit(OpCodes.Stloc, (short)5); // - [lastAccepted := i]
            // acceptorId = machineId;
            il.Emit(OpCodes.Ldloc, (short)8);
            il.Emit(OpCodes.Stloc, (short)6); // - [acceptorId := machineId]
            // if's and body of while закончились
            // проверка while
            // autoq.Peek() != -1
            il.MarkLabel(continueLabel);
            il.Emit(OpCodes.Ldloc_2); // autoq
            il.Emit(OpCodes.Call, method_Queue_int_Peek); // peek
            il.Emit(OpCodes.Ldc_I4_M1); // peek, -1
            il.Emit(OpCodes.Ceq); // (peek == -1)
            il.Emit(OpCodes.Brfalse, label);
            // end of while loop
            // autoq.Dequeue(); [returns -1]
            // а потом Enqueue(-1)
            // объединим это
            // autoq.Enqueue(autoq.Dequeue())
            il.Emit(OpCodes.Ldloc_2); // autoq
            il.Emit(OpCodes.Dup); // autoq, autoq
            il.Emit(OpCodes.Call, method_Queue_int_Dequeue); // autoq, -1
            il.Emit(OpCodes.Call, method_Queue_int_Enqueue); // -
            // внутренний цикл: i++
            il.Emit(OpCodes.Ldloc_3); // i
            il.Emit(OpCodes.Ldc_I4_1); // i, 1
            il.Emit(OpCodes.Add); // (i + 1)
            il.Emit(OpCodes.Stloc_3); // - [i := i + 1]
            il.Emit(OpCodes.Br, innerLoopStart); // -
            // end of inner loop
            il.MarkLabel(innerLoopEnd);
            // if (lastAccepted == -1) return null as ParsingTree [not accepted]
            il.Emit(OpCodes.Ldloc, (short)5); // lastAccepted
            il.Emit(OpCodes.Ldc_I4_M1); // lastAccepted, -1
            il.Emit(OpCodes.Ceq); // (lastAccepted == -1)
            // нужен прыжок через return null
            label = il.DefineLabel();
            il.Emit(OpCodes.Brfalse, label);
            // here: -1 => throw new UnexpectedSymbolException(end)
            il.Emit(OpCodes.Ldloc_0); // end
            il.Emit(OpCodes.Newobj, ctor_UnexpSymEx); // ex
            il.Emit(OpCodes.Throw);
            // here: not -1
            il.MarkLabel(label);
            // if (symbolNames[fsmTokens[acceptorId]] != "_") ...
            label = il.DefineLabel();
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldfld, earley); // #0, earley
            il.Emit(OpCodes.Ldfld, fld_earley_symbolNames); // sn
            il.Emit(OpCodes.Ldarg_0); // sn, #0
            il.Emit(OpCodes.Ldfld, fsmTokens); // sn, fsmTokens
            il.Emit(OpCodes.Ldloc, (short)6); // sn, fsmTokens, acceptorId
            il.Emit(OpCodes.Ldelem, t_int); // sn, fsmTokens[accid]
            il.Emit(OpCodes.Ldelem, t_string); // token name
            il.Emit(OpCodes.Ldstr, "_"); // tn, "_"
            il.Emit(OpCodes.Call, op_string_eq); // (==)
            il.Emit(OpCodes.Brtrue, label);
            // here : !=
            // tokens.Push(s.Substring(end, lastAccepted + 1 - end))
            il.Emit(OpCodes.Ldloc_1); // tokens
            il.Emit(OpCodes.Ldarg_1); // tokens, s
            il.Emit(OpCodes.Ldloc_0); // tokens, s, end
            il.Emit(OpCodes.Ldloc, (short)5); // tokens, s, end, lastacc
            il.Emit(OpCodes.Ldc_I4_1); // tokens, s, end, lastacc, 1
            il.Emit(OpCodes.Add); // tokens, s, end, (lastacc + 1)
            il.Emit(OpCodes.Ldloc_0); // tokens, s, end, (lastacc + 1), end
            il.Emit(OpCodes.Sub); // tokens, s, end, (lastacc + 1 - end)
            il.Emit(OpCodes.Call, method_string_Substring); // tokens, token
            il.Emit(OpCodes.Call, method_Stack_string_Push); // -
            // a.Add(fsmTokens[acceptorId])
            il.Emit(OpCodes.Ldloc, (short)4); // a
            il.Emit(OpCodes.Ldarg_0); // a, #0
            il.Emit(OpCodes.Ldfld, fsmTokens); // a, fsmTokens
            il.Emit(OpCodes.Ldloc, (short)6); // a, fsmTokens, acceptorId
            il.Emit(OpCodes.Ldelem, t_int); // a, fsmTokens[accid]
            il.Emit(OpCodes.Call, method_List_int_Add); // -
            // truepositions.Add(end)
            il.Emit(OpCodes.Ldloc, (short)7); // truepositions
            il.Emit(OpCodes.Ldloc_0); // truepos, end
            il.Emit(OpCodes.Call, method_List_int_Add); // -
            // here: continue
            il.MarkLabel(label);
            // end = lastAccepted + 1;
            il.Emit(OpCodes.Ldloc, (short)5); // lastacc
            il.Emit(OpCodes.Ldc_I4_1); // lastacc, 1
            il.Emit(OpCodes.Add); // (lastacc + 1)
            il.Emit(OpCodes.Stloc_0); // -
            il.Emit(OpCodes.Br, loopStart); // -
            // end of main while loop
            il.MarkLabel(loopEnd);
            // завершающие вызовы
            // return this.buildTree(tokens, this.earley.GetRightAnalysis(a.ToArray(), truepos.ToArray()), start)
            il.Emit(OpCodes.Ldarg_0); // #0
            il.Emit(OpCodes.Ldloc_1); // #0, tokens
            il.Emit(OpCodes.Ldarg_0); // #0, tokens, #0
            il.Emit(OpCodes.Ldfld, earley); // #0, tokens, earley
            il.Emit(OpCodes.Ldloc, (short)4); // #0, tokens, earley, a
            il.Emit(OpCodes.Call, method_List_int_ToArray); // #0, tokens, earley, aarr
            il.Emit(OpCodes.Ldloc, (short)7); // #0, tokens, earley, aarr, truepos
            il.Emit(OpCodes.Dup); // #0, tokens, earley, aarr, truepos, truepos
            il.Emit(OpCodes.Ldarg_1); // #0, tokens, earley, aarr, truepos, truepos, s
            il.Emit(OpCodes.Call, property_string_Length); // #0, tokens, earley, aarr, truepos, truepos, s.Length
            il.Emit(OpCodes.Call, method_List_int_Add); // #0, tokens, earley, aarr, truepos
            il.Emit(OpCodes.Call, method_List_int_ToArray); // #0, tokens, earley, aarr, trueposarr
            il.Emit(OpCodes.Call, method_earley_GetRightAnalysis); // #0, tokens, ruleIds
            il.Emit(OpCodes.Ldarg_0); // #0, tokens, ruleIds, #0
            il.Emit(OpCodes.Ldfld, earley); // #0, tokens, ruleIds, earley
            il.Emit(OpCodes.Ldfld, fld_earley_Start); // #0, tokens, ruleIds, start
            il.Emit(OpCodes.Call, buildTree); // tree
            il.Emit(OpCodes.Ret); // return tree
            // класс готов
            t.CreateType();
        }

        // главный метод компиляции
        // возвращает null, если не было ошибок, строку ошибки иначе
        // в результате происходит запись парсера в dll в директорию проекта
        public static string Build(string lexcode, string syncode, string projectName, string projectDirectory)
        {
            try
            {
                // 1) распарсить файлы проекта
                // описание лексики
                var lexrules = LexLanguage.LexParser.Parse(lexcode);
                // описание синтаксиса
                var grammar = SynLanguage.SynParser.Parse(lexrules.Select(r => r.Left), syncode);

                if (!(grammar is object))
                    throw new BuildingException("Empty language");

                // 2) создать сборку
                // имя сборки должно быть обёрнуто в специальный объект
                AssemblyName asmname = new AssemblyName(projectName);
                // создаём сборку, которую можно запускать и сохранять
                AssemblyBuilder asmbuilder = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.RunAndSave);
                string dllfilename = asmname.Name + ".dll";
                string fulldllfilename = projectDirectory + dllfilename;
                ModuleBuilder module = asmbuilder.DefineDynamicModule(asmname.Name, dllfilename);

                // 3) сгенерировать все классы
                MakeExceptions(module);
                MakeFSMClass(module);
                MakeEarleyClass(module);
                MakeParsingTreeClass(module);
                MakeParserClass(module, lexrules.Select(r => r.BuildFSM()).ToArray(), lexrules.Select(r => grammar.GetId(r.Left)).ToArray(), grammar);

                // 4) сохранить готовую сборку
                // по непонятным причинам методов Save можно сохранить сборку только
                // в той же директории, где находится генерирующее приложение
                asmbuilder.Save(dllfilename);
                // но это легко поправить, переместив сборку
                FileInfo oldf = new FileInfo(dllfilename); // старое расположение сборки
                FileInfo f = new FileInfo(fulldllfilename); // новое расположение сборки
                
                if(oldf.DirectoryName + "\\" != projectDirectory)
                {
                    if (f.Exists)
                        f.Delete();
                    File.Move(dllfilename, fulldllfilename);
                }
            }
            catch(BuildingException ex)
            {
                return ex.Message;
            }

            return null;
        }
    }
}
