using Golden.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Windows.Input;

namespace Golden.Mvvm
{
    public static class MvvmHelper
    {
        internal const string RaisePropertyChangedMethodName = "RaisePropertyChanged";
        internal const string ValidateMethodName = "Validate";
        internal const string ViewModelRegisterMethodName = "OnViewModelRegister";

        private static readonly Lazy<ModuleBuilder> mCreateDynamicModuleBuilder = new Lazy<ModuleBuilder>(() =>
        {
            var asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("DynamicAssembly_".Append(Guid.NewGuid().ToString("N"))), AssemblyBuilderAccess.Run);
            var moduleBuilder = asmBuilder.DefineDynamicModule("DynamicModule_".Append(Guid.NewGuid().ToString("N")));
            return moduleBuilder;
        });
        private static readonly Lazy<MethodInfo> mRaisePropertyChanged = new Lazy<MethodInfo>(() =>
        {
            return typeof(ObservableObject).GetMember(RaisePropertyChangedMethodName, BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public)
            .OfType<MethodInfo>().First(m =>
            {
                var parameters = m.GetParameters();
                return (parameters.Length == 1 && parameters[0].ParameterType == typeof(string));
            });
        });
        private static readonly Lazy<MethodInfo> mValidateProperty = new Lazy<MethodInfo>(() =>
        {
            return typeof(ViewModelBase).GetMember(ValidateMethodName, BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public)
            .OfType<MethodInfo>().FirstOrDefault(m =>
            {
                var parameters = m.GetParameters();
                return (parameters.Length == 1 && parameters[0].ParameterType == typeof(string));
            });
        });
        private static readonly Lazy<MethodInfo> mObjectEquals = new Lazy<MethodInfo>(() =>
        {
            return typeof(object).GetMember(nameof(object.Equals), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static)
                .OfType<MethodInfo>()
                .FirstOrDefault(m =>
                {
                    var parameters = m.GetParameters();
                    return (parameters.Length == 2 && parameters[0].ParameterType == typeof(object) && parameters[1].ParameterType == typeof(object));
                });
        });
        private static readonly Lazy<MethodInfo> mEmitCanPushType = new Lazy<MethodInfo>(() =>
        {
            return
                typeof(Utilities).Assembly
                .GetType("Golden.Utility.ProxyHelper", false)
                .GetMethod("EmitCanPushType", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        });
        private static readonly Lazy<MethodInfo> mEmitPushValue = new Lazy<MethodInfo>(() =>
        {
            return
                typeof(Utilities).Assembly
                .GetType("Golden.Utility.ProxyHelper", false)
                .GetMethod("EmitPushValue", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        });
        private static readonly Lazy<MethodInfo> mCreateViewModel = new Lazy<MethodInfo>(() =>
        {
            return typeof(MvvmHelper).GetMember(nameof(CreateViewModel), BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public)
                .OfType<MethodInfo>()
                .FirstOrDefault(m => m.IsGenericMethod);
        });
        internal static bool EmitCanPushType(Type type) => (bool)mEmitCanPushType.Value.Invoke(null, new object[] { type });
        internal static bool EmitPushValue(object value, ILGenerator ilGen) => (bool)mEmitPushValue.Value.Invoke(null, new object[] { value, ilGen });
        private static Type CreateViewModelProxy(Configuration.IViewModelConfiguration configuration)
        {
            if (configuration.Commands.Count == 0 && configuration.Properties.Count == 0 && configuration.OnCreatedMethod == null)
            {
                return configuration.Type;
            }

            var parentType = (configuration.Type ?? typeof(object));
            var fullName = parentType.FullName/*.Append("Proxy")*/;
            ILGenerator ilGen = null;
            var moduleBuilder = mCreateDynamicModuleBuilder;
            var typeAttribs = TypeAttributes.Public;
            if (parentType != typeof(object))
            {
                if (parentType.IsAbstract) typeAttribs |= TypeAttributes.Abstract;
                if (parentType.IsSealed) typeAttribs |= TypeAttributes.Sealed;
                if (parentType.IsUnicodeClass) typeAttribs |= TypeAttributes.UnicodeClass;
            }
            var typeBuilder = moduleBuilder.Value.DefineType(fullName, typeAttribs);
            typeBuilder.SetParent(parentType);

            var mConfigBuilder = typeBuilder.DefineMethod(".ConfigProxy", MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.Private | MethodAttributes.SpecialName, typeof(void), Type.EmptyTypes);
            var configILGen = mConfigBuilder.GetILGenerator();

            #region ParentConstructors
            var baseCtors = parentType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly).Where(c => !c.IsPrivate);
            foreach (var ctor in baseCtors)
            {
                var ctorParams = ctor.GetParameters();

                var mCtorBuilder = typeBuilder.DefineConstructor(ctor.Attributes, ctor.CallingConvention, ctorParams.Select(p => p.ParameterType).ToArray());
                ilGen = mCtorBuilder.GetILGenerator();
                ilGen.Emit(OpCodes.Ldarg_0);
                for (int pIndex = 1; pIndex <= ctorParams.Length; pIndex++)
                {
                    ilGen.Emit(OpCodes.Ldarg, pIndex);
                }
                ilGen.Emit(OpCodes.Call, ctor);
                ilGen.Emit(OpCodes.Nop);

                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Call, mConfigBuilder);
                ilGen.Emit(OpCodes.Nop);

                ilGen.Emit(OpCodes.Ret);
            }
            #endregion
            #region Properties
            foreach (var propConfig in configuration.Properties)
            {
                if (!TypeHelper.IsVirtualProperty(propConfig.BaseProperty))
                {
                    throw new InvalidOperationException($"The property '{propConfig.BaseProperty.Name}' must be declared virtual.");
                }

                var mFieldGet = propConfig.BaseProperty.GetGetMethod(true);
                if (mFieldGet != null && mFieldGet.IsPrivate) mFieldGet = null;
                var mFieldSet = propConfig.BaseProperty.GetSetMethod(true);
                if (mFieldSet != null && mFieldSet.IsPrivate) mFieldSet = null;
                var propertyBuilder = typeBuilder.DefineProperty(propConfig.BaseProperty.Name, PropertyAttributes.None, propConfig.BaseProperty.PropertyType, null);
                var mGetSetAttribs = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Final;
                #region Get
                var getMethodBuilder = typeBuilder.DefineMethod(string.Concat("get_", propertyBuilder.Name), mGetSetAttribs, CallingConventions.HasThis, propertyBuilder.PropertyType, Type.EmptyTypes);
                ilGen = getMethodBuilder.GetILGenerator();
                ilGen.Emit(OpCodes.Nop);
                ilGen.Emit(OpCodes.Ldarg_0);
                if (mFieldGet != null)
                {
                    ilGen.Emit(OpCodes.Call, mFieldGet);
                    ilGen.Emit(OpCodes.Ret);
                    typeBuilder.DefineMethodOverride(getMethodBuilder, mFieldGet);
                }
                propertyBuilder.SetGetMethod(getMethodBuilder);
                #endregion
                #region Set
                if (mFieldSet != null)
                {
                    var setMethodBuilder = typeBuilder.DefineMethod(string.Concat("set_", propertyBuilder.Name), mGetSetAttribs, CallingConventions.HasThis, null, new[] { propertyBuilder.PropertyType });
                    ilGen = setMethodBuilder.GetILGenerator();
                    #region CheckIfMustChange
                    var lblRet = ilGen.DefineLabel();

                    ilGen.Emit(OpCodes.Ldarg_0);
                    ilGen.Emit(OpCodes.Call, mFieldGet);
                    if (propConfig.BaseProperty.PropertyType != typeof(object)) ilGen.Emit(OpCodes.Box, propConfig.BaseProperty.PropertyType);
                    ilGen.Emit(OpCodes.Ldarg_1);
                    if (propConfig.BaseProperty.PropertyType != typeof(object)) ilGen.Emit(OpCodes.Box, propConfig.BaseProperty.PropertyType);
                    ilGen.Emit(OpCodes.Call, mObjectEquals.Value);
                    ilGen.Emit(OpCodes.Ldc_I4_1);
                    ilGen.Emit(OpCodes.Ceq);
                    ilGen.Emit(OpCodes.Brtrue_S, lblRet);
                    ilGen.Emit(OpCodes.Nop);
                    #endregion
                    #region Changing
                    if (propConfig.ChangingMethod != null)
                    {
                        ilGen.Emit(OpCodes.Nop);
                        ilGen.Emit(OpCodes.Ldarg_0);
                        ilGen.Emit(OpCodes.Call, propConfig.ChangingMethod);
                    }
                    #endregion
                    #region SetProperty
                    ilGen.Emit(OpCodes.Nop);
                    ilGen.Emit(OpCodes.Ldarg_0);
                    ilGen.Emit(OpCodes.Ldarg_1);
                    ilGen.Emit(OpCodes.Call, mFieldSet);
                    #endregion
                    #region ValidateProperty
                    if (mValidateProperty.Value != null)
                    {
                        ilGen.Emit(OpCodes.Nop);
                        ilGen.Emit(OpCodes.Ldarg_0);
                        ilGen.Emit(OpCodes.Ldstr, propertyBuilder.Name);
                        ilGen.Emit(OpCodes.Callvirt, mValidateProperty.Value);
                    }
                    #endregion
                    #region NotifyPropertyChanged
                    ilGen.Emit(OpCodes.Nop);
                    ilGen.Emit(OpCodes.Ldarg_0);
                    ilGen.Emit(OpCodes.Ldstr, propertyBuilder.Name);
                    ilGen.Emit(OpCodes.Callvirt, mRaisePropertyChanged.Value);
                    #endregion
                    #region Changed
                    if (propConfig.ChangedMethod != null)
                    {
                        ilGen.Emit(OpCodes.Nop);
                        ilGen.Emit(OpCodes.Ldarg_0);
                        ilGen.Emit(OpCodes.Call, propConfig.ChangedMethod);
                    }
                    #endregion
                    #region DependencyProperties
                    if (propConfig.DependencyProperties.Count > 0)
                    {
                        ilGen.Emit(OpCodes.Nop);
                        foreach (var name in propConfig.DependencyProperties)
                        {
                            ilGen.Emit(OpCodes.Ldarg_0);
                            ilGen.Emit(OpCodes.Ldstr, name);
                            ilGen.Emit(OpCodes.Callvirt, mRaisePropertyChanged.Value);
                        }
                    }
                    #endregion
                    ilGen.MarkLabel(lblRet);
                    ilGen.Emit(OpCodes.Ret);
                    typeBuilder.DefineMethodOverride(setMethodBuilder, mFieldSet);

                    propertyBuilder.SetSetMethod(setMethodBuilder);
                }
                #endregion
                #region DefaultValue
                if (propConfig.HasDefaultValue)
                {
                    var valueType = propConfig.DefaultValue.GetType();
                    configILGen.Emit(OpCodes.Nop);
                    configILGen.Emit(OpCodes.Ldarg_0);
                    #region Type
                    if (propConfig.DefaultValue is Type)
                    {
                        var dvType = (Type)propConfig.DefaultValue;
                        if (dvType.IsArray)
                        {
                            configILGen.Emit(OpCodes.Ldc_I4_0);
                            configILGen.Emit(OpCodes.Newarr, dvType.GetElementType());
                        }
                        else
                        {
                            var pCtor = dvType.GetConstructor(Type.EmptyTypes);
                            configILGen.Emit(OpCodes.Newobj, pCtor);
                        }
                    }
                    #endregion
                    #region String
                    else if (valueType == typeof(string))
                    {
                        if (propConfig.BaseProperty.PropertyType == typeof(string))
                        {
                            EmitPushValue(propConfig.DefaultValue, configILGen);
                        }
                        else if ("".Equals(propConfig.DefaultValue))
                        {
                            var pCtor = propConfig.BaseProperty.PropertyType.GetConstructor(Type.EmptyTypes);
                            configILGen.Emit(OpCodes.Newobj, pCtor);
                        }
                    }
                    #endregion
                    #region DateTime
                    else if (TypeHelper.IsDateTime(propConfig.BaseProperty.PropertyType))
                    {
                        var ticks = ((DateTime)propConfig.DefaultValue).Ticks;
                        EmitPushValue(ticks, configILGen);
                        var pCtor = typeof(DateTime).GetConstructor(new[] { typeof(long) });
                        if (propConfig.BaseProperty.PropertyType == typeof(DateTime?))
                        {
                            configILGen.Emit(OpCodes.Newobj, pCtor);
                            pCtor = typeof(DateTime?).GetConstructor(new[] { typeof(DateTime) });
                        }
                        configILGen.Emit(OpCodes.Newobj, pCtor);
                    }
                    #endregion
                    #region Decimal
                    else if (TypeHelper.IsDecimalType(propConfig.BaseProperty.PropertyType))
                    {
                        var decimalValue = (valueType == typeof(decimal) ? (decimal)propConfig.DefaultValue : Convert.ToDecimal(propConfig.DefaultValue));
                        var bits = decimal.GetBits(decimalValue);
                        bool sign = (bits[3] & 0x80000000) != 0;
                        int scale = (byte)((bits[3] >> 16) & 0x7F);
                        configILGen.Emit(OpCodes.Ldc_I4, bits[0]);
                        configILGen.Emit(OpCodes.Ldc_I4, bits[1]);
                        configILGen.Emit(OpCodes.Ldc_I4, bits[2]);
                        configILGen.Emit((sign ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
                        configILGen.Emit(OpCodes.Ldc_I4, scale);
                        var pCtor = typeof(decimal).GetConstructor(new[] { typeof(int), typeof(int), typeof(int), typeof(bool), typeof(byte) });
                        if (propConfig.BaseProperty.PropertyType == typeof(decimal?))
                        {
                            configILGen.Emit(OpCodes.Newobj, pCtor);
                            pCtor = typeof(decimal?).GetConstructor(new[] { typeof(decimal) });
                        }
                        configILGen.Emit(OpCodes.Newobj, pCtor);
                    }
                    #endregion
                    #region Guid
                    else if (valueType == typeof(Guid))
                    {
                        var bytes = ((Guid)propConfig.DefaultValue).ToByteArray();
                        bytes.ForEach(b => EmitPushValue(b, configILGen));
                        var pCtor = typeof(Guid).GetConstructor(new[] { typeof(byte[]) });
                        if (propConfig.BaseProperty.PropertyType == typeof(Guid?))
                        {
                            configILGen.Emit(OpCodes.Newobj, pCtor);
                            pCtor = typeof(Guid?).GetConstructor(new[] { typeof(Guid) });
                        }
                        configILGen.Emit(OpCodes.Newobj, pCtor);
                    }
                    #endregion
                    #region TimeSpan
                    else if (valueType == typeof(TimeSpan))
                    {
                        var ticks = ((TimeSpan)propConfig.DefaultValue).Ticks;
                        EmitPushValue(ticks, configILGen);
                        var pCtor = typeof(TimeSpan).GetConstructor(new[] { typeof(long) });
                        if (propConfig.BaseProperty.PropertyType == typeof(TimeSpan?))
                        {
                            configILGen.Emit(OpCodes.Newobj, pCtor);
                            pCtor = typeof(TimeSpan?).GetConstructor(new[] { typeof(TimeSpan) });
                        }
                        configILGen.Emit(OpCodes.Newobj, pCtor);
                    }
                    #endregion
                    #region IntPtr
                    else if (valueType == typeof(IntPtr))
                    {
                        var lPtr = ((IntPtr)propConfig.DefaultValue).ToInt64();
                        EmitPushValue(lPtr, configILGen);
                        var pCtor = typeof(IntPtr).GetConstructor(new[] { typeof(long) });
                        if (propConfig.BaseProperty.PropertyType == typeof(IntPtr?))
                        {
                            configILGen.Emit(OpCodes.Newobj, pCtor);
                            pCtor = typeof(IntPtr?).GetConstructor(new[] { typeof(IntPtr) });
                        }
                        configILGen.Emit(OpCodes.Newobj, pCtor);
                    }
                    #endregion
                    #region UIntPtr
                    else if (valueType == typeof(UIntPtr))
                    {
                        var ulPtr = ((UIntPtr)propConfig.DefaultValue).ToUInt64();
                        EmitPushValue(ulPtr, configILGen);
                        var pCtor = typeof(UIntPtr).GetConstructor(new[] { typeof(ulong) });
                        if (propConfig.BaseProperty.PropertyType == typeof(UIntPtr?))
                        {
                            configILGen.Emit(OpCodes.Newobj, pCtor);
                            pCtor = typeof(UIntPtr?).GetConstructor(new[] { typeof(UIntPtr) });
                        }
                        configILGen.Emit(OpCodes.Newobj, pCtor);
                    }
                    #endregion
                    #region Otherwise
                    else
                    {
                        EmitPushValue(propConfig.DefaultValue, configILGen);
                        if (!EmitCanPushType(propConfig.BaseProperty.PropertyType))
                        {
                            var pCtor = propConfig.BaseProperty.PropertyType.GetConstructor(new Type[] { valueType });
                            configILGen.Emit(OpCodes.Newobj, pCtor);
                        }
                    }
                    #endregion
                    configILGen.Emit(OpCodes.Call, mFieldSet);
                    configILGen.Emit(OpCodes.Nop);
                }
                #endregion
            }
            #endregion
            #region Commands
            if (configuration.Commands.Count > 0)
            {
                foreach (var cmdConfig in configuration.Commands)
                {
                    if (cmdConfig.Method.IsPrivate)
                    {
                        throw new InvalidOperationException($"The method '{cmdConfig.Method.Name}' must be declared public or protected.");
                    }

                    var fieldBuilder = typeBuilder.DefineField(string.Concat("_", cmdConfig.Name), typeof(ICommand), FieldAttributes.Private | FieldAttributes.InitOnly);
                    #region CommandProperty
                    var propertyBuilder = typeBuilder.DefineProperty(cmdConfig.Name, PropertyAttributes.None, typeof(ICommand), null);
                    {
                        var getMethodBuilder = typeBuilder.DefineMethod(
                            string.Concat("get_", propertyBuilder.Name),
                            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
                            typeof(ICommand),
                            Type.EmptyTypes);
                        ilGen = getMethodBuilder.GetILGenerator();
                        ilGen.Emit(OpCodes.Ldarg_0);
                        ilGen.Emit(OpCodes.Ldfld, fieldBuilder);
                        ilGen.Emit(OpCodes.Ret);
                        propertyBuilder.SetGetMethod(getMethodBuilder);
                    }
                    #endregion
                    #region CommandMethod
                    {
                        Type cmdType = null, cmdParamType = null;
                        var cmdParamTypes = new List<Type>();

                        configILGen.Emit(OpCodes.Ldarg_0);
                        configILGen.Emit(OpCodes.Ldarg_0);
                        configILGen.Emit(OpCodes.Ldftn, cmdConfig.Method);
                        if (cmdConfig.FirstParameterType != null)
                        {
                            cmdParamType = typeof(Action<>).MakeGenericType(cmdConfig.FirstParameterType);
                            cmdType = typeof(DelegateCommand<>).MakeGenericType(cmdConfig.FirstParameterType);
                            configILGen.Emit(OpCodes.Newobj, cmdParamType.GetConstructors()[0]);
                            cmdParamTypes.Add(cmdParamType);
                            if (cmdConfig.CanExecuteMethod != null)
                            {
                                configILGen.Emit(OpCodes.Ldarg_0);
                                configILGen.Emit(OpCodes.Ldftn, cmdConfig.CanExecuteMethod);
                                var canCmdType = typeof(Func<,>).MakeGenericType(cmdConfig.FirstParameterType, typeof(bool));
                                configILGen.Emit(OpCodes.Newobj, canCmdType.GetConstructors()[0]);
                                cmdParamTypes.Add(canCmdType);
                            }
                        }
                        else
                        {
                            cmdParamType = typeof(Action);
                            cmdType = typeof(DelegateCommand);
                            configILGen.Emit(OpCodes.Newobj, cmdParamType.GetConstructors()[0]);
                            cmdParamTypes.Add(cmdParamType);
                            if (cmdConfig.CanExecuteMethod != null)
                            {
                                configILGen.Emit(OpCodes.Ldarg_0);
                                configILGen.Emit(OpCodes.Ldftn, cmdConfig.CanExecuteMethod);
                                var canCmdType = typeof(Func<>).MakeGenericType(typeof(bool));
                                configILGen.Emit(OpCodes.Newobj, canCmdType.GetConstructors()[0]);
                                cmdParamTypes.Add(canCmdType);
                            }
                        }
                        configILGen.Emit(OpCodes.Newobj, cmdType.GetConstructor(cmdParamTypes.ToArray()));
                        configILGen.Emit(OpCodes.Stfld, fieldBuilder);
                    }
                    #endregion
                }
                configILGen.Emit(OpCodes.Nop);
            }
            #endregion
            #region OnCreatedMethod
            if (configuration.OnCreatedMethod != null)
            {
                configILGen.Emit(OpCodes.Nop);
                configILGen.Emit(OpCodes.Ldarg_0);
                configILGen.Emit(OpCodes.Call, configuration.OnCreatedMethod);
            }
            #endregion
            configILGen.Emit(OpCodes.Ret);
            return typeBuilder.CreateType();
        }
        public static T GetViewModel<T>(IView view)
        {
            return (T)view.DataContext;
        }
        private static readonly Dictionary<Type, Lazy<Type>> _ViewModelMap = new Dictionary<Type, Lazy<Type>>();
        public static T CreateViewModel<T>(params object[] ctorArgs) where T : ViewModelBase
        {
            return (T)CreateViewModel(typeof(T), ctorArgs);
        }
        public static ViewModelBase CreateViewModel(Type type, params object[] ctorArgs)
        {
            if (!_ViewModelMap.ContainsKey(type))
                RegisterConfiguration(type);

            return (ViewModelBase)Activator.CreateInstance(_ViewModelMap[type].Value, ctorArgs);
        }
        public static Type CreateViewModelProxy(Type type)
        {
            if (!_ViewModelMap.ContainsKey(type))
                RegisterConfiguration(type);

            return _ViewModelMap[type].Value;
        }
        public static Type CreateViewModelProxy<T>() where T : ViewModelBase
        {
            return CreateViewModelProxy(typeof(T));
        }
        private static void RegisterConfiguration(Type type, Configuration.IViewModelConfiguration configuration)
        {
            if (configuration == null) configuration = (Configuration.IViewModelConfiguration)Activator.CreateInstance(typeof(Configuration.ViewModelConfiguration<>).MakeGenericType(type));
            _ViewModelMap[type] = new Lazy<Type>(() => CreateViewModelProxy(configuration));
        }
        private static void RegisterConfiguration(Type type)
        {
            var creator = new Lazy<Type>(() =>
            {
                var configType = typeof(Configuration.ViewModelConfiguration<>).MakeGenericType(type);
                var config = (Configuration.IViewModelConfiguration)Activator.CreateInstance(configType);

                //By Attributes
                {
                    var typeMethods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(m => m.IsDefined<Configuration.Annotations.Initialize>() || m.IsDefined<Configuration.Annotations.Command>());
                    #region Initialize
                    var mInit = typeMethods.FirstOrDefault(m => m.IsDefined<Configuration.Annotations.Initialize>());
                    if (mInit != null)
                    {
                        configType.InvokeMember(
                            nameof(Configuration.ViewModelConfiguration<ViewModelBase>.OnInitilize),
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod,
                            null,
                            config,
                            new object[] { mInit });
                    }
                    #endregion
                    #region Commands
                    var mCmds = typeMethods
                        .Select(m => new { Method = m, Config = m.GetCustomAttribute<Configuration.Annotations.Command>() })
                        .Where(x => x.Config != null);
                    foreach (var mCmd in mCmds)
                    {
                        var cmdConf =
                            configType.InvokeMember(
                                nameof(Configuration.ViewModelConfiguration<ViewModelBase>.Command),
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod,
                                null,
                                config,
                                new object[] { mCmd.Method });
                        var cmdConfType = cmdConf.GetType();
                        cmdConfType.InvokeMember(
                            nameof(Configuration.CommandConfiguration<object>.HasCommandName),
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod,
                            null,
                            cmdConf,
                            new object[] { (mCmd.Config.Name ?? mCmd.Method.Name.Append("Command")) });
                        if (mCmd.Config.CanExecute != null)
                        {
                            cmdConfType.InvokeMember(
                                nameof(Configuration.CommandConfiguration<object>.CanExecute),
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod,
                                null,
                                cmdConf,
                                new object[] { mCmd.Config.CanExecute });
                        }
                    }
                    #endregion
                    #region Properties
                    var typeProps = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Select(p => new { Property = p, Config = p.GetCustomAttribute<Configuration.Annotations.Property>() })
                        .Where(x => x.Config != null);
                    foreach (var prop in typeProps)
                    {
                        var propConf =
                            configType.InvokeMember(
                                nameof(Configuration.ViewModelConfiguration<ViewModelBase>.Property),
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod,
                                null,
                                config,
                                new object[] { prop.Property });
                        var propConfType = propConf.GetType();

                        if (prop.Config.DefaultValue != null)
                        {
                            propConfType.InvokeMember(
                                nameof(Configuration.PropertyConfiguration<object, object>.HasDefaultValue),
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod,
                                null,
                                propConf,
                                new object[] { prop.Config.DefaultValue });
                        }
                        if (prop.Config.HasNewInstanceType != null)
                        {
                            propConfType.InvokeMember(
                                nameof(Configuration.PropertyConfiguration<object, object>.HasNewInstance),
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod,
                                null,
                                propConf,
                                new object[] { prop.Config.HasNewInstanceType });
                        }
                        else if (prop.Config.HasNewInstance)
                        {
                            propConfType.InvokeMember(
                                nameof(Configuration.PropertyConfiguration<object, object>.HasNewInstance),
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod,
                                null,
                                propConf,
                                new object[0]);
                        }
                        if (prop.Config.OnChanging != null)
                        {
                            propConfType.InvokeMember(
                                nameof(Configuration.PropertyConfiguration<object, object>.OnChanging),
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod,
                                null,
                                propConf,
                                new object[] { prop.Config.OnChanging });
                        }
                        if (prop.Config.OnChanged != null)
                        {
                            propConfType.InvokeMember(
                                nameof(Configuration.PropertyConfiguration<object, object>.OnChanged),
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod,
                                null,
                                propConf,
                                new object[] { prop.Config.OnChanged });
                        }
                        if (prop.Config.Dependencies != null && prop.Config.Dependencies.Length > 0)
                        {
                            prop.Config.Dependencies.ForEach(dpName =>
                            {
                                propConfType.InvokeMember(
                                    nameof(Configuration.PropertyConfiguration<object, object>.HasDependency),
                                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod,
                                    null,
                                    propConf,
                                    new object[] { dpName });
                            });
                        }
                    }
                    #endregion
                }

                var mRegister = type.GetMethod(ViewModelRegisterMethodName, BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                //if (mRegister == null)
                //    mRegister = type.GetMethod(ViewModelRegisterMethodName, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                if (mRegister != null)
                {
                    if (mRegister.IsGenericMethod)
                        mRegister = mRegister.MakeGenericMethod(type);
                    object owner = null;
                    if (!mRegister.IsStatic)
                    {
                        var ctorArgs =
                        (
                            type.GetConstructors()
                            .Select(ci => new { Info = ci, Parameters = ci.GetParameters().Select(p => (p.ParameterType.IsByRef ? p.ParameterType.GetElementType() : p.ParameterType)).ToArray() })
                            .OrderBy(x => x.Parameters.Length)
                            .FirstOrDefault()?
                            .Parameters.Select(t => TypeHelper.GetDefault(t)).ToArray()
                            ??
                            new object[0]
                        );
                        owner = Activator.CreateInstance(type, ctorArgs);
                    }
                    mRegister.Invoke(owner, new object[] { config });
                    Utilities.DisposeAndNull(ref owner);
                }
                return CreateViewModelProxy(config);
            });
            _ViewModelMap[type] = creator;
        }
        public static void RegisterConfiguration<T>() where T : ViewModelBase
        {
            RegisterConfiguration(typeof(T));
        }
        public static void RegisterConfiguration<T>(Configuration.ViewModelConfiguration<T> configuration) where T : ViewModelBase
        {
            RegisterConfiguration(typeof(T), configuration);
        }
        public static void RegisterConfiguration<T>(Action<Configuration.ViewModelConfiguration<T>> configurator) where T : ViewModelBase
        {
            var config = new Configuration.ViewModelConfiguration<T>();
            configurator.Invoke(config);
            RegisterConfiguration(config);
        }
    }
}
