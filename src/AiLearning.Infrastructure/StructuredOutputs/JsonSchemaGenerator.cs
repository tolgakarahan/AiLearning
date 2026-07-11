using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.CompilerServices;

namespace AiLearning.Infrastructure.StructuredOutputs;

//Sdk içeride ne yapıyor onu görmek amaçlı yazıldı.
public static class JsonSchemaGenerator
{
    public static BinaryData GenerateFor<T>()
    {
        var schema = CreateObjectSchema(typeof(T));
        var json = JsonSerializer.Serialize(schema);

        return BinaryData.FromString(json);
    }

    private static readonly NullabilityInfoContext NullabilityContext = new();

    private static bool IsRequiredProperty(PropertyInfo property)
    {
        var propertyType = property.PropertyType;

        if (propertyType.IsValueType)
        {
            return Nullable.GetUnderlyingType(propertyType) is null;
        }

        var nullability = NullabilityContext.Create(property);

        return nullability.WriteState == NullabilityState.NotNull;
    }

    private static Dictionary<string, object> CreateObjectSchema(Type type)
    {
        var properties = new Dictionary<string, object>();
        var required = new List<string>();

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var jsonPropertyName = ToJsonPropertyName(property);
            var isRequired = IsRequiredProperty(property);

            properties[jsonPropertyName] = CreatePropertySchema(
                property.PropertyType,
                allowNull: !isRequired);

            if (isRequired)
            {
                required.Add(jsonPropertyName);
            }
        }

        var schema = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["additionalProperties"] = false
        };

        if (required.Count > 0)
        {
            schema["required"] = required;
        }

        return schema;
    }

    private static Dictionary<string, object> AllowNullIfNeeded(Dictionary<string, object> schema, bool allowNull)
    {
        if (!allowNull)
            return schema;

        if (schema.TryGetValue("type", out var typeValue))
        {
            schema["type"] = new object[] { typeValue, "null" };
        }

        return schema;
    }
    private static bool TryGetEnumerableItemType(Type type, out Type itemType)
    {
        if (type.IsArray)
        {
            itemType = type.GetElementType()!;
            return true;
        }

        if (type.IsGenericType &&
            type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            itemType = type.GetGenericArguments()[0];
            return true;
        }

        var enumerableInterface = type
            .GetInterfaces()
            .FirstOrDefault(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerableInterface is not null)
        {
            itemType = enumerableInterface.GetGenericArguments()[0];
            return true;
        }

        itemType = null!;
        return false;
    }

    private static Dictionary<string, object> CreatePropertySchema(Type type, bool allowNull = false)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type == typeof(string))
        {
            return AllowNullIfNeeded(new Dictionary<string, object>
            {
                ["type"] = "string"
            }, allowNull);
        }

        if (TryGetEnumerableItemType(type, out var itemType))
        {
            return new Dictionary<string, object>
            {
                ["type"] = "array",
                ["items"] = CreatePropertySchema(itemType)
            };
        }

        if (type.IsEnum)
        {
            return AllowNullIfNeeded(new Dictionary<string, object>
            {
                ["type"] = "string",
                ["enum"] = Enum.GetNames(type)
            }, allowNull);
        }

        if (type == typeof(int) ||
            type == typeof(long) ||
            type == typeof(short))
        {
            return new Dictionary<string, object>
            {
                ["type"] = "integer"
            };
        }

        if (type == typeof(double) ||
            type == typeof(float) ||
            type == typeof(decimal))
        {
            return new Dictionary<string, object>
            {
                ["type"] = "number"
            };
        }

        if (type == typeof(bool))
        {
            return new Dictionary<string, object>
            {
                ["type"] = "boolean"
            };
        }

        if (type == typeof(DateTime) ||
            type == typeof(DateTimeOffset))
        {
            return new Dictionary<string, object>
            {
                ["type"] = "string",
                ["format"] = "date-time"
            };
        }

        if (type == typeof(Guid))
        {
            return new Dictionary<string, object>
            {
                ["type"] = "string",
                ["format"] = "uuid"
            };
        }

        return AllowNullIfNeeded(CreateObjectSchema(type), allowNull);
    }

    private static string ToJsonPropertyName(PropertyInfo property)
    {
        var attribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();

        if (attribute is not null)
            return attribute.Name;

        return JsonNamingPolicy.CamelCase.ConvertName(property.Name);
    }
}