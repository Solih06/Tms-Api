using System.Linq.Expressions;
using System.Reflection;

namespace TmsApi.Application.Utilities;

public static class DataShaper
{
    public static IEnumerable<Dictionary<string, object?>> ShapeData<T>(
        this IEnumerable<T> source, string? fields)
    {
        if (string.IsNullOrWhiteSpace(fields))
        {
            return source.Select(GetAllProperties);
        }

        var requestedFields = fields.Split(',', StringSplitOptions.TrimEntries);
        var properties = typeof(T).GetProperties(
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
            .Where(p => requestedFields.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (properties.Count == 0)
        {
            throw new BadHttpRequestException(
                $"None of the requested fields ({fields}) exist. Valid fields: {string.Join(", ", typeof(T).GetProperties().Select(p => p.Name))}");
        }

        return source.Select(e => GetSelectedProperties(e, properties));
    }

    private static Dictionary<string, object?> GetAllProperties<T>(T entity)
    {
        return typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(p => p.Name, p => p.GetValue(entity));
    }

    private static Dictionary<string, object?> GetSelectedProperties<T>(
        T entity, List<PropertyInfo> properties)
    {
        return properties.ToDictionary(p => p.Name, p => p.GetValue(entity));
    }
}