using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Common;
using VetFlow.Application.Sales.Queries.SalesDetails;
using VetFlow.Application.Sales.Queries.SalesList;
using VetFlow.Domain.Sales;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Sales;

/// <summary>
/// Sales list query implementation (REQ-SAL-005). Projects straight to the
/// response DTO — deliberate CQRS-lite read that bypasses the domain
/// (ADR-0014 §5). Search covers the system number and the customer name
/// (Arabic normalized) — never notes (BR-SAL-019); the number is matched as an
/// exact value, not by partial format (BR-SAL-002). An invoice without a
/// customer name (DEC-SAL-002) is simply never matched by a name search.
/// </summary>
public sealed class SalesListQueryHandler(VetFlowDbContext dbContext)
    : IQueryHandler<SalesListQuery, PagedResult<SalesListItemDto>>
{
    private const string LikeEscapeCharacter = "\\";

    public async Task<PagedResult<SalesListItemDto>> HandleAsync(
        SalesListQuery query,
        CancellationToken cancellationToken)
    {
        var invoices = ApplyFilters(dbContext.SalesInvoices.AsNoTracking(), query);

        var totalCount = await invoices.CountAsync(cancellationToken);

        var items = await ApplySorting(invoices, query)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(invoice => new SalesListItemDto
            {
                Id = invoice.Id,
                Number = invoice.Number,
                CustomerName = invoice.CustomerName,
                SaleDate = invoice.SaleDate,
                Status = invoice.Status == SalesInvoiceStatus.Committed
                    ? SalesInvoiceStatusDto.Committed
                    : SalesInvoiceStatusDto.Draft,
                Total = new MoneyDto { Amount = invoice.TotalAmount, Currency = Currencies.EgyptianPound },
                CreatedAt = invoice.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<SalesListItemDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        };
    }

    private static IQueryable<SalesInvoice> ApplyFilters(
        IQueryable<SalesInvoice> invoices,
        SalesListQuery query)
    {
        if (query.Status is { } status)
        {
            var invoiceStatus = status == SalesInvoiceStatusFilter.Committed
                ? SalesInvoiceStatus.Committed
                : SalesInvoiceStatus.Draft;
            invoices = invoices.Where(invoice => invoice.Status == invoiceStatus);
        }

        if (query.SaleDateFrom is { } from)
        {
            invoices = invoices.Where(invoice => invoice.SaleDate >= from);
        }

        if (query.SaleDateTo is { } to)
        {
            invoices = invoices.Where(invoice => invoice.SaleDate <= to);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var rawSearch = query.Search.Trim();
            var pattern = $"%{EscapeLike(ArabicSearchText.Normalize(rawSearch))}%";
            invoices = invoices.Where(invoice =>
                EF.Functions.ILike(EF.Property<string>(invoice, SearchableText.PropertyName), pattern, LikeEscapeCharacter)
                || invoice.Number == rawSearch);
        }

        return invoices;
    }

    private static IOrderedQueryable<SalesInvoice> ApplySorting(
        IQueryable<SalesInvoice> invoices,
        SalesListQuery query)
    {
        var ascending = query.Direction == SortDirection.Ascending;

        var ordered = query.Sort switch
        {
            SalesListSortField.Number => ascending
                ? invoices.OrderBy(invoice => invoice.Number)
                : invoices.OrderByDescending(invoice => invoice.Number),
            SalesListSortField.SaleDate => ascending
                ? invoices.OrderBy(invoice => invoice.SaleDate)
                : invoices.OrderByDescending(invoice => invoice.SaleDate),
            SalesListSortField.Customer => ascending
                ? invoices.OrderBy(invoice => invoice.CustomerName)
                : invoices.OrderByDescending(invoice => invoice.CustomerName),
            SalesListSortField.Status => ascending
                ? invoices.OrderBy(invoice => invoice.Status)
                : invoices.OrderByDescending(invoice => invoice.Status),
            SalesListSortField.Total => ascending
                ? invoices.OrderBy(invoice => invoice.TotalAmount)
                : invoices.OrderByDescending(invoice => invoice.TotalAmount),
            _ => ascending
                ? invoices.OrderBy(invoice => invoice.SaleDate)
                : invoices.OrderByDescending(invoice => invoice.SaleDate),
        };

        // A unique final key gives offset pagination a total order, so pages stay
        // stable even when the sort key ties — sale dates commonly collide and
        // customer names may repeat or be null (free text — DEC-SAL-002); without
        // it, tied rows can be skipped or repeated across pages.
        return ordered.ThenBy(invoice => invoice.Id);
    }

    private static string EscapeLike(string value) =>
        value.Replace(LikeEscapeCharacter, LikeEscapeCharacter + LikeEscapeCharacter, StringComparison.Ordinal)
            .Replace("%", LikeEscapeCharacter + "%", StringComparison.Ordinal)
            .Replace("_", LikeEscapeCharacter + "_", StringComparison.Ordinal);
}
