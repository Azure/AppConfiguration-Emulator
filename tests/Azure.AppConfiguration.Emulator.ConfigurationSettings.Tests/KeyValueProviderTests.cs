using Microsoft.Extensions.Options;
using Moq;
using Azure.AppConfiguration.Emulator.Tenant;

namespace Azure.AppConfiguration.Emulator.ConfigurationSettings.Tests
{
    public class KeyValueProviderTests
    {
        private readonly IOptions<TenantOptions> _mockTenantOptions = Options.Create(new TenantOptions());

        [Fact]
        public async Task GetKeyValue_ReturnsKeyValue_WhenKeyAndLabelMatch()
        {
            var testKeyValues = new List<KeyValue>
            {
                new KeyValue
                {
                    Key = "test-key",
                    Label = "dev",
                    Value = "dev-value"
                },
                new KeyValue
                {
                    Key = "test-key",
                    Label = "prod",
                    Value = "prod-value"
                }
            };

            var mockStorage = new Mock<IKeyValueStorage>();
            mockStorage.Setup(s => s.QueryKeyValues(It.IsAny<CancellationToken>()))
                .Returns(testKeyValues.ToAsyncEnumerable());

            var provider = new KeyValueProvider(mockStorage.Object, _mockTenantOptions);
            await provider.StartAsync(CancellationToken.None);

            KeyValue result = await provider.GetKeyValue("test-key", "dev", CancellationToken.None);
            Assert.NotNull(result);
            Assert.Equal("test-key", result.Key);
            Assert.Equal("dev", result.Label);
            Assert.Equal("dev-value", result.Value);

            result = await provider.GetKeyValue("test-key", "prod", CancellationToken.None);
            Assert.NotNull(result);
            Assert.Equal("test-key", result.Key);
            Assert.Equal("prod", result.Label);
            Assert.Equal("prod-value", result.Value);
        }

        [Fact]
        public async Task GetKeyValue_ReturnsNull_WhenKeyDoesNotExist()
        {
            var testKeyValues = new List<KeyValue>
            {
                new KeyValue
                {
                    Key = "test-key",
                    Label = "dev",
                    Value = "value"
                }
            };

            var mockStorage = new Mock<IKeyValueStorage>();
            mockStorage.Setup(s => s.QueryKeyValues(It.IsAny<CancellationToken>()))
                .Returns(testKeyValues.ToAsyncEnumerable());

            var provider = new KeyValueProvider(mockStorage.Object, _mockTenantOptions);
            await provider.StartAsync(CancellationToken.None);

            KeyValue result = await provider.GetKeyValue("nonexistent-key", "dev", CancellationToken.None);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetKeyValue_ReturnsNull_WhenLabelDoesNotMatch()
        {
            var testKeyValues = new List<KeyValue>
            {
                new KeyValue
                {
                    Key = "test-key",
                    Label = "dev", // Different from the label we'll search for
                    Value = "value"
                }
            };

            // Create mock storage with just the key values needed for this test
            var mockStorage = new Mock<IKeyValueStorage>();
            mockStorage.Setup(s => s.QueryKeyValues(It.IsAny<CancellationToken>()))
                .Returns(testKeyValues.ToAsyncEnumerable());

            var provider = new KeyValueProvider(mockStorage.Object, _mockTenantOptions);
            await provider.StartAsync(CancellationToken.None);

            KeyValue result = await provider.GetKeyValue("test-key", "prod", CancellationToken.None);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetKeyValue_ReturnsKeyValueWithEmptyLabel_WhenLabelIsEmpty()
        {
            var testKeyValues = new List<KeyValue>
            {
                new KeyValue
                {
                    Key = "test-key",
                    Label = "",
                    Value = "value"
                }
            };

            var mockStorage = new Mock<IKeyValueStorage>();
            mockStorage.Setup(s => s.QueryKeyValues(It.IsAny<CancellationToken>()))
                .Returns(testKeyValues.ToAsyncEnumerable());

            var provider = new KeyValueProvider(mockStorage.Object, _mockTenantOptions);
            await provider.StartAsync(CancellationToken.None);

            KeyValue result = await provider.GetKeyValue("test-key", "", CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test-key", result.Key);
            Assert.Equal("", result.Label);
            Assert.Equal("value", result.Value);
        }

        [Fact]
        public async Task Set_AddsNewKeyValue_WhenKeyDoesNotExist()
        {
            var testKeyValues = new List<KeyValue>
            {
                new KeyValue
                {
                    Key = "existing-key",
                    Label = "existing-label",
                    Value = "existing-value"
                }
            };

            var newKeyValue = new KeyValue
            {
                Key = "new-key",
                Label = "new-label",
                Value = "new-value"
            };

            KeyValue? capturedKeyValue = null;

            // Create mock storage with just the key values needed for this test
            var mockStorage = new Mock<IKeyValueStorage>();
            mockStorage.Setup(s => s.QueryKeyValues(It.IsAny<CancellationToken>()))
                .Returns(testKeyValues.ToAsyncEnumerable());
            mockStorage.Setup(s => s.AppendKeyValue(It.IsAny<KeyValue>(), It.IsAny<CancellationToken>()))
                .Callback<KeyValue, CancellationToken>((kv, ct) => capturedKeyValue = kv)
                .Returns(Task.CompletedTask);

            var provider = new KeyValueProvider(mockStorage.Object, _mockTenantOptions);
            await provider.StartAsync(CancellationToken.None);

            KeyValue result = await provider.Set(newKeyValue, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newKeyValue.Key, result.Key);
            Assert.Equal(newKeyValue.Label, result.Label);
            Assert.Equal(newKeyValue.Value, result.Value);
            Assert.NotNull(result.Etag); // Should have an ETag assigned

            // Verify the storage was called
            mockStorage.Verify(s => s.AppendKeyValue(It.IsAny<KeyValue>(), It.IsAny<CancellationToken>()), Times.Once);

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
            // Create existing key value
            var existingKeyValue = new KeyValue
            {
                Key = "test-key",
                Label = "dev",
                Value = "value"
            };

            // Create test-specific key values
            var testKeyValues = new List<KeyValue>
            {
                existingKeyValue
            };

            var updatedKeyValue = new KeyValue
            {
                Key = existingKeyValue.Key,
                Label = existingKeyValue.Label,
                Value = "updated-value"
            };

            KeyValue? capturedKeyValue = null;

            var mockStorage = new Mock<IKeyValueStorage>();
            mockStorage.Setup(s => s.QueryKeyValues(It.IsAny<CancellationToken>()))
                .Returns(testKeyValues.ToAsyncEnumerable());
            mockStorage.Setup(s => s.AppendKeyValue(It.IsAny<KeyValue>(), It.IsAny<CancellationToken>()))
                .Callback<KeyValue, CancellationToken>((kv, ct) => capturedKeyValue = kv)
                .Returns(Task.CompletedTask);

            var provider = new KeyValueProvider(mockStorage.Object, _mockTenantOptions);
            await provider.StartAsync(CancellationToken.None);

            KeyValue result = await provider.Set(updatedKeyValue, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(updatedKeyValue.Key, result.Key);
            Assert.Equal(updatedKeyValue.Label, result.Label);
            Assert.Equal(updatedKeyValue.Value, result.Value);

            // Verify the storage was called
            mockStorage.Verify(s => s.AppendKeyValue(It.IsAny<KeyValue>(), It.IsAny<CancellationToken>()), Times.Once);

            // Verify the key-value passed to storage has the correct properties
            Assert.NotNull(capturedKeyValue);
            Assert.Equal(updatedKeyValue.Key, capturedKeyValue.Key);
            Assert.Equal(updatedKeyValue.Label, capturedKeyValue.Label);
            Assert.Equal(updatedKeyValue.Value, capturedKeyValue.Value);
            Assert.NotNull(capturedKeyValue.Etag);
            Assert.Equal(DateTimeOffset.UtcNow.Date, capturedKeyValue.Timestamp.Date); // Should have current timestamp
            Assert.Null(capturedKeyValue.Deleted); // Should not be marked as deleted
        }

        [Fact]
        public async Task Remove_MarksKeyValueAsDeleted()
        {
            var existingKeyValue = new KeyValue
            {
                Key = "test-key",
                Label = "dev",
                Value = "value"
            };

            var testKeyValues = new List<KeyValue>
            {
                existingKeyValue
            };

            var keyValueToRemove = new KeyValue
            {
                Key = existingKeyValue.Key,
                Label = existingKeyValue.Label
            };

            KeyValue? capturedKeyValue = null;

            // Create mock storage with just the key values needed for this test
            var mockStorage = new Mock<IKeyValueStorage>();
            mockStorage.Setup(s => s.QueryKeyValues(It.IsAny<CancellationToken>()))
                .Returns(testKeyValues.ToAsyncEnumerable());
            mockStorage.Setup(s => s.AppendKeyValue(It.IsAny<KeyValue>(), It.IsAny<CancellationToken>()))
                .Callback<KeyValue, CancellationToken>((kv, ct) => capturedKeyValue = kv)
                .Returns(Task.CompletedTask);

            var provider = new KeyValueProvider(mockStorage.Object, _mockTenantOptions);
            await provider.StartAsync(CancellationToken.None);

            await provider.Remove(keyValueToRemove, CancellationToken.None);

            mockStorage.Verify(
                s => s.AppendKeyValue(It.IsAny<KeyValue>(), It.IsAny<CancellationToken>()),
                Times.Once);

            Assert.NotNull(capturedKeyValue);
            Assert.Equal(keyValueToRemove.Key, capturedKeyValue.Key);
            Assert.Equal(keyValueToRemove.Label, capturedKeyValue.Label);
            Assert.NotNull(capturedKeyValue.Deleted); // Deleted property should be set
            Assert.Equal(DateTimeOffset.UtcNow.Date, capturedKeyValue.Deleted.Value.Date); // Should have current timestamp
        }

        [Fact]
        public async Task QueryKeyValues_ReturnsAllItems_WhenNoFiltersProvided()
        {
            var testKeyValues = new List<KeyValue>
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
                    Label = "",
                    Value = "value3"
                }
            };

            var mockStorage = new Mock<IKeyValueStorage>();
            mockStorage.Setup(s => s.QueryKeyValues(It.IsAny<CancellationToken>()))
                .Returns(testKeyValues.ToAsyncEnumerable());

            var provider = new KeyValueProvider(mockStorage.Object, _mockTenantOptions);
            await provider.StartAsync(CancellationToken.None);

            Page<KeyValue> result = await provider.QueryKeyValues(new KeyValueSearchOptions(), CancellationToken.None);

            Assert.Equal(3, result.Count);
            Assert.Equal(testKeyValues.Count, result.ToList().Count);
            Assert.Contains(result, kv => kv.Key == "app1/settings/key1" && kv.Label == "dev");
            Assert.Contains(result, kv => kv.Key == "app1/settings/key2" && kv.Label == "dev");
            Assert.Contains(result, kv => kv.Key == "app2/settings/key3" && kv.Label == "");
        }

        [Fact]
        public async Task QueryKeyValues_FiltersItemsByKeyPrefix_WhenKeyFilterProvided()
        {
            var testKeyValues = new List<KeyValue>
            {
                new KeyValue
                {
                    Key = "app1/settings/key1", // This should be included in results
                    Label = "dev",
                    Value = "value1"
                },
                new KeyValue
                {
                    Key = "app1/settings/key2", // This should be included in results
                    Label = "dev",
                    Value = "value2"
                },
                new KeyValue
                {
                    Key = "app2/settings/key3", // This should NOT be included in results
                    Label = "dev",
                    Value = "value3"
                }
            };

            var mockStorage = new Mock<IKeyValueStorage>();
            mockStorage.Setup(s => s.QueryKeyValues(It.IsAny<CancellationToken>()))
                .Returns(testKeyValues.ToAsyncEnumerable());

            var provider = new KeyValueProvider(mockStorage.Object, _mockTenantOptions);
            await provider.StartAsync(CancellationToken.None);

            var options = new KeyValueSearchOptions
            {
                KeyFilter = new StringFilter { Prefix = "app1/" }
            };

            Page<KeyValue> result = await provider.QueryKeyValues(options, CancellationToken.None);

            Assert.Equal(2, result.Count);
            Assert.All(result, item => Assert.StartsWith("app1/", item.Key));
            Assert.DoesNotContain(result, kv => kv.Key == "app2/settings/key3");
        }

        [Fact]
        public async Task QueryKeyValues_FiltersItemsByExactKey_WhenKeyEqualsFilterProvided()
        {
            var testKeyValues = new List<KeyValue>
            {
                new KeyValue
                {
                    Key = "app1/settings/key1", // This should be included in results
                    Label = "dev",
                    Value = "value1"
                },
                new KeyValue
                {
                    Key = "app1/settings/key2", // This should NOT be included in results
                    Label = "dev",
                    Value = "value2"
                },
                new KeyValue
                {
                    Key = "app2/settings/key3", // This should NOT be included in results
                    Label = "dev",
                    Value = "value3"
                }
            };

            var mockStorage = new Mock<IKeyValueStorage>();
            mockStorage.Setup(s => s.QueryKeyValues(It.IsAny<CancellationToken>()))
                .Returns(testKeyValues.ToAsyncEnumerable());

            var provider = new KeyValueProvider(mockStorage.Object, _mockTenantOptions);
            await provider.StartAsync(CancellationToken.None);

            var options = new KeyValueSearchOptions
            {
                KeyFilter = new StringFilter { EqualsTo = "app1/settings/key1" }
            };

            Page<KeyValue> result = await provider.QueryKeyValues(options, CancellationToken.None);

            Assert.Single(result);
            Assert.Equal("app1/settings/key1", result.First().Key);
        }

        [Fact]
        public async Task QueryKeyValues_FiltersItemsByLabel_WhenLabelFilterProvided()
        {
            var testKeyValues = new List<KeyValue>
            {
                new KeyValue
                {
                    Key = "test-key1",
                    Label = "dev",
                    Value = "value"
                },
                new KeyValue
                {
                    Key = "test-key2",
                    Label = "dev",
                    Value = "value"
                },
                new KeyValue
                {
                    Key = "test-key3",
                    Label = "prod",
                    Value = "value"
                },
            };

            var mockStorage = new Mock<IKeyValueStorage>();
            mockStorage.Setup(s => s.QueryKeyValues(It.IsAny<CancellationToken>()))
                .Returns(testKeyValues.ToAsyncEnumerable());

            var provider = new KeyValueProvider(mockStorage.Object, _mockTenantOptions);
            await provider.StartAsync(CancellationToken.None);

            var options = new KeyValueSearchOptions
            {
                LabelFilter = new StringFilter { EqualsTo = "dev" }
            };

            Page<KeyValue> result = await provider.QueryKeyValues(options, CancellationToken.None);

            Assert.Equal(2, result.Count);
            Assert.All(result, item => Assert.Equal("dev", item.Label));
        }

        [Fact]
        public async Task QueryKeyValues_CombinesKeyAndLabelFilters_WhenBothProvided()
        {
            var testKeyValues = new List<KeyValue>
            {
                new KeyValue
                {
                    Key = "test-key1",
                    Label = "dev",
                    Value = "value"
                },
                new KeyValue
                {
                    Key = "test-key2",
                    Label = "dev",
                    Value = "value"
                },
                new KeyValue
                {
                    Key = "test-key3",
                    Label = "prod",
                    Value = "value"
                },
            };

            var mockStorage = new Mock<IKeyValueStorage>();
            mockStorage.Setup(s => s.QueryKeyValues(It.IsAny<CancellationToken>()))
                .Returns(testKeyValues.ToAsyncEnumerable());

            var provider = new KeyValueProvider(mockStorage.Object, _mockTenantOptions);
            await provider.StartAsync(CancellationToken.None);

            var options = new KeyValueSearchOptions
            {
                KeyFilter = new StringFilter { Prefix = "test" },
                LabelFilter = new StringFilter { EqualsTo = "dev" }
            };

            Page<KeyValue> result = await provider.QueryKeyValues(options, CancellationToken.None);

            Assert.Equal(2, result.Count);
            Assert.All(result, item => Assert.Equal("dev", item.Label));

            options = new KeyValueSearchOptions
            {
                KeyFilter = new StringFilter { EqualsTo = "test-key1" },
                LabelFilter = new StringFilter { EqualsTo = "dev" }
            };

            result = await provider.QueryKeyValues(options, CancellationToken.None);

            Assert.Single(result);
            Assert.Equal("test-key1", result.First().Key);
        }

        [Fact]
        public async Task QueryKeyValues_ReturnsEmptyPage_WhenNoItemsMatchFilters()
        {
            var testKeyValues = new List<KeyValue>
            {
                new KeyValue
                {
                    Key = "test-key1",
                    Label = "dev",
                    Value = "value"
                },
                new KeyValue
                {
                    Key = "test-key2",
                    Label = "dev",
                    Value = "value"
                },
                new KeyValue
                {
                    Key = "test-key3",
                    Label = "prod",
                    Value = "value"
                },
            };

            var mockStorage = new Mock<IKeyValueStorage>();
            mockStorage.Setup(s => s.QueryKeyValues(It.IsAny<CancellationToken>()))
                .Returns(testKeyValues.ToAsyncEnumerable());

            var provider = new KeyValueProvider(mockStorage.Object, _mockTenantOptions);
            await provider.StartAsync(CancellationToken.None);

            var options = new KeyValueSearchOptions
            {
                KeyFilter = new StringFilter { Prefix = "test" },
                LabelFilter = new StringFilter { EqualsTo = "staging" }
            };

            Page<KeyValue> result = await provider.QueryKeyValues(options, CancellationToken.None);

            Assert.Empty(result);
        }

        [Fact]
        public async Task QueryKeyValues_SetsContinuationToken_WhenPageSizeExceeded()
        {
            var testKeyValues = new List<KeyValue>
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
                    Label = "dev",
                    Value = "value3"
                }
            };

            var mockStorage = new Mock<IKeyValueStorage>();
            mockStorage.Setup(s => s.QueryKeyValues(It.IsAny<CancellationToken>()))
                .Returns(testKeyValues.ToAsyncEnumerable());

            var tenantOptions = new TenantOptions
            {
                OutputPageSize = 1 // Set small page size to test pagination
            };
            var mockTenantOptions = Options.Create(tenantOptions);
            var provider = new KeyValueProvider(mockStorage.Object, mockTenantOptions);
            await provider.StartAsync(CancellationToken.None);

            Page<KeyValue> result = await provider.QueryKeyValues(new KeyValueSearchOptions(), CancellationToken.None);

            Assert.Equal(1, tenantOptions.OutputPageSize);
            Assert.NotNull(result.ContinuationToken);
            Assert.Contains(result.First().Key, result.ContinuationToken);
            Assert.Contains(result.First().Label, result.ContinuationToken);
        }

        [Fact]
        public async Task QueryKeys_ReturnsAllUniqueKeys_WhenNoFiltersProvided()
        {
            // Arrange
            // Create test-specific key values
            var testKeyValues = new List<KeyValue>
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
                    Label = "",
                    Value = "value3"
                }
            };

            var mockStorage = new Mock<IKeyValueStorage>();
            mockStorage.Setup(s => s.QueryKeyValues(It.IsAny<CancellationToken>()))
                .Returns(testKeyValues.ToAsyncEnumerable());

            var provider = new KeyValueProvider(mockStorage.Object, _mockTenantOptions);
            await provider.StartAsync(CancellationToken.None);

            Page<Key> result = await provider.QueryKeys(new KeySearchOptions(), CancellationToken.None);

            Assert.Equal(3, result.Count);
            Assert.Contains(result, key => key.Name == "app1/settings/key1");
            Assert.Contains(result, key => key.Name == "app1/settings/key2");
            Assert.Contains(result, key => key.Name == "app2/settings/key3");
        }

        [Fact]
        public async Task QueryKeys_FiltersKeysByPrefix_WhenKeyFilterProvided()
        {
            var testKeyValues = new List<KeyValue>
            {
                new KeyValue
                {
                    Key = "app1/settings/key1", // Should be included in results
                    Label = "dev",
                    Value = "value1"
                },
                new KeyValue
                {
                    Key = "app1/settings/key2", // Should be included in results
                    Label = "dev",
                    Value = "value2"
                },
                new KeyValue
                {
                    Key = "app2/settings/key3", // Should NOT be included in results
                    Label = "dev",
                    Value = "value3"
                }
            };

            var mockStorage = new Mock<IKeyValueStorage>();
            mockStorage.Setup(s => s.QueryKeyValues(It.IsAny<CancellationToken>()))
                .Returns(testKeyValues.ToAsyncEnumerable());

            var provider = new KeyValueProvider(mockStorage.Object, _mockTenantOptions);
            await provider.StartAsync(CancellationToken.None);

            var options = new KeySearchOptions
            {
                KeyFilter = new StringFilter { Prefix = "app1/" }
            };

            Page<Key> result = await provider.QueryKeys(options, CancellationToken.None);

            Assert.Equal(2, result.Count); // Should only return keys starting with app1/
            Assert.All(result, key => Assert.StartsWith("app1/", key.Name));
            Assert.DoesNotContain(result, key => key.Name == "app2/settings/key3");
        }

        [Fact]
        public async Task QueryKeys_ReturnsDistinctKeys_WhenMultipleItemsHaveSameKey()
        {
            var testKeyValues = new List<KeyValue>
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
                    Key = "app1/settings/key1", // Duplicate key, different label
                    Label = "prod",
                    Value = "value1-prod"
                }
            };

            var mockStorage = new Mock<IKeyValueStorage>();
            mockStorage.Setup(s => s.QueryKeyValues(It.IsAny<CancellationToken>()))
                .Returns(testKeyValues.ToAsyncEnumerable());

            var provider = new KeyValueProvider(mockStorage.Object, _mockTenantOptions);
            await provider.StartAsync(CancellationToken.None);

            Page<Key> result = await provider.QueryKeys(new KeySearchOptions(), CancellationToken.None);

            Assert.Equal(2, result.Count); // Only unique keys should be returned
            Assert.Contains(result, key => key.Name == "app1/settings/key1");
            Assert.Contains(result, key => key.Name == "app1/settings/key2");
        }

        [Fact]
        public async Task QueryLabels_ReturnsUniqueLabels_WhenNoFiltersProvided()
        {
            var testKeyValues = new List<KeyValue>
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
                    Label = "",
                    Value = "value3"
                },
                new KeyValue
                {
                    Key = "app3/settings/key4",
                    Label = "prod",
                    Value = "value4"
                }
            };

            var mockStorage = new Mock<IKeyValueStorage>();
            mockStorage.Setup(s => s.QueryKeyValues(It.IsAny<CancellationToken>()))
                .Returns(testKeyValues.ToAsyncEnumerable());

            var provider = new KeyValueProvider(mockStorage.Object, _mockTenantOptions);
            await provider.StartAsync(CancellationToken.None);

            Page<Label> result = await provider.QueryLabels(new LabelSearchOptions(), CancellationToken.None);

            Assert.Equal(3, result.Count());
            var labels = result.ToList();
            Assert.Contains(labels, l => l.Name == "dev");
            Assert.Contains(labels, l => l.Name == "");
            Assert.Contains(labels, l => l.Name == "prod");
        }

        [Fact]
        public async Task QueryLabels_FiltersItemsByExactLabel_WhenLabelFilterProvided()
        {
            var testKeyValues = new List<KeyValue>
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
                    Label = "",
                    Value = "value3"
                }
            };

            var mockStorage = new Mock<IKeyValueStorage>();
            mockStorage.Setup(s => s.QueryKeyValues(It.IsAny<CancellationToken>()))
                .Returns(testKeyValues.ToAsyncEnumerable());

            var provider = new KeyValueProvider(mockStorage.Object, _mockTenantOptions);
            await provider.StartAsync(CancellationToken.None);

            var options = new LabelSearchOptions
            {
                LabelFilter = new StringFilter { EqualsTo = "dev" }
            };

            Page<Label> result = await provider.QueryLabels(options, CancellationToken.None);

            Assert.Single(result);
            var labels = result.ToList();
            Assert.Contains(labels, l => l.Name == "dev");
            Assert.DoesNotContain(labels, l => l.Name == "");
        }

        [Fact]
        public async Task QueryRevisions_IncludesDeletedItems_WhileRegularQueriesDont()
        {
            // Create a new list of test key values including a deleted one
            var testKeyValuesWithDeleted = new List<KeyValue>
            {
                new KeyValue
                {
                    Key = "kv1",
                    Label = "prod",
                    Value = "v1",
                    Etag = "k4CS96c5WjZ3YL7aSRnryQ",
                    Timestamp = DateTimeOffset.UtcNow.AddMinutes(-10),
                    RevisionTTL = TimeSpan.FromDays(7)
                },
                new KeyValue
                {
                    Key = "kv1",
                    Label = "prod",
                    Etag = "E6UIwxN_UBJVQWv1cwdnsw",
                    Timestamp = DateTimeOffset.UtcNow.AddMinutes(-9),
                    Deleted = DateTimeOffset.UtcNow.AddMinutes(-9), // Marked as deleted
                    RevisionTTL = TimeSpan.FromDays(7)
                },
                new KeyValue
                {
                    Key = "kv1",
                    Label = "prod",
                    Value = "v2",
                    Etag = "BEhc_7t-4hFuIllQGJzYWQ",
                    Timestamp = DateTimeOffset.UtcNow.AddMinutes(-8),
                    RevisionTTL = TimeSpan.FromDays(7)
                },
                new KeyValue
                {
                    Key = "kv1",
                    Label = "prod",
                    Etag = "YbI3hYG2DUWOXMCF33Os8Q",
                    Timestamp = DateTimeOffset.UtcNow.AddMinutes(-7),
                    Deleted = DateTimeOffset.UtcNow.AddMinutes(-7), // Marked as deleted again
                    RevisionTTL = TimeSpan.FromDays(7)
                },
                new KeyValue
                {
                    Key = "kv1",
                    Label = "prod",
                    Value = "v3",
                    Etag = "beV9smccBkywiv8c54XM7g",
                    Timestamp = DateTimeOffset.UtcNow.AddMinutes(-6),
                    RevisionTTL = TimeSpan.FromDays(7)
                }
            };

            var mockStorage = new Mock<IKeyValueStorage>();
            mockStorage.Setup(s => s.QueryKeyValues(It.IsAny<CancellationToken>()))
                .Returns(testKeyValuesWithDeleted.ToAsyncEnumerable());

            var provider = new KeyValueProvider(mockStorage.Object, _mockTenantOptions);
            await provider.StartAsync(CancellationToken.None);

            // 1. QueryRevisions should include all versions, including deleted ones
            var revisions = await provider.QueryRevisions(
                new KeyValueSearchOptions { KeyFilter = new StringFilter { EqualsTo = "kv1" } },
                CancellationToken.None);

            // 2. GetKeyValue should only return the latest non-deleted version
            var keyValue = await provider.GetKeyValue("kv1", "prod", CancellationToken.None);

            // 3. QueryKeyValues should only return non-deleted items
            var keyValues = await provider.QueryKeyValues(
                new KeyValueSearchOptions { KeyFilter = new StringFilter { EqualsTo = "kv1" } },
                CancellationToken.None);

            // 1. Verify QueryRevisions includes all 3 versions (including deleted ones)
            Assert.Equal(3, revisions.Count());
            Assert.Contains(revisions, kv => kv.Etag == "k4CS96c5WjZ3YL7aSRnryQ"); // First version
            Assert.Contains(revisions, kv => kv.Etag == "BEhc_7t-4hFuIllQGJzYWQ"); // Second version
            Assert.Contains(revisions, kv => kv.Etag == "beV9smccBkywiv8c54XM7g"); // Third version

            // 2. Verify GetKeyValue returns only the latest non-deleted version (v3)
            Assert.NotNull(keyValue);
            Assert.Equal("kv1", keyValue.Key);
            Assert.Equal("prod", keyValue.Label);
            Assert.Equal("v3", keyValue.Value);
            Assert.Equal("beV9smccBkywiv8c54XM7g", keyValue.Etag);

            // 3. Verify QueryKeyValues returns only the latest non-deleted version
            Assert.Single(keyValues);
            Assert.Equal("kv1", keyValues.First().Key);
            Assert.Equal("prod", keyValues.First().Label);
            Assert.Equal("v3", keyValues.First().Value);
            Assert.Equal("beV9smccBkywiv8c54XM7g", keyValues.First().Etag);
        }

        [Fact]
        public async Task QueryKeyValues_FiltersItemsBySingleTag_WhenTagsFilterProvided()
        {
            var testKeyValues = new List<KeyValue>
            {
                new KeyValue
                {
                    Key = "test-key1",
                    Label = "test",
                    Value = "value1",
                    Tags = new Dictionary<string, string>
                    {
                        { "tag1", "value1" }
                    }
                },
                new KeyValue
                {
                    Key = "test-key1",
                    Label = "dev",
                    Value = "value2",
                    Tags = new Dictionary<string, string>
                    {
                        { "tag1", "value1" },
                        { "tag2", "value2" }
                    }
                },
                new KeyValue
                {
                    Key = "test-key1",
                    Label = "staging",
                    Value = "value3",
                    Tags = new Dictionary<string, string>
                    {
                        { "tag1", "value3" }
                    }
                }
            };

            var mockStorage = new Mock<IKeyValueStorage>();
            mockStorage.Setup(s => s.QueryKeyValues(It.IsAny<CancellationToken>()))
                .Returns(testKeyValues.ToAsyncEnumerable());

            var provider = new KeyValueProvider(mockStorage.Object, _mockTenantOptions);
            await provider.StartAsync(CancellationToken.None);

            var options = new KeyValueSearchOptions
            {
                Tags = new Dictionary<string, string>
                {
                    { "tag1", "value1" }
                }
            };

            Page<KeyValue> result = await provider.QueryKeyValues(options, CancellationToken.None);

            Assert.Equal(2, result.Count);
            Assert.All(result, item =>
            {
                Assert.NotNull(item.Tags);
                Assert.True(item.Tags.ContainsKey("tag1"));
                Assert.Equal("value1", item.Tags["tag1"]);
            });
            Assert.Contains(result, kv => kv.Value == "value1");
            Assert.Contains(result, kv => kv.Value == "value2");
            Assert.DoesNotContain(result, kv => kv.Value == "value3"); // This has tag1=value3
        }

        [Fact]
        public async Task QueryKeyValues_CombinesTagsAndOtherFilters_WhenMultipleFilterTypesProvided()
        {
            var testKeyValues = new List<KeyValue>
            {
                new KeyValue
                {
                    Key = "test-key1",
                    Label = "test",
                    Value = "test-value",
                    Tags = new Dictionary<string, string>
                    {
                        { "tag1", "value1" },
                        { "tag2", "value2" }
                    }
                },
                new KeyValue
                {
                    Key = "test-key1",
                    Label = "dev",
                    Value = "dev-value",
                    Tags = new Dictionary<string, string>
                    {
                        { "tag1", "value1" },
                        { "tag2", "value2" }
                    }
                },
                new KeyValue
                {
                    Key = "test-key1",
                    Label = "",
                    Value = "value3",
                    Tags = new Dictionary<string, string>
                    {
                        { "tag2", "value2" },
                        { "tag3", "value3" }
                    }
                }
            };

            var mockStorage = new Mock<IKeyValueStorage>();
            mockStorage.Setup(s => s.QueryKeyValues(It.IsAny<CancellationToken>()))
                .Returns(testKeyValues.ToAsyncEnumerable());

            var provider = new KeyValueProvider(mockStorage.Object, _mockTenantOptions);
            await provider.StartAsync(CancellationToken.None);

            var options = new KeyValueSearchOptions
            {
                KeyFilter = new StringFilter { Prefix = "test" },
                LabelFilter = new StringFilter { EqualsTo = "dev" },
                Tags = new Dictionary<string, string>
                {
                    { "tag1", "value1" }
                }
            };

            Page<KeyValue> result = await provider.QueryKeyValues(options, CancellationToken.None);

            Assert.Single(result);
            var item = result.First();
            Assert.Equal("test-key1", item.Key);
            Assert.Equal("dev", item.Label);
            Assert.Equal("dev-value", item.Value);
            Assert.NotNull(item.Tags);
            Assert.True(item.Tags.ContainsKey("tag1"));
            Assert.Equal("value1", item.Tags["tag1"]);
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
