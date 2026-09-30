using FluentValidation;

namespace PatientPortal.Api.Common;

public static class PagingRules
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}

public interface IPagedRequest
{
    int Page { get; }
    int PageSize { get; }
}

public abstract class PagedRequestValidator<TRequest> : AbstractValidator<TRequest>
    where TRequest : IPagedRequest
{
    protected PagedRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PagingRules.MaxPageSize);
    }
}

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    bool HasNextPage
);

public static class Paging
{
    public static int Offset(IPagedRequest request) => (request.Page - 1) * request.PageSize;

    // Queries fetch one row more than the page size; the extra row only says another page exists.
    public static int FetchLimit(IPagedRequest request) => request.PageSize + 1;

    public static PagedResponse<T> ToPage<T>(IReadOnlyList<T> fetched, IPagedRequest request) =>
        new(
            [.. fetched.Take(request.PageSize)],
            request.Page,
            request.PageSize,
            fetched.Count > request.PageSize
        );
}
