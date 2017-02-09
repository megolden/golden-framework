namespace Golden
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    internal class ParameterReplaceVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _OldParameter, _NewParameter;

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node == _OldParameter) node = _NewParameter;
            return base.VisitParameter(node);
        }
        public ParameterReplaceVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            _OldParameter = oldParameter;
            _NewParameter = newParameter;
        }
        public static Expression Replace(Expression expression, ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            return (new ParameterReplaceVisitor(oldParameter, newParameter)).Visit(expression);
        }
    }
    internal class EFReplaceVisitor : ExpressionVisitor
    {
        #region Fields
        private static readonly Lazy<EFReplaceVisitor> _DefaultInstance = new Lazy<EFReplaceVisitor>(() => new EFReplaceVisitor());
        private static readonly Lazy<Type> _EFExtensionsType = new Lazy<Type>(() =>
        {
            var asmEF = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.ManifestModule.Name.Equals("EntityFramework.dll"));
            return asmEF?.GetType("System.Data.Entity.QueryableExtensions", false, false);
        });
        private static readonly Lazy<MethodInfo> mEFInclude = new Lazy<MethodInfo>(() =>
            _EFExtensionsType.Value?.GetMember("Include", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).OfType<MethodInfo>()
            .FirstOrDefault(mi => mi.GetGenericArguments().Length == 2));
        private static readonly Lazy<MethodInfo> mEFIncludeS = new Lazy<MethodInfo>(() =>
            _EFExtensionsType.Value?.GetMember("Include", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).OfType<MethodInfo>()
            .FirstOrDefault(mi => mi.GetGenericArguments().Length == 1));
        private static readonly Lazy<MethodInfo> mEFXTake = new Lazy<MethodInfo>(() =>
            _EFExtensionsType.Value?.GetMethod("Take", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
        private static readonly Lazy<MethodInfo> mEFXSkip = new Lazy<MethodInfo>(() =>
            _EFExtensionsType.Value?.GetMethod("Skip", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
        private static readonly Lazy<MethodInfo> mEFAsNoTracking = new Lazy<MethodInfo>(() =>
            _EFExtensionsType.Value?.GetMember("AsNoTracking", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).OfType<MethodInfo>()
            .FirstOrDefault(mi => mi.IsGenericMethod));
        #endregion
        #region Properties
        public static EFReplaceVisitor DefaultInstance
        {
            get { return _DefaultInstance.Value; }
        }
        #endregion
        #region Methods
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (mEFInclude.Value != null && node.Method.DeclaringType == typeof(ObjectQueryExtensions))
            {
                if (node.Method.Name.Equals(nameof(ObjectQueryExtensions.Include), StringComparison.Ordinal))
                {
                    if (node.Method.GetGenericArguments().Length == 2)
                        node = Expression.Call(mEFInclude.Value.MakeGenericMethod(node.Method.GetGenericArguments()), node.Arguments);
                    else
                        node = Expression.Call(mEFIncludeS.Value.MakeGenericMethod(node.Method.GetGenericArguments()), node.Arguments);
                }
                else if (node.Method.Name.Equals(nameof(ObjectQueryExtensions.Skip), StringComparison.Ordinal))
                {
                    node = Expression.Call(mEFXSkip.Value.MakeGenericMethod(node.Method.GetGenericArguments()), node.Arguments);
                }
                else if (node.Method.Name.Equals(nameof(ObjectQueryExtensions.Take), StringComparison.Ordinal))
                {
                    node = Expression.Call(mEFXTake.Value.MakeGenericMethod(node.Method.GetGenericArguments()), node.Arguments);
                }
                else if (node.Method.Name.Equals(nameof(ObjectQueryExtensions.AsNoTracking), StringComparison.Ordinal))
                {
                    node = Expression.Call(mEFAsNoTracking.Value.MakeGenericMethod(node.Method.GetGenericArguments()), node.Arguments);
                }
            }
            return base.VisitMethodCall(node);
        }
        #endregion
    }
    internal static class ObjectQueryExtensions
    {
        public static IQueryable<T> Include<T, TProperty>(this IQueryable<T> source, Expression<Func<T, TProperty>> path)
        {
            throw new NotSupportedException();
        }
        public static IQueryable<T> Include<T>(this IQueryable<T> source, string path)
        {
            throw new NotSupportedException();
        }
        public static IQueryable<T> Skip<T>(this IQueryable<T> source, Expression<Func<int>> countAccessor)
        {
            throw new NotSupportedException();
        }
        public static IQueryable<T> Take<T>(this IQueryable<T> source, Expression<Func<int>> countAccessor)
        {
            throw new NotSupportedException();
        }
        public static IQueryable<T> AsNoTracking<T>(this IQueryable<T> source) where T : class
        {
            throw new NotSupportedException();
        }
    }
    /// <summary>
    /// A simple class for creating an object-based query object and then applying it to an <see cref="IQueryable{TSource}"/> data source.
    /// </summary>
    /// <typeparam name="T">The type of elements</typeparam>
    public class ObjectQuery<T>
    {
        #region Fields

        private readonly Type _ElementType;
        private ParameterExpression _SourceParameter;
        private Expression _Expression;
        #region QueryableMethods
        private static readonly Lazy<MethodInfo> mWhere = new Lazy<MethodInfo>(() =>
            typeof(Queryable)
                .GetMember(nameof(Queryable.Where), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static).OfType<MethodInfo>()
                .FirstOrDefault(mi =>
                {
                    var parameters = mi.GetParameters();
                    if (parameters.Length != 2) return false;
                    var p1Args = parameters[1].ParameterType.GetGenericArguments();
                    if (p1Args.Length != 1) return false;
                    return (p1Args[0].GetGenericArguments().Length == 2);
                }));
        private static readonly Lazy<MethodInfo> mTake = new Lazy<MethodInfo>(() =>
            typeof(Queryable).GetMethod(nameof(Queryable.Take), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static));
        private static readonly Lazy<MethodInfo> mSkip = new Lazy<MethodInfo>(() =>
            typeof(Queryable).GetMethod(nameof(Queryable.Skip), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static));
        private static readonly Lazy<MethodInfo> mOrderBy = new Lazy<MethodInfo>(() =>
            typeof(Queryable)
                .GetMember(nameof(Queryable.OrderBy), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static).OfType<MethodInfo>()
                .FirstOrDefault(mi => mi.GetParameters().Length == 2));
        private static readonly Lazy<MethodInfo> mOrderByDescending = new Lazy<MethodInfo>(() =>
            typeof(Queryable)
                .GetMember(nameof(Queryable.OrderByDescending), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static).OfType<MethodInfo>()
                .FirstOrDefault(mi => mi.GetParameters().Length == 2));
        private static readonly Lazy<MethodInfo> mThenBy = new Lazy<MethodInfo>(() =>
            typeof(Queryable)
                .GetMember(nameof(Queryable.ThenBy), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static).OfType<MethodInfo>()
                .FirstOrDefault(mi => mi.GetParameters().Length == 2));
        private static readonly Lazy<MethodInfo> mThenByDescending = new Lazy<MethodInfo>(() =>
            typeof(Queryable)
                .GetMember(nameof(Queryable.ThenByDescending), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static).OfType<MethodInfo>()
                .FirstOrDefault(mi => mi.GetParameters().Length == 2));
        private static readonly Lazy<MethodInfo> mSelect = new Lazy<MethodInfo>(() =>
            typeof(Queryable)
                .GetMember(nameof(Queryable.Select), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static).OfType<MethodInfo>()
                .FirstOrDefault(mi =>
                {
                    var parameters = mi.GetParameters();
                    if (parameters.Length != 2) return false;
                    var p1Args = parameters[1].ParameterType.GetGenericArguments();
                    if (p1Args.Length != 1) return false;
                    return (p1Args[0].GetGenericArguments().Length == 2);
                }));
        private static readonly Lazy<MethodInfo> mSelectManyS = new Lazy<MethodInfo>(() =>
            typeof(Queryable)
                .GetMember(nameof(Queryable.SelectMany), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static).OfType<MethodInfo>()
                .FirstOrDefault(mi =>
                {
                    var parameters = mi.GetParameters();
                    if (parameters.Length != 2) return false;
                    var p1Args = parameters[1].ParameterType.GetGenericArguments();
                    if (p1Args.Length != 1) return false;
                    return (p1Args[0].GetGenericArguments().Length == 2);
                }));
        private static readonly Lazy<MethodInfo> mSelectManyCS = new Lazy<MethodInfo>(() =>
            typeof(Queryable)
                .GetMember(nameof(Queryable.SelectMany), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static).OfType<MethodInfo>()
                .FirstOrDefault(mi =>
                {
                    var parameters = mi.GetParameters();
                    if (parameters.Length != 3) return false;
                    var p1Args = parameters[1].ParameterType.GetGenericArguments();
                    if (p1Args.Length != 1) return false;
                    return (p1Args[0].GetGenericArguments().Length == 2);
                }));
        private static readonly Lazy<MethodInfo> mConcat = new Lazy<MethodInfo>(() =>
            typeof(Queryable).GetMethod(nameof(Queryable.Concat), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static));
        private static readonly Lazy<MethodInfo> mDistinct = new Lazy<MethodInfo>(() =>
            typeof(Queryable)
                .GetMember(nameof(Queryable.Distinct), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static).OfType<MethodInfo>()
                .FirstOrDefault(mi => mi.GetParameters().Length == 1));
        private static readonly Lazy<MethodInfo> mExcept = new Lazy<MethodInfo>(() =>
            typeof(Queryable)
                .GetMember(nameof(Queryable.Except), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static).OfType<MethodInfo>()
                .FirstOrDefault(mi => mi.GetParameters().Length == 2));
        private static readonly Lazy<MethodInfo> mIntersect = new Lazy<MethodInfo>(() =>
            typeof(Queryable)
                .GetMember(nameof(Queryable.Intersect), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static).OfType<MethodInfo>()
                .FirstOrDefault(mi => mi.GetParameters().Length == 2));
        private static readonly Lazy<MethodInfo> mUnion = new Lazy<MethodInfo>(() =>
            typeof(Queryable)
                .GetMember(nameof(Queryable.Union), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static).OfType<MethodInfo>()
                .FirstOrDefault(mi => mi.GetParameters().Length == 2));
        private static readonly Lazy<MethodInfo> mDefaultIfEmpty = new Lazy<MethodInfo>(() =>
            typeof(Queryable)
                .GetMember(nameof(Queryable.DefaultIfEmpty), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static).OfType<MethodInfo>()
                .FirstOrDefault(mi => mi.GetParameters().Length == 1));
        private static readonly Lazy<MethodInfo> mDefaultIfEmptyV = new Lazy<MethodInfo>(() =>
            typeof(Queryable)
                .GetMember(nameof(Queryable.DefaultIfEmpty), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static).OfType<MethodInfo>()
                .FirstOrDefault(mi => mi.GetParameters().Length == 2));
        private static readonly Lazy<MethodInfo> mCast = new Lazy<MethodInfo>(() =>
            typeof(Queryable).GetMethod(nameof(Queryable.Cast), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static));
        private static readonly Lazy<MethodInfo> mOfType = new Lazy<MethodInfo>(() =>
            typeof(Queryable).GetMethod(nameof(Queryable.OfType), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static));
        private static readonly Lazy<MethodInfo> mGroupByK = new Lazy<MethodInfo>(() =>
            typeof(Queryable)
                .GetMember(nameof(Queryable.GroupBy), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static).OfType<MethodInfo>()
                .FirstOrDefault(mi => mi.GetGenericArguments().Length == 2 && mi.GetParameters().Length == 2));
        private static readonly Lazy<MethodInfo> mGroupByKE = new Lazy<MethodInfo>(() =>
            typeof(Queryable)
                .GetMember(nameof(Queryable.GroupBy), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static).OfType<MethodInfo>()
                .FirstOrDefault(mi =>
                {
                    var genArgs = mi.GetGenericArguments();
                    var parameters = mi.GetParameters();
                    if (genArgs.Length != 3 || parameters.Length != 3) return false;
                    var p1Args = parameters[2].ParameterType.GetGenericArguments();
                    if (p1Args.Length != 1) return false;
                    return (p1Args[0].GetGenericArguments().Length == 2);
                }));
        private static readonly Lazy<MethodInfo> mGroupByKR = new Lazy<MethodInfo>(() =>
            typeof(Queryable)
                .GetMember(nameof(Queryable.GroupBy), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static).OfType<MethodInfo>()
                .FirstOrDefault(mi =>
                {
                    var genArgs = mi.GetGenericArguments();
                    var parameters = mi.GetParameters();
                    if (genArgs.Length != 3 || parameters.Length != 3) return false;
                    var p1Args = parameters[2].ParameterType.GetGenericArguments();
                    if (p1Args.Length != 1) return false;
                    return (p1Args[0].GetGenericArguments().Length == 3);
                }));
        private static readonly Lazy<MethodInfo> mGroupByKER = new Lazy<MethodInfo>(() =>
            typeof(Queryable)
                .GetMember(nameof(Queryable.GroupBy), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static).OfType<MethodInfo>()
                .FirstOrDefault(mi => mi.GetGenericArguments().Length == 4 && mi.GetParameters().Length == 4));
        private static readonly Lazy<MethodInfo> mJoin = new Lazy<MethodInfo>(() =>
            typeof(Queryable)
                .GetMember(nameof(Queryable.Join), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static).OfType<MethodInfo>()
                .FirstOrDefault(mi => mi.GetGenericArguments().Length == 4 && mi.GetParameters().Length == 5));
        private static readonly Lazy<MethodInfo> mGroupJoin = new Lazy<MethodInfo>(() =>
            typeof(Queryable)
                .GetMember(nameof(Queryable.GroupJoin), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static).OfType<MethodInfo>()
                .FirstOrDefault(mi => mi.GetGenericArguments().Length == 4 && mi.GetParameters().Length == 5));
        #endregion
        #region ObjectQueryMethods
        private static readonly Lazy<MethodInfo> mInclude = new Lazy<MethodInfo>(() =>
            typeof(ObjectQueryExtensions)
                .GetMember(nameof(ObjectQueryExtensions.Include), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).OfType<MethodInfo>()
                .FirstOrDefault(mi => mi.GetGenericArguments().Length == 2));
        private static readonly Lazy<MethodInfo> mIncludeS = new Lazy<MethodInfo>(() =>
            typeof(ObjectQueryExtensions)
                .GetMember(nameof(ObjectQueryExtensions.Include), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).OfType<MethodInfo>()
                .FirstOrDefault(mi => mi.GetGenericArguments().Length == 1));
        private static readonly Lazy<MethodInfo> mXTake = new Lazy<MethodInfo>(() =>
            typeof(ObjectQueryExtensions).GetMethod(nameof(ObjectQueryExtensions.Take), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
        private static readonly Lazy<MethodInfo> mXSkip = new Lazy<MethodInfo>(() =>
            typeof(ObjectQueryExtensions).GetMethod(nameof(ObjectQueryExtensions.Skip), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
        private static readonly Lazy<MethodInfo> mAsNoTracking = new Lazy<MethodInfo>(() =>
            typeof(ObjectQueryExtensions)
                .GetMember(nameof(ObjectQueryExtensions.AsNoTracking), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).OfType<MethodInfo>()
                .FirstOrDefault(mi => mi.IsGenericMethod));
        #endregion

        #endregion
        #region Properties

        /// <summary>
        /// Gets the expression tree that is associated with current instance of <see cref="ObjectQuery{}"/>.
        /// </summary>
        public Expression Expression
        {
            get { return _Expression; }
        }

        #endregion
        #region Methods

        /// <summary>
        /// Creates an instance of <see cref="ObjectQuery{T}"/>.
        /// </summary>
        public ObjectQuery() : this(null)
        {
        }
        /// <summary>
        /// Creates an instance of <see cref="ObjectQuery{T}"/> with an initial predicate expression.
        /// </summary>
        /// <param name="predicate">The initial predicate expression</param>
        public ObjectQuery(Expression<Func<T, bool>> predicate) : this(null, null)
        {
            if (predicate != null) _Expression = Expression.Call(mWhere.Value.MakeGenericMethod(_ElementType), _Expression, predicate);
        }
        private ObjectQuery(ParameterExpression sourceParameter, Expression expression)
        {
            _ElementType = typeof(T);
            Update(sourceParameter, expression);
        }
        private ObjectQuery<T> Update(ParameterExpression sourceParameter, Expression expression)
        {
            _SourceParameter = (sourceParameter ?? Expression.Parameter(typeof(IQueryable<T>), "source"));
            _Expression = (expression ?? _SourceParameter);
            return this;
        }
        public Expression<Func<T, bool>> GetQueryExpression()
        {
            var newExpression = EFReplaceVisitor.DefaultInstance.Visit(_Expression);
            var whereMethod = newExpression as MethodCallExpression;
            if (whereMethod != null && whereMethod.Method == mWhere.Value.MakeGenericMethod(_ElementType))
            {
                var queryExp = whereMethod.Arguments[1].GetQuoted() as Expression<Func<T, bool>>;
                if (queryExp != null) return queryExp;
            }
            throw new InvalidOperationException("Can not convert current expression to " + typeof(Expression<Func<T, bool>>).FullName);
        }
        public IQueryable<T> ApplyTo<TSource>(IQueryable<TSource> source)
        {
            var newExpression = EFReplaceVisitor.DefaultInstance.Visit(_Expression);
            return Expression.Lambda<Func<IQueryable<TSource>, IQueryable<T>>>(newExpression, _SourceParameter).Compile().Invoke(source);
        }
        public IEnumerable<T> ApplyTo<TSource>(IEnumerable<TSource> source)
        {
            return ApplyTo(source.AsQueryable()).AsEnumerable();
        }
        public ObjectQuery<T> TakePage(int pageNumber, int pageSize)
        {
            return this
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);
        }
        public ObjectQuery<T> And(Expression<Func<T, bool>> predicate)
        {
            if (_Expression.NodeType == ExpressionType.Call)
            {
                var currME = (MethodCallExpression)_Expression;
                if (currME.Method.DeclaringType == typeof(Queryable) && currME.Method.Name.Equals(nameof(Queryable.Where), StringComparison.Ordinal))
                {
                    var currLambda = currME.Arguments[1].GetQuoted<LambdaExpression>();
                    predicate = ParameterReplaceVisitor.Replace(predicate, predicate.Parameters[0], currLambda.Parameters[0]).GetQuoted<Expression<Func<T, bool>>>();
                    var newExpression = currLambda.Body.GetQuoted();
                    newExpression = Expression.AndAlso(newExpression, predicate.Body);
                    newExpression = Expression.Lambda(newExpression, currLambda.Parameters[0]);
                    newExpression = Expression.Call(currME.Method, currME.Arguments[0], newExpression);
                    return Update(_SourceParameter, newExpression);
                }
            }

            if (_Expression == _SourceParameter) return this.Where(predicate);

            throw new InvalidOperationException("Suitable condition expression not found. you must first call 'Where' method.");
        }
        //public ObjectQuery<T> And(bool condition, Expression<Func<T, bool>> truePredicate, Expression<Func<T, bool>> falsePredicate = null)
        //{
        //	if (condition) return this.And(truePredicate);
        //	if (falsePredicate != null) return this.And(falsePredicate);
        //	return this;
        //}
        public ObjectQuery<T> Or(Expression<Func<T, bool>> predicate)
        {
            if (_Expression.NodeType == ExpressionType.Call)
            {
                var currME = (MethodCallExpression)_Expression;
                if (currME.Method.DeclaringType == typeof(Queryable) && currME.Method.Name.Equals(nameof(Queryable.Where), StringComparison.Ordinal))
                {
                    var currLambda = currME.Arguments[1].GetQuoted<LambdaExpression>();
                    predicate = ParameterReplaceVisitor.Replace(predicate, predicate.Parameters[0], currLambda.Parameters[0]).GetQuoted<Expression<Func<T, bool>>>();
                    var newExpression = currLambda.Body.GetQuoted();
                    newExpression = Expression.OrElse(newExpression, predicate.Body);
                    newExpression = Expression.Lambda(newExpression, currLambda.Parameters[0]);
                    newExpression = Expression.Call(currME.Method, currME.Arguments[0], newExpression);
                    return Update(_SourceParameter, newExpression);
                }
            }

            if (_Expression == _SourceParameter) return this.Where(predicate);

            throw new InvalidOperationException("Suitable condition expression not found. you must first call 'Where' method.");
        }
        //public ObjectQuery<T> Or(bool condition, Expression<Func<T, bool>> truePredicate, Expression<Func<T, bool>> falsePredicate = null)
        //{
        //	if (condition) return this.Or(truePredicate);
        //	if (falsePredicate != null) return this.Or(falsePredicate);
        //	return this;
        //}
        public ObjectQuery<T> Not()
        {
            if (_Expression.NodeType == ExpressionType.Call)
            {
                var currME = (MethodCallExpression)_Expression;
                if (currME.Method.DeclaringType == typeof(Queryable) && currME.Method.Name.Equals(nameof(Queryable.Where), StringComparison.Ordinal))
                {
                    var currLambda = currME.Arguments[1].GetQuoted<LambdaExpression>();
                    var newExpression = currLambda.Body.GetQuoted();
                    newExpression = Expression.Not(newExpression);
                    newExpression = Expression.Lambda(newExpression, currLambda.Parameters[0]);
                    newExpression = Expression.Call(currME.Method, currME.Arguments[0], newExpression);
                    return Update(_SourceParameter, newExpression);
                }
            }

            throw new InvalidOperationException("Suitable condition expression not found. you must first call 'Where' method.");
        }
        public override string ToString()
        {
            return _Expression.ToString();
        }
        #region Queryable
        public ObjectQuery<T> Where(Expression<Func<T, bool>> predicate)
        {
            var newExpression = Expression.Call(mWhere.Value.MakeGenericMethod(_ElementType), _Expression, predicate);
            return Update(_SourceParameter, newExpression);
        }
        public ObjectQuery<T> Take(int count)
        {
            var newExpression = Expression.Call(mTake.Value.MakeGenericMethod(_ElementType), _Expression, Expression.Constant(count, typeof(int)));
            return Update(_SourceParameter, newExpression);
        }
        public ObjectQuery<T> Skip(int count)
        {
            var newExpression = Expression.Call(mSkip.Value.MakeGenericMethod(_ElementType), _Expression, Expression.Constant(count, typeof(int)));
            return Update(_SourceParameter, newExpression);
        }
        public ObjectQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            var newExpression = Expression.Call(mOrderBy.Value.MakeGenericMethod(_ElementType, typeof(TKey)), _Expression, keySelector);
            return Update(_SourceParameter, newExpression);
        }
        public ObjectQuery<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            var newExpression = Expression.Call(mOrderByDescending.Value.MakeGenericMethod(_ElementType, typeof(TKey)), _Expression, keySelector);
            return Update(_SourceParameter, newExpression);
        }
        public ObjectQuery<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            var newExpression = Expression.Call(mThenBy.Value.MakeGenericMethod(_ElementType, typeof(TKey)), _Expression, keySelector);
            return Update(_SourceParameter, newExpression);
        }
        public ObjectQuery<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            var newExpression = Expression.Call(mThenByDescending.Value.MakeGenericMethod(_ElementType, typeof(TKey)), _Expression, keySelector);
            return Update(_SourceParameter, newExpression);
        }
        public ObjectQuery<TResult> Select<TResult>(Expression<Func<T, TResult>> selector)
        {
            var newExpression = Expression.Call(mSelect.Value.MakeGenericMethod(_ElementType, typeof(TResult)), _Expression, selector);
            return new ObjectQuery<TResult>(_SourceParameter, newExpression);
        }
        public ObjectQuery<TResult> SelectMany<TResult>(Expression<Func<T, IEnumerable<TResult>>> selector)
        {
            var newExpression = Expression.Call(mSelectManyS.Value.MakeGenericMethod(_ElementType, typeof(TResult)), _Expression, selector);
            return new ObjectQuery<TResult>(_SourceParameter, newExpression);
        }
        public ObjectQuery<TResult> SelectMany<TCollection, TResult>(Expression<Func<T, IEnumerable<TCollection>>> collectionSelector, Expression<Func<T, TCollection, TResult>> resultSelector)
        {
            var newExpression = Expression.Call(mSelectManyCS.Value.MakeGenericMethod(_ElementType, typeof(TCollection), typeof(TResult)), _Expression, collectionSelector, resultSelector);
            return new ObjectQuery<TResult>(_SourceParameter, newExpression);
        }
        public ObjectQuery<T> Concat(IEnumerable<T> source2)
        {
            var newExpression = Expression.Call(mConcat.Value.MakeGenericMethod(_ElementType), _Expression, Expression.Constant(source2, source2.GetType()));
            return Update(_SourceParameter, newExpression);
        }
        public ObjectQuery<T> Distinct()
        {
            var newExpression = Expression.Call(mDistinct.Value.MakeGenericMethod(_ElementType), _Expression);
            return Update(_SourceParameter, newExpression);
        }
        public ObjectQuery<T> Except(IEnumerable<T> source2)
        {
            var newExpression = Expression.Call(mExcept.Value.MakeGenericMethod(_ElementType), _Expression, Expression.Constant(source2, source2.GetType()));
            return Update(_SourceParameter, newExpression);
        }
        public ObjectQuery<T> Intersect(IEnumerable<T> source2)
        {
            var newExpression = Expression.Call(mIntersect.Value.MakeGenericMethod(_ElementType), _Expression, Expression.Constant(source2, source2.GetType()));
            return Update(_SourceParameter, newExpression);
        }
        public ObjectQuery<T> Union(IEnumerable<T> source2)
        {
            var newExpression = Expression.Call(mUnion.Value.MakeGenericMethod(_ElementType), _Expression, Expression.Constant(source2, source2.GetType()));
            return Update(_SourceParameter, newExpression);
        }
        public ObjectQuery<T> DefaultIfEmpty()
        {
            var newExpression = Expression.Call(mDefaultIfEmpty.Value.MakeGenericMethod(_ElementType), _Expression);
            return Update(_SourceParameter, newExpression);
        }
        public ObjectQuery<T> DefaultIfEmpty(T defaultValue)
        {
            var newExpression = Expression.Call(mDefaultIfEmptyV.Value.MakeGenericMethod(_ElementType), _Expression, Expression.Constant(defaultValue, _ElementType));
            return Update(_SourceParameter, newExpression);
        }
        public ObjectQuery<TResult> Cast<TResult>()
        {
            var newExpression = Expression.Call(mCast.Value.MakeGenericMethod(typeof(TResult)), _Expression);
            return new ObjectQuery<TResult>(_SourceParameter, newExpression);
        }
        public ObjectQuery<TResult> OfType<TResult>()
        {
            var newExpression = Expression.Call(mOfType.Value.MakeGenericMethod(typeof(TResult)), _Expression);
            return new ObjectQuery<TResult>(_SourceParameter, newExpression);
        }
        public ObjectQuery<IGrouping<TKey, T>> GroupBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            var newExpression = Expression.Call(mGroupByK.Value.MakeGenericMethod(_ElementType, typeof(TKey)), _Expression, keySelector);
            return new ObjectQuery<IGrouping<TKey, T>>(_SourceParameter, newExpression);
        }
        public ObjectQuery<IGrouping<TKey, TElement>> GroupBy<TKey, TElement>(Expression<Func<T, TKey>> keySelector, Expression<Func<T, TElement>> elementSelector)
        {
            var newExpression = Expression.Call(mGroupByKE.Value.MakeGenericMethod(_ElementType, typeof(TKey), typeof(TElement)), _Expression, keySelector, elementSelector);
            return new ObjectQuery<IGrouping<TKey, TElement>>(_SourceParameter, newExpression);
        }
        public ObjectQuery<TResult> GroupBy<TKey, TResult>(Expression<Func<T, TKey>> keySelector, Expression<Func<TKey, IEnumerable<T>, TResult>> resultSelector)
        {
            var newExpression = Expression.Call(mGroupByKR.Value.MakeGenericMethod(_ElementType, typeof(TKey), typeof(TResult)), _Expression, keySelector, resultSelector);
            return new ObjectQuery<TResult>(_SourceParameter, newExpression);
        }
        public ObjectQuery<TResult> GroupBy<TKey, TElement, TResult>(Expression<Func<T, TKey>> keySelector, Expression<Func<T, TElement>> elementSelector, Expression<Func<TKey, IEnumerable<TElement>, TResult>> resultSelector)
        {
            var newExpression = Expression.Call(mGroupByKER.Value.MakeGenericMethod(_ElementType, typeof(TKey), typeof(TElement), typeof(TResult)), _Expression, keySelector, elementSelector, resultSelector);
            return new ObjectQuery<TResult>(_SourceParameter, newExpression);
        }
        public ObjectQuery<TResult> Join<TInner, TKey, TResult>(IEnumerable<TInner> inner, Expression<Func<T, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<T, TInner, TResult>> resultSelector)
        {
            var newExpression = Expression.Call(mJoin.Value.MakeGenericMethod(_ElementType, typeof(TInner), typeof(TKey), typeof(TResult)), _Expression, Expression.Constant(inner, inner.GetType()), outerKeySelector, innerKeySelector, resultSelector);
            return new ObjectQuery<TResult>(_SourceParameter, newExpression);
        }
        public ObjectQuery<TResult> GroupJoin<TInner, TKey, TResult>(IEnumerable<TInner> inner, Expression<Func<T, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<T, IEnumerable<TInner>, TResult>> resultSelector)
        {
            var newExpression = Expression.Call(mGroupJoin.Value.MakeGenericMethod(_ElementType, typeof(TInner), typeof(TKey), typeof(TResult)), _Expression, Expression.Constant(inner, inner.GetType()), outerKeySelector, innerKeySelector, resultSelector);
            return new ObjectQuery<TResult>(_SourceParameter, newExpression);
        }
        #endregion
        #region EntityFramework
        public ObjectQuery<T> Include<TProperty>(Expression<Func<T, TProperty>> path)
        {
            var newExpression = Expression.Call(mInclude.Value.MakeGenericMethod(_ElementType, typeof(TProperty)), _Expression, path);
            return Update(_SourceParameter, newExpression);
        }
        public ObjectQuery<T> Include(string path)
        {
            var newExpression = Expression.Call(mIncludeS.Value.MakeGenericMethod(_ElementType), _Expression, Expression.Constant(path, typeof(string)));
            return Update(_SourceParameter, newExpression);
        }
        public ObjectQuery<T> Skip(Expression<Func<int>> countAccessor)
        {
            var newExpression = Expression.Call(mXSkip.Value.MakeGenericMethod(_ElementType), _Expression, countAccessor);
            return Update(_SourceParameter, newExpression);
        }
        public ObjectQuery<T> Take(Expression<Func<int>> countAccessor)
        {
            var newExpression = Expression.Call(mXTake.Value.MakeGenericMethod(_ElementType), _Expression, countAccessor);
            return Update(_SourceParameter, newExpression);
        }
        public ObjectQuery<T> TakePage(Expression<Func<int>> pageNumberAccessor, Expression<Func<int>> pageSizeAccessor)
        {
            var temp = pageNumberAccessor.Body;
            if (temp.NodeType == ExpressionType.Quote) temp = ((UnaryExpression)temp).Operand;
            temp = Expression.Subtract(temp, Expression.Constant(1, typeof(int)));
            temp = Expression.Multiply(temp, pageSizeAccessor.Body);
            return this
                .Skip(Expression.Lambda<Func<int>>(temp))
                .Take(pageSizeAccessor);
        }
        internal ObjectQuery<T> AsNoTracking()
        {
            var newExpression = Expression.Call(mAsNoTracking.Value.MakeGenericMethod(_ElementType), _Expression);
            return Update(_SourceParameter, newExpression);
        }
        #endregion

        #endregion
    }
}

namespace System.Linq
{
    using Golden;
    using System.Collections.Generic;

    public static class ObjectQueryExtensions
    {
        /// <summary>
        /// Applying an <see cref="ObjectQuery{T}" /> object on input data source.
        /// </summary>
        /// <typeparam name="TSource">The type of the data in the input data source</typeparam>
        /// <typeparam name="TResult">The type of the value returned by query object</typeparam>
        /// <param name="source">The input data source</param>
        /// <param name="query">The query object for applying</param>
        /// <returns>An <see cref="IQueryable{T}"/> whose elements are the result of applying object query on input data source.</returns>
        public static IQueryable<TResult> ApplyQuery<TSource, TResult>(this IQueryable<TSource> source, ObjectQuery<TResult> query)
        {
            if (object.ReferenceEquals(query, null)) return (IQueryable<TResult>)source;
            return query.ApplyTo(source);
        }
        /// <summary>
        /// Applying an <see cref="ObjectQuery{T}" /> object on input data source.
        /// </summary>
        /// <typeparam name="TSource">The type of the data in the input data source</typeparam>
        /// <typeparam name="TResult">The type of the value returned by query object</typeparam>
        /// <param name="source">The input data source</param>
        /// <param name="query">The query object for applying</param>
        /// <returns>An <see cref="IEnumerable{T}"/> whose elements are the result of applying object query on input data source.</returns>
        public static IEnumerable<TResult> ApplyQuery<TSource, TResult>(this IEnumerable<TSource> source, ObjectQuery<TResult> query)
        {
            if (object.ReferenceEquals(query, null)) return (IEnumerable<TResult>)source;
            return query.ApplyTo(source);
        }
    }
}
