using System;
using System.Linq.Expressions;
using System.Reflection;

namespace FrameLog.Helpers
{
    public static class ExpressionHelper
    {
        public static string GetPropertyName<TModel, TProperty>(this Expression<Func<TModel, TProperty>> lambda)
        {
            Type type = typeof(TModel);

            MemberExpression member = lambda.Body as MemberExpression;
            if (member == null)
                throw new ArgumentException(string.Format("Expression '{0}' refers to a method, not a property.", lambda.ToString()));

            PropertyInfo propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException(string.Format("Expression '{0}' refers to a field, not a property.", lambda.ToString()));

            return propInfo.Name;
        }
    }
}
