#requires -Version 2.0

$methodLookup = @{}

function Invoke-GenericMethod
{
    <#
    .Synopsis
       Invokes Generic methods on .NET Framework types
    .DESCRIPTION
       Allows the caller to invoke a Generic method on a .NET object or class with a single function call.  Invoke-GenericMethod handles identifying the proper method overload, parameters with default values, and to some extent, the same type conversion behavior you expect when calling a normal .NET Framework method from PowerShell.
    .PARAMETER InputObject
       The object on which to invoke an instance generic method.
    .PARAMETER Type
       The .NET class on which to invoke a static generic method.
    .PARAMETER MethodName
       The name of the generic method to be invoked.
    .PARAMETER GenericType
       One or more types which are specified when calling the generic method.  For example, if a method's signature is "string MethodName<T>();", and you want T to be a String, then you would pass "string" or ([string]) to the Type parameter of Invoke-GenericMethod.
    .PARAMETER ArgumentList
       The arguments to be passed on to the generic method when it is invoked.  The order of the arguments must match that of the .NET method's signature; named parameters are not currently supported.
    .EXAMPLE
       Invoke-GenericMethod -InputObject $someObject -MethodName SomeMethodName -GenericType string -ArgumentList $arg1,$arg2,$arg3

       Invokes a generic method on an object.  The signature of this method would be something like this (containing 3 arguments and a single Generic type argument):  object SomeMethodName<T>(object arg1, object arg2, object arg3);
    .EXAMPLE
       $someObject | Invoke-GenericMethod -MethodName SomeMethodName -GenericType string -ArgumentList $arg1,$arg2,$arg3

       Same as example 1, except $someObject is passed to the function via the pipeline.
    .EXAMPLE
       Invoke-GenericMethod -Type SomeClass -MethodName SomeMethodName -GenericType string,int -ArgumentList $arg1,$arg2,$arg3

       Invokes a static generic method on a class.  The signature of this method would be something like this (containing 3 arguments and two Generic type arguments):  static object SomeMethodName<T1,T2> (object arg1, object arg2, object arg3);
    .INPUTS
       System.Object
    .OUTPUTS
       System.Object
    .NOTES
       Known issues:

       Ref / Out parameters and [PSReference] objects are currently not working properly, and I don't think there's a way to fix that from within PowerShell.  I'll have to expand on the
       PSGenericTypes.MethodInvoker.InvokeMethod() C# code to account for that.
    #>

    [CmdletBinding(DefaultParameterSetName = 'Instance')]
    param (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true, ParameterSetName = 'Instance')]
        $InputObject,

        [Parameter(Mandatory = $true, ParameterSetName = 'Static')]
        [Type]
        $Type,

        [Parameter(Mandatory = $true)]
        [string]
        $MethodName,

        [Parameter(Mandatory = $true)]
        [Type[]]
        $GenericType,

        [Object[]]
        $ArgumentList
    )

    process
    {
        switch ($PSCmdlet.ParameterSetName)
        {
            'Instance'
            {
                $_type  = $InputObject.GetType()
                $object = $InputObject
                $flags  = [System.Reflection.BindingFlags] 'Instance, Public'
            }

            'Static'
            {
                $_type  = $Type
                $object = $null
                $flags  = [System.Reflection.BindingFlags] 'Static, Public'
            }
        }

        if ($null -ne $ArgumentList)
        {
            $argList = $ArgumentList.Clone()
        }
        else
        {
            $argList = @()
        }

        $params = @{
            Type         = $_type
            BindingFlags = $flags
            MethodName   = $MethodName
            GenericType  = $GenericType
            ArgumentList = [ref]$argList
        }
        $hash = ([pscustomobject]$params).GetHashCode()
        if($methodLookup.Contains($hash)) {
            $method = $methodLookup[$hash]
        } else {
            $method = Get-GenericMethod @params
            $methodLookup.Add($hash,$method)
        }
        if ($null -eq $method)
        {
            Write-Error "No matching method was found"
            return
        }

        # I'm not sure why, but PowerShell appears to be passing instances of PSObject when $argList contains generic types.  Instead of calling
        # $method.Invoke here from PowerShell, I had to write the PSGenericMethods.MethodInvoker.InvokeMethod helper code in C# to enumerate the
        # argument list and replace any instances of PSObject with their BaseObject before calling $method.Invoke().

        return [PSGenericMethods.MethodInvoker]::InvokeMethod($method, $object, $argList)

    } # process

} # function Invoke-GenericMethod

function Get-GenericMethod
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [Type]
        $Type,

        [Parameter(Mandatory = $true)]
        [string]
        $MethodName,

        [Parameter(Mandatory = $true)]
        [Type[]]
        $GenericType,

        [ref]
        $ArgumentList,

        [System.Reflection.BindingFlags]
        $BindingFlags = [System.Reflection.BindingFlags]::Default,

        [switch]
        $WithCoercion
    )

    if ($null -eq $ArgumentList.Value)
    {
        $originalArgList = @()
    }
    else
    {
        $originalArgList = @($ArgumentList.Value)
    }

    foreach ($method in $Type.GetMethods($BindingFlags))
    {
        $argList = $originalArgList.Clone()

        if (-not $method.IsGenericMethod -or $method.Name -ne $MethodName) { continue }
        if ($GenericType.Count -ne $method.GetGenericArguments().Count) { continue }

        if (Test-GenericMethodParameters -MethodInfo $method -ArgumentList ([ref]$argList) -GenericType $GenericType -WithCoercion:$WithCoercion)
        {
            $ArgumentList.Value = $argList
            return $method.MakeGenericMethod($GenericType)
        }
    }

    if (-not $WithCoercion)
    {
        $null = $PSBoundParameters.Remove('WithCoercion')
        return Get-GenericMethod @PSBoundParameters -WithCoercion
    }

} # function Get-GenericMethod

function Test-GenericMethodParameters
{
    [CmdletBinding()]
    param (
        [System.Reflection.MethodInfo] $MethodInfo,

        [ref]
        $ArgumentList,

        [Parameter(Mandatory = $true)]
        [Type[]]
        $GenericType,

        [switch]
        $WithCoercion
    )

    if ($null -eq $ArgumentList.Value)
    {
        $argList = @()
    }
    else
    {
        $argList = @($ArgumentList.Value)
    }

    $parameterList = $MethodInfo.GetParameters()

    $arrayType = $null

    $hasParamsArray = HasParamsArray -ParameterList $parameterList

    if ($parameterList.Count -lt $argList.Count -and -not $hasParamsArray)
    {
        return $false
    }

    $methodGenericType = $MethodInfo.GetGenericArguments()

    for ($i = 0; $i -lt $argList.Count; $i++)
    {
        $params = @{
            ArgumentList       = $argList
            ParameterList      = $ParameterList
            WithCoercion       = $WithCoercion
            RuntimeGenericType = $GenericType
            MethodGenericType  = $methodGenericType
            Index              = [ref]$i
            ArrayType          = [ref]$arrayType
        }

        $isOk = TryMatchParameter @params

        if (-not $isOk) { return $false }
    }

    $defaults = New-Object System.Collections.ArrayList

    for ($i = $argList.Count; $i -lt $parameterList.Count; $i++)
    {
        if (-not $parameterList[$i].HasDefaultValue)  { return $false }
        $null = $defaults.Add($parameterList[$i].DefaultValue)
    }

    # When calling a method with a params array using MethodInfo, you have to pass in the array; the
    # params argument approach doesn't work.

    if ($hasParamsArray)
    {
        $firstArrayIndex = $parameterList.Count - 1
        $lastArrayIndex = $argList.Count - 1

        $newArgList = $argList[0..$firstArrayIndex]
        $newArgList[$firstArrayIndex] = $argList[$firstArrayIndex..$lastArrayIndex] -as $arrayType
        $argList = $newArgList
    }

    $ArgumentList.Value = $argList + $defaults.ToArray()

    return $true

} # function Test-GenericMethodParameters

function TryMatchParameter
{
    param (
        [System.Reflection.ParameterInfo[]]
        $ParameterList,

        [object[]]
        $ArgumentList,

        [Type[]]
        $MethodGenericType,

        [Type[]]
        $RuntimeGenericType,

        [switch]
        $WithCoercion,

        [ref] $Index,
        [ref] $ArrayType
    )

    $params = @{
        ParameterType = $ParameterList[$Index.Value].ParameterType
        RuntimeType   = $RuntimeGenericType
        GenericType   = $MethodGenericType
    }

    $runtimeType = Resolve-RuntimeType @params

    if ($null -eq $runtimeType)
    {
        throw "Could not determine runtime type of parameter '$($ParameterList[$Index.Value].Name)'"
    }

    $isParamsArray = IsParamsArray -ParameterInfo $ParameterList[$Index.Value]

    if ($isParamsArray)
    {
        $ArrayType.Value = $runtimeType
        $runtimeType     = $runtimeType.GetElementType()
    }

    do
    {
        $isOk = TryMatchArgument @PSBoundParameters -RuntimeType $runtimeType
        if (-not $isOk) { return $false }

        if ($isParamsArray) { $Index.Value++ }
    }
    while ($isParamsArray -and $Index.Value -lt $ArgumentList.Count)

    return $true
}

function TryMatchArgument
{
    param (
        [System.Reflection.ParameterInfo[]]
        $ParameterList,

        [object[]]
        $ArgumentList,

        [Type[]]
        $MethodGenericType,

        [Type[]]
        $RuntimeGenericType,

        [switch]
        $WithCoercion,

        [ref] $Index,
        [ref] $ArrayType,

        [Type] $RuntimeType
    )

    $argValue = $ArgumentList[$Index.Value]
    $argType = Get-Type $argValue

    $isByRef = $RuntimeType.IsByRef
    if ($isByRef)
    {
        if ($ArgumentList[$Index.Value] -isnot [ref]) { return $false }

        $RuntimeType = $RuntimeType.GetElementType()
        $argValue = $argValue.Value
        $argType = Get-Type $argValue
    }

    $isNullNullable = $false

    while ($RuntimeType.FullName -like 'System.Nullable``1*')
    {
        if ($null -eq $argValue)
        {
            $isNullNullable = $true
            break
        }

        $RuntimeType = $RuntimeType.GetGenericArguments()[0]
    }

    if ($isNullNullable) { continue }

    if ($null -eq $argValue)
    {
        return -not $RuntimeType.IsValueType
    }
    else
    {
        if ($argType -ne $RuntimeType)
        {
            $argValue = $argValue -as $RuntimeType
            if (-not $WithCoercion -or $null -eq $argValue)  { return $false }
        }

        if ($isByRef)
        {
            $ArgumentList[$Index.Value].Value = $argValue
        }
        else
        {
            $ArgumentList[$Index.Value] = $argValue
        }
    }

    return $true
}
function HasParamsArray([System.Reflection.ParameterInfo[]] $ParameterList)
{
    return $ParameterList.Count -gt 0 -and (IsParamsArray -ParameterInfo $ParameterList[-1])
}

function IsParamsArray([System.Reflection.ParameterInfo] $ParameterInfo)
{
    return @($ParameterInfo.GetCustomAttributes([System.ParamArrayAttribute], $true)).Count -gt 0
}

function Resolve-RuntimeType
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [Type]
        $ParameterType,

        [Parameter(Mandatory = $true)]
        [Type[]]
        $RuntimeType,

        [Parameter(Mandatory = $true)]
        [Type[]]
        $GenericType
    )

    if ($ParameterType.IsByRef)
    {
        $elementType = Resolve-RuntimeType -ParameterType $ParameterType.GetElementType() -RuntimeType $RuntimeType -GenericType $GenericType
        return $elementType.MakeByRefType()
    }
    elseif ($ParameterType.IsGenericParameter)
    {
        for ($i = 0; $i -lt $GenericType.Count; $i++)
        {
            if ($ParameterType -eq $GenericType[$i])
            {
                return $RuntimeType[$i]
            }
        }
    }
    elseif ($ParameterType.IsArray)
    {
        $arrayType = $ParameterType
        $elementType = Resolve-RuntimeType -ParameterType $ParameterType.GetElementType() -RuntimeType $RuntimeType -GenericType $GenericType

        if ($ParameterType.GetElementType().IsGenericParameter)
        {
            $arrayRank = $arrayType.GetArrayRank()

            if ($arrayRank -eq 1)
            {
                $arrayType = $elementType.MakeArrayType()
            }
            else
            {
                $arrayType = $elementType.MakeArrayType($arrayRank)
            }
        }

        return $arrayType
    }
    elseif ($ParameterType.ContainsGenericParameters)
    {
        $genericArguments = $ParameterType.GetGenericArguments()
        $runtimeArguments = New-Object System.Collections.ArrayList

        foreach ($argument in $genericArguments)
        {
            $null = $runtimeArguments.Add((Resolve-RuntimeType -ParameterType $argument -RuntimeType $RuntimeType -GenericType $GenericType))
        }

        $definition = $ParameterType
        if (-not $definition.IsGenericTypeDefinition)
        {
            $definition = $definition.GetGenericTypeDefinition()
        }

        return $definition.MakeGenericType($runtimeArguments.ToArray())
    }
    else
    {
        return $ParameterType
    }
}

function Get-Type($object)
{
    if ($null -eq $object) { return $null }
    return $object.GetType()
}

Add-Type -ErrorAction Stop -TypeDefinition @'
    namespace PSGenericMethods
    {
        using System;
        using System.Reflection;
        using System.Management.Automation;

        public static class MethodInvoker
        {
            public static object InvokeMethod(MethodInfo method, object target, object[] arguments)
            {
                if (method == null) { throw new ArgumentNullException("method"); }

                object[] args = null;

                if (arguments != null)
                {
                    args = (object[])arguments.Clone();
                    for (int i = 0; i < args.Length; i++)
                    {
                        PSObject pso = args[i] as PSObject;
                        if (pso != null)
                        {
                            args[i] = pso.BaseObject;
                        }

                        PSReference psref = args[i] as PSReference;

                        if (psref != null)
                        {
                            args[i] = psref.Value;
                        }
                    }
                }

                object result = method.Invoke(target, args);

                for (int i = 0; i < arguments.Length; i++)
                {
                    PSReference psref = arguments[i] as PSReference;

                    if (psref != null)
                    {
                        psref.Value = args[i];
                    }
                }

                return result;
            }
        }
    }
'@