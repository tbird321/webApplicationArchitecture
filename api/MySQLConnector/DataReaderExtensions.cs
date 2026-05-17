using System;
using System.Data.Common;
public static class DataReaderExtensions
{
    public static bool ColumnExists(DbDataReader reader, string columnName)
    {
        for (int i = 0; i < reader.FieldCount; i++)
        {
            if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    public static T SafeRead<T>(this DbDataReader reader, string columnName)
    {

        if (!ColumnExists(reader, columnName))
        {
            return default(T);
        }

        int ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
        {
            return default(T);
        }
        else
        {
            return (T)reader.GetValue(ordinal);
        }
    }
}
