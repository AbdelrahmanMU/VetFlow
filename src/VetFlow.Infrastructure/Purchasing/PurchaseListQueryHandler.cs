using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Common;
using VetFlow.Application.Purchasing.Queries.PurchaseList;
using VetFlow.Domain.Purchasing;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Purchasing;

/// <summary>
/// Purchase list query implementation (REQ-PUR-001). Projects straight to the
/// response DTO — deliberate CQRS-lite read that bypasses the domain
/// (ADR-0014 §5). Search covers the system number, the supplier name (Arabic
/// normalized), and the supplier reference — never notes (BR-PUR-004); the
/// number is matched as an exact value, not by partial format (BR-PUR-002).
/// </summary>
public sealed class PurchaseListQueryHandler(VetFlowDbContext dbContext)
    : IQueryHandler<PurchaseListQuery, PagedResult<PurchaseListItemDto>>
{
    private const string LikeEscapeCharacter = "\\";

    public async Task<PagedResult<PurchaseListItemDto>> HandleAsync(
        PurchaseListQuery query,
        CancellationToken cancellationToken)
    {
        var invoices = ApplyFilters(dbContext.PurchaseInvoices.AsNoTracking(), query);

        var totalCount = await invoices.CountAsync(cancellationToken);

        var items = await ApplySorting(invoices, query)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(invoice => new PurchaseListItemDto
            {
                Id = invoice.Id,
                Number = invoice.Number,
                SupplierName = invoice.SupplierName,
                SupplierInvoiceReference = invoice.SupplierInvoiceReference,
                InvoiceDate = invoice.InvoiceDate,
                Status = invoice.Status == PurchaseInvoiceStatus.Received
                    ? PurchaseInvoiceStatusDto.Received
                    : invoice.Status == PurchaseInvoiceStatus.Cancelled
                        ? PurchaseInvoiceStatusDto.Cancelled
                        : PurchaseInvoiceStatusDto.Draft,
                Total = new MoneyDto { Amount = invoice.TotalAmount, Currency = Currencies.EgyptianPound },
                CreatedAt = invoice.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<PurchaseListItemDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        };
    }

    private static IQueryable<PurchaseInvoice> ApplyFilters(
        IQueryable<PurchaseInvoice> invoices,
        PurchaseListQuery query)
    {
        if (query.Status is { } status)
        {
            var invoiceStatus = status switch
            {
                PurchaseInvoiceStatusFilter.Received => PurchaseInvoiceStatus.Received,
                PurchaseInvoiceStatusFilter.Cancelled => PurchaseInvoiceStatus.Cancelled,
                _ => PurchaseInvoiceStatus.Draft,
            };
            invoices = invoices.Where(invoice => invoice.Status == invoiceStatus);
        }

        if (query.InvoiceDateFrom is { } from)
        {
            invoices = invoices.Where(invoice => invoice.InvoiceDate >= from);
        }

        if (query.InvoiceDateTo is { } to)
        {
            invoices = invoices.Where(invoice => invoice.InvoiceDate <= to);
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

    private static IOrderedQueryable<PurchaseInvoice> ApplySorting(
        IQueryable<PurchaseInvoice> invoices,
        PurchaseListQuery query)
    {
        var ascending = query.Direction == SortDirection.Ascending;

        var ordered = query.Sort switch
        {
            PurchaseListSortField.Number => ascending
                ? invoices.OrderBy(invoice => invoice.Number)
                : invoices.OrderByDescending(invoice => invoice.Number),
            PurchaseListSortField.Supplier => ascending
                ? invoices.OrderBy(invoice => invoice.SupplierName)
                : invoices.OrderByDescending(invoice => invoice.SupplierName),
            PurchaseListSortField.Status => ascending
                ? invoices.OrderBy(invoice => invoice.Status)
                : invoices.OrderByDescending(invoice => invoice.Status),
            PurchaseListSortField.Total => ascending
                ? invoices.OrderBy(invoice => invoice.TotalAmount)
                : invoices.OrderByDescending(invoice => invoice.TotalAmount),
            _ => ascending
                ? invoices.OrderBy(invoice => invoice.InvoiceDate)
                : invoices.OrderByDescending(invoice => invoice.InvoiceDate),
        };

        // A unique final key gives offset pagination a total order, so pages stay
        // stable even when the sort key ties — invoice dates commonly collide and
        // supplier names may repeat (free text — BR-PUR-004 edge case); without it,
        // tied rows can be skipped or repeated across pages.
        return ordered.ThenBy(invoice => invoice.Id);
    }

    private static string EscapeLike(string value) =>
        value.Replace(LikeEscapeCharacter, LikeEscapeCharacter + LikeEscapeCharacter, StringComparison.Ordinal)
            .Replace("%", LikeEscapeCharacter + "%", StringComparison.Ordinal)
            .Replace("_", LikeEscapeCharacter + "_", StringComparison.Ordinal);
}
