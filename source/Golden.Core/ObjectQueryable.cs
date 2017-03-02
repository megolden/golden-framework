namespace Golden
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    /// <summary>
    /// A simple class for creating an object-based query and then applying it to an <see cref="IQueryable{TSource}"/> data source.
    /// </summary>
    /// <typeparam name="T">The type of elements</typeparam>
    public class ObjectQueryable<T> : IOrderedQueryable<T>
    {
        #region Types

        private class ObjectQueryableProvider : IQueryProvider
        {
            public IQueryable CreateQuery(Expression expression)
            {
                throw new NotImplementedException();
            }
            public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            {
                return new ObjectQueryable<TElement>(expression, this);
            }
            public object Execute(Expression expression)
            {
                throw new NotImplementedException();
            }
            public TResult Execute<TResult>(Expression expression)
            {
                throw new NotImplementedException();
            }
        }

        #endregion
        #region Fields

        private readonly Type elementType;
        private readonly Expression expression;
        private readonly IQueryProvider provider;

        #endregion
        #region Properties

        Type IQueryable.ElementType
        {
            get { return elementType; }
        }
        Expression IQueryable.Expression
        {
            get { return expression; }
        }
        IQueryProvider IQueryable.Provider
        {
            get { return provider; }
        }
        public Dictionary<string, object> UserData { get; } =
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        #endregion
        #region Methods

        private ObjectQueryable(Expression expression, IQueryProvider provider)
        {
            this.elementType = typeof(T);
            this.expression = (expression ?? Expression.Constant(this));
            this.provider = (provider ?? new ObjectQueryableProvider());
        }
        public ObjectQueryable() : this(null, null)
        {
        }
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            throw new InvalidOperationException();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new InvalidOperationException();
        }
        public ObjectQueryable<T> SetUserData(string key, object value)
        {
            this.UserData[key] = value;
            return this;
        }
        public TValue GetUserData<TValue>(string key)
        {
            return Utility.Utilities.Convert<TValue>(UserData[key]);
        }
        
        #endregion
    }
    public static class ObjectQueryable
    {
        private class UserDataVisitor : ExpressionVisitor
        {
            private IDictionary<string, object> userData;
            private bool loaded;

            protected override Expression VisitConstant(ConstantExpression node)
            {
                var valueType = node.Value?.GetType();
                if (valueType != null)
                {
                    if (valueType.IsGenericType && valueType == typeof(ObjectQueryable<>).MakeGenericType(valueType.GetGenericArguments()[0]))
                    {
                        this.userData = (IDictionary<string, object>)Utility.TypeHelper.GetMemberValue(nameof(ObjectQueryable<object>.UserData), node.Value);
                        loaded = true;
                    }
                }
                return base.VisitConstant(node);
            }
            private UserDataVisitor()
            {
                this.userData = null;
                loaded = false;
            }
            public static IDictionary<string, object> GetUserData(IQueryable query)
            {
                var extractor = new UserDataVisitor();
                extractor.Visit(query.Expression);
                if (extractor.loaded)
                    return extractor.userData;
                //throw new ArgumentOutOfRangeException(nameof(query));
                return null;
            }
        }

        public static IDictionary<string, object> GetUserData(this IQueryable query)
        {
            return UserDataVisitor.GetUserData(query);
        }
    }
}
