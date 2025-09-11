using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Linq;
using Sitecore.ContentSearch.Linq.Utilities;
using Spe.Commands.Data.Search;
using Spe.Core.Utility;
using DynamicExpression = Sitecore.ContentSearch.Utilities.DynamicExpression;

namespace Spe.Core.Extensions
{
    internal static class ExpressionExtensions
    {
        public static Expression<Func<T, bool>> AddPredicate<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second, SearchOperation operation = SearchOperation.And) where T : ISearchResult
        {
            switch (operation)
            {
                    case SearchOperation.And:
                        return first.And(second);
                    case SearchOperation.Or:
                        return first.Or(second);
                    default:
                        return first;
            }
        }

        public static IQueryable SelectProperties<T>(this IQueryable<T> queryable, IEnumerable<string> propertyNames)
        {
            // get propertyinfo's from original type
            var properties = typeof(T).GetProperties().Where(p => propertyNames.Contains(p.Name));
            // Creating anonymous type using dictionary of property name and property type
            var genericTypeName = queryable.ElementType.FullName;
            var propertyInfos = properties as PropertyInfo[] ?? properties.ToArray();
            var anonymousType = AnonymousTypeUtils.CreateType(genericTypeName, propertyInfos.ToDictionary(p => p.Name, p => p.PropertyType));
            var anonymousTypeConstructor = anonymousType.GetConstructors().Single();
            var anonymousTypeMembers = anonymousType.GetProperties().Cast<MemberInfo>().ToArray();

            // Create the x => expression
            var lambdaParameterExpression = Expression.Parameter(typeof(T));
            // Create the x.<propertyName>'s
            var propertyExpressions = propertyInfos.Select(p => Expression.Property(lambdaParameterExpression, p));

            // Create the new {} expression using 
            var anonymousTypeNewExpression = Expression.New(anonymousTypeConstructor, propertyExpressions, anonymousTypeMembers);

            var selectLambdaMethod = GetExpressionLambdaMethod(lambdaParameterExpression.Type, anonymousType);
            var selectBodyLambdaParameters = new object[] { anonymousTypeNewExpression, new[] { lambdaParameterExpression } };
            var selectBodyLambdaExpression = (LambdaExpression)selectLambdaMethod.Invoke(null, selectBodyLambdaParameters);

            var selectMethod = GetQueryableSelectMethod(typeof(T), anonymousType);
            //TODO: Is it possible to infer the type and hence allow for IQueryable<T>
            var selectedQueryable = selectMethod.Invoke(null, new object[] { queryable, selectBodyLambdaExpression }) as IQueryable;

            return selectedQueryable;
        }

        private static MethodInfo GetExpressionLambdaMethod(Type entityType, Type funcReturnType)
        { 
            var prototypeLambdaMethod = GetStaticMethod(() => Expression.Lambda<Func<object, object>>(default(Expression), default(IEnumerable<ParameterExpression>))); 
            var lambdaGenericMethodDefinition = prototypeLambdaMethod.GetGenericMethodDefinition(); 
            var funcType = typeof(Func<,>).MakeGenericType(entityType, funcReturnType); 
            var lambdaMethod = lambdaGenericMethodDefinition.MakeGenericMethod(funcType); 
            return lambdaMethod; 
        } 
        
        private static MethodInfo GetQueryableSelectMethod(Type entityType, Type returnType)
        { 
            var prototypeSelectMethod = GetStaticMethod(() => Queryable.Select(default(IQueryable<object>), default(Expression<Func<object, object>>))); 
            var selectGenericMethodDefinition = prototypeSelectMethod.GetGenericMethodDefinition();
            return selectGenericMethodDefinition.MakeGenericMethod(entityType, returnType);
        } 
        
        private static MethodInfo GetStaticMethod(Expression<Action> expression)
        { 
            var lambda = expression as LambdaExpression; 
            var methodCallExpression = lambda.Body as MethodCallExpression; 
            return methodCallExpression.Method; 
        }

        public static IQueryable<T> Filter<T>(this IQueryable<T> source,
            string predicate,
            params object[] values)
        {
            var lambda = DynamicExpression.ParseLambda(source.ElementType, typeof (bool), predicate, values);
            return (IQueryable<T>) source.Provider.CreateQuery(Expression.Call(typeof(QueryableExtensions), nameof(Filter),
                new Type[1]
                {
                    source.ElementType
                }, source.Expression, Expression.Quote(lambda)));
        }
    }
}