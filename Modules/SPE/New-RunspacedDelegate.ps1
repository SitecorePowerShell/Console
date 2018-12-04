#https://github.com/tahir-hassan/PSRunspacedDelegate
$customType = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation.Runspaces;

namespace PowerShell
{
    public class RunspacedDelegateFactory
    {
        public static Delegate NewRunspacedDelegate(Delegate _delegate, Runspace runspace)
        {
            Action setRunspace = () => Runspace.DefaultRunspace = runspace;

            return ConcatActionToDelegate(setRunspace, _delegate);
        }

        private static Expression ExpressionInvoke(Delegate _delegate, params Expression[] arguments)
        {
            var invokeMethod = _delegate.GetType().GetMethod("Invoke");

            return Expression.Call(Expression.Constant(_delegate), invokeMethod, arguments);
        }

        public static Delegate ConcatActionToDelegate(Action a, Delegate d)
        {
            var parameters =
                d.GetType().GetMethod("Invoke").GetParameters()
                .Select(p => Expression.Parameter(p.ParameterType, p.Name))
                .ToArray();

            Expression body = Expression.Block(ExpressionInvoke(a), ExpressionInvoke(d, parameters));

            var lambda = Expression.Lambda(d.GetType(), body, parameters);

            var compiled = lambda.Compile();
            
            return compiled;
        }
    }
}
"@
Add-Type -TypeDefinition $customType

function New-RunspacedDelegate {
    param([Parameter(Mandatory=$true)][System.Delegate]$Delegate, [Runspace]$Runspace=[Runspace]::DefaultRunspace)

    [PowerShell.RunspacedDelegateFactory]::NewRunspacedDelegate($Delegate, $Runspace);
}