﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FreeSql.DataAnnotations
{
    public class TableFluent
    {
        public TableFluent(Type entityType, TableAttribute table)
        {
            _entityType = entityType;
            _properties = _entityType.GetPropertiesDictIgnoreCase();
            _table = table;
        }

        Type _entityType;
        Dictionary<string, PropertyInfo> _properties;
        TableAttribute _table;

        /// <summary>
        /// 数据库表名
        /// </summary>
        public TableFluent Name(string value)
        {
            _table.Name = value;
            return this;
        }
        /// <summary>
        /// 指定数据库旧的表名，修改实体命名时，同时设置此参数为修改之前的值，CodeFirst才可以正确修改数据库表；否则将视为【创建新表】
        /// </summary>
        public TableFluent OldName(string value)
        {
            _table.OldName = value;
            return this;
        }

        /// <summary>
        /// 禁用 CodeFirst 同步结构迁移
        /// </summary>
        public TableFluent DisableSyncStructure(bool value)
        {
            _table.DisableSyncStructure = value;
            return this;
        }
		/// <summary>
		/// 格式：属性名=开始时间(递增)<para></para>
		/// 按年分表：[Table(Name = "log_{yyyy}", AsTable = "create_time=2022-1-1(1 year)")]<para></para>
		/// 按月分表：[Table(Name = "log_{yyyyMM}", AsTable = "create_time=2022-5-1(1 month)")]<para></para>
		/// 按日分表：[Table(Name = "log_{yyyyMMdd}", AsTable = "create_time=2022-5-1(5 day)")]<para></para>
		/// 按时分表：[Table(Name = "log_{yyyyMMddHH}", AsTable = "create_time=2022-5-1(6 hour)")]<para></para>
		/// </summary>
		public TableFluent AsTable(string value)
		{
			_table.AsTable = value;
			return this;
		}

		public ColumnFluent Property(string proto)
        {
            if (_properties.TryGetValue(proto, out var tryProto) == false) throw new KeyNotFoundException(CoreErrorStrings.NotFound_PropertyName(proto));
            var col = _table._columns.GetOrAdd(tryProto.Name, name => new ColumnAttribute { });
            return new ColumnFluent(col, tryProto, _entityType);
        }

        /// <summary>
        /// 导航关系Fluent，与 NavigateAttribute 对应
        /// </summary>
        /// <param name="proto"></param>
        /// <param name="bind"></param>
        /// <param name="manyToMany">多对多关系的中间实体类型</param>
        /// <returns></returns>
        public TableFluent Navigate(string proto, string bind, Type manyToMany = null) => NavigateInternal(proto, bind, null, manyToMany);
        public TableFluent Navigate(string proto, string bind, string tempPrimary) => NavigateInternal(proto, bind, tempPrimary, null);
        TableFluent NavigateInternal(string proto, string bind, string tempPrimary, Type manyToMany)
        {
            if (_properties.TryGetValue(proto, out var tryProto) == false) throw new KeyNotFoundException(CoreErrorStrings.NotFound_Property(proto));
            var nav = new NavigateAttribute { Bind = bind, TempPrimary = tempPrimary, ManyToMany = manyToMany };
            _table._navigates.AddOrUpdate(tryProto.Name, nav, (name, old) => nav);
            return this;
        }

        /// <summary>
        /// 设置实体的索引
        /// </summary>
        /// <param name="name">索引名</param>
        /// <param name="fields">索引字段，为属性名以逗号分隔，如：Create_time ASC, Title ASC</param>
        /// <param name="isUnique">是否唯一</param>
        /// <returns></returns>
        public TableFluent Index(string name, string fields, bool isUnique = false)
        {
            var idx = new IndexAttribute(name, fields, isUnique);
            _table._indexs.AddOrUpdate(name, idx, (_, __) => idx);
            return this;
        }
        public TableFluent IndexRemove(string name)
        {
            _table._indexs.TryRemove(name, out var oldidx);
            return this;
        }
    }

    public class TableFluent<T>
    {
        public TableFluent(TableAttribute table)
        {
            _properties = typeof(T).GetPropertiesDictIgnoreCase();
            _table = table;
        }

        Dictionary<string, PropertyInfo> _properties;
        TableAttribute _table;

        /// <summary>
        /// 数据库表名
        /// </summary>
        public TableFluent<T> Name(string value)
        {
            _table.Name = value;
            return this;
        }
        /// <summary>
        /// 指定数据库旧的表名，修改实体命名时，同时设置此参数为修改之前的值，CodeFirst才可以正确修改数据库表；否则将视为【创建新表】
        /// </summary>
        public TableFluent<T> OldName(string value)
        {
            _table.OldName = value;
            return this;
        }

        /// <summary>
        /// 禁用 CodeFirst 同步结构迁移
        /// </summary>
        public TableFluent<T> DisableSyncStructure(bool value)
        {
            _table.DisableSyncStructure = value;
            return this;
		}
		/// <summary>
		/// 格式：属性名=开始时间(递增)<para></para>
		/// 按年分表：[Table(Name = "log_{yyyy}", AsTable = "create_time=2022-1-1(1 year)")]<para></para>
		/// 按月分表：[Table(Name = "log_{yyyyMM}", AsTable = "create_time=2022-5-1(1 month)")]<para></para>
		/// 按日分表：[Table(Name = "log_{yyyyMMdd}", AsTable = "create_time=2022-5-1(5 day)")]<para></para>
		/// 按时分表：[Table(Name = "log_{yyyyMMddHH}", AsTable = "create_time=2022-5-1(6 hour)")]<para></para>
		/// </summary>
		public TableFluent<T> AsTable(string value)
		{
			_table.AsTable = value;
			return this;
		}

		public ColumnFluent Property<TProto>(Expression<Func<T, TProto>> column)
        {
            var exp = column?.Body;
            if (exp?.NodeType == ExpressionType.Convert) exp = (exp as UnaryExpression)?.Operand;
            var proto = (exp as MemberExpression)?.Member;
            if (proto == null) throw new FormatException(CoreErrorStrings.Bad_Expression_Format(column));
            return Property(proto.Name);
        }
        public ColumnFluent Property(string proto)
        {
            if (_properties.TryGetValue(proto, out var tryProto) == false) throw new KeyNotFoundException(CoreErrorStrings.NotFound_PropertyName(proto));
            var col = _table._columns.GetOrAdd(tryProto.Name, name => new ColumnAttribute { });
            return new ColumnFluent(col, tryProto, typeof(T));
        }

        /// <summary>
        /// 导航关系Fluent，与 NavigateAttribute 对应
        /// </summary>
        /// <typeparam name="TProto"></typeparam>
        /// <param name="proto"></param>
        /// <param name="bind"></param>
        /// <param name="manyToMany">多对多关系的中间实体类型</param>
        /// <returns></returns>
        public TableFluent<T> Navigate<TProto>(Expression<Func<T, TProto>> proto, string bind, Type manyToMany = null) => NavigateInternal(proto, bind, null, manyToMany);
        public TableFluent<T> Navigate<TProto>(Expression<Func<T, TProto>> proto, string bind, string tempPrimary) => NavigateInternal(proto, bind, tempPrimary, null);
        TableFluent<T> NavigateInternal<TProto>(Expression<Func<T, TProto>> proto, string bind, string tempPrimary, Type manyToMany = null)
        {
            var exp = proto?.Body;
            if (exp.NodeType == ExpressionType.Convert) exp = (exp as UnaryExpression)?.Operand;
            var member = (exp as MemberExpression)?.Member;
            if (member == null) throw new FormatException(CoreErrorStrings.Bad_Expression_Format(proto));
            return NavigateInternal(member.Name, bind, tempPrimary, manyToMany);
        }
        public TableFluent<T> Navigate(string proto, string bind, Type manyToMany = null) => NavigateInternal(proto, bind, null, manyToMany);
        public TableFluent<T> Navigate(string proto, string bind, string tempPrimary) => NavigateInternal(proto, bind, tempPrimary, null);
        TableFluent<T> NavigateInternal(string proto, string bind, string tempPrimary, Type manyToMany)
        {
            if (_properties.TryGetValue(proto, out var tryProto) == false) throw new KeyNotFoundException(CoreErrorStrings.NotFound_PropertyName(proto));
            var nav = new NavigateAttribute { Bind = bind, TempPrimary = tempPrimary, ManyToMany = manyToMany };
            _table._navigates.AddOrUpdate(tryProto.Name, nav, (name, old) => nav);
            return this;
        }

        /// <summary>
        /// 设置实体的索引
        /// </summary>
        /// <param name="name">索引名</param>
        /// <param name="fields">索引字段，为属性名以逗号分隔，如：Create_time ASC, Title ASC</param>
        /// <param name="isUnique">是否唯一</param>
        /// <returns></returns>
        public TableFluent<T> Index(string name, string fields, bool isUnique = false)
        {
            var idx = new IndexAttribute(name, fields, isUnique);
            _table._indexs.AddOrUpdate(name, idx, (_, __) => idx);
            return this;
        }
        public TableFluent<T> IndexRemove(string name)
        {
            _table._indexs.TryRemove(name, out var oldidx);
            return this;
        }
    }
}
