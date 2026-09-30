using System.Data;
using Dapper;
using Npgsql;
using NpgsqlTypes;

namespace PatientPortal.Api.Infrastructure.Database;

public static class DapperConfiguration
{
    // Npgsql reports timestamptz as a UTC DateTime and arrays as System.Array, neither of which
    // Dapper will pass to a record constructor parameter of the intended type without a handler.
    public static void Register()
    {
        SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
        SqlMapper.AddTypeHandler(new DateOnlyHandler());
        SqlMapper.AddTypeHandler(new TextArrayHandler());
        SqlMapper.AddTypeHandler(new UuidArrayHandler());
    }

    private sealed class DateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset>
    {
        public override void SetValue(IDbDataParameter parameter, DateTimeOffset value) =>
            parameter.Value = value.ToUniversalTime();

        public override DateTimeOffset Parse(object value) =>
            value switch
            {
                DateTimeOffset offset => offset,
                DateTime dateTime => new DateTimeOffset(
                    DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
                ),
                _ => throw new InvalidCastException(
                    $"Cannot convert {value.GetType()} to DateTimeOffset."
                ),
            };
    }

    private sealed class DateOnlyHandler : SqlMapper.TypeHandler<DateOnly>
    {
        public override void SetValue(IDbDataParameter parameter, DateOnly value) =>
            parameter.Value = value;

        public override DateOnly Parse(object value) =>
            value switch
            {
                DateOnly date => date,
                DateTime dateTime => DateOnly.FromDateTime(dateTime),
                _ => throw new InvalidCastException(
                    $"Cannot convert {value.GetType()} to DateOnly."
                ),
            };
    }

    private sealed class TextArrayHandler : SqlMapper.TypeHandler<string[]>
    {
        public override void SetValue(IDbDataParameter parameter, string[]? value)
        {
            if (parameter is NpgsqlParameter npgsql)
            {
                npgsql.NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text;
            }
            parameter.Value = value;
        }

        public override string[] Parse(object value) => (string[])value;
    }

    private sealed class UuidArrayHandler : SqlMapper.TypeHandler<Guid[]>
    {
        public override void SetValue(IDbDataParameter parameter, Guid[]? value)
        {
            if (parameter is NpgsqlParameter npgsql)
            {
                npgsql.NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Uuid;
            }
            parameter.Value = value;
        }

        public override Guid[] Parse(object value) => (Guid[])value;
    }
}
