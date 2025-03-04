﻿using FreeSql.Internal;
using FreeSql.Internal.CommonProvider;
using FreeSql.Internal.Model;
using FreeSql.Internal.ObjectPool;
using System;
using System.Collections;
using System.Data.Common;
using System.Data.OleDb;
using System.Linq;
using System.Threading;

namespace FreeSql.MsAccess
{
    class MsAccessAdo : FreeSql.Internal.CommonProvider.AdoProvider
    {
        public MsAccessAdo() : base(DataType.MsAccess, null, null) { }
        public MsAccessAdo(CommonUtils util, string masterConnectionString, string[] slaveConnectionStrings, Func<DbConnection> connectionFactory) : base(DataType.MsAccess, masterConnectionString, slaveConnectionStrings)
        {
            base._util = util;
            if (connectionFactory != null)
            {
                var pool = new FreeSql.Internal.CommonProvider.DbConnectionPool(DataType.MsAccess, connectionFactory);
                ConnectionString = pool.TestConnection?.ConnectionString;
                MasterPool = pool;
                return;
            }

            var isAdoPool = masterConnectionString?.StartsWith("AdoConnectionPool,") ?? false;
            if (isAdoPool) masterConnectionString = masterConnectionString.Substring("AdoConnectionPool,".Length);
            if (!string.IsNullOrEmpty(masterConnectionString))
                MasterPool = isAdoPool ?
                    new DbConnectionStringPool(base.DataType, CoreErrorStrings.S_MasterDatabase, () => new OleDbConnection(masterConnectionString)) as IObjectPool<DbConnection> :
                    new MsAccessConnectionPool(CoreErrorStrings.S_MasterDatabase, masterConnectionString, null, null);

            slaveConnectionStrings?.ToList().ForEach(slaveConnectionString =>
            {
                var slavePool = isAdoPool ?
                    new DbConnectionStringPool(base.DataType, $"{CoreErrorStrings.S_SlaveDatabase}{SlavePools.Count + 1}", () => new OleDbConnection(slaveConnectionString)) as IObjectPool<DbConnection> :
                    new MsAccessConnectionPool($"{CoreErrorStrings.S_SlaveDatabase}{SlavePools.Count + 1}", slaveConnectionString, () => Interlocked.Decrement(ref slaveUnavailables), () => Interlocked.Increment(ref slaveUnavailables));
                SlavePools.Add(slavePool);
            });
        }

        public override object AddslashesProcessParam(object param, Type mapType, ColumnInfo mapColumn)
        {
            if (param == null) return "NULL";
            if (mapType != null && mapType != param.GetType() && (param is IEnumerable == false))
                param = Utils.GetDataReaderValue(mapType, param);

            if (param is bool || param is bool?)
                return (bool)param ? -1 : 0;
            else if (param is string)
                return string.Concat("'", param.ToString().Replace("'", "''"), "'");
            else if (param is char)
                return string.Concat("'", param.ToString().Replace("'", "''").Replace('\0', ' '), "'");
            else if (param is Enum)
                return AddslashesTypeHandler(param.GetType(), param) ?? ((Enum)param).ToInt64();
            else if (decimal.TryParse(string.Concat(param), out var trydec))
                return param;

            else if (param is DateTime)
            {
                var result = AddslashesTypeHandler(typeof(DateTime), param);
                if (result != null) return result;
                if (param.Equals(DateTime.MinValue) == true) param = new DateTime(1970, 1, 1);
                return string.Concat("cdate('", ((DateTime)param).ToString("yyyy-MM-dd HH:mm:ss"), "')");
            }
            else if (param is DateTime?)
            {
                var result = AddslashesTypeHandler(typeof(DateTime?), param);
                if (result != null) return result;
                if (param.Equals(DateTime.MinValue) == true) param = new DateTime(1970, 1, 1);
                return string.Concat("cdate('", ((DateTime)param).ToString("yyyy-MM-dd HH:mm:ss"), "')");
            }

            else if (param is TimeSpan || param is TimeSpan?)
            {
                var ts = (TimeSpan)param;
                return $"'{ts.Hours}:{ts.Minutes}:{ts.Seconds}'";
            }
            else if (param is byte[])
                return $"0x{CommonUtils.BytesSqlRaw(param as byte[])}";
            else if (param is IEnumerable)
                return AddslashesIEnumerable(param, mapType, mapColumn);

            return string.Concat("'", param.ToString().Replace("'", "''"), "'");
        }

        public override DbCommand CreateCommand()
        {
            return new OleDbCommand();
        }

        public override void ReturnConnection(IObjectPool<DbConnection> pool, Object<DbConnection> conn, Exception ex)
        {
            var rawPool = pool as MsAccessConnectionPool;
            if (rawPool != null) rawPool.Return(conn, ex);
            else pool.Return(conn);
        }

        public override DbParameter[] GetDbParamtersByObject(string sql, object obj) => _util.GetDbParamtersByObject(sql, obj);
    }
}