﻿// Copyright (c) Arjen Post. See License.txt in the project root for license information.
// Credits go to Richard Dingwall (https://github.com/rdingwall) for the original idea of the IDataReader serializer.

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace ProtoBuf.Data.Light
{
    internal static class ProtoBufDataWriter
    {
        internal static void WriteReader(Stream stream, IDataReader reader)
        {
            var resultIndex = 0;

            using (var protoWriter = new ProtoWriter(stream, null, null))
            {
                ProtoWriter.WriteFieldHeader(FieldHeaders.RecordsAffected, WireType.Variant, protoWriter);
                ProtoWriter.WriteInt32(reader.RecordsAffected, protoWriter);

                do
                {
                    ProtoWriter.WriteFieldHeader(FieldHeaders.Result, WireType.StartGroup, protoWriter);

                    var resultToken = ProtoWriter.StartSubItem(resultIndex, protoWriter);
                    var columns = GetColumns(reader);

                    WriteColumns(columns, protoWriter);
                    WriteRows(reader, columns, protoWriter);

                    ProtoWriter.EndSubItem(resultToken, protoWriter);

                    resultIndex++;
                }
                while (reader.NextResult());
            }
        }

        private static List<ProtoBufDataColumn> GetColumns(IDataReader dataReader)
        {
            var columns = new List<ProtoBufDataColumn>(dataReader.FieldCount);

            for (var i = 0; i < dataReader.FieldCount; i++)
            {
                var dataType = dataReader.GetFieldType(i);

                columns.Add(new ProtoBufDataColumn
                {
                    Name = dataReader.GetName(i),
                    Ordinal = i,
                    DataType = dataType,
                    ProtoBufDataType = TypeHelper.GetProtoBufDataType(dataType)
                });
            }

            return columns;
        }

        private static void WriteColumns(List<ProtoBufDataColumn> columns, ProtoWriter writer)
        {
            ProtoWriter.WriteFieldHeader(FieldHeaders.Columns, WireType.StartGroup, writer);

            var columnsToken = ProtoWriter.StartSubItem(columns, writer);

            foreach (var column in columns)
            {
                ProtoWriter.WriteFieldHeader(FieldHeaders.Column, WireType.StartGroup, writer);

                var columnToken = ProtoWriter.StartSubItem(column, writer);

                ProtoWriter.WriteFieldHeader(FieldHeaders.ColumnName, WireType.String, writer);
                ProtoWriter.WriteString(column.Name, writer);
                ProtoWriter.WriteFieldHeader(FieldHeaders.ColumnType, WireType.Variant, writer);
                ProtoWriter.WriteInt32((int)column.ProtoBufDataType, writer);

                ProtoWriter.EndSubItem(columnToken, writer);
            }

            ProtoWriter.EndSubItem(columnsToken, writer);
        }

        private static void WriteRows(IDataReader reader, List<ProtoBufDataColumn> columns, ProtoWriter writer)
        {
            ProtoWriter.WriteFieldHeader(FieldHeaders.Records, WireType.StartGroup, writer);

            var rowsToken = ProtoWriter.StartSubItem(reader, writer);

            var rowIndex = 0;

            while (reader.Read())
            {
                ProtoWriter.WriteFieldHeader(FieldHeaders.Record, WireType.StartGroup, writer);

                var rowToken = ProtoWriter.StartSubItem(rowIndex, writer);

                for (var i = 0; i < columns.Count; i++)
                {
                    var fieldHeader = i + 1;

                    if (!reader.IsDBNull(columns[i].Ordinal))
                    {
                        switch (columns[i].ProtoBufDataType)
                        {
                            case ProtoBufDataType.Bool:
                                ProtoWriter.WriteFieldHeader(fieldHeader, WireType.Variant, writer);
                                ProtoWriter.WriteBoolean(reader.GetBoolean(columns[i].Ordinal), writer);
                                break;
                            case ProtoBufDataType.Byte:
                                ProtoWriter.WriteFieldHeader(fieldHeader, WireType.Variant, writer);
                                ProtoWriter.WriteByte(reader.GetByte(columns[i].Ordinal), writer);
                                break;
                            case ProtoBufDataType.ByteArray:
                                ProtoWriter.WriteFieldHeader(fieldHeader, WireType.String, writer);
                                var value = reader[columns[i].Ordinal];
                                ProtoWriter.WriteBytes((byte[])value, 0, ((byte[])value).Length, writer);
                                break;
                            case ProtoBufDataType.Char:
                                ProtoWriter.WriteFieldHeader(fieldHeader, WireType.Variant, writer);
                                ProtoWriter.WriteInt16((short)reader.GetChar(columns[i].Ordinal), writer);
                                break;
                            case ProtoBufDataType.CharArray:
                                ProtoWriter.WriteFieldHeader(fieldHeader, WireType.String, writer);
                                ProtoWriter.WriteString(new string((char[])reader[columns[i].Ordinal]), writer);
                                break;
                            case ProtoBufDataType.DateTime:
                                ProtoWriter.WriteFieldHeader(fieldHeader, WireType.StartGroup, writer);
                                BclHelpers.WriteDateTime(reader.GetDateTime(columns[i].Ordinal), writer);
                                break;
                            case ProtoBufDataType.Decimal:
                                ProtoWriter.WriteFieldHeader(fieldHeader, WireType.StartGroup, writer);
                                BclHelpers.WriteDecimal(reader.GetDecimal(columns[i].Ordinal), writer);
                                break;
                            case ProtoBufDataType.Double:
                                ProtoWriter.WriteFieldHeader(fieldHeader, WireType.Fixed64, writer);
                                ProtoWriter.WriteDouble(reader.GetDouble(columns[i].Ordinal), writer);
                                break;
                            case ProtoBufDataType.Float:
                                ProtoWriter.WriteFieldHeader(fieldHeader, WireType.Fixed32, writer);
                                ProtoWriter.WriteSingle(reader.GetFloat(columns[i].Ordinal), writer);
                                break;
                            case ProtoBufDataType.Guid:
                                ProtoWriter.WriteFieldHeader(fieldHeader, WireType.StartGroup, writer);
                                BclHelpers.WriteGuid(reader.GetGuid(columns[i].Ordinal), writer);
                                break;
                            case ProtoBufDataType.Int:
                                ProtoWriter.WriteFieldHeader(fieldHeader, WireType.Variant, writer);
                                ProtoWriter.WriteInt32(reader.GetInt32(columns[i].Ordinal), writer);
                                break;
                            case ProtoBufDataType.Long:
                                ProtoWriter.WriteFieldHeader(fieldHeader, WireType.Variant, writer);
                                ProtoWriter.WriteInt64(reader.GetInt64(columns[i].Ordinal), writer);
                                break;
                            case ProtoBufDataType.Short:
                                ProtoWriter.WriteFieldHeader(fieldHeader, WireType.Variant, writer);
                                ProtoWriter.WriteInt16(reader.GetInt16(columns[i].Ordinal), writer);
                                break;
                            case ProtoBufDataType.String:
                                ProtoWriter.WriteFieldHeader(fieldHeader, WireType.String, writer);
                                ProtoWriter.WriteString(reader.GetString(columns[i].Ordinal), writer);
                                break;
                            case ProtoBufDataType.TimeSpan:
                                ProtoWriter.WriteFieldHeader(fieldHeader, WireType.StartGroup, writer);
                                BclHelpers.WriteTimeSpan((TimeSpan)reader[columns[i].Ordinal], writer);
                                break;
                        }
                    }
                }

                ProtoWriter.EndSubItem(rowToken, writer);

                rowIndex++;
            }

            ProtoWriter.EndSubItem(rowsToken, writer);
        }
    }
}
