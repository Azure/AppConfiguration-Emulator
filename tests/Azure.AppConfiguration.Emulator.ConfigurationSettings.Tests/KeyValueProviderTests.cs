using Microsoft.Extensions.Options;
using Moq;
using Azure.AppConfiguration.Emulator.Tenant;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings.Tests
{
    public class KeyValueProviderTests
    {
        private readonly Mock<IKeyValueStorage> _mockStorage;
        private readonly KeyValueProvider _provider;
        private readonly List<KeyValue> _testKeyValues;

        public KeyValueProviderTests()
        {
            // Create test data - only Key and Label fields are required for testing
            _testKeyValues = new List<KeyValue>
            {
                new KeyValue
                {
                    Key = "app1/settings/key1",
                    Label = "dev",
                    Value = "value1"
                },
                new KeyValue
                {
                    Key = "app1/settings/key2",
                    Label = "dev",
                    Value = "value2"
                },
                new KeyValue
                {
                    Key = "app2/settings/key3",
                    Label = "", // Empty label
                    Value = "value3"
                }
            };

            // Set up mock storage
            _mockStorage = new Mock<IKeyValueStorage>();
            _mockStorage.Setup(s => s.QueryKeyValues(It.IsAny<CancellationToken>()))
                .Returns(_testKeyValues.ToAsyncEnumerable());

            // Set up tenant options
            var tenantOptions = new TenantOptions();
            var mockTenantOptions = Options.Create(tenantOptions);

            // Create the provider
            _provider = new KeyValueProvider(_mockStorage.Object, mockTenantOptions);

            // Ensure the provider is initialized
            _provider.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        [Fact]
        public async Task GetKeyValue_ReturnsKeyValue_WhenKeyAndLabelMatch()
        {
            // Arrange
            string key = "app1/settings/key1";
            string label = "dev";

            // Act
            var result = await _provider.GetKeyValue(key, label, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(key, result.Key);
            Assert.Equal(label, result.Label);
            Assert.Equal("value1", result.Value);
        }

        [Fact]
        public async Task GetKeyValue_ReturnsNull_WhenKeyDoesNotExist()
        {
            // Arrange
            string key = "nonexistent-key";
            string label = "dev";

            // Act
            var result = await _provider.GetKeyValue(key, label, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetKeyValue_ReturnsNull_WhenLabelDoesNotMatch()
        {
            // Arrange
            string key = "app1/settings/key1";
            string label = "nonexistent-label";

            // Act
            var result = await _provider.GetKeyValue(key, label, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetKeyValue_ReturnsKeyValueWithEmptyLabel_WhenLabelIsEmpty()
        {
            // Arrange
            string key = "app2/settings/key3";
            string label = ""; // Empty label

            // Act
            var result = await _provider.GetKeyValue(key, label, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(key, result.Key);
            Assert.Equal(label, result.Label);
            Assert.Equal("value3", result.Value);
        }

        [Fact]
        public async Task Set_AddsNewKeyValue_WhenKeyDoesNotExist()
        {
            // Arrange
            var newKeyValue = new KeyValue
            {
                Key = "new-key",
                Label = "new-label",
                Value = "new-value"
            };

            KeyValue? capturedKeyValue = null;

            _mockStorage.Setup(s => s.AppendKeyValue(It.IsAny<KeyValue>(), It.IsAny<CancellationToken>()))
                .Callback<KeyValue, CancellationToken>((kv, ct) => capturedKeyValue = kv)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _provider.Set(newKeyValue, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newKeyValue.Key, result.Key);
            Assert.Equal(newKeyValue.Label, result.Label);
            Assert.Equal(newKeyValue.Value, result.Value);
            Assert.NotNull(result.Etag); // Should have an ETag assigned

            // Verify the storage was called
            _mockStorage.Verify(s => s.AppendKeyValue(It.IsAny<KeyValue>(), It.IsAny<CancellationToken>()), Times.Once);

            // Verify the key-value passed to storage has the correct properties
            Assert.NotNull(capturedKeyValue);
            Assert.Equal(newKeyValue.Key, capturedKeyValue.Key);
            Assert.Equal(newKeyValue.Label, capturedKeyValue.Label);
            Assert.Equal(newKeyValue.Value, capturedKeyValue.Value);
            Assert.NotNull(capturedKeyValue.Etag);
            Assert.Equal(DateTimeOffset.UtcNow.Date, capturedKeyValue.Timestamp.Date); // Should have current timestamp
        }

        [Fact]
        public async Task Set_UpdatesExistingKeyValue_WhenKeyExists()
        {
            // Arrange
            var existingKeyValue = _testKeyValues.First();
            var updatedKeyValue = new KeyValue
            {
                Key = existingKeyValue.Key,
                Label = existingKeyValue.Label,
                Value = "updated-value",
                Etag = existingKeyValue.Etag // Must match for update
            };

            KeyValue? capturedKeyValue = null;

            _mockStorage.Setup(s => s.AppendKeyValue(It.IsAny<KeyValue>(), It.IsAny<CancellationToken>()))
                .Callback<KeyValue, CancellationToken>((kv, ct) => capturedKeyValue = kv)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _provider.Set(updatedKeyValue, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updatedKeyValue.Key, result.Key);
            Assert.Equal(updatedKeyValue.Label, result.Label);
            Assert.Equal(updatedKeyValue.Value, result.Value);
            Assert.NotEqual(updatedKeyValue.Etag, result.Etag); // Should have a new ETag

            // Verify the storage was called
            _mockStorage.Verify(s => s.AppendKeyValue(It.IsAny<KeyValue>(), It.IsAny<CancellationToken>()), Times.Once);

            // Verify the key-value passed to storage has the correct properties
            Assert.NotNull(capturedKeyValue);
            Assert.Equal(updatedKeyValue.Key, capturedKeyValue.Key);
            Assert.Equal(updatedKeyValue.Label, capturedKeyValue.Label);
            Assert.Equal(updatedKeyValue.Value, capturedKeyValue.Value);
            Assert.NotNull(capturedKeyValue.Etag);
            Assert.NotEqual(updatedKeyValue.Etag, capturedKeyValue.Etag); // Should have a new ETag
            Assert.Equal(DateTimeOffset.UtcNow.Date, capturedKeyValue.Timestamp.Date); // Should have current timestamp
            Assert.Null(capturedKeyValue.Deleted); // Should not be marked as deleted
        }

        [Fact]
        public async Task Remove_MarksKeyValueAsDeleted()
        {
            // Arrange
            var existingKeyValue = _testKeyValues.First();
            var keyValueToRemove = new KeyValue
            {
                Key = existingKeyValue.Key,
                Label = existingKeyValue.Label
            };

            KeyValue? capturedKeyValue = null;

            _mockStorage.Setup(s => s.AppendKeyValue(It.IsAny<KeyValue>(), It.IsAny<CancellationToken>()))
                .Callback<KeyValue, CancellationToken>((kv, ct) => capturedKeyValue = kv)
                .Returns(Task.CompletedTask);

            // Act
            await _provider.Remove(keyValueToRemove, CancellationToken.None);

            // Assert
            // Verify the storage was called
            _mockStorage.Verify(
                s => s.AppendKeyValue(It.IsAny<KeyValue>(), It.IsAny<CancellationToken>()),
                Times.Once);

            // Verify that the key-value passed to storage has the correct properties
            Assert.NotNull(capturedKeyValue);
            Assert.Equal(keyValueToRemove.Key, capturedKeyValue.Key);
            Assert.Equal(keyValueToRemove.Label, capturedKeyValue.Label);
            Assert.NotNull(capturedKeyValue.Deleted); // Deleted property should be set
            Assert.Equal(DateTimeOffset.UtcNow.Date, capturedKeyValue.Deleted.Value.Date); // Should have current timestamp
            Assert.Null(capturedKeyValue.Value); // Value should be null for deleted items
        }

        [Fact]
        public async Task QueryKeyValues_ReturnsAllItems_WhenNoFiltersProvided()
        {
            // Arrange
            var options = new KeyValueSearchOptions();

            // Act
            var result = await _provider.QueryKeyValues(options, CancellationToken.None);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal(_testKeyValues.Count, result.ToList().Count);
            Assert.Contains(result, kv => kv.Key == "app1/settings/key1" && kv.Label == "dev");
            Assert.Contains(result, kv => kv.Key == "app1/settings/key2" && kv.Label == "dev");
            Assert.Contains(result, kv => kv.Key == "app2/settings/key3" && kv.Label == "");
        }

        [Fact]
        public async Task QueryKeyValues_FiltersItemsByKeyPrefix_WhenKeyFilterProvided()
        {
            // Arrange
            var options = new KeyValueSearchOptions
            {
                KeyFilter = new StringFilter { Prefix = "app1/" }
            };

            // Act
            var result = await _provider.QueryKeyValues(options, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, item => Assert.StartsWith("app1/", item.Key));
            Assert.DoesNotContain(result, kv => kv.Key == "app2/settings/key3");
        }

        [Fact]
        public async Task QueryKeyValues_FiltersItemsByExactKey_WhenKeyEqualsFilterProvided()
        {
            // Arrange
            var options = new KeyValueSearchOptions
            {
                KeyFilter = new StringFilter { EqualsTo = "app1/settings/key1" }
            };

            // Act
            var result = await _provider.QueryKeyValues(options, CancellationToken.None);

            // Assert
            Assert.Single(result);
            Assert.Equal("app1/settings/key1", result.First().Key);
        }

        [Fact]
        public async Task QueryKeyValues_FiltersItemsByLabel_WhenLabelFilterProvided()
        {
            // Arrange
            var options = new KeyValueSearchOptions
            {
                LabelFilter = new StringFilter { EqualsTo = "dev" }
            };

            // Act
            var result = await _provider.QueryKeyValues(options, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, item => Assert.Equal("dev", item.Label));
            Assert.DoesNotContain(result, kv => kv.Label == "");
        }

        [Fact]
        public async Task QueryKeyValues_FiltersItemsByEmptyLabel_WhenEmptyLabelFilterProvided()
        {
            // Arrange
            var options = new KeyValueSearchOptions
            {
                LabelFilter = new StringFilter { EqualsTo = "" }
            };

            // Act
            var result = await _provider.QueryKeyValues(options, CancellationToken.None);

            // Assert
            Assert.Single(result);
            Assert.All(result, item => Assert.Equal("", item.Label));
            Assert.Contains(result, kv => kv.Key == "app2/settings/key3");
        }

        [Fact]
        public async Task QueryKeyValues_CombinesKeyAndLabelFilters_WhenBothProvided()
        {
            // Arrange
            var options = new KeyValueSearchOptions
            {
                KeyFilter = new StringFilter { Prefix = "app1/" },
                LabelFilter = new StringFilter { EqualsTo = "dev" }
            };

            // Act
            var result = await _provider.QueryKeyValues(options, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, item => Assert.StartsWith("app1/", item.Key));
            Assert.All(result, item => Assert.Equal("dev", item.Label));
        }

        [Fact]
        public async Task QueryKeyValues_ReturnsEmptyPage_WhenNoItemsMatchFilters()
        {
            // Arrange
            var options = new KeyValueSearchOptions
            {
                KeyFilter = new StringFilter { Prefix = "nonexistent/" }
            };

            // Act
            var result = await _provider.QueryKeyValues(options, CancellationToken.None);

            // Assert
            Assert.Empty(result);
            Assert.Equal(0, result.Count);
            Assert.Null(result.ContinuationToken);
        }

        [Fact]
        public async Task QueryKeyValues_SetsContinuationToken_WhenPageSizeExceeded()
        {
            // Arrange
            // Set tenant options with small page size
            var tenantOptions = new TenantOptions
            {
                OutputPageSize = 1 // Set small page size to test pagination
            };
            var mockTenantOptions = Options.Create(tenantOptions);

            // Create a new provider with the custom page size
            var provider = new KeyValueProvider(_mockStorage.Object, mockTenantOptions);
            await provider.StartAsync(CancellationToken.None);

            var options = new KeyValueSearchOptions();

            // Act
            var result = await provider.QueryKeyValues(options, CancellationToken.None);

            // Assert
            Assert.Equal(1, tenantOptions.OutputPageSize);
            Assert.NotNull(result.ContinuationToken);
            Assert.Contains(result.First().Key, result.ContinuationToken);
            Assert.Contains(result.First().Label, result.ContinuationToken);
        }

        [Fact]
        public async Task QueryKeys_ReturnsAllUniqueKeys_WhenNoFiltersProvided()
        {
            // Arrange
            var options = new KeySearchOptions();

            // Act
            var result = await _provider.QueryKeys(options, CancellationToken.None);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Contains(result, key => key.Name == "app1/settings/key1");
            Assert.Contains(result, key => key.Name == "app1/settings/key2");
            Assert.Contains(result, key => key.Name == "app2/settings/key3");
        }

        [Fact]
        public async Task QueryKeys_FiltersKeysByPrefix_WhenKeyFilterProvided()
        {
            // Arrange
            var options = new KeySearchOptions
            {
                KeyFilter = new StringFilter { Prefix = "app1/" }
            };

            // Act
            var result = await _provider.QueryKeys(options, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Count); // Should only return keys starting with app1/
            Assert.All(result, key => Assert.StartsWith("app1/", key.Name));
            Assert.DoesNotContain(result, key => key.Name == "app2/settings/key3");
        }

        [Fact]
        public async Task QueryKeys_ReturnsDistinctKeys_WhenMultipleItemsHaveSameKey()
        {
            // Arrange
            // Add a test key value with the same key but different label
            var testData = new List<KeyValue>(_testKeyValues)
            {
                new KeyValue
                {
                    Key = "app1/settings/key1",
                    Label = "prod", // Different label, same key as existing item
                    Value = "value1-prod"
                }
            };

            var mockStorage = new Mock<IKeyValueStorage>();
            mockStorage.Setup(s => s.QueryKeyValues(It.IsAny<CancellationToken>()))
                .Returns(testData.ToAsyncEnumerable());

            var tenantOptions = new TenantOptions();
            var mockTenantOptions = Options.Create(tenantOptions);

            var provider = new KeyValueProvider(mockStorage.Object, mockTenantOptions);
            await provider.StartAsync(CancellationToken.None);

            var options = new KeySearchOptions();

            // Act
            var result = await provider.QueryKeys(options, CancellationToken.None);

            // Assert
            Assert.Equal(3, result.Count); // Still 3 unique keys despite having 4 items
            Assert.Contains(result, key => key.Name == "app1/settings/key1");
            Assert.Contains(result, key => key.Name == "app2/settings/key3");
        }

        [Fact]
        public async Task QueryLabels_ReturnsUniqueLabels_WhenNoFiltersProvided()
        {
            // Arrange
            var options = new LabelSearchOptions();

            // Act
            var result = await _provider.QueryLabels(options, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Count()); // "dev" and "" (empty) labels
            var labels = result.ToList();
            Assert.Contains(labels, l => l.Name == "dev");
            Assert.Contains(labels, l => l.Name == "");
        }

        [Fact]
        public async Task QueryLabels_FiltersItemsByExactLabel_WhenLabelFilterProvided()
        {
            // Arrange
            var options = new LabelSearchOptions
            {
                LabelFilter = new StringFilter { EqualsTo = "dev" }
            };

            // Act
            var result = await _provider.QueryLabels(options, CancellationToken.None);

            // Assert
            Assert.Single(result);
            var labels = result.ToList();
            Assert.Contains(labels, l => l.Name == "dev");
            Assert.DoesNotContain(labels, l => l.Name == "");
        }

        [Fact]
        public async Task QueryLabels_FiltersItemsByEmptyLabel_WhenEmptyLabelFilterProvided()
        {
            // Arrange
            var options = new LabelSearchOptions
            {
                LabelFilter = new StringFilter { EqualsTo = "" }
            };

            // Act
            var result = await _provider.QueryLabels(options, CancellationToken.None);

            // Assert
            Assert.Single(result);
            var labels = result.ToList();
            Assert.Contains(labels, l => l.Name == "");
            Assert.DoesNotContain(labels, l => l.Name == "dev");
        }

        [Fact]
        public async Task QueryLabels_ReturnsDistinctLabels_WhenDuplicateLabelsExist()
        {
            // Arrange
            // Create a new test setup with duplicate labels but different keys
            var keyValues = new List<KeyValue>
            {
                new KeyValue { Key = "key1", Label = "dev", Value = "value1" },
                new KeyValue { Key = "key2", Label = "dev", Value = "value2" },
                new KeyValue { Key = "key3", Label = "prod", Value = "value3" }
            };

            var mockStorage = new Mock<IKeyValueStorage>();
            mockStorage.Setup(s => s.QueryKeyValues(It.IsAny<CancellationToken>()))
                .Returns(keyValues.ToAsyncEnumerable());

            var tenantOptions = new TenantOptions();
            var mockTenantOptions = Options.Create(tenantOptions);
            var provider = new KeyValueProvider(mockStorage.Object, mockTenantOptions);
            await provider.StartAsync(CancellationToken.None);

            var options = new LabelSearchOptions();

            // Act
            var result = await provider.QueryLabels(options, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Count()); // Should only have "dev" and "prod", not duplicates
            var labels = result.ToList();
            Assert.Contains(labels, l => l.Name == "dev");
            Assert.Contains(labels, l => l.Name == "prod");
        }
    }

    // Extension method to convert IEnumerable to IAsyncEnumerable for testing
    public static class EnumerableExtensions
    {
        public static IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
        {
            return new AsyncEnumerableWrapper<T>(source);
        }

        private class AsyncEnumerableWrapper<T> : IAsyncEnumerable<T>
        {
            private readonly IEnumerable<T> _source;

            public AsyncEnumerableWrapper(IEnumerable<T> source)
            {
                _source = source;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                return new AsyncEnumeratorWrapper<T>(_source.GetEnumerator());
            }
        }

        private class AsyncEnumeratorWrapper<T> : IAsyncEnumerator<T>
        {
            private readonly IEnumerator<T> _enumerator;

            public AsyncEnumeratorWrapper(IEnumerator<T> enumerator)
            {
                _enumerator = enumerator;
            }

            public T Current => _enumerator.Current;

            public ValueTask DisposeAsync()
            {
                _enumerator.Dispose();
                return ValueTask.CompletedTask;
            }

            public ValueTask<bool> MoveNextAsync()
            {
                return new ValueTask<bool>(_enumerator.MoveNext());
            }
        }
    }
}
