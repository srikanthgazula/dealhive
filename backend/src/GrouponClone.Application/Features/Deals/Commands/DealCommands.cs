using MediatR;
using Microsoft.EntityFrameworkCore;
using GrouponClone.Application.Interfaces;
using GrouponClone.Domain.Exceptions;

namespace GrouponClone.Application.Features.Deals.Commands;

// ─── Commands ────────────────────────────────────────────────

public record SubmitDealForApprovalCommand(Guid DealId) : IRequest;
public record RecordDealViewCommand(Guid DealId) : IRequest;
public record ApproveDealCommand(Guid DealId) : IRequest;
public record RejectDealCommand(Guid DealId, string Reason) : IRequest;
public record PauseDealCommand(Guid DealId) : IRequest;

// ─── Handlers ────────────────────────────────────────────────

public class SubmitDealForApprovalCommandHandler : IRequestHandler<SubmitDealForApprovalCommand>
{
    private readonly IDealRepository _deals;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public SubmitDealForApprovalCommandHandler(IDealRepository deals, IUnitOfWork uow, ICurrentUserService cu)
    {
        _deals = deals; _uow = uow; _currentUser = cu;
    }

    public async Task Handle(SubmitDealForApprovalCommand req, CancellationToken ct)
    {
        var deal = await _deals.GetByIdAsync(req.DealId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Deal), req.DealId);

        if (deal.VendorId != _currentUser.UserId)
            throw new ForbiddenException();

        deal.SubmitForApproval();
        await _uow.SaveChangesAsync(ct);
    }
}

public class RecordDealViewCommandHandler : IRequestHandler<RecordDealViewCommand>
{
    private readonly IDealRepository _deals;
    private readonly IUnitOfWork _uow;

    public RecordDealViewCommandHandler(IDealRepository deals, IUnitOfWork uow) { _deals = deals; _uow = uow; }

    public async Task Handle(RecordDealViewCommand req, CancellationToken ct)
    {
        var deal = await _deals.GetByIdAsync(req.DealId, ct);
        if (deal is null) return;
        deal.IncrementViews();
        await _uow.SaveChangesAsync(ct);
    }
}

public class ApproveDealCommandHandler : IRequestHandler<ApproveDealCommand>
{
    private readonly IDealRepository _deals;
    private readonly IUnitOfWork _uow;

    public ApproveDealCommandHandler(IDealRepository deals, IUnitOfWork uow) { _deals = deals; _uow = uow; }

    public async Task Handle(ApproveDealCommand req, CancellationToken ct)
    {
        var deal = await _deals.GetByIdAsync(req.DealId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Deal), req.DealId);
        deal.Approve();
        await _uow.SaveChangesAsync(ct);
    }
}

public class RejectDealCommandHandler : IRequestHandler<RejectDealCommand>
{
    private readonly IDealRepository _deals;
    private readonly IUnitOfWork _uow;

    public RejectDealCommandHandler(IDealRepository deals, IUnitOfWork uow) { _deals = deals; _uow = uow; }

    public async Task Handle(RejectDealCommand req, CancellationToken ct)
    {
        var deal = await _deals.GetByIdAsync(req.DealId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Deal), req.DealId);
        deal.Reject(req.Reason);
        await _uow.SaveChangesAsync(ct);
    }
}

public class PauseDealCommandHandler : IRequestHandler<PauseDealCommand>
{
    private readonly IDealRepository _deals;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public PauseDealCommandHandler(IDealRepository deals, IUnitOfWork uow, ICurrentUserService cu)
    {
        _deals = deals; _uow = uow; _currentUser = cu;
    }

    public async Task Handle(PauseDealCommand req, CancellationToken ct)
    {
        var deal = await _deals.GetByIdAsync(req.DealId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Deal), req.DealId);

        if (deal.VendorId != _currentUser.UserId && _currentUser.Role != "Admin")
            throw new ForbiddenException();

        deal.Pause();
        await _uow.SaveChangesAsync(ct);
    }
}
