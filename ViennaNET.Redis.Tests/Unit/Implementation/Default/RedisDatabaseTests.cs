﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using ViennaNET.Redis.Implementation.Default;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using StackExchange.Redis;

namespace ViennaNET.Redis.Tests.Unit.Implementation.Default
{
  [TestFixture(Category = "Unit", TestOf = typeof(RedisDatabase))]
  public class RedisDatabaseTests
  {
    private IRedisDatabase _redisDatabase;
    private Mock<IDatabase> _databaseMock;

    [OneTimeSetUp]
    public void RedisDatabaseTestsSetUp()
    {
      var databaseMock = new Mock<IDatabase>();
      databaseMock.Setup(x => x.StringGet(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
                  .Returns(new RedisValue[]
                  {
                    JsonConvert.SerializeObject(new FooDto { Name = "First", Version = 123456 }),
                    JsonConvert.SerializeObject(new FooDto { Name = "Second", Version = 123654 })
                  });
      databaseMock.Setup(x => x.StringGet(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                  .Returns(JsonConvert.SerializeObject(new FooDto { Name = "Name", Version = 123456 }));
      databaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
                  .Returns(Task.FromResult(new RedisValue[]
                  {
                    JsonConvert.SerializeObject(new FooDto { Name = "First", Version = 123456 }),
                    JsonConvert.SerializeObject(new FooDto { Name = "Second", Version = 123654 })
                  }));
      databaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                  .Returns(Task.FromResult((RedisValue)JsonConvert.SerializeObject(new FooDto { Name = "Name", Version = 123456 })));

      databaseMock.Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(),
                                               It.IsAny<CommandFlags>()))
                  .Returns(Task.FromResult(true));

      databaseMock.Setup(x => x.StringSetAsync(It.IsAny<KeyValuePair<RedisKey, RedisValue>[]>(), It.IsAny<When>(),
                                               It.IsAny<CommandFlags>()))
                  .Returns(Task.FromResult(true));

      databaseMock.Setup(x => x.StringSet(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(),
                                          It.IsAny<CommandFlags>()))
                  .Returns(true);

      databaseMock.Setup(x => x.StringSet(It.IsAny<KeyValuePair<RedisKey, RedisValue>[]>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
                  .Returns(true);
      databaseMock.Setup(x => x.KeyDelete(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                  .Returns(true);
      databaseMock.Setup(x => x.KeyDelete(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
                  .Returns(0);
      databaseMock.Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                  .Returns(Task.FromResult(true));
      databaseMock.Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
                  .Returns(Task.FromResult((long)0));
      _databaseMock = databaseMock;

      _redisDatabase = new RedisDatabase(false, _databaseMock.Object,new NullLogger<RedisDatabase>(), new Dictionary<string, TimeSpan> { { "default", TimeSpan.Zero } },
                                         "somePrefix");
    }

    private class FooDto
    {
      public string Name { get; set; }

      public int Version { get; set; }

      private bool Equals(FooDto other)
      {
        return string.Equals(Name, other.Name) && Version == other.Version;
      }

      public override bool Equals(object obj)
      {
        if (ReferenceEquals(null, obj))
        {
          return false;
        }

        if (ReferenceEquals(this, obj))
        {
          return true;
        }

        if (obj.GetType() != GetType())
        {
          return false;
        }

        return Equals((FooDto)obj);
      }

      public override int GetHashCode()
      {
        unchecked
        {
          return ((Name != null
                    ? Name.GetHashCode()
                    : 0) * 397) ^ Version;
        }
      }

      public static bool operator ==(FooDto left, FooDto right)
      {
        return Equals(left, right);
      }

      public static bool operator !=(FooDto left, FooDto right)
      {
        return !Equals(left, right);
      }
    }

    [Test]
    public void KeyDelete_RedisDatabase_DatabaseMethodCalledCorrectly()
    {
      _redisDatabase.KeyDelete("key");

      _databaseMock.Verify(x => x.KeyDelete("somePrefixkey", CommandFlags.None), Times.Once);
    }

    [Test]
    public async Task KeyDeleteAsync_RedisDatabase_DatabaseMethodCalledCorrectly()
    {
      await _redisDatabase.KeyDeleteAsync("key");

      _databaseMock.Verify(x => x.KeyDeleteAsync("somePrefixkey", CommandFlags.None), Times.Once);
    }

    [Test]
    public async Task KeyDeleteAsyncList_RedisDatabase_DatabaseMethodCalledCorrectly()
    {
      await _redisDatabase.KeyDeleteAsync(new List<string> { "key", "key1" });

      _databaseMock.Verify(x => x.KeyDeleteAsync(new RedisKey[] { "somePrefixkey", "somePrefixkey1" }, CommandFlags.None), Times.Once);
    }

    [Test]
    public void KeyDeleteList_RedisDatabase_DatabaseMethodCalledCorrectly()
    {
      _redisDatabase.KeyDelete(new List<string> { "key", "key1" });

      _databaseMock.Verify(x => x.KeyDelete(new RedisKey[] { "somePrefixkey", "somePrefixkey1" }, CommandFlags.None), Times.Once);
    }

    [Test]
    public void ObjectGet_RedisDatabase_DatabaseMethodCalledWithPrefix()
    {
      var result = _redisDatabase.ObjectGet<FooDto>("key");

      Assert.That(result.Name, Is.EqualTo("Name"));
      Assert.That(result.Version, Is.EqualTo(123456));
      _databaseMock.Verify(x => x.StringGet("somePrefixkey", CommandFlags.None), Times.Once);
    }

    [Test]
    public async Task ObjectGetAsync_RedisDatabase_DatabaseMethodCalledWithPrefix()
    {
      var result = await _redisDatabase.ObjectGetAsync<FooDto>("key");

      Assert.That(result.Name, Is.EqualTo("Name"));
      Assert.That(result.Version, Is.EqualTo(123456));
      _databaseMock.Verify(x => x.StringGetAsync("somePrefixkey", CommandFlags.None), Times.Once);
    }

    [Test]
    public void ObjectGetList_RedisDatabase_DatabaseMethodCalledWithPrefix()
    {
      var result = _redisDatabase.ObjectGet<FooDto>(new List<string> { "key", "key1" });

      Assert.That(result.First(), Is.EqualTo(new FooDto { Name = "First", Version = 123456 }));
      Assert.That(result.Last(), Is.EqualTo(new FooDto { Name = "Second", Version = 123654 }));
      _databaseMock.Verify(x => x.StringGet(new RedisKey[] { "somePrefixkey", "somePrefixkey1" }, CommandFlags.None), Times.Once);
    }

    [Test]
    public async Task ObjectGetListAsync_RedisDatabase_DatabaseMethodCalledWithPrefix()
    {
      var result = await _redisDatabase.ObjectGetAsync<FooDto>(new List<string> { "key", "key1" });

      Assert.That(result.First(), Is.EqualTo(new FooDto { Name = "First", Version = 123456 }));
      Assert.That(result.Last(), Is.EqualTo(new FooDto { Name = "Second", Version = 123654 }));
      _databaseMock.Verify(x => x.StringGetAsync(new RedisKey[] { "somePrefixkey", "somePrefixkey1" }, CommandFlags.None), Times.Once);
    }

    [Test]
    public void ObjectSet_RedisDatabase_DatabaseMethodCalledCorrectly()
    {
      var foo = new FooDto { Name = "First", Version = 123456 };

      var result = _redisDatabase.ObjectSet("key", foo);

      Assert.That(result, Is.EqualTo(true));
      _databaseMock.Verify(x => x.StringSet("somePrefixkey", JsonConvert.SerializeObject(foo), null, When.Always, CommandFlags.None),
                           Times.Once);
    }

    [Test]
    public async Task ObjectSetAsync_RedisDatabase_DatabaseMethodCalledCorrectly()
    {
      var foo = new FooDto { Name = "First", Version = 123456 };

      var result = await _redisDatabase.ObjectSetAsync("key", foo);

      Assert.That(result, Is.EqualTo(true));
      _databaseMock.Verify(x => x.StringSetAsync("somePrefixkey", JsonConvert.SerializeObject(foo), null, When.Always, CommandFlags.None),
                           Times.Once);
    }

    [Test]
    public void ObjectSetDictionary_RedisDatabase_DatabaseMethodCalledCorrectly()
    {
      var foo = new FooDto { Name = "First", Version = 123456 };

      var result = _redisDatabase.ObjectSet(new Dictionary<string, object> { { "key", foo } });

      Assert.That(result, Is.EqualTo(true));
      _databaseMock.Verify(x => x.StringSet(new[] { new KeyValuePair<RedisKey, RedisValue>("somePrefixkey", JsonConvert.SerializeObject(foo)) }, When.Always, CommandFlags.None),
                           Times.Once);
    }

    [Test]
    public async Task ObjectSetDictionaryAsync_RedisDatabase_DatabaseMethodCalledCorrectly()
    {
      var foo = new FooDto { Name = "First", Version = 123456 };

      var result = await _redisDatabase.ObjectSetAsync(new Dictionary<string, object> { { "key", foo } });

      Assert.That(result, Is.EqualTo(true));
      _databaseMock.Verify(x => x.StringSetAsync(new[] { new KeyValuePair<RedisKey, RedisValue>("somePrefixkey", JsonConvert.SerializeObject(foo)) }, When.Always, CommandFlags.None),
                           Times.Once);
    }

    [Test]
    public void ObjectSetLifetime_RedisDatabase_DatabaseMethodCalledCorrectly()
    {
      var foo = new FooDto { Name = "First", Version = 123456 };

      var result = _redisDatabase.ObjectSet("key", foo, "default");

      Assert.That(result, Is.EqualTo(true));
      _databaseMock.Verify(x => x.StringSet("somePrefixkey", JsonConvert.SerializeObject(foo), TimeSpan.Zero, When.Always, CommandFlags.None),
                           Times.Once);
    }

    [Test]
    public async Task ObjectSetLifetimeAsync_RedisDatabase_DatabaseMethodCalledCorrectly()
    {
      var foo = new FooDto { Name = "First", Version = 123456 };

      var result = await _redisDatabase.ObjectSetAsync("key", foo, "default");

      Assert.That(result, Is.EqualTo(true));
      _databaseMock.Verify(x => x.StringSetAsync("somePrefixkey", JsonConvert.SerializeObject(foo), TimeSpan.Zero, When.Always, CommandFlags.None),
                           Times.Once);
    }

    [Test]
    public void StringGet_RedisDatabase_DatabaseMethodCalledWithPrefix()
    {
      _redisDatabase.StringGet("stringkey");

      _databaseMock.Verify(x => x.StringGet("somePrefixstringkey", CommandFlags.None), Times.Once);
    }

    [Test]
    public async Task StringGetAsync_RedisDatabase_DatabaseMethodCalledWithPrefix()
    {
      await _redisDatabase.StringGetAsync("stringkey");

      _databaseMock.Verify(x => x.StringGetAsync("somePrefixstringkey", CommandFlags.None), Times.Once);
    }

    [Test]
    public void StringGetList_RedisDatabase_DatabaseMethodCalledWithPrefix()
    {
      _redisDatabase.StringGet(new List<string> { "stringkey", "stringkey1" });

      _databaseMock.Verify(x => x.StringGet(new RedisKey[] { "somePrefixstringkey", "somePrefixstringkey1" }, CommandFlags.None), Times.Once);
    }

    [Test]
    public async Task StringGetListAsync_RedisDatabase_DatabaseMethodCalledWithPrefix()
    {
      await _redisDatabase.StringGetAsync(new List<string> { "stringkey", "stringkey1" });

      _databaseMock.Verify(x => x.StringGetAsync(new RedisKey[] { "somePrefixstringkey", "somePrefixstringkey1" }, CommandFlags.None), Times.Once);
    }

    [Test]
    public void StringSet_RedisDatabase_DatabaseMethodCalledCorrectly()
    {
      _redisDatabase.StringSet("key", "value");

      _databaseMock.Verify(x => x.StringSet("somePrefixkey", "value", null, When.Always, CommandFlags.None), Times.Once);
    }

    [Test]
    public void StringSet_WithDiagnostic_RedisDatabase_DatabaseMethodCalledCorrectly()
    {
      _redisDatabase.StringSet("key", "value_for_diag", isDiagnostic: true);

      _databaseMock.Verify(x => x.StringSet("somePrefixkey", "value_for_diag", null, When.Always, CommandFlags.None), Times.Once);
    }

    [Test]
    public async Task StringSetAsync_RedisDatabase_DatabaseMethodCalledCorrectly()
    {
      await _redisDatabase.StringSetAsync("key", "value");

      _databaseMock.Verify(x => x.StringSetAsync("somePrefixkey", "value", null, When.Always, CommandFlags.None), Times.Once);
    }

    [Test]
    public void StringSetDictionary_RedisDatabase_DatabaseMethodCalledCorrectly()
    {
      _redisDatabase.StringSet(new Dictionary<string, string> { { "key", "value" } });

      _databaseMock.Verify(x => x.StringSet(new[] { new KeyValuePair<RedisKey, RedisValue>("somePrefixkey", "value") }, When.Always, CommandFlags.None),
                           Times.Once);
    }

    [Test]
    public async Task StringSetDictionaryAsync_RedisDatabase_DatabaseMethodCalledCorrectly()
    {
      await _redisDatabase.StringSetAsync(new Dictionary<string, string> { { "key", "value" } });

      _databaseMock.Verify(x => x.StringSetAsync(new[] { new KeyValuePair<RedisKey, RedisValue>("somePrefixkey", "value") }, When.Always, CommandFlags.None),
                           Times.Once);
    }

    [Test]
    public void StringSetLifetime_RedisDatabase_DatabaseMethodCalledCorrectly()
    {
      _redisDatabase.StringSet("key", "value", "default");

      _databaseMock.Verify(x => x.StringSet("somePrefixkey", "value", TimeSpan.Zero, When.Always, CommandFlags.None), Times.Once);
    }

    [Test]
    public async Task StringSetLifetimeAsync_RedisDatabase_DatabaseMethodCalledCorrectly()
    {
      await _redisDatabase.StringSetAsync("key", "value", "default");

      _databaseMock.Verify(x => x.StringSetAsync("somePrefixkey", "value", TimeSpan.Zero, When.Always, CommandFlags.None), Times.Once);
    }
  }
}
