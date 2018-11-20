using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Golden
{
    public class ExpressionReplacer : ExpressionVisitor
    {
        private readonly Expression oldExpression, newExpression;

        public override Expression Visit(Expression node)
        {
            if (node == oldExpression) return newExpression;
            return base.Visit(node);
        }
        private ExpressionReplacer(Expression oldExpression, Expression newExpression)
        {
            this.oldExpression = oldExpression;
            this.newExpression = newExpression;
        }
        public static Expression Replace(Expression sourceExpression, Expression oldExpression, Expression newExpression)
        {
            return (new ExpressionReplacer(oldExpression, newExpression)).Visit(sourceExpression);
        }
    }
}
