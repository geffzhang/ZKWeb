﻿using System.Data;
using System.Data.SqlClient;
using Microsoft.Data.Sqlite;
using ZKWeb.Database;
using Npgsql;
using System;
using System.Threading;
using System.Linq;
using Dapper;
using System.Linq.Expressions;
using System.FastReflection;
using System.Collections.Generic;
using ZKWeb.Storage;
using Dommel;
using MySql.Data.MySqlClient;
using ZKWebStandard.Ioc;
using System.Data.Common;
using System.Globalization;

namespace ZKWeb.ORM.Dapper {
#pragma warning disable S3881 // "IDisposable" should be implemented correctly
	/// <summary>
	/// Dapper database context<br/>
	/// Dapper的数据库上下文<br/>
	/// </summary>
	public class DapperDatabaseContext : IDatabaseContext {
#pragma warning restore S3881 // "IDisposable" should be implemented correctly
		/// <summary>
		/// Dapper entity mappings<br/>
		/// Dapper的实体映射<br/>
		/// </summary>
		public DapperEntityMappings Mappings { get; protected set; }
		/// <summary>
		/// Database connection<br/>
		/// 数据库连接<br/>
		/// </summary>
		protected IDbConnection Connection { get; set; }
		/// <summary>
		/// Database transaction<br/>
		/// 数据库事务<br/>
		/// </summary>
		protected IDbTransaction Transaction { get; set; }
		/// <summary>
		/// Transaction level counter<br/>
		/// 事务嵌套计数<br/>
		/// </summary>
		protected int TransactionLevel;
		/// <summary>
		/// ORM name<br/>
		/// ORM名称<br/>
		/// </summary>
		public string ORM { get { return ConstORM; } }
#pragma warning disable CS1591
		public const string ConstORM = "Dapper";
#pragma warning restore CS1591
		/// <summary>
		/// Database type<br/>
		/// 数据库类型<br/>
		/// </summary>
		public string Database { get { return databaseType; } }
#pragma warning disable CS1591
		protected string databaseType;
#pragma warning restore CS1591
		/// <summary>
		/// Underlying database connection<br/>
		/// 返回底层的数据库连接<br/>
		/// </summary>
		public object DbConnection { get { return Connection; } }
		/// <summary>
		/// Database command logger<br/>
		/// 数据库命令记录器<br/>
		/// </summary>
		public IDatabaseCommandLogger CommandLogger { get; set; }

		/// <summary>
		/// Initialize<br/>
		/// 初始化<br/>
		/// </summary>
		/// <param name="mappings">Dapper entity mappings</param>
		/// <param name="database">Database type</param>
		/// <param name="connectionString">Connection string</param>
		public DapperDatabaseContext(DapperEntityMappings mappings,
			string database, string connectionString) {
			Mappings = mappings;
			Connection = null;
			Transaction = null;
			TransactionLevel = 0;
			databaseType = database;
			// Get default command logger
			CommandLogger = Application.Ioc.Resolve<IDatabaseCommandLogger>(IfUnresolved.ReturnDefault);
			// Create database connection
			var pathConfig = Application.Ioc.Resolve<LocalPathConfig>();
			if (string.Compare(database, "MSSQL", true, CultureInfo.InvariantCulture) == 0) {
				Connection = new Wrappers.SqlConnection(
					new SqlConnection(connectionString), this);
			} else if (string.Compare(database, "SQLite", true, CultureInfo.InvariantCulture) == 0) {
				connectionString = connectionString.Replace("{{App_Data}}", pathConfig.AppDataDirectory);
				Connection = new Wrappers.SqliteConnection(
					new SqliteConnection(connectionString), this);
			} else if (string.Compare(database, "MySQL", true, CultureInfo.InvariantCulture) == 0) {
				Connection = new Wrappers.MySqlConnection(
					new MySqlConnection(connectionString), this);
			} else if (string.Compare(database, "PostgreSQL", true, CultureInfo.InvariantCulture) == 0) {
				Connection = new Wrappers.NpgsqlConnection(
					new NpgsqlConnection(connectionString), this);
			} else {
				throw new ArgumentException($"unsupported database type {database}");
			}
			Connection.Open();
		}

		/// <summary>
		/// Finalize<br/>
		/// 析构函数<br/>
		/// </summary>
		~DapperDatabaseContext() {
			Dispose();
		}

		/// <summary>
		/// Dispose connection and transaction<br/>
		/// 销毁连接和事务<br/>
		/// </summary>
		public void Dispose() {
			Transaction?.Dispose();
			Transaction = null;
			Connection?.Dispose();
			Connection = null;
		}

		/// <summary>
		/// Begin a transaction<br/>
		/// 开始一个事务<br/>
		/// </summary>
		public void BeginTransaction(IsolationLevel? isolationLevel = null) {
			var level = Interlocked.Increment(ref TransactionLevel);
			if (level == 1) {
				if (Transaction != null) {
					throw new InvalidOperationException("Transaction exists");
				}
				Transaction = (isolationLevel == null) ?
					Connection.BeginTransaction() :
					Connection.BeginTransaction(isolationLevel.Value);
			}
		}

		/// <summary>
		/// Finish the transaction<br/>
		/// 结束一个事务<br/>
		/// </summary>
		public void FinishTransaction() {
			var level = Interlocked.Decrement(ref TransactionLevel);
			if (level == 0) {
				if (Transaction == null) {
					throw new InvalidOperationException("Transaction not exists");
				}
				Transaction.Commit();
				Transaction.Dispose();
				Transaction = null;
			} else if (level < 0) {
				Interlocked.Exchange(ref level, 0);
				throw new InvalidOperationException(
					"You can't call FinishTransaction more times than BeginTransaction");
			}
		}

		/// <summary>
		/// Get the query object for specific entity type<br/>
		/// Attention: It's very slow, you should use RawQuery<br/>
		/// 获取指定实体类型的查询对象<br/>
		/// 注意: 它很慢, 你应该使用RawQuery<br/>
		/// </summary>
		public IQueryable<T> Query<T>()
			where T : class, IEntity {
			// Dommel's GetAll isn't support passing transaction
			// since this method is the common fallback of query, only patch this function for now
			var tablename = DommelMapper.Resolvers.Table(typeof(T));
			var sql = "select * from " + tablename;
			return Connection.Query<T>(sql, transaction: Transaction, buffered: true).AsQueryable();
		}

		/// <summary>
		/// Get single entity that matched the given predicate<br/>
		/// It should return null if no matched entity found<br/>
		/// 获取符合传入条件的单个实体<br/>
		/// 如果无符合条件的实体应该返回null<br/>
		/// </summary>
		public T Get<T>(Expression<Func<T, bool>> predicate)
			where T : class, IEntity {
			// If predicate is about compare primary key then we can use `Get` method
			if (predicate.Body is BinaryExpression binaryExpr &&
				binaryExpr.NodeType == ExpressionType.Equal &&
				binaryExpr.Left is MemberExpression &&
				((MemberExpression)binaryExpr.Left).Member.Name ==
				Mappings.GetMapping(typeof(T)).IdMember.Name &&
				binaryExpr.Right is ConstantExpression) {
				var primaryKey = ((ConstantExpression)binaryExpr.Right).Value;
				return Connection.Get<T>(primaryKey);
			}
			try {
				return Connection.Select(predicate).FirstOrDefault();
			} catch (DbException) {
				return Query<T>().Where(predicate).FirstOrDefault(); // fallback
			}
		}

		/// <summary>
		/// Get how many entities that matched the given predicate<br/>
		/// 获取符合传入条件的实体数量<br/>
		/// </summary>
		public long Count<T>(Expression<Func<T, bool>> predicate)
			where T : class, IEntity {
			try {
				return Connection.Select(predicate).LongCount();
			} catch (DbException) {
				return Query<T>().Where(predicate).LongCount(); // fallback
			}
		}

		/// <summary>
		/// Insert or update entity<br/>
		/// 插入或更新实体<br/>
		/// </summary>
		protected void InsertOrUpdate<T>(T entity)
			where T : class, IEntity {
			// If the primary key is empty, insert it
			// Otherwise try update first, if not exist then perform the insert
			var mapping = Mappings.GetMapping(typeof(T));
			var primaryKey = mapping.IdMember.FastGetValue(entity);
			if (primaryKey == null ||
				object.Equals(primaryKey, 0) ||
				object.Equals(primaryKey, -1) ||
				object.Equals(primaryKey, Guid.Empty)) {
				// Update generated primary key
				primaryKey = Connection.Insert(entity, Transaction);
				mapping.IdMember.FastSetValue(entity, primaryKey);
			} else if (!Connection.Update(entity, Transaction)) {
				Connection.Insert(entity, Transaction);
			}
		}

		/// <summary>
		/// Save entity to database<br/>
		/// 保存实体到数据库<br/>
		/// </summary>
		public void Save<T>(ref T entity, Action<T> update = null)
			where T : class, IEntity {
			var callbacks = Application.Ioc.ResolveMany<IEntityOperationHandler<T>>().ToList();
			var entityLocal = entity; // can't use ref parameter in lambda
			callbacks.ForEach(c => c.BeforeSave(this, entityLocal)); // notify before save
			update?.Invoke(entityLocal);
			InsertOrUpdate(entityLocal);
			callbacks.ForEach(c => c.AfterSave(this, entityLocal)); // notify after save
			entity = entityLocal;
		}

		/// <summary>
		/// Delete entity from database<br/>
		/// 删除数据库中的实体<br/>
		/// </summary>
		public void Delete<T>(T entity)
			where T : class, IEntity {
			var callbacks = Application.Ioc.ResolveMany<IEntityOperationHandler<T>>().ToList();
			callbacks.ForEach(c => c.BeforeDelete(this, entity)); // notify before delete
			Connection.Delete(entity, Transaction);
			callbacks.ForEach(c => c.AfterDelete(this, entity)); // notify after delete
		}

		/// <summary>
		/// Batch save entities<br/>
		/// 批量保存实体<br/>
		/// </summary>
		public void BatchSave<T>(ref IEnumerable<T> entities, Action<T> update = null)
			where T : class, IEntity {
			var entitiesLocal = entities.ToList();
			var callbacks = Application.Ioc.ResolveMany<IEntityOperationHandler<T>>().ToList();
			foreach (var entity in entitiesLocal) {
				callbacks.ForEach(c => c.BeforeSave(this, entity)); // notify before save
				update?.Invoke(entity);
				InsertOrUpdate(entity);
				callbacks.ForEach(c => c.AfterSave(this, entity)); // notify after save
			}
			entities = entitiesLocal;
		}

		/// <summary>
		/// Batch update entities<br/>
		/// 批量更新实体<br/>
		/// </summary>
		public long BatchUpdate<T>(Expression<Func<T, bool>> predicate, Action<T> update)
			where T : class, IEntity {
			IEnumerable<T> entities;
			try {
				entities = Connection.Select(predicate);
			} catch (DbException) {
				entities = Query<T>().Where(predicate).AsEnumerable(); // fallback
			}
			BatchSave(ref entities, update);
			return entities.LongCount();
		}

		/// <summary>
		/// Batch delete entities<br/>
		/// 批量删除实体<br/>
		/// </summary>
		public long BatchDelete<T>(Expression<Func<T, bool>> predicate, Action<T> beforeDelete = null)
			where T : class, IEntity {
			List<T> entities;
			try {
				entities = Connection.Select(predicate).ToList();
			} catch (DbException) {
				entities = Query<T>().Where(predicate).ToList(); // fallback
			}
			var callbacks = Application.Ioc.ResolveMany<IEntityOperationHandler<T>>().ToList();
			foreach (var entity in entities) {
				beforeDelete?.Invoke(entity);
				callbacks.ForEach(c => c.BeforeDelete(this, entity)); // notify before delete
				Connection.Delete(entity, Transaction);
				callbacks.ForEach(c => c.AfterDelete(this, entity)); // notify after delete
			}
			return entities.Count;
		}

		/// <summary>
		/// Batch save entities in faster way<br/>
		/// 快速批量保存实体<br/>
		/// </summary>
		public void FastBatchSave<T, TPrimaryKey>(IEnumerable<T> entities)
			where T : class, IEntity<TPrimaryKey> {
			foreach (var entity in entities) {
				InsertOrUpdate(entity);
			}
		}

		/// <summary>
		/// Batch delete entities in faster way<br/>
		/// 快速批量删除实体<br/>
		/// </summary>
		public long FastBatchDelete<T, TPrimaryKey>(Expression<Func<T, bool>> predicate)
			where T : class, IEntity<TPrimaryKey>, new() {
			return Connection.DeleteMultiple(predicate, Transaction) ? int.MaxValue : 0;
		}

		/// <summary>
		/// Perform a raw update to database<br/>
		/// 执行一个原生的更新操作<br/>
		/// </summary>
		public long RawUpdate(object query, object parameters) {
			CommandLogger?.LogCommand(this, (string)query, parameters);
			return Connection.Execute((string)query, parameters, Transaction);
		}

		/// <summary>
		/// Perform a raw query to database<br/>
		/// 执行一个原生的查询操作<br/>
		/// </summary>
		public IEnumerable<T> RawQuery<T>(object query, object parameters)
			where T : class {
			CommandLogger?.LogCommand(this, (string)query, parameters);
			return Connection.Query<T>((string)query, parameters, Transaction);
		}
	}
}
