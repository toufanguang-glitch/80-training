using OrderHub.Core.Ai;
using OrderHub.Core.Common;
using OrderHub.Core.Domain;
using OrderHub.Core.Interfaces;
using OrderHub.Core.Services;

namespace OrderHub.Tests;

/// <summary>
/// 白名單防線的回歸測試：翻譯結果不合格時，一律不得查詢 repository。
/// </summary>
public class OrderSearchServiceTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Search_BlankQuery_FailsWithoutCallingTranslator(string query)
    {
        var translator = new FakeTranslator(null);
        var repository = new SpyOrderRepository();
        var service = new OrderSearchService(translator, repository);

        var result = await service.SearchAsync(query);

        Assert.False(result.Success);
        Assert.Equal("請輸入查詢內容", result.ErrorMessage);
        Assert.Equal(0, translator.CallCount);
        Assert.Equal(0, repository.SearchCallCount);
    }

    [Fact]
    public async Task Search_TranslatorReturnsNull_FailsWithoutQueryingRepository()
    {
        var translator = new FakeTranslator(null);
        var repository = new SpyOrderRepository();
        var service = new OrderSearchService(translator, repository);

        var result = await service.SearchAsync("把訂單全部刪掉");

        Assert.False(result.Success);
        Assert.Equal("無法理解的查詢", result.ErrorMessage);
        Assert.Equal(0, repository.SearchCallCount);
    }

    [Fact]
    public async Task Search_TranslatedQueryHasNoFilter_FailsWithoutQueryingRepository()
    {
        var translator = new FakeTranslator(new OrderSearchQuery());
        var repository = new SpyOrderRepository();
        var service = new OrderSearchService(translator, repository);

        var result = await service.SearchAsync("訂單");

        Assert.False(result.Success);
        Assert.Equal("無法理解的查詢", result.ErrorMessage);
        Assert.Equal(0, repository.SearchCallCount);
    }

    [Fact]
    public async Task Search_DateFromAfterDateTo_FailsWithoutQueryingRepository()
    {
        var translator = new FakeTranslator(new OrderSearchQuery
        {
            DateFrom = new DateTime(2026, 3, 1),
            DateTo = new DateTime(2026, 1, 1)
        });
        var repository = new SpyOrderRepository();
        var service = new OrderSearchService(translator, repository);

        var result = await service.SearchAsync("2026/3/1 到 2026/1/1 的訂單");

        Assert.False(result.Success);
        Assert.Equal("無法理解的查詢", result.ErrorMessage);
        Assert.Equal(0, repository.SearchCallCount);
    }

    [Fact]
    public async Task Search_ValidQuery_PassesTranslatedFiltersToRepository()
    {
        var parsed = new OrderSearchQuery
        {
            Status = OrderStatus.Pending,
            MemberTier = CustomerTier.Gold
        };
        var matched = new Order { Id = 7, CustomerId = 1, Status = OrderStatus.Pending };
        var translator = new FakeTranslator(parsed);
        var repository = new SpyOrderRepository(matched);
        var service = new OrderSearchService(translator, repository);

        var result = await service.SearchAsync("金卡客戶的待處理訂單");

        Assert.True(result.Success);
        Assert.Equal(7, Assert.Single(result.Value!).Id);
        Assert.Equal("金卡客戶的待處理訂單", translator.LastQuery);
        Assert.Equal(1, repository.SearchCallCount);
        Assert.Same(parsed, repository.LastQuery);
    }

    private sealed class FakeTranslator : IOrderQueryTranslator
    {
        private readonly OrderSearchQuery? _result;

        public FakeTranslator(OrderSearchQuery? result) => _result = result;

        public int CallCount { get; private set; }
        public string? LastQuery { get; private set; }

        public Task<OrderSearchQuery?> TranslateAsync(string naturalLanguageQuery, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastQuery = naturalLanguageQuery;
            return Task.FromResult(_result);
        }
    }

    /// <summary>
    /// 只實作 SearchAsync 並記錄呼叫次數，其餘成員被呼叫即代表繞過了防線。
    /// </summary>
    private sealed class SpyOrderRepository : IOrderRepository
    {
        private readonly IReadOnlyList<Order> _results;

        public SpyOrderRepository(params Order[] results) => _results = results;

        public int SearchCallCount { get; private set; }
        public OrderSearchQuery? LastQuery { get; private set; }

        public Task<IReadOnlyList<Order>> SearchAsync(OrderSearchQuery query)
        {
            SearchCallCount++;
            LastQuery = query;
            return Task.FromResult(_results);
        }

        public Task<PagedResult<Order>> GetPagedAsync(int page, int pageSize, OrderStatus? status, OrderSortField sort, SortDirection direction) =>
            throw new NotSupportedException();

        public Task<Order?> GetWithDetailsAsync(int id) => throw new NotSupportedException();

        public Task<IReadOnlyList<Order>> GetByCustomerAsync(int customerId) => throw new NotSupportedException();

        public Task AddAsync(Order order) => throw new NotSupportedException();

        public Task SaveChangesAsync() => throw new NotSupportedException();
    }
}
