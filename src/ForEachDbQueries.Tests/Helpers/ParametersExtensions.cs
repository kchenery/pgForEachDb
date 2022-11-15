using System.Collections.Generic;
using Dapper;

namespace ForEachDbQueries.Tests;

public static class ParametersExtensions
{
    public static DynamicParameters AsDynamicParameters(this object parameters)
    {
        return (DynamicParameters) parameters;
    }

    public static IEnumerable<object> ValueList(this DynamicParameters parameters)
    {
        foreach(var paramName in parameters.ParameterNames)
        {
            yield return parameters.Get<object>(paramName);
        }
    }

    public static Dictionary<string, object> AsDictionary(this DynamicParameters parameters)
    {
        var dict = new Dictionary<string, object>();
        
        foreach (var paramName in parameters.ParameterNames)
        {
            dict.Add(paramName, parameters.Get<object>(paramName));
        }

        return dict;
    }
}