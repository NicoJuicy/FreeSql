﻿using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FreeSql.Tests.Sqlite
{
    public class SqliteInsertTest
    {

        IInsert<Topic> insert => g.sqlite.Insert<Topic>();

        [Table(Name = "tb_topic_insert")]
        class Topic
        {
            [Column(IsIdentity = true, IsPrimary = true)]
            public int Id { get; set; }
            public int Clicks { get; set; }
            public TestTypeInfo Type { get; set; }
            public string Title { get; set; }
            public DateTime CreateTime { get; set; }
        }

        [Fact]
        public void AppendData()
        {
            var items = new List<Topic>();
            for (var a = 0; a < 10; a++) items.Add(new Topic { Id = a + 1, Title = $"newTitle{a}", Clicks = a * 100 });

            var sql = insert.AppendData(items.First()).ToSql();
            Assert.Equal("INSERT INTO \"tb_topic_insert\"(\"Clicks\", \"Title\", \"CreateTime\") VALUES(@Clicks_0, @Title_0, @CreateTime_0)", sql);

            sql = insert.AppendData(items).ToSql();
            Assert.Equal("INSERT INTO \"tb_topic_insert\"(\"Clicks\", \"Title\", \"CreateTime\") VALUES(@Clicks_0, @Title_0, @CreateTime_0), (@Clicks_1, @Title_1, @CreateTime_1), (@Clicks_2, @Title_2, @CreateTime_2), (@Clicks_3, @Title_3, @CreateTime_3), (@Clicks_4, @Title_4, @CreateTime_4), (@Clicks_5, @Title_5, @CreateTime_5), (@Clicks_6, @Title_6, @CreateTime_6), (@Clicks_7, @Title_7, @CreateTime_7), (@Clicks_8, @Title_8, @CreateTime_8), (@Clicks_9, @Title_9, @CreateTime_9)", sql);

            sql = insert.AppendData(items).InsertColumns(a => a.Title).ToSql();
            Assert.Equal("INSERT INTO \"tb_topic_insert\"(\"Title\") VALUES(@Title_0), (@Title_1), (@Title_2), (@Title_3), (@Title_4), (@Title_5), (@Title_6), (@Title_7), (@Title_8), (@Title_9)", sql);

            sql = insert.AppendData(items).IgnoreColumns(a => a.CreateTime).ToSql();
            Assert.Equal("INSERT INTO \"tb_topic_insert\"(\"Clicks\", \"Title\") VALUES(@Clicks_0, @Title_0), (@Clicks_1, @Title_1), (@Clicks_2, @Title_2), (@Clicks_3, @Title_3), (@Clicks_4, @Title_4), (@Clicks_5, @Title_5), (@Clicks_6, @Title_6), (@Clicks_7, @Title_7), (@Clicks_8, @Title_8), (@Clicks_9, @Title_9)", sql);
        }

        [Fact]
        public void InsertColumns()
        {
            var items = new List<Topic>();
            for (var a = 0; a < 10; a++) items.Add(new Topic { Id = a + 1, Title = $"newTitle{a}", Clicks = a * 100 });

            var sql = insert.AppendData(items).InsertColumns(a => a.Title).ToSql();
            Assert.Equal("INSERT INTO \"tb_topic_insert\"(\"Title\") VALUES(@Title_0), (@Title_1), (@Title_2), (@Title_3), (@Title_4), (@Title_5), (@Title_6), (@Title_7), (@Title_8), (@Title_9)", sql);

            sql = insert.AppendData(items).InsertColumns(a => new { a.Title, a.Clicks }).ToSql();
            Assert.Equal("INSERT INTO \"tb_topic_insert\"(\"Clicks\", \"Title\") VALUES(@Clicks_0, @Title_0), (@Clicks_1, @Title_1), (@Clicks_2, @Title_2), (@Clicks_3, @Title_3), (@Clicks_4, @Title_4), (@Clicks_5, @Title_5), (@Clicks_6, @Title_6), (@Clicks_7, @Title_7), (@Clicks_8, @Title_8), (@Clicks_9, @Title_9)", sql);
        }
        [Fact]
        public void IgnoreColumns()
        {
            var items = new List<Topic>();
            for (var a = 0; a < 10; a++) items.Add(new Topic { Id = a + 1, Title = $"newTitle{a}", Clicks = a * 100 });

            var sql = insert.AppendData(items).IgnoreColumns(a => a.CreateTime).ToSql();
            Assert.Equal("INSERT INTO \"tb_topic_insert\"(\"Clicks\", \"Title\") VALUES(@Clicks_0, @Title_0), (@Clicks_1, @Title_1), (@Clicks_2, @Title_2), (@Clicks_3, @Title_3), (@Clicks_4, @Title_4), (@Clicks_5, @Title_5), (@Clicks_6, @Title_6), (@Clicks_7, @Title_7), (@Clicks_8, @Title_8), (@Clicks_9, @Title_9)", sql);

            sql = insert.AppendData(items).IgnoreColumns(a => new { a.Title, a.CreateTime }).ToSql();
            Assert.Equal("INSERT INTO \"tb_topic_insert\"(\"Clicks\") VALUES(@Clicks_0), (@Clicks_1), (@Clicks_2), (@Clicks_3), (@Clicks_4), (@Clicks_5), (@Clicks_6), (@Clicks_7), (@Clicks_8), (@Clicks_9)", sql);

            g.sqlite.Delete<TopicIgnore>().Where("1=1").ExecuteAffrows();
            var itemsIgnore = new List<TopicIgnore>();
            for (var a = 0; a < 2072; a++) itemsIgnore.Add(new TopicIgnore { Id = a + 1, Title = $"newtitle{a}", Clicks = a * 100, CreateTime = DateTime.Now });
            g.sqlite.Insert<TopicIgnore>().AppendData(itemsIgnore).IgnoreColumns(a => new { a.Title }).ExecuteAffrows();
            Assert.Equal(2072, itemsIgnore.Count);
            Assert.Equal(2072, g.sqlite.Select<TopicIgnore>().Where(a => a.Title == null).Count());
        }
        [Table(Name = "tb_topicIgnoreColumns")]
        class TopicIgnore
        {
            [Column(IsIdentity = true, IsPrimary = true)]
            public int Id { get; set; }
            public int Clicks { get; set; }
            public string Title { get; set; }
            public DateTime CreateTime { get; set; }
        }
        [Fact]
        public void ExecuteAffrows()
        {
            var items = new List<Topic>();
            for (var a = 0; a < 10; a++) items.Add(new Topic { Id = a + 1, Title = $"newTitle{a}", Clicks = a * 100 });

            Assert.Equal(1, insert.AppendData(items.First()).ExecuteAffrows());
            Assert.Equal(10, insert.AppendData(items).ExecuteAffrows());

            Assert.Equal(10, g.sqlite.Select<Topic>().Limit(10).InsertInto(null, a => new Topic
            {
                Title = a.Title
            }));
        }
        [Fact]
        public void ExecuteIdentity()
        {
            var items = new List<Topic>();
            for (var a = 0; a < 10; a++) items.Add(new Topic { Id = a + 1, Title = $"newTitle{a}", Clicks = a * 100 });

            Assert.NotEqual(0, insert.AppendData(items.First()).ExecuteIdentity());
        }
        [Fact]
        public void ExecuteInserted()
        {
        }

        [Fact]
        public void AsTable()
        {
            var items = new List<Topic>();
            for (var a = 0; a < 10; a++) items.Add(new Topic { Id = a + 1, Title = $"newTitle{a}", Clicks = a * 100 });

            var sql = insert.AppendData(items.First()).AsTable(a => "Topic_InsertAsTable").ToSql();
            Assert.Equal("INSERT INTO \"Topic_InsertAsTable\"(\"Clicks\", \"Title\", \"CreateTime\") VALUES(@Clicks_0, @Title_0, @CreateTime_0)", sql);

            sql = insert.AppendData(items).AsTable(a => "Topic_InsertAsTable").ToSql();
            Assert.Equal("INSERT INTO \"Topic_InsertAsTable\"(\"Clicks\", \"Title\", \"CreateTime\") VALUES(@Clicks_0, @Title_0, @CreateTime_0), (@Clicks_1, @Title_1, @CreateTime_1), (@Clicks_2, @Title_2, @CreateTime_2), (@Clicks_3, @Title_3, @CreateTime_3), (@Clicks_4, @Title_4, @CreateTime_4), (@Clicks_5, @Title_5, @CreateTime_5), (@Clicks_6, @Title_6, @CreateTime_6), (@Clicks_7, @Title_7, @CreateTime_7), (@Clicks_8, @Title_8, @CreateTime_8), (@Clicks_9, @Title_9, @CreateTime_9)", sql);

            sql = insert.AppendData(items).InsertColumns(a => a.Title).AsTable(a => "Topic_InsertAsTable").ToSql();
            Assert.Equal("INSERT INTO \"Topic_InsertAsTable\"(\"Title\") VALUES(@Title_0), (@Title_1), (@Title_2), (@Title_3), (@Title_4), (@Title_5), (@Title_6), (@Title_7), (@Title_8), (@Title_9)", sql);

            sql = insert.AppendData(items).IgnoreColumns(a => a.CreateTime).AsTable(a => "Topic_InsertAsTable").ToSql();
            Assert.Equal("INSERT INTO \"Topic_InsertAsTable\"(\"Clicks\", \"Title\") VALUES(@Clicks_0, @Title_0), (@Clicks_1, @Title_1), (@Clicks_2, @Title_2), (@Clicks_3, @Title_3), (@Clicks_4, @Title_4), (@Clicks_5, @Title_5), (@Clicks_6, @Title_6), (@Clicks_7, @Title_7), (@Clicks_8, @Title_8), (@Clicks_9, @Title_9)", sql);

            sql = insert.AppendData(items).InsertColumns(a => a.Title).AsTable(a => "Topic_InsertAsTable").ToSql();
            Assert.Equal("INSERT INTO \"Topic_InsertAsTable\"(\"Title\") VALUES(@Title_0), (@Title_1), (@Title_2), (@Title_3), (@Title_4), (@Title_5), (@Title_6), (@Title_7), (@Title_8), (@Title_9)", sql);

            sql = insert.AppendData(items).InsertColumns(a => new { a.Title, a.Clicks }).AsTable(a => "Topic_InsertAsTable").ToSql();
            Assert.Equal("INSERT INTO \"Topic_InsertAsTable\"(\"Clicks\", \"Title\") VALUES(@Clicks_0, @Title_0), (@Clicks_1, @Title_1), (@Clicks_2, @Title_2), (@Clicks_3, @Title_3), (@Clicks_4, @Title_4), (@Clicks_5, @Title_5), (@Clicks_6, @Title_6), (@Clicks_7, @Title_7), (@Clicks_8, @Title_8), (@Clicks_9, @Title_9)", sql);

            sql = insert.AppendData(items).IgnoreColumns(a => a.CreateTime).AsTable(a => "Topic_InsertAsTable").ToSql();
            Assert.Equal("INSERT INTO \"Topic_InsertAsTable\"(\"Clicks\", \"Title\") VALUES(@Clicks_0, @Title_0), (@Clicks_1, @Title_1), (@Clicks_2, @Title_2), (@Clicks_3, @Title_3), (@Clicks_4, @Title_4), (@Clicks_5, @Title_5), (@Clicks_6, @Title_6), (@Clicks_7, @Title_7), (@Clicks_8, @Title_8), (@Clicks_9, @Title_9)", sql);

            sql = insert.AppendData(items).IgnoreColumns(a => new { a.Title, a.CreateTime }).AsTable(a => "Topic_InsertAsTable").ToSql();
            Assert.Equal("INSERT INTO \"Topic_InsertAsTable\"(\"Clicks\") VALUES(@Clicks_0), (@Clicks_1), (@Clicks_2), (@Clicks_3), (@Clicks_4), (@Clicks_5), (@Clicks_6), (@Clicks_7), (@Clicks_8), (@Clicks_9)", sql);
        }

        [Fact]
        public void BulkInsert()
        {
            g.sqlite.Delete<Topic>().Where(m => true).ExecuteAffrows();
            var list = new List<Topic>();
            for (int i = 0; i < 10; i++)
            {
                list.Add(new Topic { Id = i, Clicks = i * 2, Title = "BULK" + i.ToString(), CreateTime = DateTime.Now });
            }
            insert.AppendData(list).ExecuteSqliteBulkInsert();
            Assert.Equal(10, g.sqlite.Select<Topic>().Where(m => m.Title.StartsWith("BULK")).Count());

            g.sqlite.Delete<Topic>().Where(m => true).ExecuteAffrows();
            g.sqlite.Transaction(() =>
            {
                g.sqlite.Insert(list).ExecuteSqliteBulkInsert();
                Assert.Equal(10, g.sqlite.Select<Topic>().Where(m => m.Title.StartsWith("BULK")).Count());
            });
        }


    }
}
