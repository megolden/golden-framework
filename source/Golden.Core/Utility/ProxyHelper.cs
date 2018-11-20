using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Golden.Utility
{
    internal static class ProxyHelper
    {
        //#region Fields
        //private static readonly Lazy<MethodInfo> mDelegateCombine = new Lazy<MethodInfo>(() =>
        //    typeof(Delegate).GetMember(nameof(Delegate.Combine), BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Static).OfType<MethodInfo>().FirstOrDefault(m =>
        //    {
        //        var parameters = m.GetParameters();
        //        return (parameters.Length == 2 && parameters[0].ParameterType == typeof(Delegate) && parameters[1].ParameterType == typeof(Delegate));
        //    }));
        //private static readonly Lazy<MethodInfo> mDelegateRemove = new Lazy<MethodInfo>(() =>
        //    typeof(Delegate).GetMember(nameof(Delegate.Remove), BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Static).OfType<MethodInfo>().FirstOrDefault(m =>
        //    {
        //        var parameters = m.GetParameters();
        //        return (parameters.Length == 2 && parameters[0].ParameterType == typeof(Delegate) && parameters[1].ParameterType == typeof(Delegate));
        //    }));
        //private static readonly Lazy<MethodInfo> mObjectEquals = new Lazy<MethodInfo>(() =>
        //    typeof(object).GetMember(nameof(object.Equals), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static).OfType<MethodInfo>().FirstOrDefault(m =>
        //    {
        //        var parameters = m.GetParameters();
        //        return (parameters.Length == 2 && parameters[0].ParameterType == typeof(object) && parameters[1].ParameterType == typeof(object));
        //    }));
        //private static readonly Lazy<MethodInfo> mValidateObjProperty = new Lazy<MethodInfo>(() =>
        //    typeof(TypeHelper).GetMethod(nameof(ValidateObjectProeprty), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
        //private static readonly Lazy<MethodInfo> mValidateObj = new Lazy<MethodInfo>(() =>
        //    typeof(TypeHelper).GetMethod(nameof(ValidateObject), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
        //private static readonly Lazy<ConstructorInfo> mPCEventArgsCtor = new Lazy<ConstructorInfo>(() =>
        //    typeof(PropertyChangedEventArgs).GetConstructor(new Type[] { typeof(string) }));
        //private static readonly Lazy<MethodInfo> mPCHandlerInvoke = new Lazy<MethodInfo>(() =>
        //    typeof(PropertyChangedEventHandler).GetMethod(nameof(PropertyChangedEventHandler.Invoke)));
        //private static readonly Lazy<MethodInfo> mOORaisePropertyChanged = new Lazy<MethodInfo>(() =>
        //    typeof(ObservableObject).GetMember(ObservableObject.RaisePropertyChangedMethodName, BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public).OfType<MethodInfo>().FirstOrDefault(m =>
        //    {
        //        var parameters = m.GetParameters();
        //        return (parameters.Length == 1 && parameters[0].ParameterType == typeof(string));
        //    }));
        //private static readonly Lazy<ModuleBuilder> mCreateDynamicModuleBuilder = new Lazy<ModuleBuilder>(() =>
        //    {
        //        var asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("DynamicAssembly_".Append(Guid.NewGuid().ToString("N"))), AssemblyBuilderAccess.Run);
        //        var moduleBuilder = asmBuilder.DefineDynamicModule("DynamicModule_".Append(Guid.NewGuid().ToString("N")));
        //        return moduleBuilder;
        //    });
        //#endregion
        //private static MethodInfo ImplementINotifyPropertyChanged(TypeBuilder typeBuilder)
        //{
        //    if (typeof(ObservableObject).IsAssignableFrom(typeBuilder)) return mOORaisePropertyChanged.Value;

        //    ILGenerator ilGen = null;
        //    var notifyType = typeof(INotifyPropertyChanged);
        //    var hasNotify = notifyType.IsAssignableFrom(typeBuilder);
        //    if (!hasNotify) typeBuilder.AddInterfaceImplementation(notifyType);

        //    var handlerType = typeof(PropertyChangedEventHandler);
        //    var notifyEvent = notifyType.GetEvent(nameof(INotifyPropertyChanged.PropertyChanged), BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
        //    var notifyEventAdd = notifyEvent.GetAddMethod(true);
        //    var notifyEventRemove = notifyEvent.GetRemoveMethod(true);
        //    var handlerField = typeBuilder.DefineField(string.Concat("_", notifyType.FullName, ".PropertyChangedHandler"), handlerType, FieldAttributes.Private);
        //    #region Event
        //    var eventMethodsAttribs = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.Final;
        //    var mEventAdd = typeBuilder.DefineMethod(string.Concat("add_", handlerField.Name), eventMethodsAttribs);
        //    mEventAdd.SetParameters(handlerType);
        //    ilGen = mEventAdd.GetILGenerator();
        //    ilGen.Emit(OpCodes.Nop);
        //    ilGen.Emit(OpCodes.Ldarg_0);
        //    ilGen.Emit(OpCodes.Ldarg_0);
        //    ilGen.Emit(OpCodes.Ldfld, handlerField);
        //    ilGen.Emit(OpCodes.Ldarg_1);
        //    ilGen.Emit(OpCodes.Call, mDelegateCombine.Value);
        //    ilGen.Emit(OpCodes.Castclass, handlerType);
        //    ilGen.Emit(OpCodes.Stfld, handlerField);
        //    ilGen.Emit(OpCodes.Ret);
        //    typeBuilder.DefineMethodOverride(mEventAdd, notifyEventAdd);
        //    var mEventRemove = typeBuilder.DefineMethod(string.Concat("remove_", handlerField.Name), eventMethodsAttribs);
        //    mEventRemove.SetParameters(handlerType);
        //    ilGen = mEventRemove.GetILGenerator();
        //    ilGen.Emit(OpCodes.Nop);
        //    ilGen.Emit(OpCodes.Ldarg_0);
        //    ilGen.Emit(OpCodes.Ldarg_0);
        //    ilGen.Emit(OpCodes.Ldfld, handlerField);
        //    ilGen.Emit(OpCodes.Ldarg_1);
        //    ilGen.Emit(OpCodes.Call, mDelegateRemove.Value);
        //    ilGen.Emit(OpCodes.Castclass, handlerType);
        //    ilGen.Emit(OpCodes.Stfld, handlerField);
        //    ilGen.Emit(OpCodes.Ret);
        //    typeBuilder.DefineMethodOverride(mEventRemove, notifyEventRemove);
        //    var _Event = typeBuilder.DefineEvent(string.Concat("_", notifyType.FullName, ".PropertyChanged"), EventAttributes.None, handlerType);
        //    _Event.SetAddOnMethod(mEventAdd);
        //    _Event.SetRemoveOnMethod(mEventRemove);
        //    #endregion
        //    #region RaisePropertyChanged
        //    var argsType = typeof(PropertyChangedEventArgs);
        //    var mRaisePropertyChangedBuilder = typeBuilder.DefineMethod("RaisePropertyChanged", MethodAttributes.Public | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.Family);
        //    mRaisePropertyChangedBuilder.SetParameters(typeof(string));
        //    ilGen = mRaisePropertyChangedBuilder.GetILGenerator();
        //    var lblRet = ilGen.DefineLabel();
        //    var eArgs = ilGen.DeclareLocal(argsType);
        //    ilGen.Emit(OpCodes.Nop);
        //    ilGen.Emit(OpCodes.Ldarg_0);
        //    ilGen.Emit(OpCodes.Ldfld, handlerField);
        //    ilGen.Emit(OpCodes.Brfalse_S, lblRet);

        //    ilGen.Emit(OpCodes.Ldarg_1);
        //    ilGen.Emit(OpCodes.Newobj, mPCEventArgsCtor.Value);
        //    ilGen.Emit(OpCodes.Stloc_0);

        //    ilGen.Emit(OpCodes.Nop);
        //    ilGen.Emit(OpCodes.Ldarg_0);
        //    ilGen.Emit(OpCodes.Ldfld, handlerField);
        //    ilGen.Emit(OpCodes.Ldarg_0);
        //    ilGen.Emit(OpCodes.Ldloc_0);
        //    ilGen.Emit(OpCodes.Callvirt, mPCHandlerInvoke.Value);

        //    ilGen.MarkLabel(lblRet);
        //    ilGen.Emit(OpCodes.Ret);
        //    #endregion
        //    return mRaisePropertyChangedBuilder;
        //}
        //public static bool ValidateObject(object obj, out string errorMessage)
        //{
        //    errorMessage = null;
        //    var configObj = obj.GetType().GetField(string.Concat("_", typeof(IDataErrorInfo).FullName, ".Configuration"), BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
        //    if (object.ReferenceEquals(configObj, null)) return true;

        //    var iov = configObj.GetType().GetInterface(nameof(Configuration.IObjectValidation), false);
        //    if (iov == null) return true;

        //    var parameters = new object[] { obj, errorMessage };
        //    var result = (bool)iov.GetMethod(nameof(Configuration.IObjectValidation.Validate)).Invoke(configObj, parameters);
        //    errorMessage = Utilities.Convert<string>(parameters[1]);
        //    return result;
        //}
        //public static bool ValidateObjectProeprty(string propertyName, object obj, out string errorMessage)
        //{
        //    errorMessage = null;
        //    var configObj = obj.GetType().GetField(string.Concat("_", typeof(IDataErrorInfo).FullName, ".Configuration"), BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
        //    if (object.ReferenceEquals(configObj, null)) return true;

        //    var iov = configObj.GetType().GetInterface(nameof(Configuration.IObjectValidation), false);
        //    if (iov == null) return true;

        //    var parameters = new object[] { propertyName, obj, errorMessage };
        //    var result = (bool)iov.GetMethod(nameof(Configuration.IObjectValidation.ValidateProperty)).Invoke(configObj, parameters);
        //    errorMessage = Utility.Utilities.Convert<string>(parameters[2]);
        //    return result;
        //}
        //private static FieldInfo ImplementIDataErrorInfo(TypeBuilder typeBuilder, Type configurationType)
        //{
        //    ILGenerator ilGen = null;
        //    var infoType = typeof(IDataErrorInfo);
        //    var hasInfo = infoType.IsAssignableFrom(typeBuilder);
        //    if (hasInfo == false) typeBuilder.AddInterfaceImplementation(infoType);

        //    var configField = typeBuilder.DefineField(string.Concat("_", infoType.FullName, ".Configuration"), configurationType, FieldAttributes.Private);

        //    var errorProp = typeBuilder.DefineProperty(string.Concat(infoType.FullName, ".Error"), PropertyAttributes.None, typeof(string), null);
        //    var mGetSetAttribs = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.Final;
        //    #region Get
        //    var getMethodBuilder = typeBuilder.DefineMethod(string.Concat("get_", errorProp.Name), mGetSetAttribs, errorProp.PropertyType, Type.EmptyTypes);
        //    ilGen = getMethodBuilder.GetILGenerator();
        //    var lblRet = ilGen.DefineLabel();
        //    var lblRetEmptyStr = ilGen.DefineLabel();
        //    var errMsg = ilGen.DeclareLocal(typeof(string));
        //    ilGen.Emit(OpCodes.Nop);

        //    //ilGen.Emit(OpCodes.Ldarg_0);
        //    //ilGen.Emit(OpCodes.Ldfld, configField);
        //    //ilGen.Emit(OpCodes.Brfalse_S, lblRetEmptyStr);

        //    //ilGen.Emit(OpCodes.Nop);
        //    ilGen.Emit(OpCodes.Ldarg_0);
        //    //ilGen.Emit(OpCodes.Ldfld, configField);
        //    //ilGen.Emit(OpCodes.Ldarg_0);
        //    ilGen.Emit(OpCodes.Ldloca_S, errMsg);
        //    ilGen.Emit(OpCodes.Call, mValidateObj.Value);
        //    ilGen.Emit(OpCodes.Brtrue_S, lblRetEmptyStr);
        //    ilGen.Emit(OpCodes.Ldloc_0);
        //    ilGen.Emit(OpCodes.Brfalse_S, lblRetEmptyStr);
        //    ilGen.Emit(OpCodes.Ldloc_0);
        //    ilGen.Emit(OpCodes.Br_S, lblRet);

        //    ilGen.MarkLabel(lblRetEmptyStr);
        //    ilGen.Emit(OpCodes.Ldstr, "");
        //    ilGen.MarkLabel(lblRet);

        //    ilGen.Emit(OpCodes.Ret);
        //    typeBuilder.DefineMethodOverride(getMethodBuilder, infoType.GetProperty(nameof(IDataErrorInfo.Error), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetGetMethod(false));
        //    errorProp.SetGetMethod(getMethodBuilder);
        //    #endregion
        //    var itemProp = typeBuilder.DefineProperty(string.Concat(infoType.FullName, ".Item"), PropertyAttributes.None, typeof(string), null);
        //    #region Get
        //    getMethodBuilder = typeBuilder.DefineMethod(string.Concat("get_", errorProp.Name), mGetSetAttribs, errorProp.PropertyType, Type.EmptyTypes);
        //    getMethodBuilder.SetParameters(typeof(string));
        //    ilGen = getMethodBuilder.GetILGenerator();
        //    lblRet = ilGen.DefineLabel();
        //    lblRetEmptyStr = ilGen.DefineLabel();
        //    errMsg = ilGen.DeclareLocal(typeof(string));
        //    ilGen.Emit(OpCodes.Nop);

        //    //ilGen.Emit(OpCodes.Ldarg_0);
        //    //ilGen.Emit(OpCodes.Ldfld, configField);
        //    //ilGen.Emit(OpCodes.Brfalse_S, lblRetEmptyStr);

        //    //ilGen.Emit(OpCodes.Nop);
        //    //ilGen.Emit(OpCodes.Ldarg_0);
        //    //ilGen.Emit(OpCodes.Ldfld, configField);
        //    ilGen.Emit(OpCodes.Ldarg_1);
        //    ilGen.Emit(OpCodes.Ldarg_0);
        //    ilGen.Emit(OpCodes.Ldloca_S, errMsg);
        //    ilGen.Emit(OpCodes.Call, mValidateObjProperty.Value);
        //    ilGen.Emit(OpCodes.Brtrue_S, lblRetEmptyStr);
        //    ilGen.Emit(OpCodes.Ldloc_0);
        //    ilGen.Emit(OpCodes.Brfalse_S, lblRetEmptyStr);
        //    ilGen.Emit(OpCodes.Ldloc_0);
        //    ilGen.Emit(OpCodes.Br_S, lblRet);

        //    ilGen.MarkLabel(lblRetEmptyStr);
        //    ilGen.Emit(OpCodes.Ldstr, "");
        //    ilGen.MarkLabel(lblRet);
        //    ilGen.Emit(OpCodes.Ret);
        //    typeBuilder.DefineMethodOverride(getMethodBuilder, infoType.GetProperty("Item", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetGetMethod(false));
        //    itemProp.SetGetMethod(getMethodBuilder);
        //    #endregion

        //    return configField;
        //}
        //private static TypeBuilder CreateTypeBuilder(Configuration.IObjectConfiguration configuration, string fullName, bool notifyPropertyChanged = false, bool dataErrorInfo = false, Type dataErrorConfigurationType = null, Action<TypeBuilder, ILGenerator> fnOnCreated = null)
        //{
        //    if (configuration == null || !configuration.HasConfiguration) throw new ArgumentOutOfRangeException(nameof(configuration));
        //    var parentType = (configuration.Type ?? typeof(object));
        //    ILGenerator ilGen = null;
        //    var moduleBuilder = mCreateDynamicModuleBuilder;
        //    var typeBuilder = moduleBuilder.Value.DefineType(fullName, TypeAttributes.Public);
        //    typeBuilder.SetParent(parentType);
        //    MethodInfo mRaisePropertyChanged = null;
        //    FieldInfo fErrorConfiguration = null;
        //    if (notifyPropertyChanged) mRaisePropertyChanged = ImplementINotifyPropertyChanged(typeBuilder);
        //    if (dataErrorInfo) fErrorConfiguration = ImplementIDataErrorInfo(typeBuilder, dataErrorConfigurationType);

        //    var mConfigBuilder = typeBuilder.DefineMethod(".ConfigProxy", MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.Private | MethodAttributes.SpecialName, typeof(void), Type.EmptyTypes);
        //    var configILGen = mConfigBuilder.GetILGenerator();

        //    #region ParentConstructors
        //    //var ctorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis | CallingConventions.Standard, Type.EmptyTypes);
        //    //var ctorILGen = ctorBuilder.GetILGenerator();
        //    //var mBaseDefaultCtor = parentType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
        //    //ctorILGen.Emit(OpCodes.Ldarg_0);
        //    //ctorILGen.Emit(OpCodes.Call, mBaseDefaultCtor);
        //    //ctorILGen.Emit(OpCodes.Nop);
        //    //HHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH
        //    var baseCtors = parentType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly).Where(c => !c.IsPrivate).ToArray();
        //    foreach (var ctor in baseCtors)
        //    {
        //        var ctorParams = ctor.GetParameters();
        //        //if (ctorParams.Length == 0) continue;

        //        var mCtorBuilder = typeBuilder.DefineConstructor(ctor.Attributes, ctor.CallingConvention, ctorParams.Select(p => p.ParameterType).ToArray());
        //        ilGen = mCtorBuilder.GetILGenerator();
        //        ilGen.Emit(OpCodes.Ldarg_0);
        //        for (int pIndex = 1; pIndex <= ctorParams.Length; pIndex++) ilGen.Emit(OpCodes.Ldarg, pIndex);
        //        ilGen.Emit(OpCodes.Call, ctor);
        //        ilGen.Emit(OpCodes.Nop);

        //        ilGen.Emit(OpCodes.Ldarg_0);
        //        ilGen.Emit(OpCodes.Call, mConfigBuilder);
        //        ilGen.Emit(OpCodes.Nop);

        //        ilGen.Emit(OpCodes.Ret);
        //    }
        //    #endregion
        //    foreach (var propConfig in configuration.Properties)
        //    {
        //        bool isBaseProperty = (propConfig.BaseProperty != null);
        //        var mFieldGet = (isBaseProperty ? propConfig.BaseProperty.GetGetMethod(true) : null);
        //        if (mFieldGet != null && mFieldGet.IsPrivate) mFieldGet = null;
        //        var mFieldSet = (isBaseProperty ? propConfig.BaseProperty.GetSetMethod(true) : null);
        //        if (mFieldSet != null && mFieldSet.IsPrivate) mFieldSet = null;
        //        FieldBuilder fieldBuilder = (!isBaseProperty ? typeBuilder.DefineField(string.Concat("_", propConfig.Name), propConfig.Type, FieldAttributes.Private) : null);
        //        var propertyBuilder = typeBuilder.DefineProperty(propConfig.Name, PropertyAttributes.None, propConfig.Type, null);
        //        bool isVirtual = propConfig.BaseProperty != null && TypeHelper.IsVirtualProperty(propConfig.BaseProperty);
        //        var mGetSetAttribs = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.NewSlot;
        //        if (isVirtual) mGetSetAttribs |= MethodAttributes.Virtual;
        //        #region Get
        //        var getMethodBuilder = typeBuilder.DefineMethod(string.Concat("get_", propertyBuilder.Name), mGetSetAttribs, propertyBuilder.PropertyType, Type.EmptyTypes);
        //        ilGen = getMethodBuilder.GetILGenerator();
        //        ilGen.Emit(OpCodes.Nop);
        //        ilGen.Emit(OpCodes.Ldarg_0);
        //        if (!isBaseProperty)
        //        {
        //            ilGen.Emit(OpCodes.Ldfld, fieldBuilder);
        //            ilGen.Emit(OpCodes.Ret);
        //        }
        //        else if (mFieldGet != null)
        //        {
        //            ilGen.Emit(OpCodes.Call, mFieldGet);
        //            ilGen.Emit(OpCodes.Ret);
        //            if (isVirtual) typeBuilder.DefineMethodOverride(getMethodBuilder, mFieldGet);
        //        }
        //        propertyBuilder.SetGetMethod(getMethodBuilder);
        //        #endregion
        //        #region Set
        //        if (!isBaseProperty || mFieldSet != null)
        //        {
        //            var setMethodBuilder = typeBuilder.DefineMethod(string.Concat("set_", propertyBuilder.Name), mGetSetAttribs, null, new[] { propertyBuilder.PropertyType });
        //            ilGen = setMethodBuilder.GetILGenerator();
        //            Label? lblRet = null;
        //            #region CheckIfMustChange
        //            if (notifyPropertyChanged || dataErrorInfo)
        //            {
        //                lblRet = ilGen.DefineLabel();

        //                ilGen.Emit(OpCodes.Ldarg_0);
        //                if (isBaseProperty)
        //                    ilGen.Emit(OpCodes.Call, mFieldGet);
        //                else
        //                    ilGen.Emit(OpCodes.Ldfld, fieldBuilder);
        //                if (propConfig.Type != typeof(object)) ilGen.Emit(OpCodes.Box, propConfig.Type);
        //                ilGen.Emit(OpCodes.Ldarg_1);
        //                if (propConfig.Type != typeof(object)) ilGen.Emit(OpCodes.Box, propConfig.Type);
        //                ilGen.Emit(OpCodes.Call, mObjectEquals.Value);
        //                ilGen.Emit(OpCodes.Ldc_I4_1);
        //                ilGen.Emit(OpCodes.Ceq);
        //                ilGen.Emit(OpCodes.Brtrue_S, lblRet.Value);
        //                ilGen.Emit(OpCodes.Nop);
        //            }
        //            #endregion
        //            #region Changing
        //            if (propConfig.ChangingMethod != null)
        //            {
        //                ilGen.Emit(OpCodes.Nop);
        //                ilGen.Emit(OpCodes.Ldarg_0);
        //                ilGen.Emit(OpCodes.Call, propConfig.ChangingMethod);
        //            }
        //            #endregion
        //            #region SetProperty
        //            ilGen.Emit(OpCodes.Nop);
        //            ilGen.Emit(OpCodes.Ldarg_0);
        //            ilGen.Emit(OpCodes.Ldarg_1);
        //            if (isBaseProperty)
        //                ilGen.Emit(OpCodes.Call, mFieldSet);
        //            else
        //                ilGen.Emit(OpCodes.Stfld, fieldBuilder);
        //            #endregion
        //            #region NotifyPropertyChanged
        //            if (notifyPropertyChanged)
        //            {
        //                ilGen.Emit(OpCodes.Nop);
        //                ilGen.Emit(OpCodes.Ldarg_0);
        //                ilGen.Emit(OpCodes.Ldstr, propertyBuilder.Name);
        //                ilGen.Emit(OpCodes.Callvirt, mRaisePropertyChanged);
        //            }
        //            #endregion
        //            #region Changed
        //            if (propConfig.ChangedMethod != null)
        //            {
        //                ilGen.Emit(OpCodes.Nop);
        //                ilGen.Emit(OpCodes.Ldarg_0);
        //                ilGen.Emit(OpCodes.Call, propConfig.ChangedMethod);
        //            }
        //            #endregion
        //            #region DependencyProperties
        //            if (propConfig.DependencyProperties.Count > 0)
        //            {
        //                ilGen.Emit(OpCodes.Nop);
        //                foreach (var name in propConfig.DependencyProperties)
        //                {
        //                    ilGen.Emit(OpCodes.Ldarg_0);
        //                    ilGen.Emit(OpCodes.Ldstr, name);
        //                    ilGen.Emit(OpCodes.Callvirt, mRaisePropertyChanged);
        //                }
        //            }
        //            #endregion
        //            if (lblRet.HasValue) ilGen.MarkLabel(lblRet.Value);
        //            ilGen.Emit(OpCodes.Ret);
        //            if (isBaseProperty && isVirtual) typeBuilder.DefineMethodOverride(setMethodBuilder, mFieldSet);
        //            propertyBuilder.SetSetMethod(setMethodBuilder);
        //        }
        //        #endregion
        //        #region DefaultValue
        //        if (propConfig.HasDefaultValue)
        //        {
        //            var valueType = propConfig.DefaultValue.GetType();
        //            configILGen.Emit(OpCodes.Nop);
        //            configILGen.Emit(OpCodes.Ldarg_0);
        //            #region Type
        //            if (propConfig.DefaultValue is Type)
        //            {
        //                var dvType = (Type)propConfig.DefaultValue;
        //                if (dvType.IsArray)
        //                {
        //                    configILGen.Emit(OpCodes.Ldc_I4_0);
        //                    configILGen.Emit(OpCodes.Newarr, dvType.GetElementType());
        //                }
        //                else
        //                {
        //                    var pCtor = dvType.GetConstructor(Type.EmptyTypes);
        //                    configILGen.Emit(OpCodes.Newobj, pCtor);
        //                }
        //            }
        //            #endregion
        //            #region String
        //            else if (valueType == typeof(string))
        //            {
        //                if (propConfig.Type == typeof(string))
        //                {
        //                    EmitPushValue(propConfig.DefaultValue, configILGen);
        //                }
        //                else if ("".Equals(propConfig.DefaultValue))
        //                {
        //                    var pCtor = propConfig.Type.GetConstructor(Type.EmptyTypes);
        //                    configILGen.Emit(OpCodes.Newobj, pCtor);
        //                }
        //            }
        //            #endregion
        //            #region DateTime
        //            else if (TypeHelper.IsDateTime(propConfig.Type))
        //            {
        //                var ticks = ((DateTime)propConfig.DefaultValue).Ticks;
        //                EmitPushValue(ticks, configILGen);
        //                var pCtor = typeof(DateTime).GetConstructor(new[] { typeof(long) });
        //                if (propConfig.Type == typeof(DateTime?))
        //                {
        //                    configILGen.Emit(OpCodes.Newobj, pCtor);
        //                    pCtor = typeof(DateTime?).GetConstructor(new[] { typeof(DateTime) });
        //                }
        //                configILGen.Emit(OpCodes.Newobj, pCtor);
        //            }
        //            #endregion
        //            #region Decimal
        //            else if (TypeHelper.IsDecimalType(propConfig.Type))
        //            {
        //                var decimalValue = (valueType == typeof(decimal) ? (decimal)propConfig.DefaultValue : Convert.ToDecimal(propConfig.DefaultValue));
        //                var bits = decimal.GetBits(decimalValue);
        //                bool sign = (bits[3] & 0x80000000) != 0;
        //                int scale = (byte)((bits[3] >> 16) & 0x7F);
        //                configILGen.Emit(OpCodes.Ldc_I4, bits[0]);
        //                configILGen.Emit(OpCodes.Ldc_I4, bits[1]);
        //                configILGen.Emit(OpCodes.Ldc_I4, bits[2]);
        //                configILGen.Emit((sign ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
        //                configILGen.Emit(OpCodes.Ldc_I4, scale);
        //                var pCtor = typeof(decimal).GetConstructor(new[] { typeof(int), typeof(int), typeof(int), typeof(bool), typeof(byte) });
        //                if (propConfig.Type == typeof(decimal?))
        //                {
        //                    configILGen.Emit(OpCodes.Newobj, pCtor);
        //                    pCtor = typeof(decimal?).GetConstructor(new[] { typeof(decimal) });
        //                }
        //                configILGen.Emit(OpCodes.Newobj, pCtor);
        //            }
        //            #endregion
        //            #region Guid
        //            else if (valueType == typeof(Guid))
        //            {
        //                var bytes = ((Guid)propConfig.DefaultValue).ToByteArray();
        //                bytes.ForEach(b => EmitPushValue(b, configILGen));
        //                var pCtor = typeof(Guid).GetConstructor(new[] { typeof(byte[]) });
        //                if (propConfig.Type == typeof(Guid?))
        //                {
        //                    configILGen.Emit(OpCodes.Newobj, pCtor);
        //                    pCtor = typeof(Guid?).GetConstructor(new[] { typeof(Guid) });
        //                }
        //                configILGen.Emit(OpCodes.Newobj, pCtor);
        //            }
        //            #endregion
        //            #region TimeSpan
        //            else if (valueType == typeof(TimeSpan))
        //            {
        //                var ticks = ((TimeSpan)propConfig.DefaultValue).Ticks;
        //                EmitPushValue(ticks, configILGen);
        //                var pCtor = typeof(TimeSpan).GetConstructor(new[] { typeof(long) });
        //                if (propConfig.Type == typeof(TimeSpan?))
        //                {
        //                    configILGen.Emit(OpCodes.Newobj, pCtor);
        //                    pCtor = typeof(TimeSpan?).GetConstructor(new[] { typeof(TimeSpan) });
        //                }
        //                configILGen.Emit(OpCodes.Newobj, pCtor);
        //            }
        //            #endregion
        //            #region IntPtr
        //            else if (valueType == typeof(IntPtr))
        //            {
        //                var lPtr = ((IntPtr)propConfig.DefaultValue).ToInt64();
        //                EmitPushValue(lPtr, configILGen);
        //                var pCtor = typeof(IntPtr).GetConstructor(new[] { typeof(long) });
        //                if (propConfig.Type == typeof(IntPtr?))
        //                {
        //                    configILGen.Emit(OpCodes.Newobj, pCtor);
        //                    pCtor = typeof(IntPtr?).GetConstructor(new[] { typeof(IntPtr) });
        //                }
        //                configILGen.Emit(OpCodes.Newobj, pCtor);
        //            }
        //            #endregion
        //            #region UIntPtr
        //            else if (valueType == typeof(UIntPtr))
        //            {
        //                var ulPtr = ((UIntPtr)propConfig.DefaultValue).ToUInt64();
        //                EmitPushValue(ulPtr, configILGen);
        //                var pCtor = typeof(UIntPtr).GetConstructor(new[] { typeof(ulong) });
        //                if (propConfig.Type == typeof(UIntPtr?))
        //                {
        //                    configILGen.Emit(OpCodes.Newobj, pCtor);
        //                    pCtor = typeof(UIntPtr?).GetConstructor(new[] { typeof(UIntPtr) });
        //                }
        //                configILGen.Emit(OpCodes.Newobj, pCtor);
        //            }
        //            #endregion
        //            #region Otherwise
        //            else
        //            {
        //                EmitPushValue(propConfig.DefaultValue, configILGen);
        //                if (!EmitCanPushType(propConfig.Type))
        //                {
        //                    var pCtor = propConfig.Type.GetConstructor(new Type[] { valueType });
        //                    configILGen.Emit(OpCodes.Newobj, pCtor);
        //                }
        //            }
        //            #endregion
        //            if (isBaseProperty)
        //                configILGen.Emit(OpCodes.Call, mFieldSet);
        //            else
        //                configILGen.Emit(OpCodes.Stfld, fieldBuilder);
        //            configILGen.Emit(OpCodes.Nop);
        //        }
        //        #endregion
        //    }
        //    if (fnOnCreated != null) fnOnCreated.Invoke(typeBuilder, configILGen);
        //    #region OnCreatedMethod
        //    if (configuration.OnCreatedMethod != null)
        //    {
        //        configILGen.Emit(OpCodes.Nop);
        //        configILGen.Emit(OpCodes.Ldarg_0);
        //        configILGen.Emit(OpCodes.Call, configuration.OnCreatedMethod);
        //    }
        //    #endregion
        //    configILGen.Emit(OpCodes.Ret);
        //    return typeBuilder;
        //}
        //public static TParent CreateObject<TParent>(Configuration.ObjectConfiguration<TParent> configuration, string fullName, bool notifyPropertyChanged = false, bool dataErrorInfo = false)
        //{
        //    var type = CreateTypeBuilder(configuration, fullName, notifyPropertyChanged, dataErrorInfo, typeof(Configuration.IObjectConfiguration), null).CreateType();
        //    var obj = Activator.CreateInstance(type);
        //    if (dataErrorInfo)
        //    {
        //        var configFieldName = string.Concat("_", typeof(IDataErrorInfo).FullName, ".Configuration");
        //        var field = type.GetField(configFieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        //        TypeHelper.SetMemberValue(field, configuration, obj);
        //    }
        //    return (TParent)obj;
        //}
        internal static bool EmitCanPushType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.Char:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.String:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
            }
            return false;
        }
        internal static bool EmitPushValue(object value, ILGenerator ilGen)
        {
            switch (Type.GetTypeCode(value.GetType()))
            {
                case TypeCode.Boolean:
                    ilGen.Emit(OpCodes.Ldc_I4, ((bool)value ? 1 : 0));
                    break;
                case TypeCode.Byte:
                    ilGen.Emit(OpCodes.Ldc_I4, (int)((byte)value));
                    ilGen.Emit(OpCodes.Conv_I1);
                    break;
                case TypeCode.Char:
                    ilGen.Emit(OpCodes.Ldc_I4, (int)((char)value));
                    break;
                case TypeCode.Double:
                    ilGen.Emit(OpCodes.Ldc_R8, (double)value);
                    break;
                case TypeCode.Int16:
                    ilGen.Emit(OpCodes.Ldc_I4, (int)((short)value));
                    ilGen.Emit(OpCodes.Conv_I2);
                    break;
                case TypeCode.Int32:
                    ilGen.Emit(OpCodes.Ldc_I4, (int)value);
                    break;
                case TypeCode.Int64:
                    ilGen.Emit(OpCodes.Ldc_I8, (long)value);
                    break;
                case TypeCode.SByte:
                    ilGen.Emit(OpCodes.Ldc_I4, (int)((sbyte)value));
                    ilGen.Emit(OpCodes.Conv_I2);
                    break;
                case TypeCode.Single:
                    ilGen.Emit(OpCodes.Ldc_R4, (float)value);
                    break;
                case TypeCode.String:
                    ilGen.Emit(OpCodes.Ldstr, (string)value);
                    break;
                case TypeCode.UInt16:
                    ilGen.Emit(OpCodes.Ldc_I4, (int)((ushort)value));
                    ilGen.Emit(OpCodes.Conv_U2);
                    break;
                case TypeCode.UInt32:
                    ilGen.Emit(OpCodes.Ldc_I4, (long)((uint)value));
                    ilGen.Emit(OpCodes.Conv_U4);
                    break;
                case TypeCode.UInt64:
                    ilGen.Emit(OpCodes.Ldc_I8, (long)(ulong)value);
                    ilGen.Emit(OpCodes.Conv_U8);
                    break;
                default:
                    return false;
            }
            return true;
        }
    }
}
