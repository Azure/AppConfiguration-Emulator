using Xunit;

namespace Azure.AppConfiguration.Emulator.Host.Tests
{
    [Collection("TestServerCollection")]
    public class RevisionTests
    {
        private readonly ITestServer _testServer;

        public RevisionTests(TestServerFixture fixture)
        {
            _testServer = fixture.TestServer;
        }

        [Fact]
        public async Task DeleteKeyValue_GetOperationsReturnEmpty()
        {
            var client = _testServer.Client;

            var key1 = "delete-test-key1";
            var value1 = "delete-test-value1";
            var createResponse1 = await TestHelpers.CreateKeyValue(client, key1, value1);
            createResponse1.EnsureSuccessStatusCode();

            var key2 = "delete-test-key2";
            var value2 = "delete-test-value2";
            var label2 = "delete-test-label";
            var createResponse2 = await TestHelpers.CreateKeyValue(client, key2, value2, label: label2);
            createResponse2.EnsureSuccessStatusCode();

            var key3 = "persist-test-key";
            var value3 = "persist-test-value";
            var createResponse3 = await TestHelpers.CreateKeyValue(client, key3, value3);
            createResponse3.EnsureSuccessStatusCode();

            var allKeysBeforeDelete = await TestHelpers.GetKeys(client);
            Assert.Contains(allKeysBeforeDelete.Items, k => k.Name == key1);
            Assert.Contains(allKeysBeforeDelete.Items, k => k.Name == key2);

            var keyValue1BeforeDelete = await TestHelpers.GetKeyValue(client, key1);
            Assert.NotNull(keyValue1BeforeDelete);

            var keyValue2BeforeDelete = await TestHelpers.GetKeyValue(client, key2, label2);
            Assert.NotNull(keyValue2BeforeDelete);

            var deleteResponse1 = await TestHelpers.DeleteKeyValue(client, key1);
            deleteResponse1.EnsureSuccessStatusCode();

            var deleteResponse2 = await TestHelpers.DeleteKeyValue(client, key2, label2);
            deleteResponse2.EnsureSuccessStatusCode();

            // Test 1: Verify direct key-value access returns null for deleted items
            var keyValue1AfterDelete = await TestHelpers.GetKeyValue(client, key1);
            Assert.Null(keyValue1AfterDelete);

            var keyValue2AfterDelete = await TestHelpers.GetKeyValue(client, key2, label2);
            Assert.Null(keyValue2AfterDelete);

            // Test 2: Verify keys endpoint no longer includes deleted keys
            var allKeysAfterDelete = await TestHelpers.GetKeys(client);
            Assert.DoesNotContain(allKeysAfterDelete.Items, k => k.Name == key1);

            // Test 3: Verify query endpoint returns empty results for deleted key-values
            var queryResults1 = await TestHelpers.QueryKeyValues(client, key: key1);
            Assert.Empty(queryResults1.Items);

            var queryResults2 = await TestHelpers.QueryKeyValues(client, key: key2, label: label2);
            Assert.Empty(queryResults2.Items);

            // Test 4: Verify the non-deleted key-value is still accessible
            var persistentKeyValue = await TestHelpers.GetKeyValue(client, key3);
            Assert.NotNull(persistentKeyValue);
            Assert.Equal(value3, persistentKeyValue.Value);
        }

        [Fact]
        public async Task GetRevisions_ReturnsHistoricalKeyValues()
        {
            var client = _testServer.Client;

            var key1 = "revision-test-key";
            var value1_v1 = "revision-value-v1";
            var createResponse1 = await TestHelpers.CreateKeyValue(client, key1, value1_v1);
            createResponse1.EnsureSuccessStatusCode();

            var value1_v2 = "revision-value-v2";
            var updateResponse1 = await TestHelpers.CreateKeyValue(client, key1, value1_v2);
            updateResponse1.EnsureSuccessStatusCode();

            var value1_v3 = "revision-value-v3";
            var updateResponse2 = await TestHelpers.CreateKeyValue(client, key1, value1_v3);
            updateResponse2.EnsureSuccessStatusCode();

            var key2 = "revision-test-key-labeled";
            var value2_v1 = "labeled-revision-v1";
            var label2 = "test-label";
            var createResponse2 = await TestHelpers.CreateKeyValue(client, key2, value2_v1, label: label2);
            createResponse2.EnsureSuccessStatusCode();

            var value2_v2 = "labeled-revision-v2";
            var updateResponse3 = await TestHelpers.CreateKeyValue(client, key2, value2_v2, label: label2);
            updateResponse3.EnsureSuccessStatusCode();

            var allRevisions = await TestHelpers.GetRevisions(client);

            Assert.NotNull(allRevisions);
            Assert.NotNull(allRevisions.Items);
            Assert.NotEmpty(allRevisions.Items);

            // The response should include all 5 revisions we created (3 for key1, 2 for key2)
            var key1Revisions = allRevisions.Items.Where(kv => kv.Key == key1).ToList();
            var key2Revisions = allRevisions.Items.Where(kv => kv.Key == key2 && kv.Label == label2).ToList();

            // Check revision counts
            Assert.True(key1Revisions.Count >= 3, $"Expected at least 3 revisions for key1, but found {key1Revisions.Count}");
            Assert.True(key2Revisions.Count >= 2, $"Expected at least 2 revisions for key2, but found {key2Revisions.Count}");

            // Get revisions for a specific key
            var key1RevisionsFiltered = await TestHelpers.GetRevisions(client, key: key1);

            // Verify filtered revisions
            Assert.NotNull(key1RevisionsFiltered);
            Assert.NotNull(key1RevisionsFiltered.Items);
            Assert.NotEmpty(key1RevisionsFiltered.Items);
            Assert.All(key1RevisionsFiltered.Items, kv => Assert.Equal(key1, kv.Key));
            Assert.True(key1RevisionsFiltered.Items.Count >= 3, $"Expected at least 3 revisions for key1, but found {key1RevisionsFiltered.Items.Count}");

            // Verify values exist in the revisions (exact order may not be guaranteed)
            Assert.Contains(key1RevisionsFiltered.Items, kv => kv.Value == value1_v1);
            Assert.Contains(key1RevisionsFiltered.Items, kv => kv.Value == value1_v2);
            Assert.Contains(key1RevisionsFiltered.Items, kv => kv.Value == value1_v3);

            // Get revisions for a specific key and label
            var key2RevisionsFiltered = await TestHelpers.GetRevisions(client, key: key2, label: label2);

            // Verify filtered labeled revisions
            Assert.NotNull(key2RevisionsFiltered);
            Assert.NotNull(key2RevisionsFiltered.Items);
            Assert.NotEmpty(key2RevisionsFiltered.Items);
            Assert.All(key2RevisionsFiltered.Items, kv => Assert.Equal(key2, kv.Key));
            Assert.All(key2RevisionsFiltered.Items, kv => Assert.Equal(label2, kv.Label));
            Assert.True(key2RevisionsFiltered.Items.Count >= 2, $"Expected at least 2 revisions for key2 with label, but found {key2RevisionsFiltered.Items.Count}");

            // Verify values exist in the revisions
            Assert.Contains(key2RevisionsFiltered.Items, kv => kv.Value == value2_v1);
            Assert.Contains(key2RevisionsFiltered.Items, kv => kv.Value == value2_v2);

            // Delete a key-value and verify revisions are still available
            var deleteResponse = await TestHelpers.DeleteKeyValue(client, key1);
            deleteResponse.EnsureSuccessStatusCode();

            // Verify key-value is deleted
            var keyValueAfterDelete = await TestHelpers.GetKeyValue(client, key1);
            Assert.Null(keyValueAfterDelete);

            // Get revisions for the deleted key
            var deletedKeyRevisions = await TestHelpers.GetRevisions(client, key: key1);

            // Verify revisions for deleted key are still available
            Assert.NotNull(deletedKeyRevisions);
            Assert.NotNull(deletedKeyRevisions.Items);
            Assert.NotEmpty(deletedKeyRevisions.Items);
            Assert.All(deletedKeyRevisions.Items, kv => Assert.Equal(key1, kv.Key));
            Assert.True(deletedKeyRevisions.Items.Count >= 3, $"Expected at least 3 revisions for deleted key, but found {deletedKeyRevisions.Items.Count}");
        }
    }
}
